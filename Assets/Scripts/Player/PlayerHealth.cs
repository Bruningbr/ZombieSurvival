using UnityEngine;
using Mirror;
using System;

namespace ZombieSurvival.Player
{
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float healthRegenRate = 1f;
        [SerializeField] private float regenDelay = 5f;
        [SerializeField] private float criticalHealthThreshold = 25f;

        [Header("Damage Effects")]
        [SerializeField] private float damageScreenDuration = 0.5f;
        [SerializeField] private float heartbeatInterval = 1f;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;

        [SyncVar] private bool isDead;

        private float lastDamageTime;
        private float heartbeatTimer;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsDead => isDead;
        public bool IsCritical => currentHealth <= criticalHealthThreshold;

        public event Action<float, float> OnHealthUpdated; // current, max
        public event Action OnDeath;
        public event Action OnRevived;
        public event Action<float> OnDamageTaken;
        public event Action OnCriticalHealth;

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            isDead = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnHealthUpdated?.Invoke(currentHealth, maxHealth);
        }

        private void Update()
        {
            if (!isServer) return;

            if (!isDead && currentHealth < maxHealth)
            {
                if (Time.time - lastDamageTime >= regenDelay)
                {
                    currentHealth = Mathf.Min(currentHealth + healthRegenRate * Time.deltaTime, maxHealth);
                }
            }

            if (isLocalPlayer && IsCritical && !isDead)
            {
                heartbeatTimer += Time.deltaTime;
                if (heartbeatTimer >= heartbeatInterval)
                {
                    heartbeatTimer = 0;
                    AudioManager.AudioManager.Instance?.PlaySFX("heartbeat");
                }
            }
        }

        [Server]
        public void TakeDamage(float damage, GameObject attacker = null)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            lastDamageTime = Time.time;

            RpcOnDamageTaken(damage);

            if (currentHealth <= 0)
            {
                Die(attacker);
            }
            else if (IsCritical)
            {
                RpcOnCriticalHealth();
            }
        }

        [Server]
        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        [Server]
        private void Die(GameObject killer)
        {
            isDead = true;
            currentHealth = 0;

            if (killer != null)
            {
                var killerRanking = killer.GetComponent<Ranking.PlayerScore>();
                if (killerRanking != null)
                {
                    killerRanking.AddKill();
                }
            }

            var myScore = GetComponent<Ranking.PlayerScore>();
            if (myScore != null)
            {
                myScore.AddDeath();
            }

            RpcOnDeath();
        }

        [Server]
        public void Revive(float healthPercent = 0.5f)
        {
            isDead = false;
            currentHealth = maxHealth * healthPercent;
            RpcOnRevived();
        }

        [ClientRpc]
        private void RpcOnDamageTaken(float damage)
        {
            OnDamageTaken?.Invoke(damage);
            AudioManager.AudioManager.Instance?.PlaySFX("player_hurt");
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            OnDeath?.Invoke();
            AudioManager.AudioManager.Instance?.PlaySFX("player_death");
        }

        [ClientRpc]
        private void RpcOnRevived()
        {
            OnRevived?.Invoke();
        }

        [ClientRpc]
        private void RpcOnCriticalHealth()
        {
            OnCriticalHealth?.Invoke();
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            OnHealthUpdated?.Invoke(newHealth, maxHealth);
        }
    }
}
