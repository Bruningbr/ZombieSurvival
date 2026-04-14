using UnityEngine;
using Mirror;

namespace ZombieSurvival.Network
{
    public class NetworkChat : NetworkBehaviour
    {
        public static NetworkChat Instance { get; private set; }

        [Header("Chat Settings")]
        [SerializeField] private int maxMessages = 50;
        [SerializeField] private float messageDuration = 10f;

        public event System.Action<string, string> OnMessageReceived; // playerName, message

        private void Awake()
        {
            Instance = this;
        }

        [Command(requiresAuthority = false)]
        public void CmdSendMessage(string message, NetworkConnectionToClient sender = null)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.Length > 200) message = message.Substring(0, 200);

            string playerName = "Unknown";
            if (sender?.identity != null)
            {
                var score = sender.identity.GetComponent<Ranking.PlayerScore>();
                if (score != null)
                {
                    playerName = score.DisplayName;
                }
            }

            RpcReceiveMessage(playerName, message);
        }

        [ClientRpc]
        private void RpcReceiveMessage(string playerName, string message)
        {
            OnMessageReceived?.Invoke(playerName, message);
            Debug.Log($"[Chat] {playerName}: {message}");
        }

        [Command(requiresAuthority = false)]
        public void CmdSendSystemMessage(string message)
        {
            RpcReceiveMessage("[SYSTEM]", message);
        }
    }
}
