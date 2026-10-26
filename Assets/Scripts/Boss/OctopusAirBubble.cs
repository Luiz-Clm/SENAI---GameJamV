using UnityEngine;
using WitchShmup.Environment;
using WitchShmup.Player;

namespace WitchShmup.Boss
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class OctopusAirBubble : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float oxygenFraction = 0.4f;
        [SerializeField] private float riseSpeed = 1.1f;
        [SerializeField] private float lifetime = 12f;

        private float age;
        private bool consumed;

        public void Initialize(float refillFraction, bool isLarge)
        {
            oxygenFraction = Mathf.Clamp01(refillFraction);
            transform.localScale = isLarge ? Vector3.one * 1.6f : Vector3.one * 0.75f;
            var collider = GetComponent<Collider2D>();
            if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            var body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            renderer.sortingOrder = 15;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var camera = Camera.main;
            if (camera != null && camera.orthographic &&
                transform.position.y > camera.transform.position.y + camera.orthographicSize + 1f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || (other.CompareTag("Player") == false &&
                other.GetComponentInParent<PlayerController>() == null)) return;

            consumed = true;
            if (PhaseTwoWaterSystem.Instance != null)
                PhaseTwoWaterSystem.Instance.AddOxygenFraction(oxygenFraction);
            Destroy(gameObject);
        }

        public void PopFromInk()
        {
            if (consumed) return;
            consumed = true;
            Destroy(gameObject);
        }
    }
}
