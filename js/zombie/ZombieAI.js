// ZombieAI.js - Zombie with AI, pathfinding, and multiple types
class ZombieAI {
    constructor(scene, position, type, environment) {
        this.scene = scene;
        this.environment = environment;
        this.type = type;
        this.alive = true;
        this.mesh = null;
        this.deathTimer = 0;

        // Stats based on type
        this.stats = this.getStatsByType(type);
        this.health = this.stats.health;
        this.maxHealth = this.stats.health;
        this.speed = this.stats.speed;
        this.damage = this.stats.damage;
        this.attackRange = this.stats.attackRange;
        this.detectionRange = this.stats.detectionRange;
        this.scoreValue = this.stats.scoreValue;

        // AI state
        this.state = 'idle'; // idle, wander, chase, attack, stunned, dead
        this.target = null;
        this.wanderTarget = null;
        this.stateTimer = 0;
        this.attackCooldown = 0;
        this.damageDealt = false;
        this.stunTimer = 0;
        this.groanTimer = MathUtils.randFloat(2, 8);

        // Animation
        this.animTimer = 0;
        this.baseY = 0;
        this.limbAngle = 0;

        this.createMesh(position);
    }

    getStatsByType(type) {
        const stats = {
            walker: {
                health: 80, speed: 2.0, damage: 10, attackRange: 2.0,
                detectionRange: 20, scoreValue: 100, color: 0x446633,
                height: 1.8, attackCooldown: 1.2
            },
            runner: {
                health: 50, speed: 5.0, damage: 8, attackRange: 1.8,
                detectionRange: 25, scoreValue: 150, color: 0x663333,
                height: 1.7, attackCooldown: 0.8
            },
            tank: {
                health: 250, speed: 1.2, damage: 25, attackRange: 2.5,
                detectionRange: 15, scoreValue: 300, color: 0x444433,
                height: 2.2, attackCooldown: 2.0
            },
            spitter: {
                health: 60, speed: 2.5, damage: 15, attackRange: 12,
                detectionRange: 30, scoreValue: 200, color: 0x336644,
                height: 1.7, attackCooldown: 3.0
            },
            screamer: {
                health: 40, speed: 3.0, damage: 5, attackRange: 1.5,
                detectionRange: 35, scoreValue: 250, color: 0x555555,
                height: 1.6, attackCooldown: 1.0
            }
        };
        return stats[type] || stats.walker;
    }

    createMesh(position) {
        const s = this.stats;
        this.mesh = new THREE.Group();
        this.mesh.userData.isZombie = true;

        // Body
        const bodyGeo = new THREE.BoxGeometry(0.6, s.height * 0.45, 0.35);
        const bodyMat = new THREE.MeshStandardMaterial({
            color: s.color, roughness: 0.9, metalness: 0.0
        });
        this.body = new THREE.Mesh(bodyGeo, bodyMat);
        this.body.position.y = s.height * 0.5;
        this.body.castShadow = true;
        this.mesh.add(this.body);

        // Head
        const headGeo = new THREE.SphereGeometry(0.2, 8, 8);
        const headMat = new THREE.MeshStandardMaterial({
            color: new THREE.Color(s.color).offsetHSL(0, 0, -0.1),
            roughness: 0.8
        });
        this.head = new THREE.Mesh(headGeo, headMat);
        this.head.position.y = s.height * 0.8;
        this.head.castShadow = true;
        this.mesh.add(this.head);

        // Eyes (glowing)
        const eyeGeo = new THREE.SphereGeometry(0.04, 4, 4);
        const eyeMat = new THREE.MeshBasicMaterial({
            color: this.type === 'spitter' ? 0x00ff00 : 0xff2200
        });
        for (let side = -1; side <= 1; side += 2) {
            const eye = new THREE.Mesh(eyeGeo, eyeMat);
            eye.position.set(side * 0.07, s.height * 0.82, 0.15);
            this.mesh.add(eye);
        }

        // Arms
        const armGeo = new THREE.BoxGeometry(0.15, s.height * 0.35, 0.15);
        this.leftArm = new THREE.Mesh(armGeo, bodyMat.clone());
        this.leftArm.position.set(-0.45, s.height * 0.5, 0.1);
        this.leftArm.geometry.translate(0, -s.height * 0.15, 0);
        this.mesh.add(this.leftArm);

        this.rightArm = new THREE.Mesh(armGeo, bodyMat.clone());
        this.rightArm.position.set(0.45, s.height * 0.5, 0.1);
        this.rightArm.geometry.translate(0, -s.height * 0.15, 0);
        this.mesh.add(this.rightArm);

        // Legs
        const legGeo = new THREE.BoxGeometry(0.18, s.height * 0.35, 0.18);
        this.leftLeg = new THREE.Mesh(legGeo, new THREE.MeshStandardMaterial({
            color: 0x333333, roughness: 0.9
        }));
        this.leftLeg.position.set(-0.15, s.height * 0.18, 0);
        this.leftLeg.geometry.translate(0, -s.height * 0.15, 0);
        this.mesh.add(this.leftLeg);

        this.rightLeg = new THREE.Mesh(legGeo, this.leftLeg.material.clone());
        this.rightLeg.position.set(0.15, s.height * 0.18, 0);
        this.rightLeg.geometry.translate(0, -s.height * 0.15, 0);
        this.mesh.add(this.rightLeg);

        // Health bar
        const hbBg = new THREE.Mesh(
            new THREE.PlaneGeometry(0.8, 0.08),
            new THREE.MeshBasicMaterial({ color: 0x330000, transparent: true, opacity: 0.7 })
        );
        hbBg.position.y = s.height + 0.3;
        this.mesh.add(hbBg);

        this.healthBar = new THREE.Mesh(
            new THREE.PlaneGeometry(0.8, 0.08),
            new THREE.MeshBasicMaterial({ color: 0xff0000, transparent: true, opacity: 0.8 })
        );
        this.healthBar.position.y = s.height + 0.3;
        this.healthBar.position.z = 0.001;
        this.mesh.add(this.healthBar);

        this.mesh.position.set(position.x, 0, position.z);
        this.scene.add(this.mesh);
    }

    takeDamage(amount, isHeadshot) {
        if (!this.alive) return false;

        this.health -= amount;

        // Flash white
        this.body.material.emissive.setHex(0x333333);
        setTimeout(() => {
            if (this.body) this.body.material.emissive.setHex(0x000000);
        }, 100);

        // Update health bar
        const pct = Math.max(0, this.health / this.maxHealth);
        this.healthBar.scale.x = pct;
        this.healthBar.position.x = -(1 - pct) * 0.4;

        if (this.health <= 0) {
            this.die();
            return true;
        }

        // Aggro on damage
        if (this.state === 'idle' || this.state === 'wander') {
            this.state = 'chase';
        }

        // Brief stun on headshot
        if (isHeadshot) {
            this.stunTimer = 0.5;
            this.state = 'stunned';
        }

        return false;
    }

    die() {
        this.alive = false;
        this.state = 'dead';
        audioManager.playSFX('zombie_death', 0.6, MathUtils.randFloat(0.7, 1.3));

        // Death animation - fall over
        this.deathTimer = 3;
    }

    update(deltaTime, playerPosition) {
        if (!this.alive) {
            this.updateDeath(deltaTime);
            return;
        }

        this.target = playerPosition;
        this.stateTimer += deltaTime;
        this.attackCooldown -= deltaTime;
        this.groanTimer -= deltaTime;

        // Groaning sounds
        if (this.groanTimer <= 0) {
            this.groanTimer = MathUtils.randFloat(4, 12);
            const dist = MathUtils.distanceFlat(this.mesh.position, playerPosition);
            if (dist < 30) {
                audioManager.playSFX('zombie_groan', Math.max(0.1, 0.5 - dist / 60),
                    MathUtils.randFloat(0.6, 1.4));
            }
        }

        // State machine
        switch (this.state) {
            case 'idle':
                this.handleIdle(deltaTime);
                break;
            case 'wander':
                this.handleWander(deltaTime);
                break;
            case 'chase':
                this.handleChase(deltaTime);
                break;
            case 'attack':
                this.handleAttack(deltaTime);
                break;
            case 'stunned':
                this.handleStunned(deltaTime);
                break;
        }

        // Detection check
        if (this.state !== 'chase' && this.state !== 'attack' && this.state !== 'stunned') {
            const distToPlayer = MathUtils.distanceFlat(this.mesh.position, playerPosition);
            if (distToPlayer < this.detectionRange) {
                this.state = 'chase';
                this.stateTimer = 0;
                if (distToPlayer < this.detectionRange * 0.6) {
                    audioManager.playSFX('zombie_detect', 0.3);
                }
            }
        }

        // Collision with environment
        this.environment.resolveCollision(this.mesh.position, null, 0.4);

        // Animate
        this.animate(deltaTime);

        // Keep on ground
        this.mesh.position.y = 0;
    }

    handleIdle(deltaTime) {
        if (this.stateTimer > MathUtils.randFloat(2, 5)) {
            this.state = 'wander';
            this.stateTimer = 0;
            this.wanderTarget = {
                x: this.mesh.position.x + MathUtils.randFloat(-15, 15),
                z: this.mesh.position.z + MathUtils.randFloat(-15, 15)
            };
        }
    }

    handleWander(deltaTime) {
        if (!this.wanderTarget) {
            this.state = 'idle';
            return;
        }

        const dist = MathUtils.distanceFlat(this.mesh.position, this.wanderTarget);
        if (dist < 1 || this.stateTimer > 8) {
            this.state = 'idle';
            this.stateTimer = 0;
            return;
        }

        this.moveToward(this.wanderTarget, this.speed * 0.4, deltaTime);
    }

    handleChase(deltaTime) {
        if (!this.target) {
            this.state = 'wander';
            return;
        }

        const dist = MathUtils.distanceFlat(this.mesh.position, this.target);

        if (dist <= this.attackRange) {
            this.state = 'attack';
            this.stateTimer = 0;
            return;
        }

        if (dist > this.detectionRange * 1.5) {
            this.state = 'wander';
            this.stateTimer = 0;
            return;
        }

        this.moveToward(this.target, this.speed, deltaTime);

        // Screamer alerts nearby zombies
        if (this.type === 'screamer' && this.stateTimer > 1 && this.stateTimer < 1.1) {
            audioManager.playSFX('zombie_detect', 0.8, 1.5);
        }
    }

    handleAttack(deltaTime) {
        if (!this.target) { this.state = 'idle'; return; }

        const dist = MathUtils.distanceFlat(this.mesh.position, this.target);

        if (dist > this.attackRange * 1.2) {
            this.state = 'chase';
            return;
        }

        // Face target
        this.faceTarget(this.target);

        // Attack
        if (this.attackCooldown <= 0) {
            this.attackCooldown = this.stats.attackCooldown;
            this.damageDealt = false;
            audioManager.playSFX('zombie_attack', 0.5, MathUtils.randFloat(0.8, 1.2));
        }
    }

    handleStunned(deltaTime) {
        this.stunTimer -= deltaTime;
        if (this.stunTimer <= 0) {
            this.state = 'chase';
        }
    }

    moveToward(target, speed, deltaTime) {
        const dx = target.x - this.mesh.position.x;
        const dz = target.z - this.mesh.position.z;
        const dist = Math.sqrt(dx * dx + dz * dz);

        if (dist > 0.1) {
            const nx = dx / dist;
            const nz = dz / dist;
            this.mesh.position.x += nx * speed * deltaTime;
            this.mesh.position.z += nz * speed * deltaTime;

            // Face movement direction
            this.mesh.rotation.y = Math.atan2(nx, nz);
        }
    }

    faceTarget(target) {
        const dx = target.x - this.mesh.position.x;
        const dz = target.z - this.mesh.position.z;
        this.mesh.rotation.y = Math.atan2(dx, dz);
    }

    animate(deltaTime) {
        this.animTimer += deltaTime;
        const isMoving = this.state === 'chase' || this.state === 'wander';
        const animSpeed = this.state === 'chase' ? 8 : 4;

        if (isMoving) {
            this.limbAngle = Math.sin(this.animTimer * animSpeed) * 0.5;
        } else {
            this.limbAngle *= 0.9;
        }

        // Arm swing
        if (this.leftArm) {
            this.leftArm.rotation.x = this.limbAngle - 0.8; // Arms forward (zombie pose)
            this.rightArm.rotation.x = -this.limbAngle - 0.8;
        }

        // Leg swing
        if (this.leftLeg) {
            this.leftLeg.rotation.x = -this.limbAngle;
            this.rightLeg.rotation.x = this.limbAngle;
        }

        // Body sway
        if (this.body) {
            this.body.rotation.z = Math.sin(this.animTimer * 2) * 0.05;
            this.body.rotation.x = isMoving ? 0.1 : 0; // Lean forward when moving
        }

        // Head tilt
        if (this.head) {
            this.head.rotation.z = Math.sin(this.animTimer * 1.5 + 1) * 0.1;
        }
    }

    updateDeath(deltaTime) {
        this.deathTimer -= deltaTime;

        // Fall over animation
        if (this.mesh.rotation.x < Math.PI / 2) {
            this.mesh.rotation.x += deltaTime * 3;
            this.mesh.position.y = -Math.sin(this.mesh.rotation.x) * 0.5;
        }

        // Fade out
        if (this.deathTimer < 1) {
            this.mesh.traverse(child => {
                if (child.material) {
                    child.material.transparent = true;
                    child.material.opacity = Math.max(0, this.deathTimer);
                }
            });
        }

        if (this.deathTimer <= 0) {
            this.destroy();
        }
    }

    destroy() {
        this.scene.remove(this.mesh);
        this.mesh = null;
    }

    get isAttacking() {
        return this.state === 'attack' && !this.damageDealt &&
            this.attackCooldown > this.stats.attackCooldown * 0.7;
    }
}
