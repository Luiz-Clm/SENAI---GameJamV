using System;
using System.Collections;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;

namespace WitchShmup.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Base Stats")]
        [SerializeField] protected float maxHealth = 3f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected int scoreValue = 100;
        [SerializeField] protected float contactDamage = 1f;

        [Header("Elemental Affinities")]
        [SerializeField] protected ElementType weakElement = ElementType.None;
        [SerializeField] protected float weaknessDamageMultiplier = 2f;

        [Header("Visual Feedback")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] protected GameObject deathEffectPrefab;

        protected Color originalColor = Color.white;
        protected Coroutine flashCoroutine;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        protected virtual void Update()
        {
            CheckOffscreenDespawn();
        }

        protected virtual void CheckOffscreenDespawn()
        {
            if (CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 3f))
            {
                Destroy(gameObject);
            }
            else if (transform.position.x < -16f)
            {
                Destroy(gameObject);
            }
        }

        public virtual void TakeDamage(float amount, ElementType element)
        {
            if (currentHealth <= 0) return;

            float finalDamage = amount;
            if (weakElement != ElementType.None && element == weakElement)
            {
                finalDamage *= weaknessDamageMultiplier;
            }

            currentHealth -= finalDamage;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (gameObject.activeInHierarchy)
            {
                FlashOnDamage();
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void FlashOnDamage()
        {
            if (spriteRenderer == null) return;

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        protected virtual IEnumerator FlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = damageFlashColor;
                yield return new WaitForSeconds(0.08f);
                spriteRenderer.color = originalColor;
            }
            flashCoroutine = null;
        }

        protected virtual void Die()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
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
