// AudioManager.js - Handles all game audio
class AudioManager {
    constructor() {
        this.ctx = null;
        this.masterGain = null;
        this.musicGain = null;
        this.sfxGain = null;
        this.buffers = {};
        this.musicSource = null;
        this.initialized = false;
        this.masterVolume = 0.8;
        this.musicVolume = 0.5;
        this.sfxVolume = 0.8;
    }

    init() {
        if (this.initialized) return;
        this.ctx = new (window.AudioContext || window.webkitAudioContext)();
        this.masterGain = this.ctx.createGain();
        this.masterGain.connect(this.ctx.destination);
        this.masterGain.gain.value = this.masterVolume;

        this.musicGain = this.ctx.createGain();
        this.musicGain.connect(this.masterGain);
        this.musicGain.gain.value = this.musicVolume;

        this.sfxGain = this.ctx.createGain();
        this.sfxGain.connect(this.masterGain);
        this.sfxGain.gain.value = this.sfxVolume;

        this.initialized = true;
        this.generateSounds();
    }

    generateSounds() {
        // Generate procedural sounds since we can't load files
        this.buffers.shoot_pistol = this.createGunshot(0.15, 800, 0.8);
        this.buffers.shoot_rifle = this.createGunshot(0.12, 600, 0.9);
        this.buffers.shoot_shotgun = this.createGunshot(0.25, 400, 1.0);
        this.buffers.shoot_smg = this.createGunshot(0.08, 900, 0.6);
        this.buffers.shoot_sniper = this.createGunshot(0.3, 300, 1.0);
        this.buffers.melee_swing = this.createSwing();
        this.buffers.reload = this.createReload();
        this.buffers.empty_click = this.createClick();
        this.buffers.zombie_groan = this.createZombieGroan();
        this.buffers.zombie_attack = this.createZombieAttack();
        this.buffers.zombie_death = this.createZombieDeath();
        this.buffers.zombie_detect = this.createZombieDetect();
        this.buffers.player_hurt = this.createPlayerHurt();
        this.buffers.player_death = this.createPlayerDeath();
        this.buffers.pickup = this.createPickup();
        this.buffers.hit_marker = this.createHitMarker();
        this.buffers.headshot = this.createHeadshot();
        this.buffers.wave_start = this.createWaveStart();
        this.buffers.wave_complete = this.createWaveComplete();
        this.buffers.footstep = this.createFootstep();
        this.buffers.heartbeat = this.createHeartbeat();
        this.buffers.button_click = this.createClick();
        this.buffers.ambient_wind = this.createWind();
    }

    createGunshot(duration, freq, volume) {
        const rate = this.ctx.sampleRate;
        const len = rate * duration;
        const buffer = this.ctx.createBuffer(1, len, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < len; i++) {
            const t = i / rate;
            const env = Math.exp(-t * 30) * volume;
            data[i] = (Math.random() * 2 - 1) * env +
                       Math.sin(2 * Math.PI * freq * t) * env * 0.3 *
                       Math.exp(-t * 50);
        }
        return buffer;
    }

    createSwing() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.3, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * (200 + t * 2000) * t) *
                       Math.exp(-t * 10) * 0.5;
        }
        return buffer;
    }

    createReload() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.5, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            const click1 = t < 0.05 ? Math.random() * Math.exp(-t * 100) : 0;
            const click2 = (t > 0.2 && t < 0.25) ? Math.random() * Math.exp(-(t - 0.2) * 100) : 0;
            const slide = (t > 0.3 && t < 0.45) ? Math.sin(t * 3000) * Math.exp(-(t - 0.3) * 20) * 0.3 : 0;
            data[i] = (click1 + click2 + slide) * 0.7;
        }
        return buffer;
    }

    createClick() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.05, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * 1500 * t) * Math.exp(-t * 200) * 0.3;
        }
        return buffer;
    }

    createZombieGroan() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 1.5, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            const freq = 80 + Math.sin(t * 3) * 30;
            const env = Math.sin(t * Math.PI / 1.5) * 0.4;
            data[i] = (Math.sin(2 * Math.PI * freq * t) * 0.5 +
                       Math.sin(2 * Math.PI * freq * 1.5 * t) * 0.3 +
                       Math.random() * 0.2) * env;
        }
        return buffer;
    }

    createZombieAttack() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.5, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            const freq = 150 + t * 200;
            data[i] = (Math.sin(2 * Math.PI * freq * t) * 0.5 +
                       Math.random() * 0.3) * Math.exp(-t * 4) * 0.6;
        }
        return buffer;
    }

    createZombieDeath() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 1.0, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            const freq = 120 - t * 60;
            data[i] = (Math.sin(2 * Math.PI * freq * t) * 0.4 +
                       Math.random() * 0.3) * Math.exp(-t * 3) * 0.5;
        }
        return buffer;
    }

    createZombieDetect() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.8, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = (Math.sin(2 * Math.PI * (200 + t * 300) * t) * 0.5 +
                       Math.random() * 0.3) * Math.exp(-t * 3) * 0.6;
        }
        return buffer;
    }

    createPlayerHurt() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.3, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = (Math.random() * 0.5 + Math.sin(t * 500) * 0.3) * Math.exp(-t * 10) * 0.5;
        }
        return buffer;
    }

    createPlayerDeath() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 1.5, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * (200 - t * 100) * t) * Math.exp(-t * 2) * 0.4;
        }
        return buffer;
    }

    createPickup() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.3, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * (600 + t * 800) * t) * Math.exp(-t * 10) * 0.4;
        }
        return buffer;
    }

    createHitMarker() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.1, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * 2000 * t) * Math.exp(-t * 50) * 0.3;
        }
        return buffer;
    }

    createHeadshot() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.15, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = (Math.sin(2 * Math.PI * 2500 * t) + Math.sin(2 * Math.PI * 3000 * t)) *
                       Math.exp(-t * 40) * 0.3;
        }
        return buffer;
    }

    createWaveStart() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 1.0, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.sin(2 * Math.PI * (100 + t * 200) * t) * Math.exp(-t * 2) * 0.5;
        }
        return buffer;
    }

    createWaveComplete() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.8, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = (Math.sin(2 * Math.PI * 523 * t) * (t < 0.3 ? 1 : 0) +
                       Math.sin(2 * Math.PI * 659 * t) * (t > 0.2 && t < 0.5 ? 1 : 0) +
                       Math.sin(2 * Math.PI * 784 * t) * (t > 0.4 ? 1 : 0)) *
                       Math.exp(-t * 3) * 0.3;
        }
        return buffer;
    }

    createFootstep() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.1, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = Math.random() * Math.exp(-t * 40) * 0.15;
        }
        return buffer;
    }

    createHeartbeat() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 0.6, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            const beat1 = Math.sin(2 * Math.PI * 60 * t) * Math.exp(-Math.pow((t - 0.05) * 20, 2));
            const beat2 = Math.sin(2 * Math.PI * 50 * t) * Math.exp(-Math.pow((t - 0.2) * 15, 2));
            data[i] = (beat1 + beat2) * 0.6;
        }
        return buffer;
    }

    createWind() {
        const rate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, rate * 3, rate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < data.length; i++) {
            const t = i / rate;
            data[i] = (Math.random() * 2 - 1) * 0.03 *
                       (1 + Math.sin(t * 0.5) * 0.5);
        }
        return buffer;
    }

    playSFX(name, volume = 1.0, pitch = 1.0) {
        if (!this.initialized || !this.buffers[name]) return;
        const source = this.ctx.createBufferSource();
        source.buffer = this.buffers[name];
        source.playbackRate.value = pitch + MathUtils.randFloat(-0.05, 0.05);
        const gain = this.ctx.createGain();
        gain.gain.value = volume;
        source.connect(gain);
        gain.connect(this.sfxGain);
        source.start();
        return source;
    }

    playLoop(name, volume = 1.0) {
        if (!this.initialized || !this.buffers[name]) return null;
        const source = this.ctx.createBufferSource();
        source.buffer = this.buffers[name];
        source.loop = true;
        const gain = this.ctx.createGain();
        gain.gain.value = volume;
        source.connect(gain);
        gain.connect(this.sfxGain);
        source.start();
        return { source, gain };
    }

    setMasterVolume(v) {
        this.masterVolume = v;
        if (this.masterGain) this.masterGain.gain.value = v;
    }

    setMusicVolume(v) {
        this.musicVolume = v;
        if (this.musicGain) this.musicGain.gain.value = v;
    }

    setSFXVolume(v) {
        this.sfxVolume = v;
        if (this.sfxGain) this.sfxGain.gain.value = v;
    }
}

const audioManager = new AudioManager();
