// MenuManager.js - Handles all menu screens
class MenuManager {
    constructor() {
        this.screens = {
            mainMenu: document.getElementById('main-menu'),
            settings: document.getElementById('settings-menu'),
            multiplayer: document.getElementById('multiplayer-menu'),
            leaderboard: document.getElementById('leaderboard-screen'),
            pause: document.getElementById('pause-menu'),
            death: document.getElementById('death-screen'),
            gameover: document.getElementById('gameover-screen'),
            loading: document.getElementById('loading-screen'),
        };

        this.currentScreen = 'mainMenu';
        this.onPlay = null;
        this.onResume = null;
        this.onQuit = null;
        this.onRespawn = null;
        this.onPlayAgain = null;
        this.settingsChanged = null;

        this.respawnTimer = 0;
        this.respawnDuration = 5;

        this.setupEvents();
    }

    setupEvents() {
        // Main Menu
        document.getElementById('btn-play').addEventListener('click', () => {
            audioManager.init();
            audioManager.playSFX('button_click', 0.5);
            if (this.onPlay) this.onPlay();
        });

        document.getElementById('btn-multiplayer').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            this.showScreen('multiplayer');
        });

        document.getElementById('btn-leaderboard').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            this.showScreen('leaderboard');
            this.refreshLeaderboard();
        });

        document.getElementById('btn-settings').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            this.showScreen('settings');
        });

        // Settings
        document.getElementById('btn-settings-back').addEventListener('click', () => {
            this.showScreen('mainMenu');
        });

        document.getElementById('btn-pause-settings').addEventListener('click', () => {
            this.showScreen('settings');
        });

        // Volume sliders
        const masterVol = document.getElementById('setting-master-vol');
        const musicVol = document.getElementById('setting-music-vol');
        const sfxVol = document.getElementById('setting-sfx-vol');
        const sensitivity = document.getElementById('setting-sensitivity');

        masterVol.addEventListener('input', () => {
            document.getElementById('master-vol-val').textContent = masterVol.value + '%';
            audioManager.setMasterVolume(masterVol.value / 100);
        });

        musicVol.addEventListener('input', () => {
            document.getElementById('music-vol-val').textContent = musicVol.value + '%';
            audioManager.setMusicVolume(musicVol.value / 100);
        });

        sfxVol.addEventListener('input', () => {
            document.getElementById('sfx-vol-val').textContent = sfxVol.value + '%';
            audioManager.setSFXVolume(sfxVol.value / 100);
        });

        sensitivity.addEventListener('input', () => {
            document.getElementById('sensitivity-val').textContent = sensitivity.value;
            if (this.settingsChanged) this.settingsChanged('sensitivity', parseInt(sensitivity.value));
        });

        // Multiplayer
        document.getElementById('btn-mp-back').addEventListener('click', () => {
            this.showScreen('mainMenu');
        });

        document.getElementById('btn-host').addEventListener('click', () => {
            audioManager.init();
            if (this.onPlay) this.onPlay('host');
        });

        document.getElementById('btn-join').addEventListener('click', () => {
            audioManager.init();
            if (this.onPlay) this.onPlay('join');
        });

        // Leaderboard
        document.getElementById('btn-lb-back').addEventListener('click', () => {
            this.showScreen('mainMenu');
        });

        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                this.refreshLeaderboard(btn.dataset.sort);
            });
        });

        // Pause
        document.getElementById('btn-resume').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            if (this.onResume) this.onResume();
        });

        document.getElementById('btn-quit').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            if (this.onQuit) this.onQuit();
        });

        // Death
        document.getElementById('btn-respawn').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            if (this.onRespawn) this.onRespawn();
        });

        document.getElementById('btn-death-quit').addEventListener('click', () => {
            if (this.onQuit) this.onQuit();
        });

        // Game Over
        document.getElementById('btn-play-again').addEventListener('click', () => {
            audioManager.playSFX('button_click', 0.5);
            if (this.onPlayAgain) this.onPlayAgain();
        });

        document.getElementById('btn-gameover-leaderboard').addEventListener('click', () => {
            this.showScreen('leaderboard');
            this.refreshLeaderboard();
        });

        document.getElementById('btn-gameover-quit').addEventListener('click', () => {
            if (this.onQuit) this.onQuit();
        });

        // Escape key for pause
        document.addEventListener('keydown', e => {
            if (e.code === 'Escape') {
                if (this.currentScreen === 'pause') {
                    if (this.onResume) this.onResume();
                }
            }
        });
    }

    showScreen(name) {
        Object.values(this.screens).forEach(s => s.classList.remove('active'));
        if (this.screens[name]) {
            this.screens[name].classList.add('active');
        }
        this.currentScreen = name;
    }

    hideAll() {
        Object.values(this.screens).forEach(s => s.classList.remove('active'));
        this.currentScreen = null;
    }

    showLoading(progress, text) {
        this.showScreen('loading');
        document.getElementById('loading-bar').style.width = progress + '%';
        if (text) document.getElementById('loading-text').textContent = text;
    }

    showDeath(stats) {
        this.showScreen('death');
        this.respawnTimer = this.respawnDuration;
        const btn = document.getElementById('btn-respawn');
        btn.disabled = true;

        document.getElementById('death-stats').innerHTML = `
            Score: <span>${stats.score.toLocaleString()}</span><br>
            Kills: <span>${stats.kills}</span><br>
            Wave: <span>${stats.wave}</span><br>
            Tempo: <span>${MathUtils.formatTime(stats.survivalTime)}</span>
        `;
    }

    showGameOver(stats, rank) {
        this.showScreen('gameover');

        document.getElementById('gameover-stats').innerHTML = `
            Score Final: <span>${stats.score.toLocaleString()}</span><br>
            Total Kills: <span>${stats.kills}</span><br>
            Deaths: <span>${stats.deaths}</span><br>
            K/D Ratio: <span>${stats.deaths > 0 ? (stats.kills / stats.deaths).toFixed(2) : stats.kills}</span><br>
            Wave Máxima: <span>${stats.wave}</span><br>
            Tempo Total: <span>${MathUtils.formatTime(stats.survivalTime)}</span>
        `;

        const rankColors = ['#ffd700', '#c0c0c0', '#cd7f32'];
        const rankText = rank <= 3 ? `#${rank} - ${['OURO', 'PRATA', 'BRONZE'][rank-1]}` : `#${rank}`;
        document.getElementById('gameover-rank').innerHTML = `Ranking: ${rankText}`;
        if (rank <= 3) {
            document.getElementById('gameover-rank').style.color = rankColors[rank - 1];
        }
    }

    refreshLeaderboard(sortBy = 'score') {
        const list = document.getElementById('leaderboard-list');
        const entries = leaderboard.getEntries(sortBy);

        // Keep header
        list.innerHTML = `
            <div class="lb-header">
                <span>#</span><span>JOGADOR</span><span>SCORE</span><span>KILLS</span><span>WAVES</span><span>TEMPO</span>
            </div>
        `;

        entries.forEach((entry, idx) => {
            const div = document.createElement('div');
            div.className = 'lb-entry';
            if (idx === 0) div.classList.add('gold');
            else if (idx === 1) div.classList.add('silver');
            else if (idx === 2) div.classList.add('bronze');

            div.innerHTML = `
                <span>${idx + 1}</span>
                <span>${entry.name}</span>
                <span>${entry.score.toLocaleString()}</span>
                <span>${entry.kills}</span>
                <span>${entry.waves}</span>
                <span>${MathUtils.formatTime(entry.survivalTime)}</span>
            `;
            list.appendChild(div);
        });

        if (entries.length === 0) {
            const empty = document.createElement('div');
            empty.style.cssText = 'text-align:center; color:#666; padding:20px;';
            empty.textContent = 'Nenhum registro ainda. Jogue para aparecer no ranking!';
            list.appendChild(empty);
        }
    }

    update(deltaTime) {
        // Respawn timer
        if (this.currentScreen === 'death' && this.respawnTimer > 0) {
            this.respawnTimer -= deltaTime;
            document.getElementById('respawn-timer').textContent =
                `Respawn em ${Math.ceil(this.respawnTimer)}s...`;
            if (this.respawnTimer <= 0) {
                document.getElementById('btn-respawn').disabled = false;
                document.getElementById('respawn-timer').textContent = 'Pronto!';
            }
        }
    }
}
