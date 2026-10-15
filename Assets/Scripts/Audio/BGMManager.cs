using System.Collections;
using UnityEngine;
using WitchShmup.Boss;

namespace WitchShmup.Audio
{
    public class BGMManager : MonoBehaviour
    {
        public static BGMManager Instance { get; private set; }

        [Header("Trilhas Sonoras de Fundo")]
        [Tooltip("Arraste aqui o arquivo de áudio da música de gameplay normal")]
        [SerializeField] private AudioClip normalMusic;

        [Tooltip("Arraste aqui o arquivo de áudio da música da luta contra o Boss")]
        [SerializeField] private AudioClip bossMusic;

        [Header("Configurações")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.5f;

        [Tooltip("Duração do crossfade suave ao trocar entre a música normal e a do boss")]
        [SerializeField] private float crossfadeDuration = 1.2f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private Coroutine fadeCoroutine;

        public AudioClip NormalMusic
        {
            get => normalMusic;
            set => normalMusic = value;
        }

        public AudioClip BossMusic
        {
            get => bossMusic;
            set => bossMusic = value;
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (activeSource != null) activeSource.volume = musicVolume;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSources();
        }

        private void SetupAudioSources()
        {
            // Cria dois AudioSources dedicados para permitir crossfade suave entre músicas
            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 2)
            {
                sourceA = sources[0];
                sourceB = sources[1];
            }
            else
            {
                sourceA = (sources.Length > 0) ? sources[0] : gameObject.AddComponent<AudioSource>();
                sourceB = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSource(sourceA);
            ConfigureSource(sourceB);
        }

        private void ConfigureSource(AudioSource src)
        {
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // Som 2D imersivo
            src.volume = 0f;
        }

        private void OnEnable()
        {
            GolemBoss.OnAnyBossFightStarted += OnBossFightStarted;
            GolemBoss.OnAnyBossFightDefeated += OnBossFightDefeated;
        }

        private void OnDisable()
        {
            GolemBoss.OnAnyBossFightStarted -= OnBossFightStarted;
            GolemBoss.OnAnyBossFightDefeated -= OnBossFightDefeated;
        }

        private void Start()
        {
            // Toca a música normal no início da fase
            PlayNormalMusic(crossfade: false);
        }

        private void OnBossFightStarted()
        {
            PlayBossMusic(crossfade: true);
        }

        private void OnBossFightDefeated()
        {
            StopMusic(fadeOut: true);
        }

        /// <summary>
        /// Toca a música de gameplay normal.
        /// </summary>
        public void PlayNormalMusic(bool crossfade = true)
        {
            PlayTrack(normalMusic, crossfade);
        }

        /// <summary>
        /// Toca a música especial de batalha contra o Boss.
        /// </summary>
        public void PlayBossMusic(bool crossfade = true)
        {
            PlayTrack(bossMusic, crossfade);
        }

        private void PlayTrack(AudioClip newClip, bool crossfade)
        {
            if (newClip == null)
            {
                // Se nenhum clipe foi assinalado ainda, apenas silencia suavemente
                if (activeSource != null && activeSource.isPlaying)
                {
                    StopMusic(fadeOut: crossfade);
                }
                return;
            }

            if (activeSource != null && activeSource.clip == newClip && activeSource.isPlaying)
            {
                return; // Já está tocando essa música
            }

            // Alterna entre sourceA e sourceB para transição
            AudioSource nextSource = (activeSource == sourceA) ? sourceB : sourceA;
            AudioSource currentSource = activeSource;

            nextSource.clip = newClip;
            nextSource.Play();

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (crossfade && currentSource != null && currentSource.isPlaying)
            {
                fadeCoroutine = StartCoroutine(CrossfadeRoutine(currentSource, nextSource, crossfadeDuration));
            }
            else
            {
                if (currentSource != null)
                {
                    currentSource.Stop();
                    currentSource.volume = 0f;
                }
                nextSource.volume = musicVolume;
            }

            activeSource = nextSource;
        }

        /// <summary>
        /// Para a música com fade out suave.
        /// </summary>
        public void StopMusic(bool fadeOut = true)
        {
            if (activeSource == null || !activeSource.isPlaying) return;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (fadeOut)
            {
                fadeCoroutine = StartCoroutine(FadeOutRoutine(activeSource, 1.5f));
            }
            else
            {
                activeSource.Stop();
                activeSource.volume = 0f;
            }
        }

        private IEnumerator CrossfadeRoutine(AudioSource fromSource, AudioSource toSource, float duration)
        {
            float elapsed = 0f;
            float startVol = fromSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // usa unscaled para continuar mesmo em pause/cutscene
                float t = Mathf.Clamp01(elapsed / duration);

                fromSource.volume = Mathf.Lerp(startVol, 0f, t);
                toSource.volume = Mathf.Lerp(0f, musicVolume, t);

                yield return null;
            }

            fromSource.Stop();
            fromSource.volume = 0f;
            toSource.volume = musicVolume;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float elapsed = 0f;
            float startVol = source.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }
    }
}
