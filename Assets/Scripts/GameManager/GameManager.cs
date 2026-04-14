using UnityEngine;
using Mirror;
using System;

namespace ZombieSurvival.GameManager
{
    public enum GameState
    {
        WaitingForPlayers,
        Countdown,
        Playing,
        WaveBreak,
        GameOver
    }

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float respawnTime = 10f;
        [SerializeField] private int maxRespawns = 3;
        [SerializeField] private bool friendlyFire = false;
        [SerializeField] private float gameOverDelay = 5f;

        [Header("Difficulty")]
        [SerializeField] private float difficultyScaleRate = 0.1f;
        [SerializeField] private float maxDifficultyMultiplier = 3f;

        [SyncVar(hook = nameof(OnGameStateChanged))]
        private GameState currentState = GameState.WaitingForPlayers;

        [SyncVar] private float gameTime;
        [SyncVar] private float difficultyMultiplier = 1f;

        public GameState CurrentState => currentState;
        public float GameTime => gameTime;
        public float DifficultyMultiplier => difficultyMultiplier;
        public bool FriendlyFire => friendlyFire;

        public event Action<GameState> OnStateChanged;
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentState = GameState.WaitingForPlayers;
        }

        private void Update()
        {
            if (!isServer) return;

            if (currentState == GameState.Playing)
            {
                gameTime += Time.deltaTime;
                UpdateDifficulty();
                CheckGameOverCondition();
            }
        }

        [Server]
        public void StartGame()
        {
            currentState = GameState.Playing;
            gameTime = 0;
            difficultyMultiplier = 1f;

            RpcOnGameStarted();
        }

        [Server]
        private void UpdateDifficulty()
        {
            difficultyMultiplier = Mathf.Min(
                1f + (gameTime / 60f) * difficultyScaleRate,
                maxDifficultyMultiplier
            );
        }

        [Server]
        private void CheckGameOverCondition()
        {
            bool allDead = true;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                var health = conn.identity.GetComponent<Player.PlayerHealth>();
                if (health != null && !health.IsDead)
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead && NetworkServer.connections.Count > 0)
            {
                EndGame();
            }
        }

        [Server]
        public void EndGame()
        {
            currentState = GameState.GameOver;

            // Submit all final scores
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                var score = conn.identity.GetComponent<Ranking.PlayerScore>();
                if (score != null)
                {
                    Ranking.LeaderboardManager.Instance?.SubmitFinalScore(score);
                }
            }

            RpcOnGameOver();
        }

        [ClientRpc]
        private void RpcOnGameStarted()
        {
            AudioManager.AudioManager.Instance?.PlayMusic("music_gameplay");
            AudioManager.AudioManager.Instance?.PlayMusic("ambient_night");
        }

        [ClientRpc]
        private void RpcOnGameOver()
        {
            OnGameOver?.Invoke();
            UI.GameOverScreen.Instance?.Show();
            AudioManager.AudioManager.Instance?.StopMusic();
            AudioManager.AudioManager.Instance?.PlaySFX("game_over");
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            OnStateChanged?.Invoke(newState);
        }

        [Server]
        public void NotifyWaveComplete(int wave)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                var score = conn.identity.GetComponent<Ranking.PlayerScore>();
                score?.CompleteWave();
            }
        }
    }
}
