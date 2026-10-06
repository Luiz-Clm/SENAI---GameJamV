using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.UI;

namespace WitchShmup.Boss
{
    public class GolemBoss : MonoBehaviour, IDamageable
    {
        public enum BossState
        {
            WaitingForCutscene,
            Intro,
            Active,
            Dying
        }

        [Header("Boss Stats")]
        [SerializeField] private float maxHealth = 70f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float contactDamage = 1f;

        [Header("Hover Movement")]
        [SerializeField] private float hoverSpeed = 1.5f;
        [SerializeField] private float hoverAmplitude = 1.8f;
        [SerializeField] private float targetArenaX = 5.8f;

        [Header("Mini Orbiting Stones")]
        [SerializeField] private int orbitingStoneCount = 8;
        [SerializeField] private float orbitRadius = 2.8f;
        [SerializeField] private float orbitSpeed = 70f;
        [SerializeField] private GameObject orbitingStonePrefab;
        private List<OrbitingStone> activeOrbitingStones = new List<OrbitingStone>();

        [Header("Special Attacks")]
        [SerializeField] private GameObject bigRockPrefab;
        [SerializeField] private GameObject stoneWallPrefab;
        [SerializeField] private Transform attackSpawnPoint;
        [SerializeField] private float attackInterval = 3.5f;

        [Header("Enrage Phase (< 50% HP)")]
        [SerializeField] private SpriteRenderer coreGlowRenderer;
        [SerializeField] private Color enrageColor = new Color(1f, 0.2f, 0.1f, 1f);

        [Header("Visual Flash & FX")]
        [SerializeField] private SpriteRenderer mainBodyRenderer;
        [SerializeField] private GameObject explosionEffectPrefab;

        private BossState state = BossState.WaitingForCutscene;
        private Transform playerTarget;
        private float hoverBaseY = 0f;
        private bool isEnraged = false;
        private Coroutine attackRoutine;
        private HUDController hud;

        public BossState State => state;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action OnBossDefeated;

        private void Awake()
        {
            currentHealth = maxHealth;
            hoverBaseY = transform.position.y;
            if (mainBodyRenderer == null)
            {
                mainBodyRenderer = GetComponentInChildren<SpriteRenderer>();
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
            if (hud != null)
            {
                hud.SetBossHealth(1f);
            }
            attackRoutine = StartCoroutine(AttackLoop());
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
                // Alterna entre arremessar Pedra Gigante e Parede de Pedra
                if (attackCounter % 2 == 0)
                {
                    yield return StartCoroutine(PerformBigRockAttack());
                }
                else
                {
                    yield return StartCoroutine(PerformStoneWallAttack());
                }

                attackCounter++;
                float delay = isEnraged ? attackInterval * 0.7f : attackInterval;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PerformBigRockAttack()
        {
            // Aviso visual (aviso de carregar pedra)
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
        }

        private IEnumerator PerformStoneWallAttack()
        {
            // O Golem ergue uma barreira de pedras
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
        }

        public void TakeDamage(float amount, ElementType element)
        {
            if (state != BossState.Active || currentHealth <= 0) return;

            // Fogo ou Gelo causam bom dano
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (hud != null)
            {
                hud.SetBossHealth(currentHealth / maxHealth);
            }

            // Ativa modo furioso ao chegar a 50% de HP
            if (!isEnraged && currentHealth <= (maxHealth * 0.5f))
            {
                EnterEnrageMode();
            }

            StartCoroutine(FlashRoutine());

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
                Color prev = mainBodyRenderer.color;
                mainBodyRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);
                yield return new WaitForSeconds(0.06f);
                mainBodyRenderer.color = prev;
            }
        }

        private void Die()
        {
            state = BossState.Dying;
            if (attackRoutine != null) StopCoroutine(attackRoutine);

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

            OnBossDefeated?.Invoke();
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
