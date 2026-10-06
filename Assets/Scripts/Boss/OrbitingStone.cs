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
