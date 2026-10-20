using System.Collections;
using UnityEngine;
using WitchShmup.Combat;

namespace WitchShmup.Boss
{
    [RequireComponent(typeof(Collider2D))]
    public class OrbitingStone : MonoBehaviour, IDamageable
    {
        [Header("Orbit Config")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float orbitRadius = 3f;
        [SerializeField] private float orbitSpeed = 90f; // Graus por segundo
        [SerializeField] private float currentAngle = 0f;

        [Header("Health & Defense (Mini-Vida da Pedra Orbital)")]
        [Tooltip("Vida desta pedra orbital. O jogador precisa destruí-la para abrir caminho até o Boss!")]
        [SerializeField] private float maxHealth = 3.5f;
        [SerializeField] private float currentHealth = 3.5f;

        [Header("Damage")]
        [Tooltip("Dano ao encostar no jogador (1 de dano)")]
        [SerializeField] private float contactDamage = 1f;

        [Header("Feedback Visual")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private GameObject breakEffectPrefab;

        private SpriteRenderer sr;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;

            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            // Adiciona Rigidbody2D cinemático para física de colisões consistente
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }

            // Usa o mesmo sprite da pedra de ataque (ataqueBoss2_4)
            var all = Resources.LoadAll<Sprite>("Sprites/ataqueBoss2");
            if (all != null && all.Length > 0)
            {
                Sprite rockSpr = null;
                foreach (var s in all)
                {
                    if (s.name.Equals("ataqueBoss2_4", System.StringComparison.OrdinalIgnoreCase))
                    {
                        rockSpr = s;
                        break;
                    }
                }
                if (rockSpr == null) rockSpr = all[0];

                sr.sprite = rockSpr;
                sr.color = Color.white;
                sr.sortingOrder = 9;
            }
            originalColor = sr.color;

            transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                boxCol.isTrigger = true;
                boxCol.size = new Vector2(0.9f, 0.9f);
            }
        }

        private void Start()
        {
            if (centerPoint == null && transform.parent != null)
            {
                centerPoint = transform.parent;
            }
        }

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
            if (centerPoint == null) return;

            currentAngle += orbitSpeed * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (centerPoint == null) return;

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            transform.position = centerPoint.position + offset;
        }

        public void TakeDamage(float amount, ElementType element = ElementType.None)
        {
            if (currentHealth <= 0) return;

            // Fogo dá 1.25x mais dano em pedras
            float mult = (element == ElementType.Fire) ? 1.25f : 1.0f;
            currentHealth -= amount * mult;

            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0)
            {
                Break();
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (sr != null)
            {
                sr.color = damageFlashColor;
                yield return new WaitForSeconds(0.06f);
                if (sr != null) sr.color = originalColor;
            }
            flashCoroutine = null;
        }

        private void Break()
        {
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 1. Dano de contato ao encostar no jogador (1 de dano)
            if (other.CompareTag("Player"))
            {
                var damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(contactDamage, ElementType.Enemy);
                }
            }
            // 2. Projéteis do jogador: a pedra toma dano e o tiro é bloqueado/destruído
            else if (other.GetComponent<Projectile>() != null)
            {
                var proj = other.GetComponent<Projectile>();
                if (proj != null && proj.IsPlayerProjectile)
                {
                    TakeDamage(proj.Damage, proj.Element);
                    Destroy(proj.gameObject);
                }
            }
        }
    }
}
