using System.Collections;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;

namespace WitchShmup.Boss
{
    public class StoneWallObstacle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float advanceSpeed = 2f;

        private void Update()
        {
            transform.Translate(Vector3.left * (advanceSpeed * Time.deltaTime), Space.World);

            if (CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 3f))
            {
                Destroy(gameObject);
            }
            else if (transform.position.x < -18f)
            {
                Destroy(gameObject);
            }
        }
    }

    [RequireComponent(typeof(Collider2D))]
    public class StoneWallSegment : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 6f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float contactDamage = 1f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private GameObject breakEffectPrefab;

        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void TakeDamage(float amount, ElementType element)
        {
            if (currentHealth <= 0) return;

            // Fogo quebra pedra mais rápido!
            float mult = (element == ElementType.Fire) ? 1.5f : 1.0f;
            currentHealth -= amount * mult;

            if (gameObject.activeInHierarchy)
            {
                Flash();
            }

            if (currentHealth <= 0)
            {
                Break();
            }
        }

        private void Flash()
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = damageFlashColor;
                yield return new WaitForSeconds(0.06f);
                spriteRenderer.color = originalColor;
            }
            flashCoroutine = null;
        }

        private void Break()
        {
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destrói este bloco da parede, abrindo passagem segura para o player passar!
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(contactDamage, ElementType.Enemy);
                }
            }
        }
    }
}
