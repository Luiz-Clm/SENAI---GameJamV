using System;
using System.Collections;
using UnityEngine;
using WitchShmup.Combat;

namespace WitchShmup.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 4;
        [SerializeField] private int currentHealth = 4;

        [Header("Invincibility Frames")]
        [SerializeField] private float invincibilityDuration = 1.5f;
        [SerializeField] private float flashInterval = 0.1f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Death Settings")]
        [SerializeField] private GameObject deathEffectPrefab;

        private bool isInvincible = false;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsInvincible => isInvincible;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount, ElementType element = ElementType.None)
        {
            if (isInvincible || currentHealth <= 0) return;

            int intDamage = Mathf.Max(1, Mathf.RoundToInt(amount));
            currentHealth -= intDamage;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvincibilityRoutine());
            }
        }

        public void Heal(int amount)
        {
            if (currentHealth <= 0) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;

            float elapsed = 0f;
            Color originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;
            Color flashColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.25f);

            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = (spriteRenderer.color == originalColor) ? flashColor : originalColor;
                }
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            isInvincible = false;
        }

        private void Die()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Contact damage with enemy or hazard
            if (other.CompareTag("Enemy") || other.CompareTag("Boss") || other.CompareTag("Hazard"))
            {
                TakeDamage(1, ElementType.Enemy);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Boss") || collision.gameObject.CompareTag("Hazard"))
            {
                TakeDamage(1, ElementType.Enemy);
            }
        }
    }
}
