using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

namespace ZombieSurvival.Ranking
{
    [System.Serializable]
    public struct PlayerScoreData
    {
        public string playerName;
        public int kills;
        public int deaths;
        public int score;
        public int wavesCompleted;
        public float survivalTime;

        public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;
    }

    public class PlayerScore : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnScoreChanged))]
        private int score;

        [SyncVar(hook = nameof(OnKillsChanged))]
        private int kills;

        [SyncVar(hook = nameof(OnDeathsChanged))]
        private int deaths;

        [SyncVar]
        private int wavesCompleted;

        [SyncVar]
        private float survivalTime;

        [SyncVar]
        private string playerDisplayName;

        public int Score => score;
        public int Kills => kills;
        public int Deaths => deaths;
        public int WavesCompleted => wavesCompleted;
        public float SurvivalTime => survivalTime;
        public string DisplayName => playerDisplayName;
        public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;

        public event Action<int> OnScoreUpdated;
        public event Action<int> OnKillsUpdated;

        private float startTime;

        public override void OnStartServer()
        {
            base.OnStartServer();
            score = 0;
            kills = 0;
            deaths = 0;
            wavesCompleted = 0;
            startTime = Time.time;
        }

        private void Update()
        {
            if (isServer)
            {
                survivalTime = Time.time - startTime;
            }
        }

        [Server]
        public void AddScore(int amount)
        {
            score += amount;
            LeaderboardManager.Instance?.UpdatePlayerScore(this);
        }

        [Server]
        public void AddKill()
        {
            kills++;
            AddScore(10);
        }

        [Server]
        public void AddDeath()
        {
            deaths++;
        }

        [Server]
        public void CompleteWave()
        {
            wavesCompleted++;
            AddScore(50 * wavesCompleted);
        }

        [Server]
        public void SetDisplayName(string name)
        {
            playerDisplayName = name;
        }

        public PlayerScoreData GetScoreData()
        {
            return new PlayerScoreData
            {
                playerName = playerDisplayName,
                kills = kills,
                deaths = deaths,
                score = score,
                wavesCompleted = wavesCompleted,
                survivalTime = survivalTime
            };
        }

        private void OnScoreChanged(int oldScore, int newScore)
        {
            OnScoreUpdated?.Invoke(newScore);
        }

        private void OnKillsChanged(int oldKills, int newKills)
        {
            OnKillsUpdated?.Invoke(newKills);
        }

        private void OnDeathsChanged(int oldDeaths, int newDeaths)
        {
            // UI update hook
        }
    }

    public class LeaderboardManager : NetworkBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxLeaderboardEntries = 100;
        [SerializeField] private string leaderboardKey = "zombie_survival_leaderboard";

        private readonly SyncList<PlayerScoreData> leaderboard = new SyncList<PlayerScoreData>();

        public SyncList<PlayerScoreData> Leaderboard => leaderboard;

        public event Action OnLeaderboardUpdated;

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
            LoadLeaderboard();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            leaderboard.Callback += OnLeaderboardChanged;
        }

        [Server]
        public void UpdatePlayerScore(PlayerScore playerScore)
        {
            var data = playerScore.GetScoreData();

            // Update or add entry
            bool found = false;
            for (int i = 0; i < leaderboard.Count; i++)
            {
                if (leaderboard[i].playerName == data.playerName)
                {
                    if (data.score > leaderboard[i].score)
                    {
                        leaderboard[i] = data;
                    }
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                leaderboard.Add(data);
            }

            SortLeaderboard();
            TrimLeaderboard();
            SaveLeaderboard();
        }

        [Server]
        public void SubmitFinalScore(PlayerScore playerScore)
        {
            UpdatePlayerScore(playerScore);
            RpcNotifyScoreSubmitted(playerScore.DisplayName, playerScore.Score);
        }

        [ClientRpc]
        private void RpcNotifyScoreSubmitted(string playerName, int score)
        {
            Debug.Log($"Score submitted: {playerName} - {score}");
        }

        private void SortLeaderboard()
        {
            // Simple bubble sort since SyncList doesn't support Sort directly
            for (int i = 0; i < leaderboard.Count - 1; i++)
            {
                for (int j = 0; j < leaderboard.Count - i - 1; j++)
                {
                    if (leaderboard[j].score < leaderboard[j + 1].score)
                    {
                        var temp = leaderboard[j];
                        leaderboard[j] = leaderboard[j + 1];
                        leaderboard[j + 1] = temp;
                    }
                }
            }
        }

        private void TrimLeaderboard()
        {
            while (leaderboard.Count > maxLeaderboardEntries)
            {
                leaderboard.RemoveAt(leaderboard.Count - 1);
            }
        }

        private void SaveLeaderboard()
        {
            // Save to PlayerPrefs as JSON (for demo; use a real backend for production)
            var wrapper = new LeaderboardWrapper();
            wrapper.entries = new List<PlayerScoreData>();
            foreach (var entry in leaderboard)
            {
                wrapper.entries.Add(entry);
            }
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(leaderboardKey, json);
            PlayerPrefs.Save();
        }

        private void LoadLeaderboard()
        {
            string json = PlayerPrefs.GetString(leaderboardKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
                if (wrapper?.entries != null)
                {
                    foreach (var entry in wrapper.entries)
                    {
                        leaderboard.Add(entry);
                    }
                }
            }
        }

        public int GetPlayerRank(string playerName)
        {
            for (int i = 0; i < leaderboard.Count; i++)
            {
                if (leaderboard[i].playerName == playerName)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        public List<PlayerScoreData> GetTopPlayers(int count)
        {
            var result = new List<PlayerScoreData>();
            int max = Mathf.Min(count, leaderboard.Count);
            for (int i = 0; i < max; i++)
            {
                result.Add(leaderboard[i]);
            }
            return result;
        }

        private void OnLeaderboardChanged(SyncList<PlayerScoreData>.Operation op, int index,
            PlayerScoreData oldItem, PlayerScoreData newItem)
        {
            OnLeaderboardUpdated?.Invoke();
        }
    }

    [System.Serializable]
    public class LeaderboardWrapper
    {
        public List<PlayerScoreData> entries;
    }
}
