// WeaponSystem.js - Complete weapon system with 6 weapon types
class WeaponSystem {
    constructor(camera, scene) {
        this.camera = camera;
        this.scene = scene;
        this.weapons = [];
        this.currentSlot = 0;
        this.isReloading = false;
        this.reloadTimer = 0;
        this.fireTimer = 0;
        this.isADS = false;
        this.muzzleFlashes = [];
        this.bulletHoles = [];
        this.maxBulletHoles = 50;

        this.onAmmoChange = null;
        this.onWeaponChange = null;

        this.setupWeapons();
        this.setupInput();
        this.createWeaponModels();
    }

    setupWeapons() {
        this.weaponDefs = {
            pistol: {
                name: 'Pistola', type: 'pistol', damage: 25, fireRate: 0.3,
                range: 80, magSize: 12, reserveMax: 60, reloadTime: 1.5,
                spread: 0.02, recoilAmount: 0.03, headshotMult: 2.0, auto: false,
                sound: 'shoot_pistol'
            },
            rifle: {
                name: 'Rifle', type: 'rifle', damage: 35, fireRate: 0.1,
                range: 120, magSize: 30, reserveMax: 120, reloadTime: 2.2,
                spread: 0.03, recoilAmount: 0.04, headshotMult: 2.5, auto: true,
                sound: 'shoot_rifle'
            },
            shotgun: {
                name: 'Shotgun', type: 'shotgun', damage: 15, fireRate: 0.8,
                range: 30, magSize: 8, reserveMax: 32, reloadTime: 2.5,
                spread: 0.12, recoilAmount: 0.08, headshotMult: 1.5, auto: false,
                pellets: 8, sound: 'shoot_shotgun'
            },
            smg: {
                name: 'SMG', type: 'smg', damage: 18, fireRate: 0.07,
                range: 60, magSize: 35, reserveMax: 140, reloadTime: 1.8,
                spread: 0.05, recoilAmount: 0.025, headshotMult: 2.0, auto: true,
                sound: 'shoot_smg'
            },
            sniper: {
                name: 'Sniper', type: 'sniper', damage: 120, fireRate: 1.5,
                range: 200, magSize: 5, reserveMax: 20, reloadTime: 3.0,
                spread: 0.005, recoilAmount: 0.1, headshotMult: 3.0, auto: false,
                sound: 'shoot_sniper'
            },
            knife: {
                name: 'Faca', type: 'melee', damage: 50, fireRate: 0.5,
                range: 3, magSize: Infinity, reserveMax: 0, reloadTime: 0,
                spread: 0, recoilAmount: 0, headshotMult: 1.5, auto: false,
                sound: 'melee_swing'
            }
        };

        // Starting weapons
        this.weapons = [
            this.createWeapon('pistol'),
            null,
            this.createWeapon('knife')
        ];
    }

    createWeapon(type) {
        const def = this.weaponDefs[type];
        return {
            ...def,
            currentAmmo: def.magSize,
            reserveAmmo: def.reserveMax
        };
    }

    setupInput() {
        this._onKeyDown = e => {
            if (e.code === 'Digit1') this.switchWeapon(0);
            if (e.code === 'Digit2') this.switchWeapon(1);
            if (e.code === 'Digit3') this.switchWeapon(2);
            if (e.code === 'KeyR') this.startReload();
        };
        this._onWheel = e => {
            if (e.deltaY > 0) {
                this.switchWeapon((this.currentSlot + 1) % 3);
            } else {
                this.switchWeapon((this.currentSlot + 2) % 3);
            }
        };
        this._onMouseDown = e => {
            if (e.button === 0) this.mouseDown = true;
            if (e.button === 2) this.isADS = true;
        };
        this._onMouseUp = e => {
            if (e.button === 0) this.mouseDown = false;
            if (e.button === 2) this.isADS = false;
        };
        this._onContextMenu = e => e.preventDefault();

        this.mouseDown = false;
        document.addEventListener('keydown', this._onKeyDown);
        document.addEventListener('wheel', this._onWheel);
        document.addEventListener('mousedown', this._onMouseDown);
        document.addEventListener('mouseup', this._onMouseUp);
        document.addEventListener('contextmenu', this._onContextMenu);
    }

    dispose() {
        document.removeEventListener('keydown', this._onKeyDown);
        document.removeEventListener('wheel', this._onWheel);
        document.removeEventListener('mousedown', this._onMouseDown);
        document.removeEventListener('mouseup', this._onMouseUp);
        document.removeEventListener('contextmenu', this._onContextMenu);
        if (this.weaponModel) {
            this.camera.remove(this.weaponModel);
        }
    }

    createWeaponModels() {
        // Weapon view model (simple geometry visible in first person)
        this.weaponModel = new THREE.Group();
        this.weaponMesh = new THREE.Mesh(
            new THREE.BoxGeometry(0.08, 0.08, 0.4),
            new THREE.MeshStandardMaterial({ color: 0x333333, metalness: 0.8, roughness: 0.3 })
        );
        this.weaponModel.add(this.weaponMesh);

        // Muzzle flash
        this.muzzleFlash = new THREE.Mesh(
            new THREE.SphereGeometry(0.05, 6, 6),
            new THREE.MeshBasicMaterial({ color: 0xffaa00, transparent: true, opacity: 0 })
        );
        this.muzzleFlash.position.z = -0.25;
        this.weaponModel.add(this.muzzleFlash);

        this.camera.add(this.weaponModel);
        this.updateWeaponModelPosition();
    }

    updateWeaponModelPosition() {
        if (this.isADS) {
            this.weaponModel.position.set(0, -0.1, -0.3);
        } else {
            this.weaponModel.position.set(0.2, -0.15, -0.35);
        }
    }

    get currentWeapon() {
        return this.weapons[this.currentSlot];
    }

    switchWeapon(slot) {
        if (slot < 0 || slot >= 3) return;
        if (!this.weapons[slot]) return;
        if (this.isReloading) return;

        this.currentSlot = slot;
        this.isReloading = false;
        this.reloadTimer = 0;

        // Update model color based on weapon
        const weapon = this.currentWeapon;
        if (weapon) {
            const colors = {
                pistol: 0x444444, rifle: 0x333322, shotgun: 0x553311,
                smg: 0x333333, sniper: 0x222222, melee: 0x888888
            };
            this.weaponMesh.material.color.setHex(colors[weapon.type] || 0x333333);

            // Scale by weapon type
            const scales = {
                pistol: [0.06, 0.1, 0.25],
                rifle: [0.06, 0.1, 0.5],
                shotgun: [0.06, 0.08, 0.55],
                smg: [0.06, 0.08, 0.35],
                sniper: [0.05, 0.06, 0.6],
                melee: [0.03, 0.03, 0.3]
            };
            const s = scales[weapon.type] || [0.08, 0.08, 0.4];
            this.weaponMesh.scale.set(s[0] * 12, s[1] * 12, s[2] * 2.5);
        }

        if (this.onWeaponChange) this.onWeaponChange(weapon);
        this.fireAmmoEvent();
    }

    startReload() {
        const weapon = this.currentWeapon;
        if (!weapon || weapon.type === 'melee') return;
        if (this.isReloading) return;
        if (weapon.currentAmmo >= weapon.magSize) return;
        if (weapon.reserveAmmo <= 0) return;

        this.isReloading = true;
        this.reloadTimer = weapon.reloadTime;
        audioManager.playSFX('reload', 0.7);
    }

    tryShoot(zombies) {
        const weapon = this.currentWeapon;
        if (!weapon) return [];
        if (this.isReloading) return [];
        if (this.fireTimer > 0) return [];

        if (weapon.currentAmmo <= 0) {
            audioManager.playSFX('empty_click', 0.5);
            if (weapon.reserveAmmo > 0) this.startReload();
            return [];
        }

        this.fireTimer = weapon.fireRate;

        if (weapon.type !== 'melee') {
            weapon.currentAmmo--;
        }

        audioManager.playSFX(weapon.sound, 0.8);

        // Muzzle flash
        if (weapon.type !== 'melee') {
            this.muzzleFlash.material.opacity = 1;
            setTimeout(() => { this.muzzleFlash.material.opacity = 0; }, 50);
        }

        // Recoil
        if (weapon.recoilAmount > 0) {
            const euler = new THREE.Euler().setFromQuaternion(this.camera.quaternion, 'YXZ');
            euler.x += weapon.recoilAmount * (this.isADS ? 0.5 : 1);
            euler.y += (Math.random() - 0.5) * weapon.recoilAmount * 0.5;
            this.camera.quaternion.setFromEuler(euler);
        }

        this.fireAmmoEvent();

        // Raycast for hits
        const hits = [];
        const pellets = weapon.pellets || 1;

        for (let p = 0; p < pellets; p++) {
            const spreadMult = this.isADS ? 0.3 : 1;
            const spreadX = (Math.random() - 0.5) * weapon.spread * spreadMult;
            const spreadY = (Math.random() - 0.5) * weapon.spread * spreadMult;

            const direction = new THREE.Vector3(spreadX, spreadY, -1);
            direction.applyQuaternion(this.camera.quaternion);
            direction.normalize();

            const ray = new THREE.Raycaster(this.camera.position.clone(), direction, 0, weapon.range);

            // Check zombie hits
            for (const zombie of zombies) {
                if (!zombie.alive) continue;
                const zombieMeshes = [];
                zombie.mesh.traverse(child => {
                    if (child.isMesh) zombieMeshes.push(child);
                });
                const intersects = ray.intersectObjects(zombieMeshes);
                if (intersects.length > 0) {
                    const hitPoint = intersects[0].point;
                    const hitY = hitPoint.y - zombie.mesh.position.y;
                    const isHeadshot = hitY > 1.5;
                    const damage = weapon.damage * (isHeadshot ? weapon.headshotMult : 1);

                    hits.push({ zombie, damage, isHeadshot, point: hitPoint });
                    this.createBulletImpact(hitPoint, 0xff0000);
                    break;
                }
            }

            // Check environment hits if no zombie hit
            if (hits.length === 0 || pellets > 1) {
                const envObjects = [];
                this.scene.traverse(child => {
                    if (child.isMesh && !child.userData.isWeapon && !child.userData.isZombie) {
                        envObjects.push(child);
                    }
                });
                const envHits = ray.intersectObjects(envObjects);
                if (envHits.length > 0) {
                    this.createBulletImpact(envHits[0].point, 0x444444);
                }
            }
        }

        return hits;
    }

    createBulletImpact(position, color) {
        const geo = new THREE.SphereGeometry(0.05, 4, 4);
        const mat = new THREE.MeshBasicMaterial({ color });
        const impact = new THREE.Mesh(geo, mat);
        impact.position.copy(position);
        this.scene.add(impact);

        this.bulletHoles.push({ mesh: impact, time: performance.now() });
        if (this.bulletHoles.length > this.maxBulletHoles) {
            const old = this.bulletHoles.shift();
            this.scene.remove(old.mesh);
        }

        // Auto-remove after 10 seconds
        setTimeout(() => {
            this.scene.remove(impact);
            const idx = this.bulletHoles.findIndex(b => b.mesh === impact);
            if (idx >= 0) this.bulletHoles.splice(idx, 1);
        }, 10000);
    }

    pickupWeapon(type) {
        // Find empty slot or replace slot 1
        const weapon = this.createWeapon(type);
        if (!this.weapons[1]) {
            this.weapons[1] = weapon;
            this.switchWeapon(1);
        } else {
            this.weapons[1] = weapon;
            this.switchWeapon(1);
        }
        audioManager.playSFX('pickup', 0.6);
    }

    addAmmo(amount) {
        const weapon = this.currentWeapon;
        if (!weapon || weapon.type === 'melee') return;
        weapon.reserveAmmo = Math.min(weapon.reserveAmmo + amount, weapon.reserveMax);
        this.fireAmmoEvent();
        audioManager.playSFX('pickup', 0.6);
    }

    fireAmmoEvent() {
        if (this.onAmmoChange && this.currentWeapon) {
            this.onAmmoChange(
                this.currentWeapon.currentAmmo,
                this.currentWeapon.reserveAmmo
            );
        }
    }

    update(deltaTime) {
        // Fire timer
        if (this.fireTimer > 0) {
            this.fireTimer -= deltaTime;
        }

        // Reload
        if (this.isReloading) {
            this.reloadTimer -= deltaTime;
            if (this.reloadTimer <= 0) {
                this.isReloading = false;
                const weapon = this.currentWeapon;
                if (weapon) {
                    const needed = weapon.magSize - weapon.currentAmmo;
                    const available = Math.min(needed, weapon.reserveAmmo);
                    weapon.currentAmmo += available;
                    weapon.reserveAmmo -= available;
                    this.fireAmmoEvent();
                }
            }
        }

        // ADS position
        this.updateWeaponModelPosition();

        // Weapon sway
        const time = performance.now() * 0.001;
        const swayX = Math.sin(time * 1.5) * 0.003;
        const swayY = Math.sin(time * 2) * 0.002;
        this.weaponModel.position.x += swayX;
        this.weaponModel.position.y += swayY;
    }
}
