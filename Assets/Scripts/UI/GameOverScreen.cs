using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace ZombieSurvival.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        public static GameOverScreen Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            playAgainButton?.onClick.AddListener(PlayAgain);
            leaderboardButton?.onClick.AddListener(ShowLeaderboard);
            mainMenuButton?.onClick.AddListener(ReturnToMenu);

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        public void Show()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            var score = localPlayer.GetComponent<Ranking.PlayerScore>();
            if (score == null) return;

            if (titleText != null)
                titleText.text = "GAME OVER";

            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {score.Score}";

            if (statsText != null)
            {
                statsText.text = $"Kills: {score.Kills}\n" +
                                 $"Deaths: {score.Deaths}\n" +
                                 $"K/D Ratio: {score.KDRatio:F2}\n" +
                                 $"Waves Survived: {score.WavesCompleted}\n" +
                                 $"Survival Time: {FormatTime(score.SurvivalTime)}";
            }

            if (rankText != null)
            {
                int rank = Ranking.LeaderboardManager.Instance?.GetPlayerRank(score.DisplayName) ?? -1;
                rankText.text = rank > 0 ? $"Rank #{rank}" : "Unranked";
            }
        }

        private void PlayAgain()
        {
            if (NetworkServer.active)
            {
                GameManager.GameManager.Instance?.StartGame();
            }
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void ShowLeaderboard()
        {
            LeaderboardUI.Instance?.Show();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void ReturnToMenu()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                Network.GameNetworkManager.singleton?.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                Network.GameNetworkManager.singleton?.StopClient();
            }
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
