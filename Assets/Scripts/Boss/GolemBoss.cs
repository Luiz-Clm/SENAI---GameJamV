using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Core;
using WitchShmup.UI;
using WitchShmup.Utils;
using WitchShmup.Endless;

namespace WitchShmup.Boss
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class GolemBoss : MonoBehaviour, IDamageable, IInfiniteModeBoss
    {
        public enum BossState
        {
            WaitingForCutscene,
            Intro,
            Active,
            Dying
        }

        [Header("Boss Stats")]
        [SerializeField] private float maxHealth = 120f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float contactDamage = 2f;

        [Header("Boss Drops")]
        [SerializeField] private GameObject bossCoreDropPrefab;
        [SerializeField] private GameObject skillPointDropPrefab;

        [Header("Hover Movement")]
        [SerializeField] private float hoverSpeed = 1.8f;
        [SerializeField] private float hoverAmplitude = 2.2f;
        [SerializeField] private float targetArenaX = 5.8f;

        [Header("Mini Orbiting Stones")]
        [SerializeField] private int orbitingStoneCount = 10;
        [SerializeField] private float orbitRadius = 2.8f;
        [SerializeField] private float orbitSpeed = 95f;
        [SerializeField] private GameObject orbitingStonePrefab;
        private List<OrbitingStone> activeOrbitingStones = new List<OrbitingStone>();

        [Header("Special Attacks")]
        [SerializeField] private GameObject bigRockPrefab;
        [SerializeField] private GameObject stoneWallPrefab;
        [SerializeField] private Transform attackSpawnPoint;
        [SerializeField] private float attackInterval = 2.2f;

        [Header("Enrage Phase (< 50% HP)")]
        [SerializeField] private SpriteRenderer coreGlowRenderer;
        [SerializeField] private Color enrageColor = new Color(1f, 0.2f, 0.1f, 1f);

        [Header("Visual Flash & FX")]
        [SerializeField] private SpriteRenderer mainBodyRenderer;
        [SerializeField] private GameObject explosionEffectPrefab;

        [Header("Animação (Sprites do Golem)")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private SimpleSpriteAnimator bossAnimator;

        private BossState state = BossState.WaitingForCutscene;
        private Transform playerTarget;
        private float hoverBaseY = 0f;
        private bool isEnraged = false;
        private Coroutine attackRoutine;
        private Coroutine flashCoroutine;
        private Color originalBodyColor = Color.white;
        private HUDController hud;

        public BossState State => state;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnBossDefeated;
        public event Action InfiniteBossDefeated;

        public static event Action OnAnyBossFightStarted;
        public static event Action OnAnyBossFightDefeated;

        [Header("Floating Overhead Health Bar")]
        [SerializeField] private GameObject overheadHealthBar;
        private GameObject worldBarContainer;
        private Transform worldBarFillTransform;
        private SpriteRenderer worldBarFillRenderer;
        private Vector3 worldBarFillBasePosition;
        [SerializeField] private float worldBarFullWidth = 2f;

        private void Awake()
        {
            currentHealth = maxHealth;
            hoverBaseY = transform.position.y;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            if (mainBodyRenderer == null)
            {
                mainBodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Fallback: carrega os sprites de idle e ataque do Golem caso não estejam serializados
            if (idleFrames == null || idleFrames.Length == 0)
            {
                idleFrames = Resources.LoadAll<Sprite>("Sprites/golem_idle_sheet_64x64");
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = Resources.LoadAll<Sprite>("Sprites/golem_ataque_sheet_64x64");
            }

            if (mainBodyRenderer != null)
            {
                if (mainBodyRenderer.sprite == null && idleFrames != null && idleFrames.Length > 0)
                {
                    mainBodyRenderer.sprite = idleFrames[0];
                }
                originalBodyColor = mainBodyRenderer.color;
            }

            if (bossAnimator == null)
            {
                bossAnimator = GetComponentInChildren<SimpleSpriteAnimator>();
            }

            BindWorldHealthBar();
            PlayIdleAnim();
        }

        private void BindWorldHealthBar()
        {
            if (overheadHealthBar == null)
            {
                Transform child = transform.Find("OverheadHealthBar");
                if (child != null) overheadHealthBar = child.gameObject;
            }

            if (overheadHealthBar == null)
            {
                Debug.LogWarning("GolemBoss: adicione OverheadHealthBar como filha no Inspector.", this);
                return;
            }

            worldBarContainer = overheadHealthBar;
            Transform fill = worldBarContainer.transform.Find("Fill");
            worldBarFillRenderer = fill != null ? fill.GetComponent<SpriteRenderer>() : null;

            worldBarFillTransform = fill;
            if (worldBarFillTransform != null)
                worldBarFillBasePosition = worldBarFillTransform.localPosition;
            worldBarContainer.SetActive(false);
        }

        private void UpdateOverheadBar(float normalizedHealth)
        {
            if (worldBarFillTransform != null)
            {
                normalizedHealth = Mathf.Clamp01(normalizedHealth);
                Vector3 fillScale = worldBarFillTransform.localScale;
                fillScale.x = Mathf.Max(0f, worldBarFullWidth) * normalizedHealth;
                worldBarFillTransform.localScale = fillScale;

                // Keep the left edge fixed so the bar drains from left to right.
                float spriteWidth = worldBarFillRenderer != null && worldBarFillRenderer.sprite != null
                    ? worldBarFillRenderer.sprite.bounds.size.x
                    : 1f;
                Vector3 fillPosition = worldBarFillBasePosition;
                fillPosition.x -= spriteWidth * Mathf.Max(0f, worldBarFullWidth) * (1f - normalizedHealth) * 0.5f;
                worldBarFillTransform.localPosition = fillPosition;
            }
        }

        /// <summary>Intercepts a projectile when an orbital stone overlaps the boss hitbox.</summary>
        public bool TryInterceptProjectile(Projectile projectile)
        {
            if (projectile == null || !projectile.IsPlayerProjectile) return false;

            foreach (OrbitingStone stone in activeOrbitingStones)
            {
                if (stone != null && stone.TryConsumeProjectile(projectile))
                    return true;
            }
            return false;
        }

        private void Start()
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerTarget = pObj.transform;

            hud = FindFirstObjectByType<HUDController>();

            // As pedras não devem existir enquanto o Golem aguarda fora da tela:
            // tiros disparados antes da cutscene poderiam destruí-las.
            if (orbitingStonePrefab == null)
                Debug.LogWarning("GolemBoss: atribua o prefab de pedra orbital no Inspector.", this);
        }

        public void SpawnOrbitingStones()
        {
            // Limpa pedras anteriores se houver
            foreach (var stone in activeOrbitingStones)
            {
                if (stone != null) Destroy(stone.gameObject);
            }
            activeOrbitingStones.Clear();

            if (orbitingStonePrefab == null || orbitingStoneCount <= 0) return;

            float angleStep = 360f / orbitingStoneCount;
            for (int i = 0; i < orbitingStoneCount; i++)
            {
                float angle = i * angleStep;
                GameObject stoneObj = Instantiate(orbitingStonePrefab, transform.position, Quaternion.identity, transform);

                var orb = stoneObj.GetComponent<OrbitingStone>();
                orb.Initialize(transform, orbitRadius, orbitSpeed, angle);
                activeOrbitingStones.Add(orb);
            }
        }

        public void StartFight()
        {
            // Só materializa a defesa orbital quando a luta realmente começa.
            if (activeOrbitingStones.Count == 0 && orbitingStonePrefab != null)
                SpawnOrbitingStones();

            state = BossState.Active;
            if (hud == null) hud = FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowBossBar("GOLEM DE PEDRA");
                hud.SetBossHealth(1f);
            }
            if (worldBarContainer != null)
            {
                worldBarContainer.SetActive(true);
                UpdateOverheadBar(1f);
            }
            PlayIdleAnim();
            attackRoutine = StartCoroutine(AttackLoop());
            OnAnyBossFightStarted?.Invoke();
        }

        public void PrepareForInfiniteFight()
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            currentHealth = maxHealth;
            isEnraged = false;
            state = BossState.WaitingForCutscene;
            if (coreGlowRenderer != null) coreGlowRenderer.color = Color.white;
            if (worldBarContainer != null) worldBarContainer.SetActive(false);
            UpdateOverheadBar(1f);
        }

        public void StartInfiniteFight()
        {
            StartFight();
        }

        // --- Helpers de animação ---
        private void PlayIdleAnim()
        {
            if (bossAnimator != null && idleFrames != null && idleFrames.Length > 0)
            {
                bossAnimator.SetFrames(idleFrames, 6f);
            }
        }

        private void PlayAttackAnim()
        {
            if (bossAnimator != null && attackFrames != null && attackFrames.Length > 0)
            {
                bossAnimator.SetFrames(attackFrames, 10f);
            }
        }

        private void Update()
        {
            if (state == BossState.Active)
            {
                HandleHover();
            }
        }

        private void HandleHover()
        {
            float newY = hoverBaseY + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = new Vector3(targetArenaX, newY, transform.position.z);
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitForSeconds(1.5f);

            int attackCounter = 0;
            while (state == BossState.Active)
            {
                // 3 padrões de ataque em rotação: Pedra | Parede | Dupla Pedra
                int pattern = attackCounter % 3;
                if (pattern == 0)
                {
                    yield return StartCoroutine(PerformBigRockAttack());
                }
                else if (pattern == 1)
                {
                    yield return StartCoroutine(PerformStoneWallAttack());
                }
                else
                {
                    // Dispara 2 pedras em sequência rápida quando enraivecido, 1 normal caso contrário
                    yield return StartCoroutine(PerformBigRockAttack());
                    if (isEnraged)
                    {
                        yield return new WaitForSeconds(0.4f);
                        yield return StartCoroutine(PerformBigRockAttack());
                    }
                }

                attackCounter++;
                float delay = isEnraged ? attackInterval * 0.5f : attackInterval;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PerformBigRockAttack()
        {
            // Aviso visual: muda para animação de ataque
            PlayAttackAnim();

            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.3f, 0.15f);
            }
            yield return new WaitForSeconds(0.6f);

            Vector3 spawnPos = (attackSpawnPoint != null) ? attackSpawnPoint.position : transform.position + new Vector3(-1.8f, 0f, 0f);
            Vector2 dir = Vector2.left;

            if (playerTarget != null)
            {
                dir = ((Vector2)playerTarget.position - (Vector2)spawnPos).normalized;
            }

            if (bigRockPrefab == null)
            {
                Debug.LogWarning("GolemBoss: atribua o prefab de pedra grande no Inspector.", this);
                PlayIdleAnim();
                yield break;
            }

            GameObject rock = Instantiate(bigRockPrefab, spawnPos, Quaternion.identity);
            var comp = rock.GetComponent<BigRockProjectile>();
            if (comp != null) comp.Initialize(dir, isEnraged ? 11f : 8.5f);

            // Volta para idle após 0.6s
            yield return new WaitForSeconds(0.6f);
            PlayIdleAnim();
        }

        private IEnumerator PerformStoneWallAttack()
        {
            // Muda para animação de ataque
            PlayAttackAnim();
            yield return new WaitForSeconds(0.4f);

            Vector3 spawnPos = transform.position + new Vector3(-2f, 0f, 0f);

            if (stoneWallPrefab == null)
            {
                Debug.LogWarning("GolemBoss: atribua o prefab da barreira de pedra no Inspector.", this);
                PlayIdleAnim();
                yield break;
            }
            Instantiate(stoneWallPrefab, spawnPos, Quaternion.identity);

            // Volta para idle após 0.5s
            yield return new WaitForSeconds(0.5f);
            PlayIdleAnim();
        }

        public void TakeDamage(float amount, ElementType element)
        {
            if (state != BossState.Active || currentHealth <= 0) return;

            // Fogo ou Gelo causam bom dano
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);

            float normalizedHp = currentHealth / maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (hud == null) hud = FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.SetBossHealth(normalizedHp);
            }
            UpdateOverheadBar(normalizedHp);

            // Ativa modo furioso ao chegar a 50% de HP
            if (!isEnraged && currentHealth <= (maxHealth * 0.5f))
            {
                EnterEnrageMode();
            }

            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void EnterEnrageMode()
        {
            isEnraged = true;
            hoverSpeed *= 1.4f;
            orbitSpeed *= 1.6f;

            if (coreGlowRenderer != null)
            {
                coreGlowRenderer.color = enrageColor;
            }

            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.6f, 0.35f);
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (mainBodyRenderer != null)
            {
                mainBodyRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
                yield return new WaitForSeconds(0.08f);
                mainBodyRenderer.color = originalBodyColor;
            }
            flashCoroutine = null;
        }

        private void Die()
        {
            state = BossState.Dying;
            if (worldBarContainer != null) worldBarContainer.SetActive(false);
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            if (mainBodyRenderer != null) mainBodyRenderer.color = originalBodyColor;

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // Explosões sucessivas dramáticas
            for (int i = 0; i < 8; i++)
            {
                Vector3 randPos = transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 2f);
                if (explosionEffectPrefab != null)
                {
                    Instantiate(explosionEffectPrefab, randPos, Quaternion.identity);
                }
                if (ScreenShake.Instance != null)
                {
                    ScreenShake.Instance.Shake(0.2f, 0.2f);
                }
                yield return new WaitForSeconds(0.2f);
            }

            if (hud != null)
            {
                hud.HideBossBar();
            }

            // Drops do Boss
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(2500);
            }

            if (bossCoreDropPrefab != null)
            {
                Instantiate(bossCoreDropPrefab, transform.position, Quaternion.identity);
            }
            if (skillPointDropPrefab != null)
            {
                for (int s = 0; s < 3; s++)
                {
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f), 0f);
                    Instantiate(skillPointDropPrefab, transform.position + offset, Quaternion.identity);
                }
            }

            OnBossDefeated?.Invoke();
            InfiniteBossDefeated?.Invoke();
            OnAnyBossFightDefeated?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(GameManager.Instance.WaitAndTriggerVictory(1.8f));
            }

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
