using UnityEngine;
using WitchShmup.Combat;

namespace WitchShmup.Boss
{
    public class OrbitingStone : MonoBehaviour
    {
        [Header("Orbit Config")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float orbitRadius = 3f;
        [SerializeField] private float orbitSpeed = 90f; // Graus por segundo
        [SerializeField] private float currentAngle = 0f;

        [Header("Damage")]
        [SerializeField] private float contactDamage = 1f;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

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

            transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
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
            // Bloqueia projéteis do player
            else if (other.GetComponent<Projectile>() != null)
            {
                var proj = other.GetComponent<Projectile>();
                // Projéteis do jogador são destruídos ao bater na pedra orbital
                Destroy(proj.gameObject);
            }
        }
    }
}
