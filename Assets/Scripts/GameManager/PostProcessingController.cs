using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieSurvival.GameManager
{
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume Reference")]
        [SerializeField] private Volume postProcessVolume;

        [Header("Damage Effect")]
        [SerializeField] private float damageVignetteIntensity = 0.6f;
        [SerializeField] private float damageChromAberration = 1f;
        [SerializeField] private float effectDecaySpeed = 2f;

        [Header("Low Health Effect")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private float lowHealthVignette = 0.4f;
        [SerializeField] private Color lowHealthVignetteColor = Color.red;
        [SerializeField] private float heartbeatPulseSpeed = 2f;

        [Header("Night Vision")]
        [SerializeField] private float nightVisionBrightness = 2f;
        [SerializeField] private Color nightVisionTint = new Color(0.2f, 1f, 0.2f);

        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
        private FilmGrain filmGrain;

        private float currentDamageEffect;
        private bool isLowHealth;
        private bool nightVisionEnabled;

        private void Start()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindFirstObjectByType<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out vignette);
                postProcessVolume.profile.TryGet(out chromaticAberration);
                postProcessVolume.profile.TryGet(out colorAdjustments);
                postProcessVolume.profile.TryGet(out bloom);
                postProcessVolume.profile.TryGet(out filmGrain);
            }
        }

        private void Update()
        {
            UpdateDamageEffect();
            UpdateLowHealthEffect();
        }

        public void OnDamage(float damageAmount)
        {
            currentDamageEffect = Mathf.Clamp01(damageAmount / 50f);
        }

        public void SetLowHealth(bool lowHealth)
        {
            isLowHealth = lowHealth;
        }

        public void ToggleNightVision()
        {
            nightVisionEnabled = !nightVisionEnabled;

            if (colorAdjustments != null)
            {
                if (nightVisionEnabled)
                {
                    colorAdjustments.postExposure.value = nightVisionBrightness;
                    colorAdjustments.colorFilter.value = nightVisionTint;
                }
                else
                {
                    colorAdjustments.postExposure.value = 0;
                    colorAdjustments.colorFilter.value = Color.white;
                }
            }

            if (filmGrain != null)
            {
                filmGrain.intensity.value = nightVisionEnabled ? 0.8f : 0f;
            }
        }

        private void UpdateDamageEffect()
        {
            if (currentDamageEffect > 0)
            {
                currentDamageEffect -= effectDecaySpeed * Time.deltaTime;
                currentDamageEffect = Mathf.Max(0, currentDamageEffect);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = currentDamageEffect * damageChromAberration;
            }
        }

        private void UpdateLowHealthEffect()
        {
            if (vignette == null) return;

            if (isLowHealth)
            {
                float pulse = Mathf.Sin(Time.time * heartbeatPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
                vignette.intensity.value = Mathf.Lerp(lowHealthVignette * 0.5f, lowHealthVignette, pulse);
                vignette.color.value = lowHealthVignetteColor;
            }
            else if (currentDamageEffect > 0)
            {
                vignette.intensity.value = currentDamageEffect * damageVignetteIntensity;
                vignette.color.value = Color.red;
            }
            else
            {
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0.2f, Time.deltaTime * 2f);
                vignette.color.value = Color.black;
            }
        }
    }
}
