using System.Collections;
using UnityEngine;

namespace WitchShmup.Utils
{
    /// <summary>
    /// Efeito visual e sonoro de coleta de item. Auto-destrói ao terminar.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ItemCollectEffect : MonoBehaviour
    {
        [Header("Animação")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 20f;
        [SerializeField] private float riseSpeed = 1.5f;     // sobe enquanto anima

        [Header("Áudio")]
        [SerializeField] private AudioClip collectSound;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.65f;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();

            if (collectSound == null)
                collectSound = SpellGlide.ProceduralAudioGenerator.CreateItemCollectSound();
        }

        private void Start()
        {
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);

            StartCoroutine(PlayAndDestroy());
        }

        private IEnumerator PlayAndDestroy()
        {
            if (frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                yield break;
            }

            float interval = 1f / frameRate;
            int total = frames.Length;

            for (int i = 0; i < total; i++)
            {
                sr.sprite = frames[i];

                // Sobe levemente
                transform.position += Vector3.up * riseSpeed * interval;

                // Scale punch: cresce e depois some
                float t = (float)i / (total - 1);
                float scale = (t < 0.5f)
                    ? Mathf.Lerp(0.5f, 1.2f, t * 2f)
                    : Mathf.Lerp(1.2f, 0.3f, (t - 0.5f) * 2f);
                transform.localScale = Vector3.one * scale;

                // Fade alpha no final
                Color col = sr.color;
                col.a = (i >= total - 2) ? Mathf.Lerp(1f, 0f, (float)(i - (total - 2)) / 2f) : 1f;
                sr.color = col;

                yield return new WaitForSeconds(interval);
            }

            Destroy(gameObject);
        }
    }
}
