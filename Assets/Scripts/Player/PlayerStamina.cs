using UnityEngine;
using Mirror;
using System;

namespace ZombieSurvival.Player
{
    public class PlayerStamina : NetworkBehaviour
    {
        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 15f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float regenDelay = 1.5f;
        [SerializeField] private float exhaustionThreshold = 0f;
        [SerializeField] private float exhaustionRecovery = 20f;

        [SyncVar(hook = nameof(OnStaminaChanged))]
        private float currentStamina;

        private float lastUseTime;
        private bool isExhausted;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public bool IsExhausted => isExhausted;

        public event Action<float, float> OnStaminaUpdated; // current, max

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentStamina = maxStamina;
        }

        private void Update()
        {
            if (!isServer) return;

            if (Time.time - lastUseTime >= regenDelay)
            {
                if (currentStamina < maxStamina)
                {
                    currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
                }

                if (isExhausted && currentStamina >= exhaustionRecovery)
                {
                    isExhausted = false;
                }
            }
        }

        [Server]
        public void UseStamina(float amount)
        {
            if (isExhausted) return;

            currentStamina = Mathf.Max(0, currentStamina - amount * staminaDrainRate);
            lastUseTime = Time.time;

            if (currentStamina <= exhaustionThreshold)
            {
                isExhausted = true;
                RpcOnExhausted();
            }
        }

        [Server]
        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        }

        [ClientRpc]
        private void RpcOnExhausted()
        {
            AudioManager.AudioManager.Instance?.PlaySFX("exhausted");
        }

        private void OnStaminaChanged(float oldStamina, float newStamina)
        {
            OnStaminaUpdated?.Invoke(newStamina, maxStamina);
        }
    }
}
