using UnityEngine;
using Mirror;

namespace ZombieSurvival.GameManager
{
    public class DayNightCycle : NetworkBehaviour
    {
        [Header("Sun Settings")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float dayDuration = 300f; // seconds per full cycle
        [SerializeField] private float startTime = 0.3f; // Start at morning

        [Header("Light Colors")]
        [SerializeField] private Gradient sunColor;
        [SerializeField] private AnimationCurve sunIntensity;
        [SerializeField] private AnimationCurve ambientIntensity;

        [Header("Fog")]
        [SerializeField] private bool useFog = true;
        [SerializeField] private Gradient fogColor;
        [SerializeField] private AnimationCurve fogDensity;
        [SerializeField] private float maxFogDensity = 0.05f;

        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private AnimationCurve skyboxExposure;

        [Header("Zombie Multiplier")]
        [SerializeField] private float nightZombieMultiplier = 2f;
        [SerializeField] private float nightDamageMultiplier = 1.5f;

        [SyncVar(hook = nameof(OnTimeChanged))]
        private float currentTimeOfDay;

        public float CurrentTime => currentTimeOfDay;
        public bool IsNight => currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;
        public bool IsDusk => currentTimeOfDay > 0.65f && currentTimeOfDay <= 0.75f;
        public bool IsDawn => currentTimeOfDay > 0.2f && currentTimeOfDay <= 0.3f;
        public float NightZombieMultiplier => IsNight ? nightZombieMultiplier : 1f;
        public float NightDamageMultiplier => IsNight ? nightDamageMultiplier : 1f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentTimeOfDay = startTime;
        }

        private void Update()
        {
            if (isServer)
            {
                currentTimeOfDay += Time.deltaTime / dayDuration;
                if (currentTimeOfDay >= 1f)
                    currentTimeOfDay -= 1f;
            }

            UpdateLighting();
        }

        private void UpdateLighting()
        {
            if (directionalLight != null)
            {
                // Rotate sun
                float sunAngle = currentTimeOfDay * 360f - 90f;
                directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

                // Sun color and intensity
                if (sunColor != null)
                    directionalLight.color = sunColor.Evaluate(currentTimeOfDay);

                if (sunIntensity != null)
                    directionalLight.intensity = sunIntensity.Evaluate(currentTimeOfDay);
            }

            // Ambient light
            if (ambientIntensity != null)
            {
                RenderSettings.ambientIntensity = ambientIntensity.Evaluate(currentTimeOfDay);
            }

            // Fog
            if (useFog)
            {
                RenderSettings.fog = true;
                if (fogColor != null)
                    RenderSettings.fogColor = fogColor.Evaluate(currentTimeOfDay);
                if (fogDensity != null)
                    RenderSettings.fogDensity = fogDensity.Evaluate(currentTimeOfDay) * maxFogDensity;
            }

            // Skybox
            if (skyboxMaterial != null && skyboxExposure != null)
            {
                skyboxMaterial.SetFloat("_Exposure", skyboxExposure.Evaluate(currentTimeOfDay));
            }
        }

        private void OnTimeChanged(float oldTime, float newTime)
        {
            // Notify UI of day/night transitions
            if (WasNight(oldTime) && !IsNightTime(newTime))
            {
                UI.GameHUD.Instance?.ShowWaveNotification(0, 0); // Dawn notification
                AudioManager.AudioManager.Instance?.PlaySFX("dawn_transition");
            }
            else if (!WasNight(oldTime) && IsNightTime(newTime))
            {
                AudioManager.AudioManager.Instance?.PlaySFX("dusk_transition");
            }
        }

        private bool WasNight(float time) => time > 0.75f || time < 0.25f;
        private bool IsNightTime(float time) => time > 0.75f || time < 0.25f;
    }
}
