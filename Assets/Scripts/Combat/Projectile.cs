using UnityEngine;

namespace WitchShmup.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField] private float speed = 14f;

        [Header("Combat Stats")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private ElementType element = ElementType.Fire;
        [SerializeField] private bool isPlayerProjectile = true;
        [SerializeField] private int pierceCount = 1;
        [SerializeField] private float lifetime = 3.5f;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;

        private int currentHits = 0;
        private float timer = 0f;

        public void Initialize(Vector2 dir, float spd, float dmg, ElementType elem, bool playerShot)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            element = elem;
            isPlayerProjectile = playerShot;
            currentHits = 0;
            timer = 0f;

            // Rotate projectile towards movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            transform.Translate(direction * (speed * Time.deltaTime), Space.World);

            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // If player projectile, do not hit player
            if (isPlayerProjectile && other.CompareTag("Player"))
                return;

            // If enemy projectile, do not hit enemies
            if (!isPlayerProjectile && (other.CompareTag("Enemy") || other.CompareTag("Boss")))
                return;

            // Check if object is damageable
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, element);
                currentHits++;

                SpawnHitEffect();

                if (currentHits >= pierceCount)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Hit solid obstacles (walls, terrain)
            if (other.CompareTag("Obstacle"))
            {
                SpawnHitEffect();
                Destroy(gameObject);
            }
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
