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
        [SerializeField] private Sprite overheadBarBackgroundSprite;
        [SerializeField] private Sprite overheadBarFillSprite;
        private GameObject worldBarContainer;
        private Transform worldBarFillTransform;
        private float worldBarFullWidth = 0.55f;
        private float baseHoverSpeed;
        private float baseOrbitSpeed;
        private float baseContactDamage;
        private Color originalCoreGlowColor = Color.white;

        private void Awake()
        {
            currentHealth = maxHealth;
            hoverBaseY = transform.position.y;
            baseHoverSpeed = hoverSpeed;
            baseOrbitSpeed = orbitSpeed;
            baseContactDamage = contactDamage;

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

            // Carrega os sprites oficiais da barra do Golem
            if (overheadBarBackgroundSprite == null)
            {
                overheadBarBackgroundSprite = Resources.Load<Sprite>("Sprites/boss_barra_fundo_decorado_192x36");
            }
            if (overheadBarFillSprite == null)
            {
                overheadBarFillSprite = Resources.Load<Sprite>("Sprites/boss_barra_vida_148x12");
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
            if (coreGlowRenderer != null) originalCoreGlowColor = coreGlowRenderer.color;

            if (bossAnimator == null)
            {
                bossAnimator = GetComponentInChildren<SimpleSpriteAnimator>();
                if (bossAnimator == null && mainBodyRenderer != null)
                {
                    bossAnimator = mainBodyRenderer.gameObject.AddComponent<SimpleSpriteAnimator>();
                }
            }

            // Cria barra de vida flutuante em cima da cabeça do Boss
            CreateWorldHealthBar();

            // Inicia com animação idle
            PlayIdleAnim();
        }

        private void CreateWorldHealthBar()
        {
            worldBarContainer = new GameObject("OverheadHealthBar");
            worldBarContainer.transform.SetParent(transform, false);
            worldBarContainer.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            // Moldura decorada oficial do Golem (192x36)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(worldBarContainer.transform, false);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sortingOrder = 20;

            if (overheadBarBackgroundSprite == null)
            {
                overheadBarBackgroundSprite = Resources.Load<Sprite>("Sprites/boss_barra_fundo_decorado_192x36");
            }
            if (overheadBarFillSprite == null)
            {
                overheadBarFillSprite = Resources.Load<Sprite>("Sprites/boss_barra_vida_148x12");
            }

            bgSr.sprite = overheadBarBackgroundSprite;
            bgSr.color = Color.white;
            bgObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // Fill oficial de vida (148x12)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(worldBarContainer.transform, false);
            // Área útil centralizada
            fillObj.transform.localPosition = new Vector3(-2.31f, 0f, 0f);
            var fillSr = fillObj.AddComponent<SpriteRenderer>();
            fillSr.sortingOrder = 21;

            fillSr.sprite = overheadBarFillSprite;
            fillSr.color = Color.white;
            fillObj.transform.localScale = new Vector3(worldBarFullWidth, 0.55f, 1f);

            worldBarFillTransform = fillObj.transform;
            worldBarContainer.SetActive(false);
        }

        private void UpdateOverheadBar(float normalizedHealth)
        {
            if (worldBarFillTransform != null)
            {
                normalizedHealth = Mathf.Clamp01(normalizedHealth);
                worldBarFillTransform.localScale = new Vector3(worldBarFullWidth * normalizedHealth, 0.55f, 1f);
            }
        }

        private void Start()
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) playerTarget = pObj.transform;

            hud = FindFirstObjectByType<HUDController>();

            SpawnOrbitingStones();
        }

        public void SpawnOrbitingStones()
        {
            // Limpa pedras anteriores se houver
            foreach (var stone in activeOrbitingStones)
            {
                if (stone != null) Destroy(stone.gameObject);
            }
            activeOrbitingStones.Clear();

            float angleStep = 360f / orbitingStoneCount;
            for (int i = 0; i < orbitingStoneCount; i++)
            {
                float angle = i * angleStep;
                GameObject stoneObj;

                if (orbitingStonePrefab != null)
                {
                    stoneObj = Instantiate(orbitingStonePrefab, transform.position, Quaternion.identity, transform);
                }
                else
                {
                    stoneObj = CreateFallbackOrbitingStone();
                    stoneObj.transform.SetParent(transform);
                }

                var orb = stoneObj.GetComponent<OrbitingStone>();
                orb.Initialize(transform, orbitRadius, orbitSpeed, angle);
                activeOrbitingStones.Add(orb);
            }
        }

        public void StartFight()
        {
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

        public void StartInfiniteFight() => StartFight();

        /// <summary>Restores the scene boss so it can appear again on each tenth wave.</summary>
        public void PrepareForInfiniteFight()
        {
            StopAllCoroutines();
            attackRoutine = null;
            flashCoroutine = null;
            currentHealth = maxHealth;
            contactDamage = baseContactDamage;
            hoverSpeed = baseHoverSpeed;
            orbitSpeed = baseOrbitSpeed;
            isEnraged = false;
            state = BossState.WaitingForCutscene;
            hoverBaseY = transform.position.y;

            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
            if (mainBodyRenderer != null) mainBodyRenderer.color = originalBodyColor;
            if (coreGlowRenderer != null) coreGlowRenderer.color = originalCoreGlowColor;
            if (worldBarContainer != null) worldBarContainer.SetActive(false);

            gameObject.SetActive(true);
            SpawnOrbitingStones();
            PlayIdleAnim();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
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

            if (bigRockPrefab != null)
            {
                GameObject rock = Instantiate(bigRockPrefab, spawnPos, Quaternion.identity);
                var comp = rock.GetComponent<BigRockProjectile>();
                if (comp != null)
                {
                    comp.Initialize(dir, isEnraged ? 11f : 8.5f);
                }
            }
            else
            {
                CreateFallbackBigRock(spawnPos, dir);
            }

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

            if (stoneWallPrefab != null)
            {
                Instantiate(stoneWallPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                CreateFallbackStoneWall(spawnPos);
            }

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
            OnAnyBossFightDefeated?.Invoke();

            if (InfiniteModeSession.IsActive)
            {
                InfiniteBossDefeated?.Invoke();
                gameObject.SetActive(false);
                yield break;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(GameManager.Instance.WaitAndTriggerVictory(1.8f));
            }

            Destroy(gameObject);
        }

        private GameObject CreateFallbackOrbitingStone()
        {
            GameObject obj = new GameObject("MiniOrbitingStone");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            sr.sortingOrder = 9;

            Texture2D tex = new Texture2D(12, 12, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[144];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.65f, 0.65f, 0.7f, 1f);
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 16f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.75f, 0.75f);

            obj.AddComponent<OrbitingStone>();
            return obj;
        }

        private void CreateFallbackBigRock(Vector3 pos, Vector2 dir)
        {
            GameObject rock = new GameObject("Boss_BigRock");
            rock.transform.position = pos;
            var sr = rock.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.58f, 0.65f, 1f);
            sr.sortingOrder = 8;

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[32 * 32];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.55f, 0.52f, 0.6f, 1f);
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

            var col = rock.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1f;

            var comp = rock.AddComponent<BigRockProjectile>();
            comp.Initialize(dir, isEnraged ? 10f : 8f);
        }

        private void CreateFallbackStoneWall(Vector3 centerPos)
        {
            GameObject wall = new GameObject("Boss_StoneWall");
            wall.transform.position = centerPos;
            wall.AddComponent<StoneWallObstacle>();

            float[] yOffsets = { -2.5f, 0f, 2.5f };
            foreach (float y in yOffsets)
            {
                GameObject seg = new GameObject("StoneSegment");
                seg.transform.SetParent(wall.transform);
                seg.transform.localPosition = new Vector3(0f, y, 0f);

                var sr = seg.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.5f, 0.48f, 0.55f, 1f);
                sr.sortingOrder = 5;

                Texture2D tex = new Texture2D(20, 36, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color[] cols = new Color[20 * 36];
                for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.5f, 0.48f, 0.55f, 1f);
                tex.SetPixels(cols);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 20, 36), new Vector2(0.5f, 0.5f), 16f);

                var col = seg.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.2f, 2.2f);

                seg.AddComponent<StoneWallSegment>();
            }
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
