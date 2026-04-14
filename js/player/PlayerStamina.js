// PlayerStamina.js - Stamina system
class PlayerStamina {
    constructor() {
        this.max = 100;
        this.current = 100;
        this.drainRate = 15;
        this.regenRate = 12;
        this.regenDelay = 1.5;
        this.lastUseTime = 0;
        this.isExhausted = false;
        this.exhaustionRecovery = 25;
    }

    get percent() { return this.current / this.max; }

    use(amount) {
        if (this.isExhausted) return false;
        this.current = Math.max(0, this.current - amount);
        this.lastUseTime = performance.now() / 1000;

        if (this.current <= 0) {
            this.isExhausted = true;
        }
        return true;
    }

    update(deltaTime) {
        const now = performance.now() / 1000;

        if (now - this.lastUseTime >= this.regenDelay) {
            if (this.current < this.max) {
                this.current = Math.min(this.current + this.regenRate * deltaTime, this.max);
            }
            if (this.isExhausted && this.current >= this.exhaustionRecovery) {
                this.isExhausted = false;
            }
        }
    }

    reset() {
        this.current = this.max;
        this.isExhausted = false;
    }
}
