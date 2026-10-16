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

        [Header("Damage & Scale")]
        [SerializeField] private float damage = 2f;
        [Tooltip("Escala visual da rocha para ficar imponente e ameaçadora")]
        [SerializeField] private float rockScale = 2.5f;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            // Carrega o sprite principal de pedra (ataqueBoss2_4) da planilha fatiada
            var all = Resources.LoadAll<Sprite>("Sprites/ataqueBoss2");
            if (all != null && all.Length > 0)
            {
                Sprite mainRock = null;
                foreach (var s in all)
                {
                    if (s.name.Equals("ataqueBoss2_4", System.StringComparison.OrdinalIgnoreCase))
                    {
                        mainRock = s;
                        break;
                    }
                }
                if (mainRock == null) mainRock = all[0];

                sr.sprite = mainRock;
                sr.color = Color.white;
                sr.sortingOrder = 12;
            }

            // Aumenta o tamanho do projétil conforme pedido
            transform.localScale = new Vector3(rockScale, rockScale, 1f);

            var circleCol = GetComponent<CircleCollider2D>();
            if (circleCol != null)
            {
                circleCol.radius = 0.55f;
            }
        }

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
