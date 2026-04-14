using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

namespace ZombieSurvival.Network
{
    public class GameNetworkManager : NetworkManager
    {
        [Header("Game Settings")]
        [SerializeField] private int minPlayersToStart = 1;
        [SerializeField] private int maxPlayersAllowed = 16;
        [SerializeField] private string gameScene = "GameScene";
        [SerializeField] private string lobbyScene = "LobbyScene";

        [Header("Spawn Settings")]
        [SerializeField] private Transform[] playerSpawnPoints;

        public static new GameNetworkManager singleton => (GameNetworkManager)NetworkManager.singleton;

        public event Action<NetworkConnectionToClient> OnPlayerJoined;
        public event Action<NetworkConnectionToClient> OnPlayerLeft;
        public event Action OnAllPlayersReady;

        private int playersReady;
        private int spawnIndex;

        public int ConnectedPlayers => numPlayers;
        public int PlayersReady => playersReady;
        public bool IsGameReady => playersReady >= minPlayersToStart;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[Server] Game server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("[Client] Connected to server.");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Find spawn point
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
            {
                var point = playerSpawnPoints[spawnIndex % playerSpawnPoints.Length];
                spawnPos = point.position;
                spawnRot = point.rotation;
                spawnIndex++;
            }

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
            NetworkServer.AddPlayerForConnection(conn, playerObj);

            // Set up player name
            var playerScore = playerObj.GetComponent<Ranking.PlayerScore>();
            if (playerScore != null)
            {
                playerScore.SetDisplayName($"Player_{conn.connectionId}");
            }

            OnPlayerJoined?.Invoke(conn);
            Debug.Log($"[Server] Player {conn.connectionId} joined. Total: {numPlayers}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            OnPlayerLeft?.Invoke(conn);

            // Submit final score before disconnect
            if (conn.identity != null)
            {
                var playerScore = conn.identity.GetComponent<Ranking.PlayerScore>();
                if (playerScore != null)
                {
                    Ranking.LeaderboardManager.Instance?.SubmitFinalScore(playerScore);
                }
            }

            base.OnServerDisconnect(conn);
            Debug.Log($"[Server] Player disconnected. Total: {numPlayers}");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            SceneManager.LoadScene(lobbyScene);
        }

        [Server]
        public void PlayerReady(NetworkConnectionToClient conn)
        {
            playersReady++;
            if (IsGameReady)
            {
                OnAllPlayersReady?.Invoke();
            }
        }

        [Server]
        public void StartGame()
        {
            if (IsGameReady)
            {
                ServerChangeScene(gameScene);
            }
        }

        [Server]
        public void RespawnPlayer(NetworkConnectionToClient conn)
        {
            if (conn.identity == null) return;

            Vector3 spawnPos = Vector3.zero;
            if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
            {
                var point = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
                spawnPos = point.position;
            }

            conn.identity.transform.position = spawnPos;

            var health = conn.identity.GetComponent<Player.PlayerHealth>();
            health?.Revive();
        }
    }
}
