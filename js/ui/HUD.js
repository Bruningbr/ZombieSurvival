// HUD.js - In-game HUD management
class HUD {
    constructor() {
        this.elements = {
            hud: document.getElementById('game-hud'),
            healthBar: document.getElementById('health-bar'),
            healthText: document.getElementById('health-text'),
            staminaBar: document.getElementById('stamina-bar'),
            ammoCurrent: document.getElementById('ammo-current'),
            ammoReserve: document.getElementById('ammo-reserve'),
            weaponName: document.getElementById('weapon-name'),
            waveNumber: document.getElementById('wave-number'),
            zombieCount: document.getElementById('zombie-count'),
            scoreDisplay: document.getElementById('score-display'),
            killsDisplay: document.getElementById('kills-display'),
            crosshair: document.getElementById('crosshair'),
            hitMarker: document.getElementById('hit-marker'),
            damageOverlay: document.getElementById('damage-overlay'),
            waveNotification: document.getElementById('wave-notification'),
            waveNotifyText: document.getElementById('wave-notify-text'),
            waveNotifySub: document.getElementById('wave-notify-sub'),
            interactionPrompt: document.getElementById('interaction-prompt'),
            weaponSlots: document.querySelectorAll('.weapon-slot')
        };

        this.hitMarkerTimer = 0;
        this.damageOverlayTimer = 0;
        this.waveNotifyTimer = 0;
    }

    show() {
        this.elements.hud.classList.remove('hidden');
    }

    hide() {
        this.elements.hud.classList.add('hidden');
    }

    updateHealth(current, max) {
        const pct = (current / max) * 100;
        this.elements.healthBar.style.width = pct + '%';
        this.elements.healthText.textContent = Math.ceil(current);

        if (pct <= 25) {
            this.elements.healthBar.style.background = 'linear-gradient(90deg, #ff0000, #ff2222)';
            this.elements.healthText.style.color = '#ff0000';
        } else if (pct <= 50) {
            this.elements.healthBar.style.background = 'linear-gradient(90deg, #ff4400, #ff6622)';
            this.elements.healthText.style.color = '#ff4400';
        } else {
            this.elements.healthBar.style.background = 'linear-gradient(90deg, #ff2222, #ff4444)';
            this.elements.healthText.style.color = '#ff3333';
        }
    }

    updateStamina(current, max) {
        const pct = (current / max) * 100;
        this.elements.staminaBar.style.width = pct + '%';

        if (pct <= 20) {
            this.elements.staminaBar.style.background = 'linear-gradient(90deg, #ff4400, #ff6600)';
        } else {
            this.elements.staminaBar.style.background = 'linear-gradient(90deg, #ffaa00, #ffcc00)';
        }
    }

    updateAmmo(current, reserve) {
        this.elements.ammoCurrent.textContent = current === Infinity ? '∞' : current;
        this.elements.ammoReserve.textContent = reserve;
    }

    updateWeapon(weapon) {
        if (!weapon) return;
        this.elements.weaponName.textContent = weapon.name;

        this.elements.weaponSlots.forEach((slot, idx) => {
            slot.classList.remove('active');
        });
    }

    updateWeaponSlots(weapons, currentSlot) {
        this.elements.weaponSlots.forEach((slot, idx) => {
            if (weapons[idx]) {
                slot.textContent = `${idx + 1}: ${weapons[idx].name}`;
                slot.classList.toggle('active', idx === currentSlot);
            } else {
                slot.textContent = `${idx + 1}: ---`;
                slot.classList.remove('active');
            }
        });
    }

    updateWave(wave, zombiesLeft) {
        this.elements.waveNumber.textContent = `WAVE ${wave}`;
        this.elements.zombieCount.textContent = `Zombies: ${zombiesLeft}`;
    }

    updateScore(score, kills) {
        this.elements.scoreDisplay.textContent = `Score: ${score.toLocaleString()}`;
        this.elements.killsDisplay.textContent = `Kills: ${kills}`;
    }

    showHitMarker(isHeadshot) {
        this.elements.hitMarker.classList.remove('hidden');
        this.elements.hitMarker.classList.toggle('headshot', isHeadshot);
        this.hitMarkerTimer = 0.2;
        audioManager.playSFX(isHeadshot ? 'headshot' : 'hit_marker', 0.5);
    }

    showDamageOverlay(intensity = 0.5) {
        this.elements.damageOverlay.style.opacity = intensity;
        this.damageOverlayTimer = 0.3;
    }

    showWaveNotification(wave, zombieCount) {
        this.elements.waveNotifyText.textContent = `WAVE ${wave}`;
        this.elements.waveNotifySub.textContent = `${zombieCount} Zombies!`;
        this.elements.waveNotification.classList.remove('hidden');
        this.waveNotifyTimer = 3;
    }

    showInteraction(text) {
        this.elements.interactionPrompt.querySelector('span').textContent = text;
        this.elements.interactionPrompt.classList.remove('hidden');
    }

    hideInteraction() {
        this.elements.interactionPrompt.classList.add('hidden');
    }

    update(deltaTime) {
        // Hit marker fade
        if (this.hitMarkerTimer > 0) {
            this.hitMarkerTimer -= deltaTime;
            if (this.hitMarkerTimer <= 0) {
                this.elements.hitMarker.classList.add('hidden');
            }
        }

        // Damage overlay fade
        if (this.damageOverlayTimer > 0) {
            this.damageOverlayTimer -= deltaTime;
            const opacity = parseFloat(this.elements.damageOverlay.style.opacity) || 0;
            this.elements.damageOverlay.style.opacity = Math.max(0, opacity - deltaTime * 2);
        }

        // Wave notification
        if (this.waveNotifyTimer > 0) {
            this.waveNotifyTimer -= deltaTime;
            if (this.waveNotifyTimer <= 0) {
                this.elements.waveNotification.classList.add('hidden');
            }
        }
    }
}
