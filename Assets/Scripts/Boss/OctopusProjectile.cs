using UnityEngine;
using UnityEngine.Rendering.Universal;
using WitchShmup.Player;

namespace WitchShmup.Boss
{
    public class OctopusProjectile : MonoBehaviour
    {
        public enum AttackKind { Ink, InkBomb, Book }

        [SerializeField] private AttackKind kind;
        [SerializeField] private float speed = 6f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float blindnessDuration = 4f;
        [SerializeField] private float bombGravity = 1.2f;
        [SerializeField] private float lifetime = 8f;

        private Vector2 direction;
        private float age;

        public void Initialize(Vector2 travelDirection, float travelSpeed, float hitDamage,
            AttackKind attackKind, float blindDuration, Sprite sprite = null)
        {
            direction = travelDirection.normalized;
            speed = travelSpeed;
            damage = hitDamage;
            kind = attackKind;
            blindnessDuration = blindDuration;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (sprite != null) renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.sortingOrder = 15;
            }
            var hitCollider = GetComponent<Collider2D>();
            if (hitCollider != null) hitCollider.isTrigger = true;
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
            if (hitCollider is CircleCollider2D circle)
                circle.radius = kind == AttackKind.InkBomb ? 0.58f : 0.46f;

            foreach (var light in GetComponentsInChildren<Light2D>(true))
            {
                if (kind == AttackKind.Book)
                {
                    light.enabled = false;
                    continue;
                }
                light.enabled = true;
                light.lightType = Light2D.LightType.Point;
                light.color = kind == AttackKind.InkBomb
                    ? new Color(1f, 0.25f, 0.92f, 1f)
                    : new Color(0.95f, 0.28f, 1f, 1f);
                light.intensity = kind == AttackKind.InkBomb ? 1.8f : 1.3f;
                light.pointLightInnerRadius = 0.25f;
                light.pointLightOuterRadius = kind == AttackKind.InkBomb ? 1.8f : 1.25f;
                light.targetSortingLayers = new[] { SortingLayer.NameToID("Default") };
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            if (kind != AttackKind.Book && renderer != null)
                renderer.flipX = true;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (kind == AttackKind.InkBomb)
                direction.y -= bombGravity * Time.deltaTime;
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
            if (age >= lifetime) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var bubble = other.GetComponentInParent<OctopusAirBubble>();
            if (bubble != null && kind != AttackKind.Book)
            {
                bubble.PopFromInk();
                Destroy(gameObject);
                return;
            }

            bool hitPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
            if (hitPlayer)
            {
                var health = other.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(damage, WitchShmup.Combat.ElementType.Enemy);
                if (kind != AttackKind.Book) InkBlindnessOverlay.Apply(blindnessDuration);
                Destroy(gameObject);
            }
        }
    }
}
