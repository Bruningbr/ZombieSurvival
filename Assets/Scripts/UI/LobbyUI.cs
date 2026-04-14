using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;

namespace ZombieSurvival.UI
{
    public class LobbyUI : MonoBehaviour
    {
        public static LobbyUI Instance { get; private set; }

        [Header("Player List")]
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;

        [Header("Controls")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject chatMessagePrefab;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Player Customization")]
        [SerializeField] private Button[] skinButtons;
        [SerializeField] private Image playerPreview;

        private bool isReady;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            readyButton?.onClick.AddListener(ToggleReady);
            startButton?.onClick.AddListener(ForceStart);

            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            // Only host can force start
            if (startButton != null)
                startButton.gameObject.SetActive(NetworkServer.active);
        }

        public void UpdatePlayerList(SyncDictionary<uint, Network.LobbyManager.LobbyPlayerInfo> players)
        {
            // Clear existing items
            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var kvp in players)
            {
                if (playerListItemPrefab == null || playerListContent == null) continue;

                var item = Instantiate(playerListItemPrefab, playerListContent);
                var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    string readyStatus = kvp.Value.isReady ? " [READY]" : "";
                    nameText.text = $"{kvp.Value.playerName}{readyStatus}";
                    nameText.color = kvp.Value.isReady ? Color.green : Color.white;
                }
            }
        }

        public void UpdateCountdown(float time)
        {
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(time).ToString();
            }
        }

        public void SetCountdownVisible(bool visible)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(visible);
        }

        private void ToggleReady()
        {
            isReady = !isReady;

            if (readyButtonText != null)
                readyButtonText.text = isReady ? "NOT READY" : "READY";

            Network.LobbyManager.Instance?.CmdSetReady();
            AudioManager.AudioManager.Instance?.PlayUISound("button_click");
        }

        private void ForceStart()
        {
            Network.GameNetworkManager.singleton?.StartGame();
        }
    }
}
