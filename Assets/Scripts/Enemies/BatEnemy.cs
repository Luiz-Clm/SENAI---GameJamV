using UnityEngine;
using WitchShmup.Combat;

namespace WitchShmup.Enemies
{
    public class BatEnemy : EnemyBase
    {
        [Header("Movement (Wavy Flight)")]
        [SerializeField] private float horizontalSpeed = 3.2f;
        [SerializeField] private float waveFrequency = 3f;
        [SerializeField] private float waveAmplitude = 1.2f;

        [Header("Charge / Rush at Player")]
        [SerializeField] private float chargeRange = 5.5f;
        [SerializeField] private float chargeSpeed = 7f;
        [SerializeField] private float chargeCooldown = 3.5f;
        private bool isCharging = false;
        private float chargeTimer = 0f;
        private float chargeCountdown = 0f;
        private Vector2 chargeDirection = Vector2.left;

        [Header("Shooting (Tiro do Morcego)")]
        [SerializeField] private GameObject fireballPrefab;
        [Tooltip("Tempo em segundos entre cada tiro do morcego. Padrão: 1.8s.")]
        [SerializeField] private float fireRate = 1.8f;
        [Tooltip("Velocidade do tiro do morcego. Aumentado de 8 para 12 para maior desafio!")]
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private Vector2 fireOffset = new Vector2(-0.5f, 0f);

        [Header("Dodge AI")]
        [SerializeField] private float detectionRadius = 3.5f;
        [SerializeField] private float dodgeSpeed = 9f;
        [SerializeField] private float dodgeDuration = 0.25f;
        [SerializeField] private float dodgeCooldown = 0.8f;

        private float fireTimer;
        private float spawnY;
        private Transform playerTarget;

        // Dodge state
        private bool isDodging = false;
        private float dodgeTimer = 0f;
        private float dodgeCooldownTimer = 0f;
        private float dodgeDirectionY = 0f;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 3f;
            currentHealth = 3f;
            scoreValue = 200;
            contactDamage = 1f;

            // Morcegos são fracos contra Gelo!
            weakElement = ElementType.Ice;
            weaknessDamageMultiplier = 2f;
        }

        private void Start()
        {
            spawnY = transform.position.y;
            fireTimer = Random.Range(0.8f, fireRate);
            chargeCountdown = Random.Range(1.5f, chargeCooldown);

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        protected override void Update()
        {
            base.Update();

            HandleChargeAI();
            HandleDodgeAI();
            HandleMovement();
            HandleShooting();
        }

        private void HandleChargeAI()
        {
            if (isCharging)
            {
                chargeTimer -= Time.deltaTime;
                if (chargeTimer <= 0f)
                {
                    isCharging = false;
                    chargeCountdown = Random.Range(2f, chargeCooldown);
                    if (playerTarget != null) spawnY = playerTarget.position.y;
                }
                return;
            }

            chargeCountdown -= Time.deltaTime;
            if (chargeCountdown > 0f || playerTarget == null) return;

            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist <= chargeRange)
            {
                // Inicia o rush na direção do player
                isCharging = true;
                chargeTimer = 0.55f;
                chargeDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
                // Efeito visual: flash vermelho
                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
            }
            else
            {
                chargeCountdown = Random.Range(0.5f, 1.5f);
            }
        }

        private void HandleMovement()
        {
            if (isCharging && playerTarget != null)
            {
                // Avança em alta velocidade direto no player
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    playerTarget.position,
                    chargeSpeed * speedMultiplier * Time.deltaTime
                );
                return;
            }

            // Restaura cor após charge
            if (!isCharging && spriteRenderer != null && flashCoroutine == null && slowCoroutine == null)
            {
                spriteRenderer.color = originalColor;
            }

            // Movimento horizontal para a esquerda (respeita desaceleração por gelo)
            float posX = transform.position.x - (horizontalSpeed * speedMultiplier * Time.deltaTime);

            float posY = transform.position.y;

            if (isDodging)
            {
                // Esquiva ativa rápida para cima ou baixo
                posY += dodgeDirectionY * dodgeSpeed * speedMultiplier * Time.deltaTime;
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                    spawnY = posY; // Reajusta a base do voo
                }
            }
            else
            {
                // Movimento senoidal natural
                spawnY = Mathf.Clamp(spawnY, -3.8f, 3.8f);
                posY = spawnY + Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
            }

            // Limita dentro da tela vertical
            posY = Mathf.Clamp(posY, -4f, 4f);
            transform.position = new Vector3(posX, posY, transform.position.z);
        }

        private void HandleDodgeAI()
        {
            dodgeCooldownTimer -= Time.deltaTime;
            if (isDodging || dodgeCooldownTimer > 0f) return;

            // Busca por projéteis do jogador se aproximando
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
            foreach (var hit in hits)
            {
                var proj = hit.GetComponent<Projectile>();
                if (proj != null && hit.transform.position.x < transform.position.x)
                {
                    // Se o projétil está vindo da esquerda na direção do morcego e na mesma altura
                    float diffY = hit.transform.position.y - transform.position.y;
                    if (Mathf.Abs(diffY) < 1.0f)
                    {
                        // Desvia para o lado oposto do tiro
                        dodgeDirectionY = (diffY >= 0f) ? -1f : 1f;

                        // Se estiver muito perto do chão ou teto, esquiva para o centro
                        if (transform.position.y > 2.5f) dodgeDirectionY = -1f;
                        if (transform.position.y < -2.5f) dodgeDirectionY = 1f;

                        isDodging = true;
                        dodgeTimer = dodgeDuration;
                        dodgeCooldownTimer = dodgeCooldown;
                        break;
                    }
                }
            }
        }

        private void HandleShooting()
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                fireTimer = fireRate;
                ShootFireball();
            }
        }

        private void ShootFireball()
        {
            Vector3 spawnPos = transform.position + (Vector3)fireOffset;
            Vector2 shootDir = Vector2.left;

            if (playerTarget != null)
            {
                shootDir = ((Vector2)playerTarget.position - (Vector2)spawnPos).normalized;
            }

            if (fireballPrefab != null)
            {
                GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
                var proj = fireball.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(shootDir, projectileSpeed, 1f, ElementType.Enemy, false);
                }
            }
            else
            {
                CreateFallbackFireball(spawnPos, shootDir);
            }
        }

        private void CreateFallbackFireball(Vector3 pos, Vector2 dir)
        {
            GameObject obj = new GameObject("Bat_Fireball");
            obj.transform.position = pos;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.4f, 0.1f, 1f);
            sr.sortingOrder = 7;

            // Mini círculo de fogo 8x8
            Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[64];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color(1f, 0.4f, 0.1f, 1f);
            tex.SetPixels(cols);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var proj = obj.AddComponent<Projectile>();
            proj.Initialize(dir, projectileSpeed, 1f, ElementType.Enemy, false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
