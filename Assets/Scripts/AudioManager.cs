using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpellGlide
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        public const string MusicVolumePrefsKey = "WitchShmup_MusicVolume";
        public const string SfxVolumePrefsKey = "WitchShmup_SfxVolume";

        [Header("Áudio Clips (Opcional - se vazio, gera som procedural)")]
        [Tooltip("Música tema do Menu")]
        [SerializeField] private AudioClip menuMusic;
        [Tooltip("Som ao clicar em botões")]
        [SerializeField] private AudioClip buttonClickClip;
        [Tooltip("Som do efeito de máquina de escrever (letra digitada)")]
        [SerializeField] private AudioClip typewriterClip;
        [Tooltip("Som ao pular texto/quadrinho")]
        [SerializeField] private AudioClip skipClip;

        [Header("Canais de Áudio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource typewriterSource;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float typewriterVolume = 0.4f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            musicVolume = PlayerPrefs.GetFloat(MusicVolumePrefsKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefsKey, sfxVolume);
            typewriterVolume = sfxVolume * 0.6f;

            EnsureAudioSources();
            EnsureClipsGenerated();
            UpdateVolumes();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "Menu") PlayMenuMusic();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Menu") PlayMenuMusic();
            else StopMenuMusic(0f);
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (typewriterSource == null)
            {
                typewriterSource = gameObject.AddComponent<AudioSource>();
                typewriterSource.loop = false;
                typewriterSource.playOnAwake = false;
            }
        }

        private void EnsureClipsGenerated()
        {
            if (menuMusic == null)
                menuMusic = ProceduralAudioGenerator.CreateMenuMusic();

            if (buttonClickClip == null)
                buttonClickClip = ProceduralAudioGenerator.CreateClickSound();

            if (typewriterClip == null)
                typewriterClip = ProceduralAudioGenerator.CreateTypewriterSound();

            if (skipClip == null)
                skipClip = ProceduralAudioGenerator.CreateSkipSound();
        }

        public void UpdateVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume;
            if (typewriterSource != null) typewriterSource.volume = typewriterVolume;
        }

        public void PlayMenuMusic()
        {
            if (musicSource != null && menuMusic != null)
            {
                musicSource.clip = menuMusic;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        public void StopMenuMusic(float fadeOutTime = 0.5f)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                if (fadeOutTime > 0f)
                {
                    StartCoroutine(FadeOutMusicRoutine(fadeOutTime));
                }
                else
                {
                    musicSource.Stop();
                }
            }
        }

        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume;
        }

        public void PlayButtonClick()
        {
            if (sfxSource != null && buttonClickClip != null)
            {
                sfxSource.PlayOneShot(buttonClickClip, sfxVolume);
            }
        }

        public void PlayTypewriterSound()
        {
            if (typewriterSource != null && typewriterClip != null)
            {
                // Variação leve de tom para soar natural
                typewriterSource.pitch = Random.Range(0.95f, 1.05f);
                typewriterSource.PlayOneShot(typewriterClip, typewriterVolume);
            }
        }

        public void PlaySkipSound()
        {
            if (sfxSource != null && skipClip != null)
            {
                sfxSource.PlayOneShot(skipClip, sfxVolume);
            }
        }


        public void SetMusicVolume(float val)
        {
            musicVolume = Mathf.Clamp01(val);
            if (musicSource != null) musicSource.volume = musicVolume;
            PlayerPrefs.SetFloat(MusicVolumePrefsKey, musicVolume);
            PlayerPrefs.Save();
            if (WitchShmup.Audio.BGMManager.Instance != null)
                WitchShmup.Audio.BGMManager.Instance.MusicVolume = musicVolume;
        }

        public void SetSFXVolume(float val)
        {
            sfxVolume = Mathf.Clamp01(val);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
            typewriterVolume = sfxVolume * 0.6f;
            if (typewriterSource != null) typewriterSource.volume = typewriterVolume;
            PlayerPrefs.SetFloat(SfxVolumePrefsKey, sfxVolume);
            PlayerPrefs.Save();
        }

        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
    }
}
