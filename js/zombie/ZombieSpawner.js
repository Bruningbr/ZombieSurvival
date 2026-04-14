// ZombieSpawner.js - Wave-based zombie spawning
class ZombieSpawner {
    constructor(scene, environment) {
        this.scene = scene;
        this.environment = environment;
        this.zombies = [];

        this.currentWave = 0;
        this.zombiesRemaining = 0;
        this.zombiesAlive = 0;
        this.zombiesToSpawn = 0;
        this.spawnTimer = 0;
        this.spawnInterval = 0.8;
        this.waveDelay = 8;
        this.waveDelayTimer = 3; // Initial delay before first wave
        this.waveActive = false;

        this.onWaveStart = null;
        this.onWaveComplete = null;
        this.onZombieKilled = null;

        // Difficulty scaling
        this.healthMultiplier = 1.0;
        this.damageMultiplier = 1.0;
        this.speedMultiplier = 1.0;
    }

    startGame() {
        this.currentWave = 0;
        this.waveDelayTimer = 1;
        this.waveActive = false;
        this.zombies = [];
    }

    startWave() {
        this.currentWave++;
        this.waveActive = true;

        // Calculate zombies for this wave
        const baseCount = 5 + this.currentWave * 3;
        this.zombiesToSpawn = Math.min(baseCount, 40);
        this.zombiesRemaining = this.zombiesToSpawn;
        this.zombiesAlive = 0;
        this.spawnTimer = 0;

        // Scale difficulty
        this.healthMultiplier = 1 + (this.currentWave - 1) * 0.15;
        this.damageMultiplier = 1 + (this.currentWave - 1) * 0.1;
        this.speedMultiplier = 1 + (this.currentWave - 1) * 0.05;

        // Faster spawning in later waves
        this.spawnInterval = Math.max(0.3, 0.8 - this.currentWave * 0.05);

        if (this.onWaveStart) this.onWaveStart(this.currentWave, this.zombiesToSpawn);
        audioManager.playSFX('wave_start', 0.6);
    }

    getZombieType() {
        const wave = this.currentWave;
        const roll = Math.random();

        if (wave < 3) {
            return 'walker'; // Only walkers first 2 waves
        } else if (wave < 5) {
            return roll < 0.7 ? 'walker' : 'runner';
        } else if (wave < 8) {
            if (roll < 0.4) return 'walker';
            if (roll < 0.7) return 'runner';
            if (roll < 0.85) return 'tank';
            return 'spitter';
        } else {
            if (roll < 0.25) return 'walker';
            if (roll < 0.45) return 'runner';
            if (roll < 0.6) return 'tank';
            if (roll < 0.75) return 'spitter';
            return 'screamer';
        }
    }

    spawnZombie(playerPosition) {
        // Pick spawn point far from player
        let bestSpawn = null;
        let bestDist = 0;

        // Spawn zombies closer to the player so they reach you quickly
        for (let i = 0; i < 8; i++) {
            const sp = this.environment.getRandomSpawnPoint();
            const dist = MathUtils.distanceFlat(sp, playerPosition);
            // Prefer spawn points 10-30 units from player (close but not on top)
            if (dist > 10 && dist < 35) {
                if (!bestSpawn || dist < bestDist) {
                    bestDist = dist;
                    bestSpawn = sp;
                }
            }
        }

        if (!bestSpawn) {
            bestSpawn = this.environment.getRandomSpawnPoint();
        }

        const type = this.getZombieType();
        const zombie = new ZombieAI(this.scene, bestSpawn, type, this.environment);

        // Apply difficulty scaling
        zombie.maxHealth *= this.healthMultiplier;
        zombie.health = zombie.maxHealth;
        zombie.damage *= this.damageMultiplier;
        zombie.speed *= this.speedMultiplier;

        this.zombies.push(zombie);
        this.zombiesAlive++;
        this.zombiesToSpawn--;
    }

    update(deltaTime, playerPosition) {
        // Wave delay
        if (!this.waveActive) {
            this.waveDelayTimer -= deltaTime;
            if (this.waveDelayTimer <= 0) {
                this.startWave();
            }
            return;
        }

        // Spawn zombies
        if (this.zombiesToSpawn > 0) {
            this.spawnTimer += deltaTime;
            if (this.spawnTimer >= this.spawnInterval) {
                this.spawnTimer = 0;
                this.spawnZombie(playerPosition);
            }
        }

        // Update zombies
        for (let i = this.zombies.length - 1; i >= 0; i--) {
            const zombie = this.zombies[i];

            if (zombie.alive) {
                zombie.update(deltaTime, playerPosition);
            } else if (zombie.mesh) {
                zombie.update(deltaTime, playerPosition); // Death animation
            } else {
                // Fully dead and removed
                this.zombies.splice(i, 1);
            }
        }

        // Check wave complete
        const aliveCount = this.zombies.filter(z => z.alive).length;
        this.zombiesAlive = aliveCount;

        if (this.zombiesToSpawn <= 0 && aliveCount === 0 && this.waveActive) {
            this.waveActive = false;
            this.waveDelayTimer = this.waveDelay;
            if (this.onWaveComplete) this.onWaveComplete(this.currentWave);
            audioManager.playSFX('wave_complete', 0.6);
        }
    }

    zombieKilled(zombie) {
        this.zombiesRemaining--;
        if (this.onZombieKilled) {
            this.onZombieKilled(zombie);
        }
    }

    getAliveZombies() {
        return this.zombies.filter(z => z.alive);
    }

    get totalZombiesAlive() {
        return this.zombies.filter(z => z.alive).length + this.zombiesToSpawn;
    }

    reset() {
        this.zombies.forEach(z => {
            if (z.mesh) z.destroy();
        });
        this.zombies = [];
        this.currentWave = 0;
        this.waveActive = false;
        this.waveDelayTimer = 3;
    }
}
