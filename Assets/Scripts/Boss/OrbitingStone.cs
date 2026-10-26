using System.Collections;
using UnityEngine;
using WitchShmup.Combat;
using WitchShmup.Player;

namespace WitchShmup.Boss
{
    /// <summary>
    /// Pedra orbital ao redor do Boss Golem.
    ///
    /// Comportamento:
    ///   - Orbita ao redor do boss (centerPoint = transform.parent por padrao).
    ///   - Ao encostar no jogador: causa 1 de dano.
    ///   - Ao receber tiro do jogador: perde vida (mini-vida) e pisca vermelho.
    ///   - Quando a vida chega a 0: explode e desaparece.
    ///   - Tiros que não encostam nas pedras podem atravessar os vãos e atingir o boss.
    ///
    /// Como ajustar no Inspector (via prefab Boss_OrbitingStone):
    ///   - maxHealth:    vida de cada pedra (padrao 3.5 = 3 ou 4 tiros de gelo)
    ///   - contactDamage: dano ao encostar no player (padrao 1)
    ///   - orbitRadius:  distancia do boss (ajustado pelo GolemBoss)
    ///   - orbitSpeed:   velocidade em graus/segundo (90 = uma volta em 4s)
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
    public class OrbitingStone : MonoBehaviour
    {
        [Header("Configuracao de Orbita")]
        [Tooltip("Ponto central da orbita (deixe vazio = usa o pai)")]
        [SerializeField] private Transform centerPoint;
        [Tooltip("Distancia do centro (raio da orbita)")]
        [SerializeField] private float orbitRadius = 3f;
        [Tooltip("Velocidade em graus por segundo")]
        [SerializeField] private float orbitSpeed = 90f;
        [SerializeField] private float currentAngle = 0f;

        [Header("Vida da Pedra (Mini-Vida)")]
        [Tooltip("Vida desta pedra orbital. Precisa destruir para chegar ao boss!")]
        [SerializeField] private float maxHealth = 3.5f;
        [SerializeField] private float currentHealth = 3.5f;

        [Header("Dano ao Jogador")]
        [Tooltip("Dano causado ao encostar no jogador (padrao: 1)")]
        [SerializeField] private float contactDamage = 1f;

        [Header("Feedback Visual")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private GameObject breakEffectPrefab;

        private SpriteRenderer sr;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private bool isDead = false;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;

            sr = GetComponent<SpriteRenderer>();

            // Garante Rigidbody2D cinematico para fisicas de colisao
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }

            // Garante que o collider esta como Trigger
            var col = GetComponent<CircleCollider2D>();
            if (col != null) col.isTrigger = true;

            if (sr != null) originalColor = sr.color;
        }

        private void Start()
        {
            // Centro = pai por padrao (o GolemBoss)
            if (centerPoint == null && transform.parent != null)
            {
                centerPoint = transform.parent;
            }
        }

        /// <summary>Inicializa orbita via codigo (chamado pelo GolemBoss).</summary>
        public void Initialize(Transform center, float radius, float speed, float startAngle)
        {
            centerPoint = center;
            orbitRadius = radius;
            orbitSpeed = speed;
            currentAngle = startAngle;
            UpdatePosition();
        }

        public void SetRadius(float newRadius)
        {
            orbitRadius = newRadius;
        }

        private void Update()
        {
            if (isDead || centerPoint == null) return;

            currentAngle += orbitSpeed * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (centerPoint == null) return;
            float rad = currentAngle * Mathf.Deg2Rad;
            transform.position = centerPoint.position +
                new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
        }

        public void TakeDamage(float amount, ElementType element = ElementType.None)
        {
            if (isDead) return;

            // Fogo faz 1.25x mais dano em pedras
            float mult = (element == ElementType.Fire) ? 1.25f : 1.0f;
            currentHealth -= amount * mult;

            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0f)
            {
                Break();
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (sr != null) sr.color = damageFlashColor;
            yield return new WaitForSeconds(0.07f);
            if (sr != null && !isDead) sr.color = originalColor;
            flashCoroutine = null;
        }

        private void Break()
        {
            isDead = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (breakEffectPrefab != null)
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject, 0.08f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            // 1. Dano ao jogador ao encostar
            var playerHp = other.GetComponent<PlayerHealth>()
                ?? other.GetComponentInParent<PlayerHealth>();
            if (playerHp != null)
            {
                playerHp.TakeDamage(contactDamage, ElementType.Enemy);
                return;
            }

            // 2. Projeteis do jogador: absorve o tiro e perde vida
            var proj = other.GetComponent<Projectile>();
            if (proj != null && proj.IsPlayerProjectile)
            {
                proj.HitOrbitingStone(this);
            }
        }

        /// <summary>Resolve a colisão de pedra antes do dano ao boss no mesmo quadro.</summary>
        public bool TryConsumeProjectile(Projectile projectile)
        {
            if (isDead || projectile == null || !projectile.IsPlayerProjectile) return false;

            Collider2D stoneCollider = GetComponent<Collider2D>();
            Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
            if (stoneCollider == null || projectileCollider == null ||
                !stoneCollider.enabled || !projectileCollider.enabled ||
                !stoneCollider.Distance(projectileCollider).isOverlapped)
            {
                return false;
            }

            return projectile.HitOrbitingStone(this);
        }
    }
}
