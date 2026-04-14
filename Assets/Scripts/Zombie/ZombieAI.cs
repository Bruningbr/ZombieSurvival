using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;

namespace ZombieSurvival.Zombie
{
    public enum ZombieType
    {
        Walker,      // Slow, basic zombie
        Runner,      // Fast but weak
        Tank,        // Slow but lots of HP
        Spitter,     // Ranged attack
        Screamer     // Alerts other zombies
    }

    public enum ZombieState
    {
        Idle,
        Wandering,
        Chasing,
        Attacking,
        Stunned,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class ZombieAI : NetworkBehaviour
    {
        [Header("Zombie Type")]
        [SerializeField] private ZombieType zombieType = ZombieType.Walker;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float detectionRange = 20f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float wanderSpeed = 1f;
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float fieldOfView = 120f;

        [Header("Detection")]
        [SerializeField] private float hearingRange = 15f;
        [SerializeField] private float noiseDetectionMultiplier = 1f;
        [SerializeField] private LayerMask playerMask;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Loot")]
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private GameObject[] lootDrops;
        [SerializeField] private float lootDropChance = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] idleSounds;
        [SerializeField] private AudioClip[] attackSounds;
        [SerializeField] private AudioClip[] hurtSounds;
        [SerializeField] private AudioClip[] deathSounds;
        [SerializeField] private AudioClip[] detectionSounds;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;

        [SyncVar(hook = nameof(OnStateChanged))]
        private ZombieState currentState = ZombieState.Idle;

        private NavMeshAgent agent;
        private Animator animator;
        private AudioSource audioSource;
        private Transform currentTarget;
        private float lastAttackTime;
        private float nextIdleSoundTime;
        private float wanderTimer;
        private Vector3 spawnPosition;
        private bool isAlerted;

        // Animation parameters
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");

        public ZombieType Type => zombieType;
        public ZombieState State => currentState;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsDead => currentState == ZombieState.Dead;
        public int ScoreValue => scoreValue;

        public event Action<ZombieAI> OnZombieDied;

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            spawnPosition = transform.position;
            ApplyZombieTypeStats();
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 30f;
        }

        private void Update()
        {
            if (!isServer || IsDead) return;

            UpdateState();
            UpdateAnimations();
            HandleIdleSounds();
        }

        private void ApplyZombieTypeStats()
        {
            switch (zombieType)
            {
                case ZombieType.Walker:
                    maxHealth = 100f; damage = 15f; chaseSpeed = 3f;
                    attackCooldown = 1.5f; scoreValue = 10;
                    break;
                case ZombieType.Runner:
                    maxHealth = 60f; damage = 10f; chaseSpeed = 7f;
                    attackCooldown = 0.8f; scoreValue = 20;
                    break;
                case ZombieType.Tank:
                    maxHealth = 300f; damage = 30f; chaseSpeed = 2f;
                    attackCooldown = 2.5f; scoreValue = 50;
                    break;
                case ZombieType.Spitter:
                    maxHealth = 80f; damage = 20f; chaseSpeed = 3.5f;
                    attackRange = 10f; attackCooldown = 3f; scoreValue = 30;
                    break;
                case ZombieType.Screamer:
                    maxHealth = 50f; damage = 5f; chaseSpeed = 4f;
                    detectionRange = 30f; scoreValue = 25;
                    break;
            }
            currentHealth = maxHealth;
        }

        private void UpdateState()
        {
            currentTarget = FindClosestPlayer();

            switch (currentState)
            {
                case ZombieState.Idle:
                    HandleIdleState();
                    break;
                case ZombieState.Wandering:
                    HandleWanderingState();
                    break;
                case ZombieState.Chasing:
                    HandleChasingState();
                    break;
                case ZombieState.Attacking:
                    HandleAttackingState();
                    break;
                case ZombieState.Stunned:
                    break;
            }
        }

        private void HandleIdleState()
        {
            agent.speed = 0;
            wanderTimer += Time.deltaTime;

            if (currentTarget != null && CanSeeTarget(currentTarget))
            {
                currentState = ZombieState.Chasing;
                PlayDetectionSound();
                return;
            }

            if (wanderTimer >= UnityEngine.Random.Range(3f, 8f))
            {
                wanderTimer = 0;
                currentState = ZombieState.Wandering;
            }
        }

        private void HandleWanderingState()
        {
            agent.speed = wanderSpeed;

            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector3 randomPoint = spawnPosition + UnityEngine.Random.insideUnitSphere * wanderRadius;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    currentState = ZombieState.Idle;
                }
            }

            if (currentTarget != null && CanSeeTarget(currentTarget))
            {
                currentState = ZombieState.Chasing;
                PlayDetectionSound();
            }
        }

        private void HandleChasingState()
        {
            if (currentTarget == null)
            {
                currentState = ZombieState.Wandering;
                return;
            }

            agent.speed = chaseSpeed;
            agent.SetDestination(currentTarget.position);

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= attackRange)
            {
                currentState = ZombieState.Attacking;
            }
            else if (distanceToTarget > detectionRange * 1.5f)
            {
                currentState = ZombieState.Wandering;
                isAlerted = false;
            }

            // Screamer type alerts nearby zombies
            if (zombieType == ZombieType.Screamer && !isAlerted)
            {
                AlertNearbyZombies();
                isAlerted = true;
            }
        }

        private void HandleAttackingState()
        {
            if (currentTarget == null)
            {
                currentState = ZombieState.Wandering;
                return;
            }

            agent.speed = 0;
            transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget > attackRange * 1.2f)
            {
                currentState = ZombieState.Chasing;
                return;
            }

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }

        private void Attack()
        {
            RpcPlayAttackAnimation();

            if (currentTarget != null)
            {
                var playerHealth = currentTarget.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, gameObject);
                }
            }
        }

        [ClientRpc]
        private void RpcPlayAttackAnimation()
        {
            animator.SetTrigger(AttackHash);
            PlayAttackSound();
        }

        private Transform FindClosestPlayer()
        {
            Collider[] players = Physics.OverlapSphere(transform.position, detectionRange, playerMask);
            Transform closest = null;
            float closestDistance = float.MaxValue;

            foreach (var player in players)
            {
                var health = player.GetComponent<Player.PlayerHealth>();
                if (health != null && health.IsDead) continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player.transform;
                }
            }

            return closest;
        }

        private bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;

            float distance = Vector3.Distance(transform.position, target.position);

            // Hearing detection
            if (distance <= hearingRange)
            {
                return true;
            }

            // Visual detection with FOV
            if (distance <= detectionRange)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= fieldOfView / 2f)
                {
                    if (!Physics.Raycast(transform.position + Vector3.up, directionToTarget, distance, obstacleMask))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AlertNearbyZombies()
        {
            Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, hearingRange * 2f);
            foreach (var col in nearbyZombies)
            {
                var zombie = col.GetComponent<ZombieAI>();
                if (zombie != null && zombie != this && !zombie.IsDead)
                {
                    if (zombie.currentState == ZombieState.Idle || zombie.currentState == ZombieState.Wandering)
                    {
                        zombie.AlertToTarget(currentTarget);
                    }
                }
            }

            AudioManager.AudioManager.Instance?.PlaySFXAtPosition("zombie_scream", transform.position);
        }

        [Server]
        public void AlertToTarget(Transform target)
        {
            currentTarget = target;
            currentState = ZombieState.Chasing;
        }

        [Server]
        public void TakeDamage(float damage, GameObject attacker = null)
        {
            if (IsDead) return;

            currentHealth -= damage;
            RpcPlayHitEffect();

            if (currentHealth <= 0)
            {
                Die(attacker);
            }
            else
            {
                // Getting hit alerts the zombie
                if (attacker != null)
                {
                    currentTarget = attacker.transform;
                    currentState = ZombieState.Chasing;
                }
            }
        }

        [Server]
        private void Die(GameObject killer)
        {
            currentState = ZombieState.Dead;
            agent.isStopped = true;
            agent.enabled = false;

            if (killer != null)
            {
                var playerScore = killer.GetComponent<Ranking.PlayerScore>();
                if (playerScore != null)
                {
                    playerScore.AddScore(scoreValue);
                    playerScore.AddKill();
                }
            }

            // Drop loot
            if (lootDrops != null && lootDrops.Length > 0 && UnityEngine.Random.value <= lootDropChance)
            {
                int randomLoot = UnityEngine.Random.Range(0, lootDrops.Length);
                if (lootDrops[randomLoot] != null)
                {
                    var loot = Instantiate(lootDrops[randomLoot], transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    NetworkServer.Spawn(loot);
                }
            }

            RpcPlayDeathEffect();
            OnZombieDied?.Invoke(this);

            // Destroy after delay
            Invoke(nameof(ServerDestroy), 5f);
        }

        [Server]
        private void ServerDestroy()
        {
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcPlayHitEffect()
        {
            animator.SetTrigger(HitHash);
            PlayHurtSound();
        }

        [ClientRpc]
        private void RpcPlayDeathEffect()
        {
            animator.SetTrigger(DeathHash);
            PlayDeathSound();

            var colliders = GetComponents<Collider>();
            foreach (var col in colliders) col.enabled = false;
        }

        private void UpdateAnimations()
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsAlertHash, currentState == ZombieState.Chasing || currentState == ZombieState.Attacking);
        }

        private void HandleIdleSounds()
        {
            if (currentState == ZombieState.Dead) return;

            if (Time.time >= nextIdleSoundTime)
            {
                PlayIdleSound();
                nextIdleSoundTime = Time.time + UnityEngine.Random.Range(5f, 15f);
            }
        }

        private void PlayIdleSound()
        {
            if (idleSounds != null && idleSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(idleSounds[UnityEngine.Random.Range(0, idleSounds.Length)]);
            }
        }

        private void PlayAttackSound()
        {
            if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)]);
            }
        }

        private void PlayHurtSound()
        {
            if (hurtSounds != null && hurtSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)]);
            }
        }

        private void PlayDeathSound()
        {
            if (deathSounds != null && deathSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(deathSounds[UnityEngine.Random.Range(0, deathSounds.Length)]);
            }
        }

        private void PlayDetectionSound()
        {
            if (detectionSounds != null && detectionSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(detectionSounds[UnityEngine.Random.Range(0, detectionSounds.Length)]);
            }
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            // Can be used by UI to show health bars
        }

        private void OnStateChanged(ZombieState oldState, ZombieState newState)
        {
            // State change effects
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }
    }
}
