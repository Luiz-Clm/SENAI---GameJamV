using System;
using System.Collections;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Core;
using WitchShmup.Drops;

namespace WitchShmup.Enemies
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
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

        [Header("Item Drops")]
        [SerializeField] protected GameObject fireHeartDropPrefab;
        [SerializeField] [Range(0f, 1f)] protected float fireHeartDropChance = 0.40f;
        [SerializeField] protected GameObject healthPotionDropPrefab;
        [SerializeField] [Range(0f, 1f)] protected float healthPotionDropChance = 0.25f;
        [SerializeField] protected GameObject skillPointDropPrefab;
        [SerializeField] [Range(0f, 1f)] protected float skillPointDropChance = 0.35f;

        [Header("Status Effects")]
        protected float speedMultiplier = 1f;
        protected Coroutine slowCoroutine;
        protected static readonly Color FrostSlowColor = new Color(0.45f, 0.85f, 1f, 1f);

        protected Color originalColor = Color.white;
        protected Coroutine flashCoroutine;
        protected Rigidbody2D rb;

        public float SpeedMultiplier => speedMultiplier;
        public bool IsSlowed => slowCoroutine != null;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        [Header("Escalonamento de Dificuldade")]
        [Tooltip("Multiplicador aplicado à vida máxima desta instância")]
        [SerializeField] protected float difficultyHealthMultiplier = 1f;

        public virtual void ApplyDifficultyMultipliers(float healthMult, float speedMult, float contactDmgMult, float fireRateMult, float projSpeedMult)
        {
            if (healthMult > 0f)
            {
                difficultyHealthMultiplier = healthMult;
                maxHealth = Mathf.Round(maxHealth * healthMult * 10f) / 10f;
                currentHealth = maxHealth;
            }
            if (contactDmgMult > 0f)
            {
                contactDamage = Mathf.Round(contactDamage * contactDmgMult * 10f) / 10f;
            }
            if (speedMult > 0f)
            {
                speedMultiplier *= speedMult;
            }
        }

        protected virtual void Awake()
        {
            currentHealth = maxHealth;

            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

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

        protected bool isDead = false;

        public virtual void TakeDamage(float amount, ElementType element)
        {
            if (isDead || currentHealth <= 0) return;

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

        public virtual void ApplySlow(float slowPercent, float duration)
        {
            if (isDead || currentHealth <= 0) return;

            if (slowCoroutine != null)
            {
                StopCoroutine(slowCoroutine);
            }
            slowCoroutine = StartCoroutine(SlowRoutine(slowPercent, duration));
        }

        protected virtual IEnumerator SlowRoutine(float slowPercent, float duration)
        {
            speedMultiplier = Mathf.Clamp(1f - slowPercent, 0.2f, 1f);

            if (spriteRenderer != null && flashCoroutine == null)
            {
                spriteRenderer.color = FrostSlowColor;
            }

            yield return new WaitForSeconds(duration);

            speedMultiplier = 1f;
            if (spriteRenderer != null && flashCoroutine == null)
            {
                spriteRenderer.color = originalColor;
            }
            slowCoroutine = null;
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
                spriteRenderer.color = (slowCoroutine != null) ? FrostSlowColor : originalColor;
            }
            flashCoroutine = null;
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            if (slowCoroutine != null) StopCoroutine(slowCoroutine);

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }

            DropItems();

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        protected virtual void DropItems()
        {
            // Monta a lista de drops possíveis com seus pesos
            // Apenas adiciona um drop se o prefab estiver atribuído
            float totalWeight = 0f;
            float fhWeight = (fireHeartDropPrefab != null) ? fireHeartDropChance : 0f;
            float hpWeight = (healthPotionDropPrefab != null) ? healthPotionDropChance : 0f;
            float spWeight = (skillPointDropPrefab != null) ? skillPointDropChance : 0f;
            totalWeight = fhWeight + hpWeight + spWeight;

            // Sem nenhum drop possível
            if (totalWeight <= 0f) return;

            // Roll único: chance de dropar ALGUMA coisa = soma dos pesos (cap 1)
            float dropRoll = UnityEngine.Random.value;
            float dropChance = Mathf.Min(1f, totalWeight);
            if (dropRoll > dropChance) return; // Não dropa nada desta vez

            // Normaliza e escolhe qual item dropar
            float pick = UnityEngine.Random.value * totalWeight;
            GameObject prefabToDrop = null;

            if (pick < fhWeight)
            {
                prefabToDrop = fireHeartDropPrefab;
            }
            else if (pick < fhWeight + hpWeight)
            {
                prefabToDrop = healthPotionDropPrefab;
            }
            else
            {
                prefabToDrop = skillPointDropPrefab;
            }

            if (prefabToDrop != null)
            {
                // Pequeno offset aleatório para não sobrepor outros drops
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
                Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);
                Instantiate(prefabToDrop, spawnPos, Quaternion.identity);
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<WitchShmup.Player.PlayerController>() != null)
            {
                var damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(contactDamage, ElementType.Enemy);
                }
            }
        }
    }
}
