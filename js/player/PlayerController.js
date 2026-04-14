// PlayerController.js - FPS movement and camera
class PlayerController {
    constructor(camera, canvas, environment) {
        this.camera = camera;
        this.canvas = canvas;
        this.environment = environment;

        // Position & rotation
        this.position = new THREE.Vector3(0, 1.7, 0);
        this.velocity = new THREE.Vector3(0, 0, 0);
        this.euler = new THREE.Euler(0, 0, 0, 'YXZ');

        // Movement settings
        this.walkSpeed = 6;
        this.sprintSpeed = 10;
        this.crouchSpeed = 3;
        this.jumpForce = 8;
        this.gravity = -20;
        this.standingHeight = 1.7;
        this.crouchHeight = 1.0;
        this.playerRadius = 0.4;

        // Mouse settings
        this.sensitivity = 0.002;

        // State
        this.isGrounded = true;
        this.isSprinting = false;
        this.isCrouching = false;
        this.isLocked = false;
        this.currentHeight = this.standingHeight;

        // Head bob
        this.headBobTimer = 0;
        this.headBobAmount = 0.04;
        this.headBobSpeed = 12;

        // Footstep
        this.footstepTimer = 0;
        this.footstepInterval = 0.5;

        // Input
        this.keys = {};
        this.mouseDX = 0;
        this.mouseDY = 0;

        this.setupControls();
    }

    setupControls() {
        document.addEventListener('keydown', e => {
            this.keys[e.code] = true;
        });
        document.addEventListener('keyup', e => {
            this.keys[e.code] = false;
        });
        document.addEventListener('mousemove', e => {
            if (this.isLocked) {
                this.mouseDX += e.movementX;
                this.mouseDY += e.movementY;
            }
        });
        this.canvas.addEventListener('click', () => {
            if (!this.isLocked) {
                this.canvas.requestPointerLock();
            }
        });
        document.addEventListener('pointerlockchange', () => {
            this.isLocked = document.pointerLockElement === this.canvas;
        });
    }

    get isMoving() {
        return this.keys['KeyW'] || this.keys['KeyS'] || this.keys['KeyA'] || this.keys['KeyD'];
    }

    get currentSpeed() {
        if (this.isCrouching) return this.crouchSpeed;
        if (this.isSprinting) return this.sprintSpeed;
        return this.walkSpeed;
    }

    get forward() {
        const dir = new THREE.Vector3(0, 0, -1);
        dir.applyQuaternion(this.camera.quaternion);
        dir.y = 0;
        dir.normalize();
        return dir;
    }

    get right() {
        const dir = new THREE.Vector3(1, 0, 0);
        dir.applyQuaternion(this.camera.quaternion);
        dir.y = 0;
        dir.normalize();
        return dir;
    }

    update(deltaTime, stamina) {
        if (!this.isLocked) return;

        // Mouse look
        this.euler.setFromQuaternion(this.camera.quaternion);
        this.euler.y -= this.mouseDX * this.sensitivity;
        this.euler.x -= this.mouseDY * this.sensitivity;
        this.euler.x = MathUtils.clamp(this.euler.x, -Math.PI / 2.1, Math.PI / 2.1);
        this.camera.quaternion.setFromEuler(this.euler);
        this.mouseDX = 0;
        this.mouseDY = 0;

        // Sprint
        this.isSprinting = this.keys['ShiftLeft'] && this.keys['KeyW'] && !this.isCrouching
                           && stamina.current > 0;

        // Crouch
        if (this.keys['KeyC'] || this.keys['ControlLeft']) {
            this.isCrouching = true;
        } else {
            this.isCrouching = false;
        }

        // Target height
        const targetH = this.isCrouching ? this.crouchHeight : this.standingHeight;
        this.currentHeight = MathUtils.lerp(this.currentHeight, targetH, deltaTime * 8);

        // Movement direction
        const moveDir = new THREE.Vector3(0, 0, 0);
        if (this.keys['KeyW']) moveDir.add(this.forward);
        if (this.keys['KeyS']) moveDir.sub(this.forward);
        if (this.keys['KeyA']) moveDir.sub(this.right);
        if (this.keys['KeyD']) moveDir.add(this.right);

        if (moveDir.lengthSq() > 0) {
            moveDir.normalize();
        }

        // Apply movement
        const speed = this.currentSpeed;
        this.position.x += moveDir.x * speed * deltaTime;
        this.position.z += moveDir.z * speed * deltaTime;

        // Collision detection
        this.environment.resolveCollision(this.position, this.velocity, this.playerRadius);

        // World bounds
        this.position.x = MathUtils.clamp(this.position.x, -95, 95);
        this.position.z = MathUtils.clamp(this.position.z, -95, 95);

        // Gravity & Jump
        if (this.isGrounded && this.keys['Space'] && !this.isCrouching) {
            this.velocity.y = this.jumpForce;
            this.isGrounded = false;
            if (stamina) stamina.use(15);
        }

        this.velocity.y += this.gravity * deltaTime;
        this.position.y += this.velocity.y * deltaTime;

        if (this.position.y <= this.currentHeight) {
            this.position.y = this.currentHeight;
            this.velocity.y = 0;
            this.isGrounded = true;
        }

        // Stamina drain for sprinting
        if (this.isSprinting && stamina) {
            stamina.use(15 * deltaTime);
        }

        // Head bob
        if (this.isMoving && this.isGrounded) {
            const bobSpeed = this.isSprinting ? this.headBobSpeed * 1.5 : this.headBobSpeed;
            this.headBobTimer += deltaTime * bobSpeed;
            const bobY = Math.sin(this.headBobTimer) * this.headBobAmount;
            this.position.y += bobY;

            // Footstep sounds
            this.footstepTimer += deltaTime;
            const interval = this.isSprinting ? this.footstepInterval * 0.6 : this.footstepInterval;
            if (this.footstepTimer >= interval) {
                this.footstepTimer = 0;
                audioManager.playSFX('footstep', 0.3, MathUtils.randFloat(0.8, 1.2));
            }
        } else {
            this.headBobTimer = 0;
            this.footstepTimer = 0;
        }

        // Update camera
        this.camera.position.copy(this.position);
    }

    setSensitivity(val) {
        this.sensitivity = val * 0.001;
    }

    teleport(x, y, z) {
        this.position.set(x, y || this.standingHeight, z);
        this.velocity.set(0, 0, 0);
    }
}
