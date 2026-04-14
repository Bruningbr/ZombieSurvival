using UnityEngine;

namespace ZombieSurvival.GameManager
{
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Weather Types")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private ParticleSystem dustParticles;

        [Header("Weather Settings")]
        [SerializeField] private float weatherChangeInterval = 120f;
        [SerializeField] private float transitionDuration = 10f;

        [Header("Rain Settings")]
        [SerializeField] private AudioClip rainAmbientSound;
        [SerializeField] private float rainFogDensity = 0.03f;

        [Header("Storm Settings")]
        [SerializeField] private Light lightningLight;
        [SerializeField] private AudioClip[] thunderSounds;
        [SerializeField] private float lightningMinInterval = 5f;
        [SerializeField] private float lightningMaxInterval = 30f;

        public enum WeatherType
        {
            Clear,
            Rainy,
            Foggy,
            Stormy,
            Dusty
        }

        private WeatherType currentWeather = WeatherType.Clear;
        private float weatherTimer;
        private float lightningTimer;
        private AudioSource weatherAudioSource;

        public WeatherType CurrentWeather => currentWeather;

        private void Start()
        {
            weatherAudioSource = gameObject.AddComponent<AudioSource>();
            weatherAudioSource.loop = true;
            weatherAudioSource.spatialBlend = 0f;

            StopAllParticles();
        }

        private void Update()
        {
            weatherTimer += Time.deltaTime;

            if (weatherTimer >= weatherChangeInterval)
            {
                weatherTimer = 0;
                ChangeWeatherRandom();
            }

            if (currentWeather == WeatherType.Stormy)
            {
                HandleLightning();
            }
        }

        public void SetWeather(WeatherType weather)
        {
            currentWeather = weather;
            StopAllParticles();

            switch (weather)
            {
                case WeatherType.Clear:
                    RenderSettings.fogDensity = 0.001f;
                    weatherAudioSource.Stop();
                    break;

                case WeatherType.Rainy:
                    if (rainParticles != null) rainParticles.Play();
                    RenderSettings.fogDensity = rainFogDensity;
                    if (rainAmbientSound != null)
                    {
                        weatherAudioSource.clip = rainAmbientSound;
                        weatherAudioSource.Play();
                    }
                    break;

                case WeatherType.Foggy:
                    if (fogParticles != null) fogParticles.Play();
                    RenderSettings.fogDensity = 0.05f;
                    break;

                case WeatherType.Stormy:
                    if (rainParticles != null) rainParticles.Play();
                    RenderSettings.fogDensity = 0.04f;
                    if (rainAmbientSound != null)
                    {
                        weatherAudioSource.clip = rainAmbientSound;
                        weatherAudioSource.volume = 0.8f;
                        weatherAudioSource.Play();
                    }
                    break;

                case WeatherType.Dusty:
                    if (dustParticles != null) dustParticles.Play();
                    RenderSettings.fogDensity = 0.02f;
                    break;
            }
        }

        private void ChangeWeatherRandom()
        {
            var values = System.Enum.GetValues(typeof(WeatherType));
            WeatherType newWeather = (WeatherType)values.GetValue(Random.Range(0, values.Length));
            SetWeather(newWeather);
        }

        private void HandleLightning()
        {
            lightningTimer -= Time.deltaTime;
            if (lightningTimer <= 0)
            {
                lightningTimer = Random.Range(lightningMinInterval, lightningMaxInterval);
                StartCoroutine(LightningFlash());
            }
        }

        private System.Collections.IEnumerator LightningFlash()
        {
            if (lightningLight != null)
            {
                lightningLight.enabled = true;
                lightningLight.intensity = Random.Range(3f, 8f);
                yield return new WaitForSeconds(0.1f);
                lightningLight.enabled = false;
                yield return new WaitForSeconds(0.05f);
                lightningLight.enabled = true;
                lightningLight.intensity = Random.Range(1f, 4f);
                yield return new WaitForSeconds(0.08f);
                lightningLight.enabled = false;
            }

            // Thunder after delay
            float thunderDelay = Random.Range(0.5f, 3f);
            yield return new WaitForSeconds(thunderDelay);

            if (thunderSounds != null && thunderSounds.Length > 0)
            {
                var thunder = thunderSounds[Random.Range(0, thunderSounds.Length)];
                AudioManager.AudioManager.Instance?.PlaySFX("thunder");
            }
        }

        private void StopAllParticles()
        {
            if (rainParticles != null) rainParticles.Stop();
            if (snowParticles != null) snowParticles.Stop();
            if (fogParticles != null) fogParticles.Stop();
            if (dustParticles != null) dustParticles.Stop();
            if (lightningLight != null) lightningLight.enabled = false;
        }
    }
}
