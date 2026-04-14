using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.Collections.Generic;

namespace ZombieSurvival.Zombie
{
    public class ZombieSpawner : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject[] zombiePrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxZombiesAlive = 30;
        [SerializeField] private float initialSpawnDelay = 5f;

        [Header("Wave Settings")]
        [SerializeField] private bool useWaveSystem = true;
        [SerializeField] private int baseZombiesPerWave = 5;
        [SerializeField] private float zombiesPerWaveMultiplier = 1.5f;
        [SerializeField] private float timeBetweenWaves = 30f;
        [SerializeField] private float spawnInterval = 0.5f;

        [Header("Difficulty Scaling")]
        [SerializeField] private float healthScalePerWave = 0.1f;
        [SerializeField] private float damageScalePerWave = 0.05f;
        [SerializeField] private float speedScalePerWave = 0.02f;

        [Header("Type Distribution")]
        [SerializeField] private float runnerChancePerWave = 0.05f;
        [SerializeField] private float tankChancePerWave = 0.03f;
        [SerializeField] private float spitterChancePerWave = 0.02f;
        [SerializeField] private float screamerChancePerWave = 0.01f;

        [SyncVar] private int currentWave;
        [SyncVar] private int zombiesAlive;
        [SyncVar] private int zombiesToSpawn;
        [SyncVar] private bool waveInProgress;

        private float spawnTimer;
        private float waveTimer;
        private List<ZombieAI> activeZombies = new List<ZombieAI>();

        public int CurrentWave => currentWave;
        public int ZombiesAlive => zombiesAlive;
        public bool WaveInProgress => waveInProgress;

        public static ZombieSpawner Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentWave = 0;
            Invoke(nameof(StartNextWave), initialSpawnDelay);
        }

        private void Update()
        {
            if (!isServer) return;

            if (waveInProgress)
            {
                HandleSpawning();
            }
            else
            {
                waveTimer += Time.deltaTime;
                if (waveTimer >= timeBetweenWaves && zombiesAlive == 0)
                {
                    StartNextWave();
                }
            }

            CleanupDeadZombies();
        }

        [Server]
        private void StartNextWave()
        {
            currentWave++;
            zombiesToSpawn = CalculateZombiesForWave(currentWave);
            waveInProgress = true;
            spawnTimer = 0;
            waveTimer = 0;

            RpcNotifyWaveStart(currentWave, zombiesToSpawn);
        }

        [ClientRpc]
        private void RpcNotifyWaveStart(int wave, int totalZombies)
        {
            UI.GameHUD.Instance?.ShowWaveNotification(wave, totalZombies);
            AudioManager.AudioManager.Instance?.PlaySFX("wave_start");
        }

        private void HandleSpawning()
        {
            if (zombiesToSpawn <= 0)
            {
                waveInProgress = false;
                RpcNotifyWaveComplete(currentWave);
                return;
            }

            if (zombiesAlive >= maxZombiesAlive) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0;
                SpawnZombie();
                zombiesToSpawn--;
            }
        }

        [ClientRpc]
        private void RpcNotifyWaveComplete(int wave)
        {
            UI.GameHUD.Instance?.ShowWaveCompleteNotification(wave);
            AudioManager.AudioManager.Instance?.PlaySFX("wave_complete");
        }

        [Server]
        private void SpawnZombie()
        {
            if (spawnPoints == null || spawnPoints.Length == 0 || zombiePrefabs == null || zombiePrefabs.Length == 0) return;

            Transform spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null) return;

            ZombieType type = DetermineZombieType();
            int prefabIndex = GetPrefabIndexForType(type);

            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 2f;
            spawnPos.y = spawnPoint.position.y;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject zombieObj = Instantiate(zombiePrefabs[prefabIndex], spawnPos, Quaternion.identity);
            var zombie = zombieObj.GetComponent<ZombieAI>();

            if (zombie != null)
            {
                ApplyWaveScaling(zombie);
                zombie.OnZombieDied += OnZombieDied;
                activeZombies.Add(zombie);
            }

            NetworkServer.Spawn(zombieObj);
            zombiesAlive++;
        }

        private Transform GetRandomSpawnPoint()
        {
            // Try to find a spawn point not visible to players
            List<Transform> validPoints = new List<Transform>();

            foreach (var point in spawnPoints)
            {
                if (point == null) continue;
                bool visible = false;

                foreach (var player in NetworkServer.connections.Values)
                {
                    if (player.identity == null) continue;
                    float distance = Vector3.Distance(point.position, player.identity.transform.position);
                    if (distance < 15f)
                    {
                        visible = true;
                        break;
                    }
                }

                if (!visible) validPoints.Add(point);
            }

            if (validPoints.Count > 0)
                return validPoints[Random.Range(0, validPoints.Count)];

            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        private ZombieType DetermineZombieType()
        {
            float rand = Random.value;
            float cumulative = 0f;

            cumulative += screamerChancePerWave * currentWave;
            if (rand < cumulative) return ZombieType.Screamer;

            cumulative += spitterChancePerWave * currentWave;
            if (rand < cumulative) return ZombieType.Spitter;

            cumulative += tankChancePerWave * currentWave;
            if (rand < cumulative) return ZombieType.Tank;

            cumulative += runnerChancePerWave * currentWave;
            if (rand < cumulative) return ZombieType.Runner;

            return ZombieType.Walker;
        }

        private int GetPrefabIndexForType(ZombieType type)
        {
            int index = (int)type;
            return index < zombiePrefabs.Length ? index : 0;
        }

        private void ApplyWaveScaling(ZombieAI zombie)
        {
            // Wave scaling is applied through the zombie's stats
            // The zombie prefab already has base stats from ZombieType
        }

        private int CalculateZombiesForWave(int wave)
        {
            return Mathf.RoundToInt(baseZombiesPerWave * Mathf.Pow(zombiesPerWaveMultiplier, wave - 1));
        }

        private void OnZombieDied(ZombieAI zombie)
        {
            zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
            zombie.OnZombieDied -= OnZombieDied;
        }

        private void CleanupDeadZombies()
        {
            activeZombies.RemoveAll(z => z == null || z.IsDead);
        }

        [Server]
        public void ForceStartWave()
        {
            if (!waveInProgress)
            {
                StartNextWave();
            }
        }
    }
}
