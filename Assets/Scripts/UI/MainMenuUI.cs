using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZombieSurvival.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Network")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField serverAddressInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Button settingsBackButton;

        private void Start()
        {
            SetupButtons();
            SetupSettings();
            ShowMainPanel();

            AudioManager.AudioManager.Instance?.PlayMusic("music_menu");
        }

        private void SetupButtons()
        {
            playButton?.onClick.AddListener(ShowPlayPanel);
            settingsButton?.onClick.AddListener(ShowSettingsPanel);
            creditsButton?.onClick.AddListener(ShowCreditsPanel);
            quitButton?.onClick.AddListener(QuitGame);
            hostButton?.onClick.AddListener(HostGame);
            joinButton?.onClick.AddListener(JoinGame);
            settingsBackButton?.onClick.AddListener(ShowMainPanel);
        }

        private void SetupSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioManager.AudioManager.Instance?.GetMasterVolume() ?? 1f;
                masterVolumeSlider.onValueChanged.AddListener(v => AudioManager.AudioManager.Instance?.SetMasterVolume(v));
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.AudioManager.Instance?.GetMusicVolume() ?? 0.5f;
                musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.AudioManager.Instance?.SetMusicVolume(v));
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.AudioManager.Instance?.GetSFXVolume() ?? 1f;
                sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.AudioManager.Instance?.SetSFXVolume(v));
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
                sensitivitySlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("MouseSensitivity", v));
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.AddListener(QualitySettings.SetQualityLevel);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
                vsyncToggle.onValueChanged.AddListener(v => QualitySettings.vSyncCount = v ? 1 : 0);
            }
        }

        private void ShowMainPanel()
        {
            mainPanel?.SetActive(true);
            settingsPanel?.SetActive(false);
            creditsPanel?.SetActive(false);
        }

        private void ShowPlayPanel()
        {
            // Show host/join panel
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void ShowSettingsPanel()
        {
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(true);
            creditsPanel?.SetActive(false);
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void ShowCreditsPanel()
        {
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            creditsPanel?.SetActive(true);
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void HostGame()
        {
            string playerName = playerNameInput?.text ?? "Player";
            PlayerPrefs.SetString("PlayerName", playerName);

            Network.GameNetworkManager.singleton?.StartHost();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void JoinGame()
        {
            string playerName = playerNameInput?.text ?? "Player";
            string address = serverAddressInput?.text ?? "localhost";

            PlayerPrefs.SetString("PlayerName", playerName);
            Network.GameNetworkManager.singleton.networkAddress = address;
            Network.GameNetworkManager.singleton?.StartClient();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void QuitGame()
        {
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
