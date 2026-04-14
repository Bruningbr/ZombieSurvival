// PlayerHealth.js - Health system with regen
class PlayerHealth {
    constructor() {
        this.max = 100;
        this.current = 100;
        this.regenRate = 2;
        this.regenDelay = 5;
        this.lastDamageTime = 0;
        this.isDead = false;
        this.isCritical = false;
        this.criticalThreshold = 25;
        this.heartbeatTimer = 0;
        this.onDeath = null;
        this.onDamage = null;
    }

    get percent() { return this.current / this.max; }

    takeDamage(amount) {
        if (this.isDead) return;
        this.current = Math.max(0, this.current - amount);
        this.lastDamageTime = performance.now() / 1000;

        audioManager.playSFX('player_hurt', 0.6);

        if (this.onDamage) this.onDamage(amount);

        if (this.current <= 0) {
            this.die();
        }
    }

    heal(amount) {
        if (this.isDead) return;
        this.current = Math.min(this.current + amount, this.max);
    }

    die() {
        this.isDead = true;
        this.current = 0;
        audioManager.playSFX('player_death', 0.8);
        if (this.onDeath) this.onDeath();
    }

    revive(healthPercent = 0.5) {
        this.isDead = false;
        this.current = this.max * healthPercent;
    }

    update(deltaTime) {
        if (this.isDead) return;

        const now = performance.now() / 1000;

        // Regen after delay
        if (now - this.lastDamageTime >= this.regenDelay && this.current < this.max) {
            this.current = Math.min(this.current + this.regenRate * deltaTime, this.max);
        }

        // Critical state
        this.isCritical = this.current <= this.criticalThreshold;
        if (this.isCritical) {
            this.heartbeatTimer += deltaTime;
            if (this.heartbeatTimer >= 1.0) {
                this.heartbeatTimer = 0;
                audioManager.playSFX('heartbeat', 0.5);
            }
        }
    }

    reset() {
        this.current = this.max;
        this.isDead = false;
        this.isCritical = false;
        this.heartbeatTimer = 0;
    }
}
