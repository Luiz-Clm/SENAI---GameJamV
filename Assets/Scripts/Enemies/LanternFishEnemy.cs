using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WitchShmup.Enemies
{
    /// <summary>Peixe-lanterna que caça o jogador submerso e tenta alcançá-lo por trás.</summary>
    public class LanternFishEnemy : EnemyBase
    {
        [Header("Nado do Peixe-Lanterna")]
        [SerializeField] private float swimSpeed = 5.5f;
        [SerializeField] private float patrolSpeed = 1.2f;
        [SerializeField] private float turnBehindDistance = 1.25f;
        [SerializeField] private float dodgeResetDistance = 1.5f;
        [SerializeField] private Vector2 lanternOffset = new Vector2(-0.8f, 0.3f);
        [Header("Luz da Lanterna")]
        [SerializeField] private Light2D lanternLight;
        [SerializeField] private Color lanternColor = new Color(1f, 0.82f, 0.32f, 1f);
        [SerializeField] private float lanternIntensity = 1.1f;
        [SerializeField] private float lanternRadius = 2.2f;

        private Transform player;
        private bool isAttackingPlayer;

        protected override void Awake()
        {
            base.Awake();
            waterZone = EnemyWaterZone.UnderwaterOnly;
            waterZoneMargin = Mathf.Max(waterZoneMargin, 0.3f);
            if (lanternLight == null) lanternLight = GetComponentInChildren<Light2D>();
            if (lanternLight == null)
            {
                var lightObject = new GameObject("Lantern_Light");
                lightObject.transform.SetParent(transform, false);
                lanternLight = lightObject.AddComponent<Light2D>();
                lanternLight.lightType = Light2D.LightType.Point;
            }
            if (lanternLight != null)
            {
                lanternLight.color = lanternColor;
                lanternLight.intensity = lanternIntensity;
                lanternLight.pointLightOuterRadius = lanternRadius;
                lanternLight.pointLightInnerRadius = lanternRadius * 0.2f;
            }
            UpdateLanternPosition();
        }

        private void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        protected override void Update()
        {
            base.Update();
            var water = WitchShmup.Environment.PhaseTwoWaterSystem.Instance;
            if (water == null) return;
            if (player == null || !water.IsPlayerSubmerged)
            {
                if (spriteRenderer != null) spriteRenderer.flipX = false;
                UpdateLanternPosition();
                transform.position += Vector3.left * (patrolSpeed * speedMultiplier * Time.deltaTime);
                return;
            }

            float safeY = water.SurfaceY - waterZoneMargin;
            Vector2 playerPosition = player.position;
            playerPosition.y = Mathf.Min(playerPosition.y, safeY);

            // Se o jogador escapar durante a investida, o peixe recomeça pela retaguarda.
            if (isAttackingPlayer && Vector2.Distance(transform.position, playerPosition) > dodgeResetDistance)
                isAttackingPlayer = false;

            float playerFacing = Mathf.Sign(player.localScale.x);
            if (playerFacing == 0f) playerFacing = 1f;
            Vector2 target;
            if (isAttackingPlayer)
            {
                target = playerPosition;
            }
            else
            {
                target = new Vector2(playerPosition.x - playerFacing * turnBehindDistance, playerPosition.y);
                if (Vector2.Distance(transform.position, target) < 0.35f)
                    isAttackingPlayer = true;
            }

            Vector2 next = Vector2.MoveTowards(transform.position, target,
                swimSpeed * speedMultiplier * Time.deltaTime);
            next.y = Mathf.Min(next.y, safeY);
            if (spriteRenderer != null && Mathf.Abs(next.x - transform.position.x) > 0.001f)
                spriteRenderer.flipX = next.x > transform.position.x;
            UpdateLanternPosition();
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private void UpdateLanternPosition()
        {
            if (lanternLight == null) return;
            float side = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
            lanternLight.transform.localPosition = new Vector3(Mathf.Abs(lanternOffset.x) * side, lanternOffset.y, 0f);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            bool touchedPlayer = other.CompareTag("Player") ||
                other.GetComponentInParent<WitchShmup.Player.PlayerController>() != null;
            base.OnTriggerEnter2D(other);
            if (touchedPlayer && !isDead) Die();
        }
    }
}
