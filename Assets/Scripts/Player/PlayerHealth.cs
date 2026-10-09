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
        private bool hasSecondWind = false;
        private bool secondWindUsed = false;
        private float damageReductionPercent = 0f;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsInvincible => isInvincible;
        public bool HasSecondWind => hasSecondWind;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnDeath;
        public event Action OnRevived;

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

        public void SetInvulnerable(bool invulnerable)
        {
            isInvincible = invulnerable;
        }

        public void SetDamageReduction(float reductionPercent)
        {
            damageReductionPercent = Mathf.Clamp01(reductionPercent);
        }

        public void EnableSecondWind()
        {
            hasSecondWind = true;
            secondWindUsed = false;
        }

        public void TakeDamage(float amount, ElementType element = ElementType.None)
        {
            if (isInvincible || currentHealth <= 0) return;

            // Pele de Pedra: redução de dano percentual
            float calculatedDamage = amount * (1f - damageReductionPercent);
            int intDamage = Mathf.Max(1, Mathf.RoundToInt(calculatedDamage));

            // Se chance de desviar ou mitigar 100% de dano pequeno com alta redução
            if (damageReductionPercent > 0.2f && UnityEngine.Random.value < damageReductionPercent)
            {
                intDamage = Mathf.Max(0, intDamage - 1);
            }

            if (intDamage <= 0) return;

            currentHealth -= intDamage;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (currentHealth <= 0)
            {
                // Segundo Fôlego: Revive uma vez se a vida zerar
                if (hasSecondWind && !secondWindUsed)
                {
                    secondWindUsed = true;
                    Revive();
                    return;
                }

                Die();
            }
            else
            {
                StartCoroutine(InvincibilityRoutine());
            }
        }

        private void Revive()
        {
            currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * 0.35f));
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnRevived?.Invoke();
            StartCoroutine(InvincibilityRoutine(3.0f));
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

        private IEnumerator InvincibilityRoutine(float duration = -1f)
        {
            isInvincible = true;

            float targetDuration = (duration > 0f) ? duration : invincibilityDuration;
            float elapsed = 0f;
            Color originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;
            Color flashColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.25f);

            while (elapsed < targetDuration)
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
