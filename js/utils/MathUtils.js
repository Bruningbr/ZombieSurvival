// MathUtils.js - Utility functions
const MathUtils = {
    clamp(val, min, max) {
        return Math.min(Math.max(val, min), max);
    },
    lerp(a, b, t) {
        return a + (b - a) * t;
    },
    randFloat(min, max) {
        return Math.random() * (max - min) + min;
    },
    randInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    },
    distance3D(a, b) {
        const dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
        return Math.sqrt(dx * dx + dy * dy + dz * dz);
    },
    distanceFlat(a, b) {
        const dx = a.x - b.x, dz = a.z - b.z;
        return Math.sqrt(dx * dx + dz * dz);
    },
    angleBetween(from, to) {
        const dx = to.x - from.x, dz = to.z - from.z;
        return Math.atan2(dx, dz);
    },
    formatTime(seconds) {
        const m = Math.floor(seconds / 60);
        const s = Math.floor(seconds % 60);
        return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    }
};
