using System.Collections;
using UnityEngine;

namespace WitchShmup.Utils
{
    /// <summary>
    /// Anexado ao prefab de efeito de morte do inimigo.
    /// Reproduz a animação de partículas sprite-based e some automaticamente.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyDeathEffect : MonoBehaviour
    {
        [Header("Animação")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 16f;
        [SerializeField] private float scalePunch = 1.4f;    // escala inicial (overshoots)
        [SerializeField] private float scaleEnd   = 0.6f;    // escala no último frame

        [Header("Áudio")]
        [SerializeField] private AudioClip deathSound;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.75f;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();

            // Gera som procedural se nenhum clip foi atribuído
            if (deathSound == null)
                deathSound = SpellGlide.ProceduralAudioGenerator.CreateEnemyDeathSound();
        }

        private void Start()
        {
            if (deathSound != null)
                AudioSource.PlayClipAtPoint(deathSound, transform.position, volume);

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

                // Escala: começa grande, termina pequena
                float t = (float)i / (total - 1);
                float scale = Mathf.Lerp(scalePunch, scaleEnd, t);
                transform.localScale = Vector3.one * scale;

                // Alpha fade no final
                Color col = sr.color;
                col.a = (i >= total - 2) ? Mathf.Lerp(1f, 0f, (float)(i - (total - 2)) / 2f) : 1f;
                sr.color = col;

                yield return new WaitForSeconds(interval);
            }

            Destroy(gameObject);
        }
    }
}
