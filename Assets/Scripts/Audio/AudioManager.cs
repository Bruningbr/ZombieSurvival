using UnityEngine;
using System.Collections.Generic;

namespace ZombieSurvival.AudioManager
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitchMin = 0.9f;
        [Range(0.5f, 1.5f)] public float pitchMax = 1.1f;
        public bool loop;
        public float spatialBlend; // 0 = 2D, 1 = 3D
    }

    [System.Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        public bool isAmbient;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private int sfxPoolSize = 20;

        [Header("Sound Effects")]
        [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();

        [Header("Music")]
        [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();

        [Header("Volume Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float ambientVolume = 0.7f;

        [Header("Fade Settings")]
        [SerializeField] private float musicFadeSpeed = 1f;

        private Dictionary<string, SoundEffect> sfxLookup;
        private Dictionary<string, MusicTrack> musicLookup;
        private List<AudioSource> sfxPool;
        private int sfxPoolIndex;
        private MusicTrack currentMusic;
        private bool isFading;
        private float fadeTarget;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Build lookups
            sfxLookup = new Dictionary<string, SoundEffect>();
            foreach (var sfx in soundEffects)
            {
                sfxLookup[sfx.name] = sfx;
            }

            musicLookup = new Dictionary<string, MusicTrack>();
            foreach (var track in musicTracks)
            {
                musicLookup[track.name] = track;
            }

            // Create SFX pool
            sfxPool = new List<AudioSource>();
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            // Setup main sources
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }

            LoadVolumeSettings();
        }

        private void Update()
        {
            if (isFading && musicSource != null)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, fadeTarget, musicFadeSpeed * Time.deltaTime);
                if (Mathf.Approximately(musicSource.volume, fadeTarget))
                {
                    isFading = false;
                    if (fadeTarget == 0)
                    {
                        musicSource.Stop();
                    }
                }
            }
        }

        // === SFX ===

        public void PlaySFX(string sfxName)
        {
            if (!sfxLookup.TryGetValue(sfxName, out var sfx)) return;
            if (sfx.clip == null) return;

            var source = GetNextSFXSource();
            source.clip = sfx.clip;
            source.volume = sfx.volume * sfxVolume * masterVolume;
            source.pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            source.loop = sfx.loop;
            source.spatialBlend = 0f;
            source.Play();
        }

        public void PlaySFXAtPosition(string sfxName, Vector3 position)
        {
            if (!sfxLookup.TryGetValue(sfxName, out var sfx)) return;
            if (sfx.clip == null) return;

            var source = GetNextSFXSource();
            source.transform.position = position;
            source.clip = sfx.clip;
            source.volume = sfx.volume * sfxVolume * masterVolume;
            source.pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            source.loop = sfx.loop;
            source.spatialBlend = 1f;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
        }

        public void PlaySFXOneShot(string sfxName, AudioSource source)
        {
            if (!sfxLookup.TryGetValue(sfxName, out var sfx)) return;
            if (sfx.clip == null || source == null) return;

            source.pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
            source.PlayOneShot(sfx.clip, sfx.volume * sfxVolume * masterVolume);
        }

        public void StopSFX(string sfxName)
        {
            foreach (var source in sfxPool)
            {
                if (source.isPlaying && sfxLookup.TryGetValue(sfxName, out var sfx) && source.clip == sfx.clip)
                {
                    source.Stop();
                }
            }
        }

        private AudioSource GetNextSFXSource()
        {
            var source = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;
            return source;
        }

        // === Music ===

        public void PlayMusic(string trackName, bool fade = true)
        {
            if (!musicLookup.TryGetValue(trackName, out var track)) return;
            if (track.clip == null) return;

            if (track.isAmbient)
            {
                ambientSource.clip = track.clip;
                ambientSource.volume = track.volume * ambientVolume * masterVolume;
                ambientSource.loop = true;
                ambientSource.Play();
                return;
            }

            currentMusic = track;
            musicSource.clip = track.clip;
            musicSource.loop = true;

            if (fade)
            {
                musicSource.volume = 0;
                fadeTarget = track.volume * musicVolume * masterVolume;
                isFading = true;
            }
            else
            {
                musicSource.volume = track.volume * musicVolume * masterVolume;
            }

            musicSource.Play();
        }

        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                fadeTarget = 0;
                isFading = true;
            }
            else
            {
                musicSource.Stop();
            }
        }

        public void StopAmbient()
        {
            ambientSource.Stop();
        }

        // === UI Sounds ===

        public void PlayUISound(string sfxName)
        {
            if (!sfxLookup.TryGetValue(sfxName, out var sfx)) return;
            if (sfx.clip == null) return;

            uiSource.PlayOneShot(sfx.clip, sfx.volume * sfxVolume * masterVolume);
        }

        // === Volume Control ===

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveVolumeSettings();
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetAmbientVolume() => ambientVolume;

        private void UpdateAllVolumes()
        {
            if (musicSource != null && currentMusic != null && !isFading)
            {
                musicSource.volume = currentMusic.volume * musicVolume * masterVolume;
            }

            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.7f);
        }
    }
}
