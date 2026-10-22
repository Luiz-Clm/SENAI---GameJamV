using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WitchShmup.Enemies
{
    /// <summary>Peixe-lanterna: vive abaixo da maré e persegue o jogador somente quando ele mergulha.</summary>
    public class LanternFishEnemy : EnemyBase
    {
        [Header("Nado do Peixe-Lanterna")]
        [SerializeField] private float swimSpeed = 5.5f;
        [SerializeField] private float patrolSpeed = 1.2f;
        [Header("Luz da Lanterna")]
        [SerializeField] private Light2D lanternLight;
        [SerializeField] private Color lanternColor = new Color(1f, 0.82f, 0.32f, 1f);
        [SerializeField] private float lanternIntensity = 1.1f;
        [SerializeField] private float lanternRadius = 2.2f;

        private Transform player;
        private Vector2 moveDirection = Vector2.left;
        private bool hasLockedChargeDirection;

        protected override void Awake()
        {
            base.Awake();
            waterZone = EnemyWaterZone.UnderwaterOnly;
            waterZoneMargin = Mathf.Max(waterZoneMargin, 0.3f);
            if (lanternLight != null)
            {
                lanternLight.color = lanternColor;
                lanternLight.intensity = lanternIntensity;
                lanternLight.pointLightOuterRadius = lanternRadius;
            }
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
            // Trava a direção uma única vez quando o jogador mergulha. O peixe
            // continua nessa trajetória mesmo se o jogador desviar, como o fantasma.
            if (!hasLockedChargeDirection && water != null && player != null && water.IsPlayerSubmerged)
            {
                float targetY = Mathf.Min(player.position.y, water.SurfaceY - waterZoneMargin);
                Vector2 toPlayer = new Vector2(player.position.x - transform.position.x,
                    targetY - transform.position.y);
                // O peixe sempre avança para a esquerda; nunca corrige o curso para trás.
                toPlayer.x = -Mathf.Max(Mathf.Abs(toPlayer.x), 0.1f);
                moveDirection = toPlayer.normalized;
                hasLockedChargeDirection = true;
            }

            float speed = hasLockedChargeDirection ? swimSpeed : patrolSpeed;
            transform.position += (Vector3)(moveDirection * (speed * speedMultiplier * Time.deltaTime));
        }
    }
}
