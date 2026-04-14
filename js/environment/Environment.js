// Environment.js - Creates the 3D game world
class Environment {
    constructor(scene) {
        this.scene = scene;
        this.colliders = [];
        this.spawnPoints = [];
        this.pickupSpawns = [];
    }

    build() {
        this.createGround();
        this.createBuildings();
        this.createStreets();
        this.createBarricades();
        this.createProps();
        this.createLights();
        this.generateSpawnPoints();
    }

    createGround() {
        const geo = new THREE.PlaneGeometry(200, 200, 20, 20);
        const mat = new THREE.MeshStandardMaterial({
            color: 0x333333,
            roughness: 0.9,
            metalness: 0.1
        });
        const ground = new THREE.Mesh(geo, mat);
        ground.rotation.x = -Math.PI / 2;
        ground.receiveShadow = true;
        ground.userData.isGround = true;
        this.scene.add(ground);

        // Road markings
        const roadGeo = new THREE.PlaneGeometry(12, 200);
        const roadMat = new THREE.MeshStandardMaterial({ color: 0x222222, roughness: 0.95 });
        const road1 = new THREE.Mesh(roadGeo, roadMat);
        road1.rotation.x = -Math.PI / 2;
        road1.position.y = 0.01;
        road1.receiveShadow = true;
        this.scene.add(road1);

        const road2 = road1.clone();
        road2.rotation.z = Math.PI / 2;
        this.scene.add(road2);

        // Road lines
        for (let i = -90; i < 90; i += 6) {
            const lineGeo = new THREE.PlaneGeometry(0.3, 3);
            const lineMat = new THREE.MeshStandardMaterial({ color: 0xcccc00 });
            const line = new THREE.Mesh(lineGeo, lineMat);
            line.rotation.x = -Math.PI / 2;
            line.position.set(0, 0.02, i);
            this.scene.add(line);
        }
    }

    createBuildings() {
        const buildingConfigs = [
            { x: -30, z: -30, w: 20, h: 15, d: 20, color: 0x555555 },
            { x: 30, z: -30, w: 15, h: 20, d: 18, color: 0x4a4a4a },
            { x: -30, z: 30, w: 18, h: 12, d: 22, color: 0x505050 },
            { x: 30, z: 30, w: 22, h: 18, d: 16, color: 0x484848 },
            { x: -60, z: 0, w: 14, h: 10, d: 30, color: 0x525252 },
            { x: 60, z: 0, w: 16, h: 25, d: 14, color: 0x464646 },
            { x: 0, z: -60, w: 25, h: 8, d: 12, color: 0x535353 },
            { x: 0, z: 60, w: 12, h: 14, d: 25, color: 0x4c4c4c },
            { x: -60, z: -60, w: 20, h: 22, d: 20, color: 0x505050 },
            { x: 60, z: -60, w: 18, h: 16, d: 18, color: 0x484848 },
            { x: -60, z: 60, w: 16, h: 12, d: 20, color: 0x525252 },
            { x: 60, z: 60, w: 20, h: 20, d: 16, color: 0x4a4a4a },
        ];

        buildingConfigs.forEach(cfg => {
            this.createBuilding(cfg.x, cfg.z, cfg.w, cfg.h, cfg.d, cfg.color);
        });
    }

    createBuilding(x, z, w, h, d, color) {
        // Main structure
        const geo = new THREE.BoxGeometry(w, h, d);
        const mat = new THREE.MeshStandardMaterial({
            color: color,
            roughness: 0.85,
            metalness: 0.05
        });
        const building = new THREE.Mesh(geo, mat);
        building.position.set(x, h / 2, z);
        building.castShadow = true;
        building.receiveShadow = true;
        this.scene.add(building);

        this.colliders.push({
            min: { x: x - w/2, y: 0, z: z - d/2 },
            max: { x: x + w/2, y: h, z: z + d/2 }
        });

        // Windows
        const windowMat = new THREE.MeshStandardMaterial({
            color: 0x112233,
            emissive: 0x112233,
            emissiveIntensity: 0.3,
            roughness: 0.1,
            metalness: 0.8
        });

        const windowSize = 1.5;
        const windowSpacing = 4;

        for (let side = 0; side < 4; side++) {
            const isXSide = side % 2 === 0;
            const sign = side < 2 ? 1 : -1;
            const sideLen = isXSide ? w : d;
            const numWindows = Math.floor(sideLen / windowSpacing) - 1;

            for (let row = 0; row < Math.floor(h / windowSpacing) - 1; row++) {
                for (let col = 0; col < numWindows; col++) {
                    const wGeo = new THREE.PlaneGeometry(windowSize, windowSize);
                    const win = new THREE.Mesh(wGeo, windowMat.clone());

                    const wy = 3 + row * windowSpacing;
                    const offset = -sideLen/2 + windowSpacing + col * windowSpacing;

                    if (isXSide) {
                        win.position.set(x + offset, wy, z + sign * (d/2 + 0.05));
                        if (sign < 0) win.rotation.y = Math.PI;
                    } else {
                        win.position.set(x + sign * (w/2 + 0.05), wy, z + offset);
                        win.rotation.y = sign * Math.PI / 2;
                    }

                    // Random lit windows
                    if (Math.random() > 0.6) {
                        win.material.emissive.set(0x443311);
                        win.material.emissiveIntensity = 0.8;
                    }

                    this.scene.add(win);
                }
            }
        }

        // Roof detail
        const roofGeo = new THREE.BoxGeometry(w + 0.5, 0.5, d + 0.5);
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x3a3a3a, roughness: 0.9 });
        const roof = new THREE.Mesh(roofGeo, roofMat);
        roof.position.set(x, h + 0.25, z);
        roof.castShadow = true;
        this.scene.add(roof);
    }

    createStreets() {
        // Sidewalks
        const sidewalkMat = new THREE.MeshStandardMaterial({ color: 0x444444, roughness: 0.9 });

        for (let i = -4; i <= 4; i += 8) {
            for (let z = -95; z < 95; z += 2) {
                const geo = new THREE.BoxGeometry(2, 0.15, 2);
                const sidewalk = new THREE.Mesh(geo, sidewalkMat);
                sidewalk.position.set(i * 1.1, 0.075, z);
                sidewalk.receiveShadow = true;
                this.scene.add(sidewalk);
            }
        }
    }

    createBarricades() {
        const barricadeMat = new THREE.MeshStandardMaterial({ color: 0x8B4513, roughness: 0.9 });
        const metalMat = new THREE.MeshStandardMaterial({ color: 0x666666, roughness: 0.6, metalness: 0.4 });

        const positions = [
            { x: 15, z: 0, ry: 0 },
            { x: -15, z: 5, ry: 0.3 },
            { x: 0, z: 15, ry: Math.PI/2 },
            { x: 0, z: -15, ry: Math.PI/2 },
            { x: 10, z: -20, ry: 0.5 },
            { x: -10, z: 20, ry: -0.5 },
        ];

        positions.forEach(pos => {
            // Wooden barricade
            const group = new THREE.Group();

            for (let i = 0; i < 3; i++) {
                const plank = new THREE.Mesh(
                    new THREE.BoxGeometry(3, 0.8, 0.2),
                    barricadeMat
                );
                plank.position.set(0, 0.5 + i * 0.85, 0);
                plank.rotation.z = MathUtils.randFloat(-0.05, 0.05);
                plank.castShadow = true;
                group.add(plank);
            }

            // Metal supports
            for (let side = -1; side <= 1; side += 2) {
                const post = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.05, 0.05, 3),
                    metalMat
                );
                post.position.set(side * 1.2, 1.5, 0);
                post.castShadow = true;
                group.add(post);
            }

            group.position.set(pos.x, 0, pos.z);
            group.rotation.y = pos.ry;
            this.scene.add(group);

            this.colliders.push({
                min: { x: pos.x - 1.5, y: 0, z: pos.z - 0.5 },
                max: { x: pos.x + 1.5, y: 3, z: pos.z + 0.5 }
            });
        });
    }

    createProps() {
        // Street lights
        const poleMat = new THREE.MeshStandardMaterial({ color: 0x333333, metalness: 0.6 });

        for (let i = -80; i <= 80; i += 20) {
            for (let side = -1; side <= 1; side += 2) {
                const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.1, 6), poleMat);
                pole.position.set(side * 8, 3, i);
                pole.castShadow = true;
                this.scene.add(pole);

                const arm = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.1, 0.1), poleMat);
                arm.position.set(side * 8 - side * 0.75, 6, i);
                this.scene.add(arm);

                const lightGeo = new THREE.SphereGeometry(0.2);
                const lightMat = new THREE.MeshStandardMaterial({
                    color: 0xffaa44,
                    emissive: 0xffaa44,
                    emissiveIntensity: 2
                });
                const lightMesh = new THREE.Mesh(lightGeo, lightMat);
                lightMesh.position.set(side * 8 - side * 1.5, 5.9, i);
                this.scene.add(lightMesh);

                const light = new THREE.PointLight(0xffaa44, 0.6, 15);
                light.position.copy(lightMesh.position);
                light.castShadow = false;
                this.scene.add(light);
            }
        }

        // Dumpsters
        const dumpsterMat = new THREE.MeshStandardMaterial({ color: 0x2d5a27, roughness: 0.8 });
        const dumpsterPositions = [
            { x: 18, z: -18 }, { x: -18, z: 18 },
            { x: 45, z: -10 }, { x: -45, z: 10 }
        ];
        dumpsterPositions.forEach(pos => {
            const dumpster = new THREE.Mesh(new THREE.BoxGeometry(2, 1.5, 1.2), dumpsterMat);
            dumpster.position.set(pos.x, 0.75, pos.z);
            dumpster.castShadow = true;
            this.scene.add(dumpster);
            this.colliders.push({
                min: { x: pos.x - 1, y: 0, z: pos.z - 0.6 },
                max: { x: pos.x + 1, y: 1.5, z: pos.z + 0.6 }
            });
        });

        // Cars (destroyed)
        const carMat = new THREE.MeshStandardMaterial({ color: 0x444455, roughness: 0.7, metalness: 0.3 });
        const carPositions = [
            { x: 3, z: -25, ry: 0.2 }, { x: -4, z: 35, ry: -0.3 },
            { x: 25, z: 3, ry: 1.5 }, { x: -25, z: -4, ry: 1.7 }
        ];
        carPositions.forEach(pos => {
            const car = new THREE.Group();
            const body = new THREE.Mesh(new THREE.BoxGeometry(4, 1.2, 2), carMat);
            body.position.y = 0.8;
            car.add(body);
            const top = new THREE.Mesh(new THREE.BoxGeometry(2.5, 1, 1.8),
                new THREE.MeshStandardMaterial({ color: 0x333344, roughness: 0.5, metalness: 0.3 }));
            top.position.set(-0.3, 1.8, 0);
            car.add(top);

            // Wheels
            const wheelGeo = new THREE.CylinderGeometry(0.35, 0.35, 0.2, 8);
            const wheelMat = new THREE.MeshStandardMaterial({ color: 0x111111 });
            [[-1.2, -1], [-1.2, 1], [1.2, -1], [1.2, 1]].forEach(([wx, wz]) => {
                const wheel = new THREE.Mesh(wheelGeo, wheelMat);
                wheel.position.set(wx, 0.35, wz);
                wheel.rotation.x = Math.PI / 2;
                car.add(wheel);
            });

            car.position.set(pos.x, 0, pos.z);
            car.rotation.y = pos.ry;
            this.scene.add(car);

            this.colliders.push({
                min: { x: pos.x - 2.5, y: 0, z: pos.z - 1.5 },
                max: { x: pos.x + 2.5, y: 2, z: pos.z + 1.5 }
            });
        });

        // Cones
        const coneMat = new THREE.MeshStandardMaterial({ color: 0xff6600 });
        for (let i = 0; i < 8; i++) {
            const cone = new THREE.Mesh(new THREE.ConeGeometry(0.2, 0.6, 6), coneMat);
            cone.position.set(
                MathUtils.randFloat(-40, 40),
                0.3,
                MathUtils.randFloat(-40, 40)
            );
            this.scene.add(cone);
        }

        // Barrels
        const barrelMat = new THREE.MeshStandardMaterial({ color: 0x554433, metalness: 0.3 });
        for (let i = 0; i < 6; i++) {
            const barrel = new THREE.Mesh(new THREE.CylinderGeometry(0.4, 0.4, 1, 8), barrelMat);
            const bx = MathUtils.randFloat(-50, 50);
            const bz = MathUtils.randFloat(-50, 50);
            barrel.position.set(bx, 0.5, bz);
            barrel.castShadow = true;
            this.scene.add(barrel);

            this.colliders.push({
                min: { x: bx - 0.4, y: 0, z: bz - 0.4 },
                max: { x: bx + 0.4, y: 1, z: bz + 0.4 }
            });
        }
    }

    createLights() {
        // Fire barrels (with particle-like effect)
        const firePositions = [
            { x: 5, z: 5 }, { x: -5, z: -5 },
            { x: 20, z: -15 }, { x: -20, z: 15 }
        ];

        firePositions.forEach(pos => {
            const barrel = new THREE.Mesh(
                new THREE.CylinderGeometry(0.4, 0.4, 1, 8),
                new THREE.MeshStandardMaterial({ color: 0x333333, metalness: 0.5 })
            );
            barrel.position.set(pos.x, 0.5, pos.z);
            this.scene.add(barrel);

            // Fire glow
            const fire = new THREE.PointLight(0xff4400, 1.5, 12);
            fire.position.set(pos.x, 1.5, pos.z);
            this.scene.add(fire);

            const fireMesh = new THREE.Mesh(
                new THREE.SphereGeometry(0.3, 8, 8),
                new THREE.MeshBasicMaterial({ color: 0xff6600, transparent: true, opacity: 0.8 })
            );
            fireMesh.position.set(pos.x, 1.2, pos.z);
            fireMesh.userData.isFireEffect = true;
            fireMesh.userData.light = fire;
            this.scene.add(fireMesh);

            this.pickupSpawns.push({ x: pos.x + 2, z: pos.z + 2 });
        });
    }

    generateSpawnPoints() {
        // Zombie spawn points around the perimeter
        for (let angle = 0; angle < Math.PI * 2; angle += Math.PI / 8) {
            const dist = MathUtils.randFloat(50, 80);
            this.spawnPoints.push({
                x: Math.cos(angle) * dist,
                z: Math.sin(angle) * dist
            });
        }

        // Additional spawn points behind buildings
        this.spawnPoints.push(
            { x: -40, z: -40 }, { x: 40, z: -40 },
            { x: -40, z: 40 }, { x: 40, z: 40 },
            { x: -70, z: 0 }, { x: 70, z: 0 },
            { x: 0, z: -70 }, { x: 0, z: 70 }
        );
    }

    checkCollision(position, radius = 0.5) {
        for (const box of this.colliders) {
            const closestX = MathUtils.clamp(position.x, box.min.x, box.max.x);
            const closestZ = MathUtils.clamp(position.z, box.min.z, box.max.z);
            const dx = position.x - closestX;
            const dz = position.z - closestZ;
            if (dx * dx + dz * dz < radius * radius) {
                return { collided: true, box };
            }
        }
        return { collided: false };
    }

    resolveCollision(position, velocity, radius = 0.5) {
        for (const box of this.colliders) {
            const closestX = MathUtils.clamp(position.x, box.min.x, box.max.x);
            const closestZ = MathUtils.clamp(position.z, box.min.z, box.max.z);
            const dx = position.x - closestX;
            const dz = position.z - closestZ;
            const distSq = dx * dx + dz * dz;

            if (distSq < radius * radius && distSq > 0) {
                const dist = Math.sqrt(distSq);
                const nx = dx / dist;
                const nz = dz / dist;
                const overlap = radius - dist;
                position.x += nx * overlap;
                position.z += nz * overlap;
            }
        }
    }

    update(time) {
        // Animate fire effects
        this.scene.traverse(obj => {
            if (obj.userData.isFireEffect) {
                obj.scale.set(
                    1 + Math.sin(time * 5) * 0.3,
                    1 + Math.sin(time * 7 + 1) * 0.4,
                    1 + Math.sin(time * 6 + 2) * 0.3
                );
                if (obj.userData.light) {
                    obj.userData.light.intensity = 1.5 + Math.sin(time * 8) * 0.5;
                }
            }
        });
    }

    getRandomSpawnPoint() {
        return this.spawnPoints[MathUtils.randInt(0, this.spawnPoints.length - 1)];
    }

    getRandomPickupSpawn() {
        return this.pickupSpawns[MathUtils.randInt(0, this.pickupSpawns.length - 1)];
    }
}
