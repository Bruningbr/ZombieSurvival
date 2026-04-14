// Game.js - Main game controller
class Game {
    constructor() {
        this.canvas = document.getElementById('game-canvas');
        this.renderer = null;
        this.scene = null;
        this.camera = null;
        this.clock = new THREE.Clock();

        // Systems
        this.environment = null;
        this.dayNight = null;
        this.player = null;
        this.health = null;
        this.stamina = null;
        this.weapons = null;
        this.spawner = null;
        this.hud = null;
        this.menuManager = null;
        this.multiplayer = null;

        // Game state
        this.isRunning = false;
        this.isPaused = false;
        this.score = 0;
        this.kills = 0;
        this.deaths = 0;
        this.survivalTime = 0;
        this.playerName = 'Jogador';

        // Pickups
        this.pickups = [];
        this.pickupSpawnTimer = 0;
        this.pickupSpawnInterval = 20;

        this.init();
    }

    init() {
        // Renderer
        this.renderer = new THREE.WebGLRenderer({
            canvas: this.canvas,
            antialias: true
        });
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.0;

        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x111122);

        // Camera
        this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 300);
        this.scene.add(this.camera);

        // Environment
        this.environment = new Environment(this.scene);
        this.environment.build();

        // Day/Night
        this.dayNight = new DayNightCycle(this.scene);

        // UI
        this.hud = new HUD();
        this.menuManager = new MenuManager();

        // Multiplayer
        this.multiplayer = new MultiplayerClient();
        this.multiplayer.scene = this.scene;

        // Menu callbacks
        this.menuManager.onPlay = (mode) => this.startGame(mode);
        this.menuManager.onResume = () => this.resumeGame();
        this.menuManager.onQuit = () => this.quitGame();
        this.menuManager.onRespawn = () => this.respawnPlayer();
        this.menuManager.onPlayAgain = () => this.startGame();
        this.menuManager.settingsChanged = (key, val) => {
            if (key === 'sensitivity' && this.player) {
                this.player.setSensitivity(val);
            }
        };

        // Window resize
        window.addEventListener('resize', () => this.onResize());

        // Pause on escape
        document.addEventListener('keydown', e => {
            if (e.code === 'Escape' && this.isRunning && !this.isPaused) {
                this.pauseGame();
            }
        });

        // Tab key for in-game leaderboard
        document.addEventListener('keydown', e => {
            if (e.code === 'Tab' && this.isRunning) {
                e.preventDefault();
            }
        });

        // Start render loop
        this.animate();
    }

    startGame(mode) {
        audioManager.init();

        this.menuManager.showLoading(0, 'Preparando o apocalipse...');

        setTimeout(() => {
            this.menuManager.showLoading(30, 'Criando mundo...');

            setTimeout(() => {
                this.menuManager.showLoading(60, 'Invocando zombies...');

                // Init systems
                this.health = new PlayerHealth();
                this.stamina = new PlayerStamina();
                this.player = new PlayerController(this.camera, this.canvas, this.environment);
                this.weapons = new WeaponSystem(this.camera, this.scene);
                this.spawner = new ZombieSpawner(this.scene, this.environment);

                // Player name
                const nameInput = document.getElementById('player-name');
                this.playerName = nameInput ? nameInput.value || 'Jogador' : 'Jogador';

                // Health callbacks
                this.health.onDeath = () => this.onPlayerDeath();
                this.health.onDamage = (amount) => {
                    this.hud.showDamageOverlay(amount / 50);
                };

                // Weapon callbacks
                this.weapons.onAmmoChange = (current, reserve) => {
                    this.hud.updateAmmo(current, reserve);
                };
                this.weapons.onWeaponChange = (weapon) => {
                    this.hud.updateWeapon(weapon);
                    this.hud.updateWeaponSlots(this.weapons.weapons, this.weapons.currentSlot);
                };

                // Spawner callbacks
                this.spawner.onWaveStart = (wave, count) => {
                    this.hud.showWaveNotification(wave, count);
                };
                this.spawner.onWaveComplete = (wave) => {
                    this.score += wave * 500;
                    this.hud.updateScore(this.score, this.kills);
                };

                // Reset state
                this.score = 0;
                this.kills = 0;
                this.deaths = 0;
                this.survivalTime = 0;
                this.pickups = [];

                // Spawn player
                this.player.teleport(0, 1.7, 0);
                this.spawner.startGame();

                // Multiplayer
                if (mode === 'host' || mode === 'join') {
                    const addr = document.getElementById('server-address').value;
                    this.multiplayer.connect(addr, this.playerName, mode === 'host');
                }

                setTimeout(() => {
                    this.menuManager.showLoading(100, 'Pronto!');

                    setTimeout(() => {
                        this.menuManager.hideAll();
                        this.hud.show();
                        this.isRunning = true;
                        this.isPaused = false;

                        // Initial weapon display
                        this.weapons.switchWeapon(0);
                        this.hud.updateHealth(this.health.current, this.health.max);
                        this.hud.updateStamina(this.stamina.current, this.stamina.max);

                        // Request pointer lock
                        this.canvas.requestPointerLock();
                    }, 500);
                }, 300);
            }, 300);
        }, 300);
    }

    pauseGame() {
        this.isPaused = true;
        document.exitPointerLock();
        this.menuManager.showScreen('pause');
    }

    resumeGame() {
        this.isPaused = false;
        this.menuManager.hideAll();
        this.canvas.requestPointerLock();
    }

    quitGame() {
        this.isRunning = false;
        this.isPaused = false;
        this.hud.hide();

        // Save to leaderboard
        if (this.kills > 0 || this.score > 0) {
            leaderboard.addEntry(
                this.playerName, this.score, this.kills, this.deaths,
                this.spawner ? this.spawner.currentWave : 0, this.survivalTime
            );
        }

        // Cleanup
        if (this.spawner) this.spawner.reset();
        this.cleanupPickups();

        if (this.multiplayer.connected) {
            this.multiplayer.disconnect();
        }

        this.menuManager.showScreen('mainMenu');
        document.exitPointerLock();
    }

    onPlayerDeath() {
        this.deaths++;
        document.exitPointerLock();
        this.menuManager.showDeath({
            score: this.score,
            kills: this.kills,
            wave: this.spawner.currentWave,
            survivalTime: this.survivalTime
        });
    }

    respawnPlayer() {
        this.health.revive(0.75);
        this.stamina.reset();
        this.player.teleport(
            MathUtils.randFloat(-10, 10),
            1.7,
            MathUtils.randFloat(-10, 10)
        );
        this.menuManager.hideAll();
        this.canvas.requestPointerLock();
    }

    gameOver() {
        this.isRunning = false;
        document.exitPointerLock();

        const rank = leaderboard.addEntry(
            this.playerName, this.score, this.kills, this.deaths,
            this.spawner.currentWave, this.survivalTime
        );

        this.menuManager.showGameOver({
            score: this.score,
            kills: this.kills,
            deaths: this.deaths,
            wave: this.spawner.currentWave,
            survivalTime: this.survivalTime
        }, rank);
    }

    spawnPickup() {
        const types = ['health', 'ammo', 'weapon_rifle', 'weapon_shotgun', 'weapon_smg', 'weapon_sniper'];
        const weights = [0.3, 0.3, 0.1, 0.1, 0.1, 0.1];
        let roll = Math.random();
        let type = types[0];
        for (let i = 0; i < weights.length; i++) {
            roll -= weights[i];
            if (roll <= 0) { type = types[i]; break; }
        }

        const pos = this.environment.getRandomPickupSpawn() ||
            { x: MathUtils.randFloat(-30, 30), z: MathUtils.randFloat(-30, 30) };

        const colors = {
            health: 0x00ff00, ammo: 0xffaa00,
            weapon_rifle: 0x4444ff, weapon_shotgun: 0xff4444,
            weapon_smg: 0x44ffff, weapon_sniper: 0xff44ff
        };

        const group = new THREE.Group();

        const base = new THREE.Mesh(
            new THREE.BoxGeometry(0.5, 0.5, 0.5),
            new THREE.MeshStandardMaterial({
                color: colors[type] || 0xffffff,
                emissive: colors[type] || 0xffffff,
                emissiveIntensity: 0.5,
                metalness: 0.3,
                roughness: 0.5
            })
        );
        group.add(base);

        // Glow
        const glow = new THREE.PointLight(colors[type], 0.5, 5);
        glow.position.y = 0.5;
        group.add(glow);

        group.position.set(pos.x, 0.5, pos.z);
        group.userData.pickupType = type;
        group.userData.bobTimer = Math.random() * Math.PI * 2;
        this.scene.add(group);
        this.pickups.push(group);
    }

    updatePickups(deltaTime) {
        this.pickupSpawnTimer += deltaTime;
        if (this.pickupSpawnTimer >= this.pickupSpawnInterval && this.pickups.length < 8) {
            this.pickupSpawnTimer = 0;
            this.spawnPickup();
        }

        const playerPos = this.player.position;
        for (let i = this.pickups.length - 1; i >= 0; i--) {
            const pickup = this.pickups[i];
            pickup.userData.bobTimer += deltaTime;
            pickup.position.y = 0.5 + Math.sin(pickup.userData.bobTimer * 2) * 0.15;
            pickup.rotation.y += deltaTime;

            // Check pickup collection
            const dist = MathUtils.distanceFlat(playerPos, pickup.position);
            if (dist < 1.5) {
                this.collectPickup(pickup);
                this.scene.remove(pickup);
                this.pickups.splice(i, 1);
            }
        }
    }

    collectPickup(pickup) {
        const type = pickup.userData.pickupType;
        audioManager.playSFX('pickup', 0.6);

        switch (type) {
            case 'health':
                this.health.heal(30);
                break;
            case 'ammo':
                this.weapons.addAmmo(30);
                break;
            case 'weapon_rifle':
                this.weapons.pickupWeapon('rifle');
                break;
            case 'weapon_shotgun':
                this.weapons.pickupWeapon('shotgun');
                break;
            case 'weapon_smg':
                this.weapons.pickupWeapon('smg');
                break;
            case 'weapon_sniper':
                this.weapons.pickupWeapon('sniper');
                break;
        }
    }

    cleanupPickups() {
        this.pickups.forEach(p => this.scene.remove(p));
        this.pickups = [];
    }

    update(deltaTime) {
        if (!this.isRunning || this.isPaused) return;
        if (this.health && this.health.isDead) {
            this.menuManager.update(deltaTime);
            return;
        }

        this.survivalTime += deltaTime;

        // Update systems
        this.player.update(deltaTime, this.stamina);
        this.health.update(deltaTime);
        this.stamina.update(deltaTime);
        this.weapons.update(deltaTime);
        this.dayNight.update(deltaTime);
        this.environment.update(performance.now() * 0.001);

        // Update spawner
        this.spawner.update(deltaTime, this.player.position);

        // Shooting
        if (this.weapons.mouseDown && this.player.isLocked) {
            const weapon = this.weapons.currentWeapon;
            if (weapon && (weapon.auto || !this._wasShooting)) {
                const hits = this.weapons.tryShoot(this.spawner.getAliveZombies());
                hits.forEach(hit => {
                    const killed = hit.zombie.takeDamage(hit.damage, hit.isHeadshot);
                    this.hud.showHitMarker(hit.isHeadshot);

                    if (killed) {
                        this.kills++;
                        const baseScore = hit.zombie.scoreValue;
                        const headBonus = hit.isHeadshot ? baseScore : 0;
                        this.score += baseScore + headBonus;
                        this.spawner.zombieKilled(hit.zombie);
                    }
                });
            }
            this._wasShooting = true;
        } else {
            this._wasShooting = false;
        }

        // Zombie attacks on player
        const aliveZombies = this.spawner.getAliveZombies();
        aliveZombies.forEach(zombie => {
            if (zombie.isAttacking) {
                const dist = MathUtils.distanceFlat(this.player.position, zombie.mesh.position);
                if (dist < zombie.attackRange) {
                    this.health.takeDamage(zombie.damage);
                }
            }
        });

        // Screamer alerting - alert nearby zombies
        aliveZombies.forEach(zombie => {
            if (zombie.type === 'screamer' && zombie.state === 'chase') {
                aliveZombies.forEach(other => {
                    if (other !== zombie && other.state !== 'chase' && other.state !== 'attack') {
                        const dist = MathUtils.distanceFlat(zombie.mesh.position, other.mesh.position);
                        if (dist < 20) {
                            other.state = 'chase';
                        }
                    }
                });
            }
        });

        // Pickups
        this.updatePickups(deltaTime);

        // Multiplayer sync
        if (this.multiplayer.connected) {
            this.multiplayer.sendPosition(
                this.player.position,
                this.camera.rotation.y,
                this.health.current
            );
            this.multiplayer.update();
        }

        // Update HUD
        this.hud.updateHealth(this.health.current, this.health.max);
        this.hud.updateStamina(this.stamina.current, this.stamina.max);
        this.hud.updateScore(this.score, this.kills);
        this.hud.updateWave(
            this.spawner.currentWave || 0,
            this.spawner.totalZombiesAlive
        );
        this.hud.updateWeaponSlots(this.weapons.weapons, this.weapons.currentSlot);
        this.hud.update(deltaTime);

        // Check nearby pickups for interaction prompt
        let nearPickup = false;
        for (const pickup of this.pickups) {
            const dist = MathUtils.distanceFlat(this.player.position, pickup.position);
            if (dist < 3) {
                const names = {
                    health: '❤ Kit Médico', ammo: '🔫 Munição',
                    weapon_rifle: '🔫 Rifle', weapon_shotgun: '🔫 Shotgun',
                    weapon_smg: '🔫 SMG', weapon_sniper: '🔫 Sniper'
                };
                this.hud.showInteraction(`[Perto] ${names[pickup.userData.pickupType] || 'Item'}`);
                nearPickup = true;
                break;
            }
        }
        if (!nearPickup) {
            this.hud.hideInteraction();
        }
    }

    animate() {
        requestAnimationFrame(() => this.animate());

        const deltaTime = Math.min(this.clock.getDelta(), 0.05);

        this.update(deltaTime);
        this.menuManager.update(deltaTime);
        this.renderer.render(this.scene, this.camera);
    }

    onResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
    }
}
