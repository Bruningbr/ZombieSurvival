using UnityEngine;
using Mirror;

namespace ZombieSurvival.GameManager
{
    public class PickupItem : NetworkBehaviour, Player.IInteractable
    {
        public enum PickupType
        {
            HealthPack,
            AmmoPack,
            WeaponPickup,
            Armor,
            SpeedBoost
        }

        [Header("Pickup Settings")]
        [SerializeField] private PickupType pickupType = PickupType.HealthPack;
        [SerializeField] private float value = 50f;
        [SerializeField] private string weaponId;
        [SerializeField] private float effectDuration = 10f;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private float bobAmplitude = 0.2f;
        [SerializeField] private float bobFrequency = 1f;

        [Header("Visual")]
        [SerializeField] private GameObject visualObject;
        [SerializeField] private ParticleSystem glowEffect;
        [SerializeField] private Light pointLight;

        [Header("Audio")]
        [SerializeField] private string pickupSoundName = "pickup_item";

        private Vector3 startPosition;
        private bool isPickedUp;

        public string InteractionPrompt
        {
            get
            {
                return pickupType switch
                {
                    PickupType.HealthPack => $"Health Pack (+{value})",
                    PickupType.AmmoPack => $"Ammo Pack (+{value})",
                    PickupType.WeaponPickup => $"Pickup Weapon",
                    PickupType.Armor => $"Armor (+{value})",
                    PickupType.SpeedBoost => $"Speed Boost ({effectDuration}s)",
                    _ => "Pickup"
                };
            }
        }

        public bool CanInteract => !isPickedUp;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (isPickedUp) return;

            // Rotate and bob animation
            if (visualObject != null)
            {
                visualObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = startPosition + Vector3.up * bob;
        }

        public void Interact(GameObject interactor)
        {
            if (isPickedUp) return;
            CmdPickup(interactor);
        }

        [Command(requiresAuthority = false)]
        private void CmdPickup(GameObject interactor)
        {
            if (isPickedUp) return;

            bool success = false;

            switch (pickupType)
            {
                case PickupType.HealthPack:
                    var health = interactor.GetComponent<Player.PlayerHealth>();
                    if (health != null && health.HealthPercent < 1f)
                    {
                        health.Heal(value);
                        success = true;
                    }
                    break;

                case PickupType.AmmoPack:
                    var inventory = interactor.GetComponent<Player.PlayerInventory>();
                    if (inventory != null && inventory.ActiveWeapon != null)
                    {
                        inventory.ActiveWeapon.AddAmmo((int)value);
                        success = true;
                    }
                    break;

                case PickupType.WeaponPickup:
                    var playerInventory = interactor.GetComponent<Player.PlayerInventory>();
                    if (playerInventory != null)
                    {
                        playerInventory.CmdPickupWeapon(weaponId);
                        success = true;
                    }
                    break;

                case PickupType.Armor:
                    // Armor system can be added
                    success = true;
                    break;

                case PickupType.SpeedBoost:
                    // Temporary speed boost
                    success = true;
                    break;
            }

            if (success)
            {
                isPickedUp = true;
                RpcOnPickedUp();
                Invoke(nameof(ServerDestroy), 0.5f);
            }
        }

        [ClientRpc]
        private void RpcOnPickedUp()
        {
            AudioManager.AudioManager.Instance?.PlaySFX(pickupSoundName);

            if (glowEffect != null) glowEffect.Stop();
            if (pointLight != null) pointLight.enabled = false;
            if (visualObject != null) visualObject.SetActive(false);
        }

        [Server]
        private void ServerDestroy()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
