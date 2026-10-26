using System.Collections;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Player;

namespace WitchShmup.Boss
{
    /// <summary>
    /// Add this component to the wall prefab root and to each stone segment.
    /// The root moves the complete wall. Segment components handle damage,
    /// pushing and projectile blocking using the collider configured on the prefab.
    /// </summary>
    public class StoneWallObstacle : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Speed at which the root wall advances toward the player.")]
        [SerializeField] private float advanceSpeed = 3f;

        [Header("Segment health")]
        [Tooltip("Health of this segment. Set to 0 to make it indestructible.")]
        [SerializeField] private float maxHealth = 8f;

        [Header("Player contact")]
        [Tooltip("Damage dealt when a segment first touches the player.")]
        [SerializeField] private float contactDamage = 2f;
        [Tooltip("Repeat damage interval while the player is pinned against the arena edge.")]
        [SerializeField] private float crushInterval = 0.65f;
        [SerializeField] private float playerHalfWidth = 0.55f;
        [SerializeField] private float playerHalfHeight = 0.4f;

        [Header("Visual feedback")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        private float currentHealth;
        private float nextCrushTime;
        private bool isDead;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private Collider2D wallCollider;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private Rigidbody2D playerRigidbody;

        private bool IsRoot => transform.parent == null || transform.parent.GetComponent<StoneWallObstacle>() == null;

        private void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;
            wallCollider = GetComponent<Collider2D>();

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }

            if (wallCollider != null) wallCollider.isTrigger = true;
            FindPlayer();
        }

        private void Update()
        {
            if (isDead || !IsRoot) return;

            transform.Translate(Vector3.left * (advanceSpeed * Time.deltaTime), Space.World);
            if ((CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 5f)) ||
                transform.position.x < -20f)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (isDead || IsRoot) return;
            PushAndCrushPlayer();
        }

        private void FindPlayer()
        {
            if (playerHealth != null) return;
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }

        private void PushAndCrushPlayer()
        {
            if (playerTransform == null || playerHealth == null)
            {
                FindPlayer();
                if (playerTransform == null || playerHealth == null) return;
            }

            if (wallCollider == null) return;
            Bounds bounds = wallCollider.bounds;
            Vector3 playerPosition = playerTransform.position;
            bool overlapsVertically = playerPosition.y + playerHalfHeight >= bounds.min.y &&
                                      playerPosition.y - playerHalfHeight <= bounds.max.y;
            bool overlapsHorizontally = playerPosition.x + playerHalfWidth >= bounds.min.x - 0.1f &&
                                        playerPosition.x - playerHalfWidth <= bounds.max.x;
            if (!overlapsVertically || !overlapsHorizontally) return;

            float backWallX = CameraBounds.Instance != null ? CameraBounds.Instance.MinX + 0.6f : -8.2f;
            float pushedX = Mathf.Max(bounds.min.x - playerHalfWidth, backWallX);
            if (pushedX < playerPosition.x)
            {
                playerPosition.x = pushedX;
                playerTransform.position = playerPosition;
                if (playerRigidbody != null) playerRigidbody.position = new Vector2(pushedX, playerPosition.y);
            }

            if (pushedX <= backWallX + 0.01f && Time.time >= nextCrushTime)
            {
                nextCrushTime = Time.time + crushInterval;
                playerHealth.TakeDamage(contactDamage, ElementType.Enemy);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead || IsRoot) return;

            PlayerHealth health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(contactDamage, ElementType.Enemy);
                nextCrushTime = Time.time + crushInterval;
                return;
            }

            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile == null || !projectile.IsPlayerProjectile) return;

            if (maxHealth > 0f) TakeDamage(projectile.Damage, projectile.Element);
            Destroy(projectile.gameObject);
        }

        public void TakeDamage(float amount, ElementType element = ElementType.None)
        {
            if (isDead || maxHealth <= 0f) return;

            float multiplier = element == ElementType.Fire ? 1.5f : 1f;
            currentHealth -= amount * multiplier;

            if (spriteRenderer != null && gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0f)
            {
                isDead = true;
                if (wallCollider != null) wallCollider.enabled = false;
                Destroy(gameObject, 0.08f);
            }
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(0.06f);
            if (spriteRenderer != null && !isDead) spriteRenderer.color = originalColor;
            flashCoroutine = null;
        }
    }
}
