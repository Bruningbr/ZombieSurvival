using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace ZombieSurvival.Network
{
    public class LobbyManager : NetworkBehaviour
    {
        [Header("Lobby Settings")]
        [SerializeField] private float countdownTime = 10f;
        [SerializeField] private int minPlayersToStart = 1;

        [SyncVar(hook = nameof(OnCountdownChanged))]
        private float currentCountdown;

        [SyncVar(hook = nameof(OnLobbyStateChanged))]
        private bool isCountingDown;

        private readonly SyncDictionary<uint, LobbyPlayerInfo> lobbyPlayers =
            new SyncDictionary<uint, LobbyPlayerInfo>();

        public struct LobbyPlayerInfo
        {
            public string playerName;
            public bool isReady;
            public int skinIndex;
        }

        public SyncDictionary<uint, LobbyPlayerInfo> LobbyPlayers => lobbyPlayers;
        public float CurrentCountdown => currentCountdown;
        public bool IsCountingDown => isCountingDown;

        public static LobbyManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public void RegisterPlayer(NetworkConnectionToClient conn, string playerName)
        {
            lobbyPlayers[conn.identity.netId] = new LobbyPlayerInfo
            {
                playerName = playerName,
                isReady = false,
                skinIndex = 0
            };

            RpcUpdatePlayerList();
        }

        [Server]
        public void UnregisterPlayer(uint netId)
        {
            if (lobbyPlayers.ContainsKey(netId))
            {
                lobbyPlayers.Remove(netId);
                RpcUpdatePlayerList();
                CheckReadyState();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetReady(NetworkConnectionToClient sender = null)
        {
            if (sender?.identity == null) return;
            uint netId = sender.identity.netId;

            if (lobbyPlayers.TryGetValue(netId, out var info))
            {
                info.isReady = !info.isReady;
                lobbyPlayers[netId] = info;
                RpcUpdatePlayerList();
                CheckReadyState();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetSkin(int skinIndex, NetworkConnectionToClient sender = null)
        {
            if (sender?.identity == null) return;
            uint netId = sender.identity.netId;

            if (lobbyPlayers.TryGetValue(netId, out var info))
            {
                info.skinIndex = skinIndex;
                lobbyPlayers[netId] = info;
                RpcUpdatePlayerList();
            }
        }

        [Server]
        private void CheckReadyState()
        {
            int readyCount = 0;
            foreach (var player in lobbyPlayers.Values)
            {
                if (player.isReady) readyCount++;
            }

            if (readyCount >= minPlayersToStart && readyCount == lobbyPlayers.Count)
            {
                if (!isCountingDown)
                {
                    StartCountdown();
                }
            }
            else
            {
                CancelCountdown();
            }
        }

        [Server]
        private void StartCountdown()
        {
            isCountingDown = true;
            currentCountdown = countdownTime;
        }

        [Server]
        private void CancelCountdown()
        {
            isCountingDown = false;
            currentCountdown = countdownTime;
        }

        private void Update()
        {
            if (!isServer || !isCountingDown) return;

            currentCountdown -= Time.deltaTime;
            if (currentCountdown <= 0)
            {
                isCountingDown = false;
                GameNetworkManager.singleton?.StartGame();
            }
        }

        [ClientRpc]
        private void RpcUpdatePlayerList()
        {
            UI.LobbyUI.Instance?.UpdatePlayerList(lobbyPlayers);
        }

        private void OnCountdownChanged(float oldValue, float newValue)
        {
            UI.LobbyUI.Instance?.UpdateCountdown(newValue);
        }

        private void OnLobbyStateChanged(bool oldValue, bool newValue)
        {
            UI.LobbyUI.Instance?.SetCountdownVisible(newValue);
        }
    }
}
