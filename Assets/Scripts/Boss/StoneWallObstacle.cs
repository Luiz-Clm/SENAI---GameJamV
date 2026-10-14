using System.Collections;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Player;

namespace WitchShmup.Boss
{
    public class StoneWallObstacle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float advanceSpeed = 2.5f;

        public float AdvanceSpeed => advanceSpeed;

        private void Update()
        {
            transform.Translate(Vector3.left * (advanceSpeed * Time.deltaTime), Space.World);

            if (CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 4f))
            {
                Destroy(gameObject);
            }
            else if (transform.position.x < -18f)
            {
                Destroy(gameObject);
            }
        }
    }

    [RequireComponent(typeof(Collider2D))]
    public class StoneWallSegment : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 6f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float contactDamage = 1f;

        [Header("Push & Crush Settings")]
        [SerializeField] private float crushInterval = 0.65f;
        [SerializeField] private int crushDamage = 1;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private GameObject breakEffectPrefab;

        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;
        private Collider2D col;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private Rigidbody2D playerRb;
        private float nextCrushTime = 0f;

        private const float PlayerHalfWidth = 0.55f;
        private const float PlayerHalfHeight = 0.40f;

        private void Awake()
        {
            currentHealth = maxHealth;
            col = GetComponent<Collider2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var pObj = GameObject.FindWithTag("Player");
                if (pObj != null)
                {
                    playerTransform = pObj.transform;
                    playerHealth = pObj.GetComponent<PlayerHealth>();
                    playerRb = pObj.GetComponent<Rigidbody2D>();
                }
            }
        }

        private void FixedUpdate()
        {
            HandlePushAndCrush();
        }

        private void HandlePushAndCrush()
        {
            if (currentHealth <= 0) return;
            if (playerTransform == null || playerHealth == null || !playerTransform.gameObject.activeInHierarchy)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            if (col == null) col = GetComponent<Collider2D>();
            if (col == null) return;

            Bounds segBounds = col.bounds;
            Vector3 playerPos = playerTransform.position;

            // Checa sobreposição vertical no corredor desta pedra
            bool inVerticalSpan = (playerPos.y + PlayerHalfHeight >= segBounds.min.y) &&
                                  (playerPos.y - PlayerHalfHeight <= segBounds.max.y);

            if (!inVerticalSpan) return;

            // A parede avança da direita para a esquerda.
            // Se o player está na frente ou sendo empurrado pela face frontal da pedra
            float wallFrontX = segBounds.min.x;
            float wallBackX = segBounds.max.x;

            if (playerPos.x + PlayerHalfWidth >= wallFrontX - 0.1f && playerPos.x - PlayerHalfWidth <= wallBackX)
            {
                // Limite traseiro da tela (parede de trás da masmorra)
                float backWallX = (CameraBounds.Instance != null) ? (CameraBounds.Instance.MinX + 0.6f) : -8.2f;

                // Empurra o player para a esquerda acompanhando o avanço da pedra
                float pushedX = wallFrontX - PlayerHalfWidth;

                if (pushedX <= backWallX + 0.2f)
                {
                    // O jogador chegou na parede de trás e a barreira continua empurrando: ESMAGA!
                    playerPos.x = backWallX;
                    playerTransform.position = playerPos;
                    if (playerRb != null) playerRb.position = new Vector2(backWallX, playerPos.y);

                    if (Time.time >= nextCrushTime)
                    {
                        nextCrushTime = Time.time + crushInterval;
                        playerHealth.TakeDamage(crushDamage, ElementType.Enemy);

                        if (ScreenShake.Instance != null)
                        {
                            ScreenShake.Instance.Shake(0.35f, 0.25f);
                        }
                    }
                }
                else
                {
                    // Empurra suavemente o jogador para a esquerda
                    if (pushedX < playerPos.x)
                    {
                        playerPos.x = pushedX;
                        playerTransform.position = playerPos;
                        if (playerRb != null) playerRb.position = new Vector2(pushedX, playerPos.y);
                    }
                }
            }
        }

        public void TakeDamage(float amount, ElementType element)
        {
            if (currentHealth <= 0) return;

            // Fogo quebra pedra mais rápido!
            float mult = (element == ElementType.Fire) ? 1.5f : 1.0f;
            currentHealth -= amount * mult;

            if (gameObject.activeInHierarchy)
            {
                Flash();
            }

            if (currentHealth <= 0)
            {
                Break();
            }
        }

        private void Flash()
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = damageFlashColor;
                yield return new WaitForSeconds(0.06f);
                spriteRenderer.color = originalColor;
            }
            flashCoroutine = null;
        }

        private void Break()
        {
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.2f, 0.15f);
            }

            // Destrói este bloco da parede, abrindo passagem segura para o player passar!
            Destroy(gameObject);
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
        }
    }
}
