// Leaderboard.js - Score tracking and persistence
class Leaderboard {
    constructor() {
        this.entries = [];
        this.storageKey = 'zombieSurvival_leaderboard';
        this.load();
    }

    load() {
        try {
            const data = localStorage.getItem(this.storageKey);
            this.entries = data ? JSON.parse(data) : [];
        } catch (e) {
            this.entries = [];
        }
    }

    save() {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(this.entries));
        } catch (e) {
            console.warn('Could not save leaderboard');
        }
    }

    addEntry(name, score, kills, deaths, waves, survivalTime) {
        const entry = {
            name: name || 'Jogador',
            score,
            kills,
            deaths,
            waves,
            survivalTime,
            date: Date.now()
        };

        this.entries.push(entry);
        this.entries.sort((a, b) => b.score - a.score);

        // Keep top 100
        if (this.entries.length > 100) {
            this.entries = this.entries.slice(0, 100);
        }

        this.save();

        // Return rank
        return this.entries.findIndex(e => e === entry) + 1;
    }

    getEntries(sortBy = 'score') {
        const sorted = [...this.entries];
        switch (sortBy) {
            case 'kills':
                sorted.sort((a, b) => b.kills - a.kills);
                break;
            case 'waves':
                sorted.sort((a, b) => b.waves - a.waves);
                break;
            case 'survivalTime':
                sorted.sort((a, b) => b.survivalTime - a.survivalTime);
                break;
            default:
                sorted.sort((a, b) => b.score - a.score);
        }
        return sorted;
    }

    getTopScore() {
        return this.entries.length > 0 ? this.entries[0].score : 0;
    }

    clear() {
        this.entries = [];
        this.save();
    }
}

const leaderboard = new Leaderboard();
