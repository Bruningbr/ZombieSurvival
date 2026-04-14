// DayNightCycle.js - Dynamic lighting and day/night cycle
class DayNightCycle {
    constructor(scene) {
        this.scene = scene;
        this.timeOfDay = 0.75; // Start at dusk for spooky atmosphere
        this.cycleDuration = 300; // 5 minutes per full cycle
        this.sunLight = null;
        this.ambientLight = null;
        this.moonLight = null;
        this.fog = null;
        this.setup();
    }

    setup() {
        // Sun
        this.sunLight = new THREE.DirectionalLight(0xffeedd, 1.0);
        this.sunLight.castShadow = true;
        this.sunLight.shadow.mapSize.width = 2048;
        this.sunLight.shadow.mapSize.height = 2048;
        this.sunLight.shadow.camera.near = 0.5;
        this.sunLight.shadow.camera.far = 200;
        this.sunLight.shadow.camera.left = -80;
        this.sunLight.shadow.camera.right = 80;
        this.sunLight.shadow.camera.top = 80;
        this.sunLight.shadow.camera.bottom = -80;
        this.scene.add(this.sunLight);

        // Ambient
        this.ambientLight = new THREE.AmbientLight(0x334455, 0.3);
        this.scene.add(this.ambientLight);

        // Moon
        this.moonLight = new THREE.DirectionalLight(0x4466aa, 0.2);
        this.scene.add(this.moonLight);

        // Hemisphere light for better ambient
        this.hemiLight = new THREE.HemisphereLight(0x8899bb, 0x333322, 0.3);
        this.scene.add(this.hemiLight);

        // Fog
        this.scene.fog = new THREE.FogExp2(0x111122, 0.008);
    }

    get isNight() {
        return this.timeOfDay > 0.75 || this.timeOfDay < 0.25;
    }

    get isDusk() {
        return this.timeOfDay > 0.65 && this.timeOfDay <= 0.75;
    }

    get isDawn() {
        return this.timeOfDay > 0.2 && this.timeOfDay <= 0.3;
    }

    update(deltaTime) {
        this.timeOfDay += deltaTime / this.cycleDuration;
        if (this.timeOfDay >= 1.0) this.timeOfDay -= 1.0;

        const t = this.timeOfDay;
        const sunAngle = t * Math.PI * 2 - Math.PI / 2;

        // Sun position
        this.sunLight.position.set(
            Math.cos(sunAngle) * 100,
            Math.sin(sunAngle) * 100,
            50
        );
        this.sunLight.target.position.set(0, 0, 0);

        // Moon opposite to sun
        this.moonLight.position.set(
            -Math.cos(sunAngle) * 80,
            -Math.sin(sunAngle) * 80 + 40,
            -30
        );

        // Sun intensity based on height
        const sunHeight = Math.sin(sunAngle);
        const sunIntensity = MathUtils.clamp(sunHeight * 2, 0, 1.2);
        this.sunLight.intensity = sunIntensity;

        // Sun color (warm at horizon, white at zenith)
        const warmth = 1 - MathUtils.clamp(sunHeight, 0, 1);
        this.sunLight.color.setRGB(
            1,
            0.9 - warmth * 0.3,
            0.8 - warmth * 0.5
        );

        // Moon intensity (visible at night)
        const moonIntensity = MathUtils.clamp(-sunHeight * 0.5, 0, 0.3);
        this.moonLight.intensity = moonIntensity;

        // Ambient light
        const ambIntensity = MathUtils.clamp(sunHeight * 0.5 + 0.15, 0.05, 0.5);
        this.ambientLight.intensity = ambIntensity;

        // Ambient color shifts
        if (this.isNight) {
            this.ambientLight.color.setHex(0x222244);
            this.hemiLight.color.setHex(0x334466);
            this.hemiLight.groundColor.setHex(0x111122);
        } else if (this.isDusk || this.isDawn) {
            this.ambientLight.color.setHex(0x553322);
            this.hemiLight.color.setHex(0x886644);
            this.hemiLight.groundColor.setHex(0x332211);
        } else {
            this.ambientLight.color.setHex(0x556677);
            this.hemiLight.color.setHex(0x8899bb);
            this.hemiLight.groundColor.setHex(0x333322);
        }

        // Fog
        if (this.isNight) {
            this.scene.fog.color.setHex(0x0a0a15);
            this.scene.fog.density = 0.012;
        } else if (this.isDusk || this.isDawn) {
            this.scene.fog.color.setHex(0x332211);
            this.scene.fog.density = 0.008;
        } else {
            this.scene.fog.color.setHex(0x889aab);
            this.scene.fog.density = 0.005;
        }

        // Background color
        if (this.isNight) {
            this.scene.background = new THREE.Color(0x0a0a18);
        } else if (this.isDusk) {
            this.scene.background = new THREE.Color(0x331a10);
        } else if (this.isDawn) {
            this.scene.background = new THREE.Color(0x2a1a20);
        } else {
            const skyBright = MathUtils.clamp(sunHeight, 0.3, 1);
            this.scene.background = new THREE.Color().setRGB(
                0.4 * skyBright,
                0.5 * skyBright,
                0.7 * skyBright
            );
        }
    }
}
