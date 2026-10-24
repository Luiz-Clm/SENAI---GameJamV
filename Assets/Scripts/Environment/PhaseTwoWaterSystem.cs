using UnityEngine;
using TMPro;
using System.Collections;
using WitchShmup.Player;

namespace WitchShmup.Environment
{
    /// <summary>Maré e regras da água específicas da Biblioteca Submersa (Fase 2).</summary>
    public class PhaseTwoWaterSystem : MonoBehaviour
    {
        public static PhaseTwoWaterSystem Instance { get; private set; }

        [Header("Maré")]
        [SerializeField] private float tideRange = 1.8f;
        [SerializeField] private float initialHold = 4f;
        [SerializeField] private Vector2 holdDuration = new Vector2(4f, 7f);
        [SerializeField] private float warningDuration = 1.2f;
        [SerializeField] private float transitionDuration = 5f;
        [SerializeField] private float initialSurfaceOffset;
        [SerializeField] private float surfaceLineThickness = 0.055f;

        [Header("Oxigênio")]
        [SerializeField] private float oxygenCapacity = 8f;
        [SerializeField] private float oxygenRecoveryRate = 2.5f;
        [SerializeField] private float damageInterval = 1.5f;
        [SerializeField] private float headOffset = 0.45f;

        [Header("Visual")]
        [SerializeField, Range(0f, 1f)] private float underwaterTintAlpha = 0.26f;
        [SerializeField] private Color waterColor = new Color(0.12f, 0.62f, 0.82f, 1f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.9f, 0.45f, 1f);
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer underwaterTintRenderer;
        [SerializeField] private SpriteRenderer waterSurfaceRenderer;
        [SerializeField] private TextMeshProUGUI oxygenStatusText;
        [SerializeField] private TextMeshProUGUI tideStatusText;

        private PlayerHealth playerHealth;
        private float centerY;
        private float surfaceY;
        private float oxygen;
        private float timer;
        private float transitionElapsed;
        private float transitionStart;
        private float transitionTarget;
        private float damageTimer;
        private bool transitioning;
        private bool warning;
        private bool bossArenaTransitioning;
        private bool bossFightActive;
        private float bossWarningTimeLeft;

        public float SurfaceY => surfaceY;
        public float Oxygen01 => oxygenCapacity <= 0f ? 1f : oxygen / oxygenCapacity;
        public bool IsPlayerSubmerged => bossFightActive || IsSubmerged(GetPlayerHeadY());
        public bool IsWarning => warning;
        public bool IsBossArenaTransitioning => bossArenaTransitioning;
        public bool IsBossFightActive => bossFightActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
            centerY = targetCamera != null ? targetCamera.transform.position.y : 0f;
            surfaceY = centerY + initialSurfaceOffset;
            oxygen = oxygenCapacity;
            if (underwaterTintRenderer == null) underwaterTintRenderer = transform.Find("Underwater_Tint")?.GetComponent<SpriteRenderer>();
            if (waterSurfaceRenderer == null) waterSurfaceRenderer = transform.Find("Water_Surface")?.GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            FindPlayer();
            timer = initialHold;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (playerHealth == null) FindPlayer();
            UpdateTide();
            UpdateWaterVisuals();
            UpdateOxygen();
            UpdateHud();
        }

        public bool IsSubmerged(float worldY) => worldY < surfaceY;

        public IEnumerator PrepareBossArena(float warningSeconds, float riseSeconds)
        {
            bossFightActive = false;
            bossArenaTransitioning = true;
            transitioning = false;
            warning = true;
            bossWarningTimeLeft = Mathf.Max(0f, warningSeconds);
            while (bossWarningTimeLeft > 0f)
            {
                bossWarningTimeLeft -= Time.deltaTime;
                yield return null;
            }

            warning = false;
            transitionStart = surfaceY;
            transitionTarget = GetScreenTopY() + 1.5f;
            transitionElapsed = 0f;
            riseSeconds = Mathf.Max(0.1f, riseSeconds);
            while (transitionElapsed < riseSeconds)
            {
                transitionElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, transitionElapsed / riseSeconds);
                surfaceY = Mathf.Lerp(transitionStart, transitionTarget, t);
                yield return null;
            }

            surfaceY = transitionTarget;
            bossArenaTransitioning = false;
            bossFightActive = true;
            transitioning = false;
            warning = false;
            timer = Random.Range(holdDuration.x, holdDuration.y);
            damageTimer = damageInterval;
        }

        public void EndBossFight()
        {
            bossArenaTransitioning = false;
            bossFightActive = false;
            warning = false;
            transitioning = false;
            timer = Random.Range(holdDuration.x, holdDuration.y);
        }

        public void AddOxygenFraction(float fraction)
        {
            oxygen = Mathf.Clamp(oxygen + oxygenCapacity * Mathf.Max(0f, fraction), 0f, oxygenCapacity);
            damageTimer = damageInterval;
        }

        private float GetScreenTopY()
        {
            if (targetCamera == null) return surfaceY + 10f;
            float halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
            return targetCamera.transform.position.y + halfHeight;
        }

        public float GetPlayerHeadY()
        {
            if (playerHealth == null) return 0f;
            var playerRenderer = playerHealth.GetComponentInChildren<SpriteRenderer>();
            return playerRenderer != null ? playerRenderer.bounds.max.y - 0.05f : playerHealth.transform.position.y + headOffset;
        }

        private void FindPlayer()
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        private void UpdateTide()
        {
            if (bossArenaTransitioning || bossFightActive) return;
            timer -= Time.deltaTime;
            if (!transitioning)
            {
                warning = timer <= warningDuration;
                if (timer <= 0f)
                {
                    float range = GetTideRange();
                    transitionStart = surfaceY;
                    transitionTarget = Random.Range(centerY - range, centerY + range);
                    if (Mathf.Abs(transitionTarget - transitionStart) < 0.45f)
                        transitionTarget = centerY + (transitionStart <= centerY ? range : -range);
                    transitionElapsed = 0f;
                    transitioning = true;
                    warning = false;
                }
            }
            else
            {
                transitionElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, transitionElapsed / Mathf.Max(0.1f, transitionDuration));
                surfaceY = Mathf.Lerp(transitionStart, transitionTarget, t);
                if (t >= 1f)
                {
                    surfaceY = transitionTarget;
                    transitioning = false;
                    timer = Random.Range(holdDuration.x, holdDuration.y);
                }
            }
        }

        private float GetTideRange()
        {
            return targetCamera != null && targetCamera.orthographic
                ? Mathf.Min(tideRange, targetCamera.orthographicSize * 0.55f)
                : tideRange;
        }

        private void UpdateOxygen()
        {
            if (playerHealth == null || playerHealth.CurrentHealth <= 0) return;
            if (bossFightActive || IsSubmerged(GetPlayerHeadY()))
            {
                oxygen = Mathf.Max(0f, oxygen - Time.deltaTime);
                if (oxygen <= 0f)
                {
                    damageTimer -= Time.deltaTime;
                    if (damageTimer <= 0f)
                    {
                        playerHealth.TakeDamage(1f);
                        damageTimer = damageInterval;
                    }
                }
            }
            else
            {
                oxygen = Mathf.Min(oxygenCapacity, oxygen + oxygenRecoveryRate * Time.deltaTime);
                damageTimer = 0f;
            }
        }

        private void UpdateWaterVisuals()
        {
            if (targetCamera == null || underwaterTintRenderer == null || waterSurfaceRenderer == null) return;
            float halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
            float bottom = targetCamera.transform.position.y - halfHeight;
            float width = targetCamera.orthographic ? halfHeight * 2f * targetCamera.aspect : 20f;
            float waterHeight = Mathf.Max(0.01f, surfaceY - bottom);
            underwaterTintRenderer.transform.position = new Vector3(targetCamera.transform.position.x, bottom + waterHeight * 0.5f, 5f);
            underwaterTintRenderer.transform.localScale = Vector3.one;
            underwaterTintRenderer.drawMode = SpriteDrawMode.Tiled;
            underwaterTintRenderer.size = new Vector2(width + 2f, waterHeight);
            underwaterTintRenderer.color = new Color(waterColor.r, waterColor.g, waterColor.b, underwaterTintAlpha);
            waterSurfaceRenderer.transform.position = new Vector3(targetCamera.transform.position.x, surfaceY, 4f);
            waterSurfaceRenderer.transform.localScale = Vector3.one;
            waterSurfaceRenderer.drawMode = SpriteDrawMode.Tiled;
            waterSurfaceRenderer.size = new Vector2(width + 2f, surfaceLineThickness);
            waterSurfaceRenderer.color = warning ? warningColor : waterColor;

            // A linha da superfície fica atrás do personagem; o filtro azul fica à frente
            // para dar a sensação de que ele entrou na água sem esconder o sprite.
            underwaterTintRenderer.sortingOrder = 11;
            waterSurfaceRenderer.sortingOrder = 9;

        }

        private void UpdateHud()
        {
            if (oxygenStatusText != null)
            {
                const int bubbleCount = 6;
                int filled = Mathf.CeilToInt(Oxygen01 * bubbleCount);
                oxygenStatusText.text = $"AR  {new string('●', filled)}{new string('○', bubbleCount - filled)}";
                oxygenStatusText.color = Oxygen01 <= 0.25f
                    ? new Color(1f, 0.35f, 0.3f, 1f)
                    : Color.white;
            }

            if (tideStatusText != null)
            {
                if (bossArenaTransitioning)
                {
                    tideStatusText.text = warning
                        ? $"ATENÇÃO: MARÉ EM {Mathf.CeilToInt(bossWarningTimeLeft)}..."
                        : "MARÉ SUBINDO!";
                    tideStatusText.color = warningColor;
                    return;
                }
                if (bossFightActive)
                {
                    tideStatusText.text = "MARÉ PARADA";
                    tideStatusText.color = waterColor;
                    return;
                }
                string direction = transitioning
                    ? (transitionTarget > transitionStart ? "SUBINDO" : "DESCENDO")
                    : (warning ? "VAI MUDAR" : "ESTÁVEL");
                tideStatusText.text = $"MARÉ  {direction}";
                tideStatusText.color = warning ? warningColor : Color.white;
            }
        }
    }
}
