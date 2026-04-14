using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace ZombieSurvival.UI
{
    public class DeathScreen : MonoBehaviour
    {
        public static DeathScreen Instance { get; private set; }

        [Header("Death Screen")]
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private TextMeshProUGUI deathText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button spectateButton;
        [SerializeField] private Button quitButton;

        [Header("Respawn")]
        [SerializeField] private float respawnCooldown = 10f;
        [SerializeField] private TextMeshProUGUI respawnTimerText;

        private float respawnTimer;
        private bool canRespawn;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            respawnButton?.onClick.AddListener(Respawn);
            spectateButton?.onClick.AddListener(Spectate);
            quitButton?.onClick.AddListener(QuitToMenu);

            if (deathPanel != null)
                deathPanel.SetActive(false);
        }

        private void Update()
        {
            if (deathPanel != null && deathPanel.activeSelf && !canRespawn)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimerText != null)
                    respawnTimerText.text = $"Respawn in {Mathf.CeilToInt(respawnTimer)}s";

                if (respawnTimer <= 0)
                {
                    canRespawn = true;
                    if (respawnButton != null)
                        respawnButton.interactable = true;
                    if (respawnTimerText != null)
                        respawnTimerText.text = "Ready to respawn!";
                }
            }
        }

        public void Show()
        {
            if (deathPanel != null)
                deathPanel.SetActive(true);

            respawnTimer = respawnCooldown;
            canRespawn = false;

            if (respawnButton != null)
                respawnButton.interactable = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateStats();

            AudioManager.AudioManager.Instance?.PlaySFX("player_death");
        }

        public void Hide()
        {
            if (deathPanel != null)
                deathPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UpdateStats()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            var score = localPlayer.GetComponent<Ranking.PlayerScore>();
            if (score != null && statsText != null)
            {
                statsText.text = $"Score: {score.Score}\n" +
                                 $"Kills: {score.Kills}\n" +
                                 $"Deaths: {score.Deaths}\n" +
                                 $"Waves: {score.WavesCompleted}\n" +
                                 $"K/D: {score.KDRatio:F2}\n" +
                                 $"Survival: {FormatTime(score.SurvivalTime)}";
            }
        }

        private void Respawn()
        {
            if (!canRespawn) return;

            var conn = NetworkClient.connection;
            if (conn != null)
            {
                Network.GameNetworkManager.singleton?.RespawnPlayer(conn as NetworkConnectionToClient);
            }

            Hide();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void Spectate()
        {
            // Enable spectator camera
            Hide();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void QuitToMenu()
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

    public class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject settingsPanel;

        private bool isPaused;

        private void Start()
        {
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(ShowSettings);
            quitButton?.onClick.AddListener(QuitToMenu);

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
        }

        public void Pause()
        {
            isPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            isPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ShowSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void QuitToMenu()
        {
            Resume();

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                Network.GameNetworkManager.singleton?.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                Network.GameNetworkManager.singleton?.StopClient();
            }
        }
    }
}
