using UnityEngine;

namespace ZombieSurvival.AudioManager
{
    public class AmbientSoundController : MonoBehaviour
    {
        [Header("Ambient Settings")]
        [SerializeField] private string dayAmbient = "ambient_day";
        [SerializeField] private string nightAmbient = "ambient_night";
        [SerializeField] private string combatMusic = "music_combat";
        [SerializeField] private string calmMusic = "music_calm";
        [SerializeField] private string menuMusic = "music_menu";

        [Header("Dynamic Audio")]
        [SerializeField] private float combatMusicRange = 20f;
        [SerializeField] private float combatCooldown = 10f;
        [SerializeField] private LayerMask zombieMask;

        private bool isInCombat;
        private float lastCombatTime;
        private GameManager.DayNightCycle dayNightCycle;

        private void Start()
        {
            dayNightCycle = FindFirstObjectByType<GameManager.DayNightCycle>();
        }

        private void Update()
        {
            CheckCombatState();
            UpdateAmbientMusic();
        }

        private void CheckCombatState()
        {
            Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, combatMusicRange, zombieMask);
            
            if (nearbyZombies.Length > 0)
            {
                if (!isInCombat)
                {
                    isInCombat = true;
                    AudioManager.Instance?.PlayMusic(combatMusic);
                }
                lastCombatTime = Time.time;
            }
            else if (isInCombat && Time.time - lastCombatTime > combatCooldown)
            {
                isInCombat = false;
                AudioManager.Instance?.PlayMusic(calmMusic);
            }
        }

        private void UpdateAmbientMusic()
        {
            if (dayNightCycle == null) return;

            string ambient = dayNightCycle.IsNight ? nightAmbient : dayAmbient;
            // Ambient sound updates handled by AudioManager
        }

        public void PlayMenuMusic()
        {
            AudioManager.Instance?.PlayMusic(menuMusic);
        }

        public void StopAllMusic()
        {
            AudioManager.Instance?.StopMusic();
            AudioManager.Instance?.StopAmbient();
        }
    }
}
