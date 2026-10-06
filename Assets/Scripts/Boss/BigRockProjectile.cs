using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;

namespace WitchShmup.Boss
{
    [RequireComponent(typeof(Collider2D))]
    public class BigRockProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 9f;
        [SerializeField] private Vector2 direction = Vector2.left;
        [SerializeField] private float rotationSpeed = 120f;

        [Header("Damage")]
        [SerializeField] private float damage = 2f;

        public void Initialize(Vector2 dir, float spd, float dmg = 2f)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;

            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.3f, 0.2f);
            }
        }

        private void Update()
        {
            transform.Translate(direction * (speed * Time.deltaTime), Space.World);
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            if (CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 4f))
            {
                Destroy(gameObject);
            }
            else if (transform.position.x < -20f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, ElementType.Enemy);
                }

                if (ScreenShake.Instance != null)
                {
                    ScreenShake.Instance.Shake(0.4f, 0.35f);
                }
            }
        }
    }
}
