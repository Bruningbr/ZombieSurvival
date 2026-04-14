using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ZombieSurvival.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI Instance { get; private set; }

        [Header("Leaderboard Panel")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Transform leaderboardContent;
        [SerializeField] private GameObject leaderboardEntryPrefab;
        [SerializeField] private Button closeButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button scoreTabButton;
        [SerializeField] private Button killsTabButton;
        [SerializeField] private Button survivalTabButton;

        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color localPlayerColor = new Color(0.5f, 0.8f, 1f);

        private enum SortMode { Score, Kills, Survival }
        private SortMode currentSort = SortMode.Score;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            closeButton?.onClick.AddListener(Hide);
            scoreTabButton?.onClick.AddListener(() => SortBy(SortMode.Score));
            killsTabButton?.onClick.AddListener(() => SortBy(SortMode.Kills));
            survivalTabButton?.onClick.AddListener(() => SortBy(SortMode.Survival));

            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);

            var leaderboard = Ranking.LeaderboardManager.Instance;
            if (leaderboard != null)
            {
                leaderboard.OnLeaderboardUpdated += RefreshUI;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Toggle();
            }
        }

        public void Show()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);

            RefreshUI();
        }

        public void Hide()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (leaderboardPanel != null)
            {
                bool isActive = !leaderboardPanel.activeSelf;
                leaderboardPanel.SetActive(isActive);
                if (isActive) RefreshUI();
            }
        }

        private void SortBy(SortMode mode)
        {
            currentSort = mode;
            RefreshUI();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        public void RefreshUI()
        {
            if (leaderboardContent == null) return;

            // Clear existing entries
            foreach (Transform child in leaderboardContent)
            {
                Destroy(child.gameObject);
            }

            var leaderboard = Ranking.LeaderboardManager.Instance;
            if (leaderboard == null) return;

            var entries = new List<Ranking.PlayerScoreData>();
            foreach (var entry in leaderboard.Leaderboard)
            {
                entries.Add(entry);
            }

            // Sort
            entries.Sort((a, b) =>
            {
                return currentSort switch
                {
                    SortMode.Kills => b.kills.CompareTo(a.kills),
                    SortMode.Survival => b.survivalTime.CompareTo(a.survivalTime),
                    _ => b.score.CompareTo(a.score)
                };
            });

            for (int i = 0; i < entries.Count; i++)
            {
                CreateLeaderboardEntry(i + 1, entries[i]);
            }
        }

        private void CreateLeaderboardEntry(int rank, Ranking.PlayerScoreData data)
        {
            if (leaderboardEntryPrefab == null || leaderboardContent == null) return;

            var entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 6)
            {
                texts[0].text = $"#{rank}";
                texts[1].text = data.playerName;
                texts[2].text = data.score.ToString();
                texts[3].text = data.kills.ToString();
                texts[4].text = data.deaths.ToString();
                texts[5].text = FormatTime(data.survivalTime);
            }

            // Color based on rank
            Color rankColor = rank switch
            {
                1 => goldColor,
                2 => silverColor,
                3 => bronzeColor,
                _ => normalColor
            };

            var images = entry.GetComponentsInChildren<Image>();
            if (images.Length > 0)
            {
                images[0].color = rankColor;
            }
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
