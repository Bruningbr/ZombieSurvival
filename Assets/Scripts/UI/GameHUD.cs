using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace ZombieSurvival.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("Health & Stamina")]
        [SerializeField] private Image healthBar;
        [SerializeField] private Image healthBarBackground;
        [SerializeField] private Image staminaBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image damageOverlay;
        [SerializeField] private Image criticalOverlay;

        [Header("Weapon Info")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image crosshair;
        [SerializeField] private Image hitMarker;
        [SerializeField] private Image headshotMarker;

        [Header("Wave Info")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI zombieCountText;
        [SerializeField] private GameObject waveNotification;
        [SerializeField] private TextMeshProUGUI waveNotificationText;
        [SerializeField] private Animator waveNotificationAnimator;

        [Header("Score")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI killsText;

        [Header("Interaction")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Camera minimapCamera;

        [Header("Scope")]
        [SerializeField] private GameObject scopeOverlay;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color warnColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float criticalThreshold = 0.25f;
        [SerializeField] private float warnThreshold = 0.5f;

        private float damageOverlayTimer;
        private float hitMarkerTimer;
        private const float DamageOverlayDuration = 0.5f;
        private const float HitMarkerDuration = 0.3f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (hitMarker != null) hitMarker.enabled = false;
            if (headshotMarker != null) headshotMarker.enabled = false;
            if (damageOverlay != null) damageOverlay.enabled = false;
            if (criticalOverlay != null) criticalOverlay.enabled = false;
            if (scopeOverlay != null) scopeOverlay.SetActive(false);
            if (waveNotification != null) waveNotification.SetActive(false);
            if (interactionPrompt != null) interactionPrompt.SetActive(false);

            // Find local player and subscribe to events
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                SubscribeToPlayer(localPlayer.gameObject);
            }
        }

        private void SubscribeToPlayer(GameObject player)
        {
            var health = player.GetComponent<Player.PlayerHealth>();
            if (health != null)
            {
                health.OnHealthUpdated += UpdateHealth;
                health.OnDamageTaken += OnDamageTaken;
                health.OnCriticalHealth += OnCriticalHealth;
                health.OnDeath += OnPlayerDeath;
            }

            var stamina = player.GetComponent<Player.PlayerStamina>();
            if (stamina != null)
            {
                stamina.OnStaminaUpdated += UpdateStamina;
            }

            var score = player.GetComponent<Ranking.PlayerScore>();
            if (score != null)
            {
                score.OnScoreUpdated += UpdateScore;
                score.OnKillsUpdated += UpdateKills;
            }

            var inventory = player.GetComponent<Player.PlayerInventory>();
            if (inventory != null)
            {
                inventory.OnWeaponSwitched += UpdateWeaponInfo;
            }
        }

        private void Update()
        {
            UpdateDamageOverlay();
            UpdateHitMarker();
            UpdateWaveInfo();
            UpdateInteractionPrompt();
        }

        public void UpdateHealth(float current, float max)
        {
            float percent = current / max;

            if (healthBar != null)
            {
                healthBar.fillAmount = percent;
                healthBar.color = percent <= criticalThreshold ? criticalColor :
                                  percent <= warnThreshold ? warnColor : healthyColor;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        public void UpdateStamina(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.fillAmount = current / max;
            }
        }

        public void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{current} / {reserve}";
            }
        }

        public void UpdateWeaponInfo(Weapons.WeaponBase weapon)
        {
            if (weapon == null) return;

            if (weaponNameText != null)
                weaponNameText.text = weapon.WeaponName;

            if (weaponIcon != null && weapon.Icon != null)
                weaponIcon.sprite = weapon.Icon;

            UpdateAmmo(weapon.CurrentAmmo, weapon.ReserveAmmo);
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        public void UpdateKills(int kills)
        {
            if (killsText != null)
                killsText.text = $"Kills: {kills}";
        }

        private void UpdateWaveInfo()
        {
            var spawner = Zombie.ZombieSpawner.Instance;
            if (spawner == null) return;

            if (waveText != null)
                waveText.text = $"Wave {spawner.CurrentWave}";

            if (zombieCountText != null)
                zombieCountText.text = $"Zombies: {spawner.ZombiesAlive}";
        }

        public void ShowWaveNotification(int wave, int totalZombies)
        {
            if (waveNotification == null) return;

            waveNotification.SetActive(true);
            if (waveNotificationText != null)
                waveNotificationText.text = $"WAVE {wave}\n{totalZombies} Zombies Incoming!";

            if (waveNotificationAnimator != null)
                waveNotificationAnimator.SetTrigger("Show");

            Invoke(nameof(HideWaveNotification), 3f);
        }

        public void ShowWaveCompleteNotification(int wave)
        {
            if (waveNotification == null) return;

            waveNotification.SetActive(true);
            if (waveNotificationText != null)
                waveNotificationText.text = $"WAVE {wave} COMPLETE!";

            if (waveNotificationAnimator != null)
                waveNotificationAnimator.SetTrigger("Show");

            Invoke(nameof(HideWaveNotification), 3f);
        }

        private void HideWaveNotification()
        {
            if (waveNotification != null)
                waveNotification.SetActive(false);
        }

        public void ShowHitMarker(bool isHeadshot)
        {
            hitMarkerTimer = HitMarkerDuration;

            if (isHeadshot)
            {
                if (headshotMarker != null) headshotMarker.enabled = true;
                AudioManager.AudioManager.Instance?.PlayUISound("headshot_marker");
            }
            else
            {
                if (hitMarker != null) hitMarker.enabled = true;
                AudioManager.AudioManager.Instance?.PlayUISound("hit_marker");
            }
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerTimer > 0)
            {
                hitMarkerTimer -= Time.deltaTime;
                if (hitMarkerTimer <= 0)
                {
                    if (hitMarker != null) hitMarker.enabled = false;
                    if (headshotMarker != null) headshotMarker.enabled = false;
                }
            }
        }

        private void OnDamageTaken(float damage)
        {
            damageOverlayTimer = DamageOverlayDuration;
            if (damageOverlay != null)
            {
                damageOverlay.enabled = true;
                var color = damageOverlay.color;
                color.a = Mathf.Clamp01(damage / 50f);
                damageOverlay.color = color;
            }
        }

        private void UpdateDamageOverlay()
        {
            if (damageOverlayTimer > 0)
            {
                damageOverlayTimer -= Time.deltaTime;
                if (damageOverlay != null)
                {
                    var color = damageOverlay.color;
                    color.a = Mathf.Lerp(0, color.a, damageOverlayTimer / DamageOverlayDuration);
                    damageOverlay.color = color;
                }
                if (damageOverlayTimer <= 0 && damageOverlay != null)
                {
                    damageOverlay.enabled = false;
                }
            }
        }

        private void OnCriticalHealth()
        {
            if (criticalOverlay != null)
                criticalOverlay.enabled = true;
        }

        private void OnPlayerDeath()
        {
            // Show death screen
            DeathScreen.Instance?.Show();
        }

        private void UpdateInteractionPrompt()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer == null) return;

            var interaction = localPlayer.GetComponent<Player.PlayerInteraction>();
            if (interaction == null) return;

            if (interaction.HasInteractable)
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
                if (interactionText != null)
                    interactionText.text = $"[E] {interaction.CurrentInteractable.InteractionPrompt}";
            }
            else
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }

        public void ShowScopeOverlay(bool show)
        {
            if (scopeOverlay != null)
                scopeOverlay.SetActive(show);
            if (crosshair != null)
                crosshair.enabled = !show;
        }
    }
}
