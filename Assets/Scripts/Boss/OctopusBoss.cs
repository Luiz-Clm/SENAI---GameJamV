using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchShmup.Core;
using WitchShmup.Environment;
using WitchShmup.Player;
using WitchShmup.UI;

namespace WitchShmup.Boss
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class OctopusBoss : MonoBehaviour, WitchShmup.Combat.IDamageable
    {
        [Header("Vida e entrada")]
        [SerializeField, Min(1f)] private float maxHealth = 300f;
        [SerializeField] private string bossDisplayName = "POLVO DAS PROFUNDEZAS";
        [SerializeField] private GameObject overheadHealthBar;
        [SerializeField, Min(0f)] private float overheadBarFullWidth = 2f;
        [SerializeField] private float contactDamage = 2f;
        [SerializeField] private float arenaXOffset = 5.2f;
        [SerializeField] private float entranceDuration = 1.1f;
        [SerializeField] private float verticalAmplitude = 0.8f;
        [SerializeField] private float verticalFrequency = 1.25f;
        [SerializeField] private float floodWarningDuration = 2f;
        [SerializeField] private float waterRiseDuration = 4f;
        [SerializeField] private GameObject bossCoreDropPrefab;

        [Header("Ataques e projéteis")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject wingedBookPrefab;
        [SerializeField] private Transform attackSpawnPoint;
        [SerializeField] private Vector2 attackPause = new Vector2(1.5f, 2.5f);
        [SerializeField] private float inkProjectileSpeed = 6f;
        [SerializeField] private float aimedInkSpeed = 7.2f;
        [SerializeField] private float inkBombSpeed = 4.2f;
        [SerializeField] private float bookSpeed = 5.5f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private float blindnessDuration = 4f;
        [SerializeField, Range(0f, 80f)] private float fanArcDegrees = 56f;
        [SerializeField, Min(3)] private int fanProjectileCount = 5;
        [SerializeField] private float aimedInkTelegraphDuration = 0.75f;
        [SerializeField] private float phaseThreeAttackSpeedMultiplier = 0.7f;
        [SerializeField] private float inkProjectileScale = 0.85f;
        [SerializeField] private float inkBombScale = 1.2f;
        [SerializeField] private float bookProjectileScale = 0.85f;

        [Header("Bolhas de oxigênio")]
        [SerializeField] private GameObject airBubblePrefab;
        [SerializeField] private float smallBubbleInterval = 5f;
        [SerializeField] private float largeBubbleInterval = 20f;
        [SerializeField] private int maximumBubblesOnScreen = 6;
        [SerializeField, Range(0f, 1f)] private float smallBubbleOxygen = 0.4f;
        [SerializeField, Range(0f, 1f)] private float largeBubbleOxygen = 1f;

        [Header("Corrente de afogamento - Fase 3")]
        [SerializeField] private float undertowDuration = 1.5f;
        [SerializeField] private float undertowDownwardSpeed = 4f;
        [SerializeField, Range(0f, 1f)] private float undertowChance = 0.28f;

        [Header("Visual e Animações")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer bookRendererSource;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float animationFps = 6f;
        [SerializeField] private WitchShmup.Utils.SimpleSpriteAnimator bossAnimator;

        private float currentHealth;
        private float bobSeed;
        private bool encounterStarted;
        private bool fightActive;
        private bool isDead;
        private float smallBubbleTimer;
        private float largeBubbleTimer;
        private Transform player;
        private PlayerController playerController;
        private HUDController hud;
        private PhaseTwoWaterSystem water;
        private Coroutine encounterRoutine;
        private Coroutine attackRoutine;
        private readonly List<OctopusAirBubble> airBubbles = new List<OctopusAirBubble>();
        private bool phaseOneFanNext = true;
        private int phaseTwoAttackIndex;
        private int phaseThreeAttackIndex;
        private Transform overheadFillTransform;
        private SpriteRenderer overheadFillRenderer;
        private Vector3 overheadFillBasePosition;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float ContactDamage => contactDamage;

        private enum AttackPhase { First, Second, Third }

        private void Awake()
        {
            currentHealth = maxHealth;
            bobSeed = Random.Range(0f, 100f);
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();

            if (bossAnimator == null) bossAnimator = GetComponent<WitchShmup.Utils.SimpleSpriteAnimator>();
            if (bossAnimator == null) bossAnimator = gameObject.AddComponent<WitchShmup.Utils.SimpleSpriteAnimator>();

            if (bossAnimator != null && idleFrames != null && idleFrames.Length > 0)
            {
                bossAnimator.SetFrames(idleFrames, animationFps);
            }

            BindOverheadHealthBar();

            if (bodyRenderer != null)
            {
                if (bodyRenderer.sprite == null && (idleFrames == null || idleFrames.Length == 0))
                    bodyRenderer.sprite = CreateOctopusSprite();
                bodyRenderer.color = normalColor;
                bodyRenderer.sortingOrder = 8;
            }

            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            water = PhaseTwoWaterSystem.Instance;
            hud = FindFirstObjectByType<HUDController>();
            FindPlayer();
        }

        private void FindPlayer()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) return;
            player = playerObject.transform;
            playerController = playerObject.GetComponent<PlayerController>();
        }

        public void BeginEncounter()
        {
            if (encounterStarted || isDead) return;
            encounterStarted = true;
            if (ParallaxBackgroundController.Instance != null)
                ParallaxBackgroundController.Instance.StopScrolling();
            encounterRoutine = StartCoroutine(EnterArena());
        }

        public void BeginEncounter(string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                bossDisplayName = displayName;
            BeginEncounter();
        }

        public void UseSceneOverheadHealthBar(GameObject sceneBar)
        {
            if (sceneBar == null) return;
            if (overheadHealthBar != null && overheadHealthBar != sceneBar)
                overheadHealthBar.SetActive(false);

            overheadHealthBar = sceneBar;
            overheadHealthBar.transform.SetParent(transform, false);
            overheadHealthBar.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            Transform fill = overheadHealthBar.transform.Find("Fill");
            overheadFillTransform = fill;
            overheadFillRenderer = fill != null ? fill.GetComponent<SpriteRenderer>() : null;
            if (fill != null) overheadFillBasePosition = fill.localPosition;
            overheadHealthBar.SetActive(false);
        }

        private void BindOverheadHealthBar()
        {
            if (overheadHealthBar == null)
            {
                Transform child = transform.Find("OverheadHealthBar");
                if (child != null) overheadHealthBar = child.gameObject;
            }

            if (overheadHealthBar == null) return;

            Transform fill = overheadHealthBar.transform.Find("Fill");
            overheadFillTransform = fill;
            overheadFillRenderer = fill != null ? fill.GetComponent<SpriteRenderer>() : null;
            if (fill != null) overheadFillBasePosition = fill.localPosition;
            overheadHealthBar.SetActive(false);
        }

        private void UpdateOverheadHealthBar(float normalizedHealth)
        {
            if (overheadFillTransform == null) return;
            normalizedHealth = Mathf.Clamp01(normalizedHealth);
            Vector3 scale = overheadFillTransform.localScale;
            scale.x = overheadBarFullWidth * normalizedHealth;
            overheadFillTransform.localScale = scale;

            float spriteWidth = overheadFillRenderer != null && overheadFillRenderer.sprite != null
                ? overheadFillRenderer.sprite.bounds.size.x
                : 1f;
            Vector3 position = overheadFillBasePosition;
            position.x -= spriteWidth * overheadBarFullWidth * (1f - normalizedHealth) * 0.5f;
            overheadFillTransform.localPosition = position;
        }

        private IEnumerator EnterArena()
        {
            water = PhaseTwoWaterSystem.Instance;
            if (water != null)
                yield return StartCoroutine(water.PrepareBossArena(floodWarningDuration, waterRiseDuration));

            Camera camera = Camera.main;
            float centerX = camera != null ? camera.transform.position.x : 0f;
            float centerY = camera != null ? camera.transform.position.y : 0f;
            float halfWidth = camera != null && camera.orthographic ? camera.orthographicSize * camera.aspect : 8f;
            float targetX = centerX + Mathf.Min(arenaXOffset, halfWidth - 0.75f);
            float targetY = centerY;
            Vector3 start = new Vector3(centerX + halfWidth + 2f, targetY, transform.position.z);
            Vector3 target = new Vector3(targetX, targetY, transform.position.z);
            transform.position = start;
            float elapsed = 0f;
            while (elapsed < entranceDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f,
                    elapsed / Mathf.Max(0.1f, entranceDuration)));
                yield return null;
            }

            transform.position = target;
            FindPlayer();
            hud = FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowBossBar(bossDisplayName);
                hud.SetBossHealth(1f);
            }

            if (overheadHealthBar != null)
            {
                overheadHealthBar.SetActive(true);
                UpdateOverheadHealthBar(1f);
            }

            fightActive = true;
            smallBubbleTimer = smallBubbleInterval;
            largeBubbleTimer = largeBubbleInterval;
            attackRoutine = StartCoroutine(AttackLoop());
            encounterRoutine = null;
        }

        private void Update()
        {
            if (!fightActive || isDead) return;
            Camera camera = Camera.main;
            float centerX = camera != null ? camera.transform.position.x : transform.position.x - arenaXOffset;
            float centerY = camera != null ? camera.transform.position.y : 0f;
            float halfWidth = camera != null && camera.orthographic ? camera.orthographicSize * camera.aspect : 8f;
            float x = centerX + Mathf.Min(arenaXOffset, halfWidth - 0.75f);
            float y = centerY + Mathf.Sin((Time.time + bobSeed) * verticalFrequency) * verticalAmplitude;
            transform.position = new Vector3(x, y, transform.position.z);
            UpdateAirBubbles(Time.deltaTime, camera, centerX, centerY, halfWidth);
        }

        private void UpdateAirBubbles(float deltaTime, Camera camera, float centerX, float centerY, float halfWidth)
        {
            airBubbles.RemoveAll(bubble => bubble == null);
            if (water == null || !water.IsBossFightActive) return;

            smallBubbleTimer -= deltaTime;
            largeBubbleTimer -= deltaTime;
            if (largeBubbleTimer <= 0f && airBubbles.Count < maximumBubblesOnScreen)
            {
                SpawnAirBubble(largeBubbleOxygen, true, centerX, centerY, halfWidth);
                largeBubbleTimer = largeBubbleInterval;
            }
            if (smallBubbleTimer <= 0f && airBubbles.Count < maximumBubblesOnScreen)
            {
                SpawnAirBubble(smallBubbleOxygen, false, centerX, centerY, halfWidth);
                smallBubbleTimer = smallBubbleInterval;
            }
        }

        private void SpawnAirBubble(float oxygenFraction, bool large, float centerX, float centerY, float halfWidth)
        {
            float halfHeight = Camera.main != null && Camera.main.orthographic ? Camera.main.orthographicSize : 5f;
            float x = Random.Range(centerX - halfWidth + 0.5f, centerX + halfWidth - 0.5f);
            float y = Random.Range(centerY - halfHeight + 0.5f, centerY + 0.25f);
            GameObject bubbleObject = airBubblePrefab != null
                ? Instantiate(airBubblePrefab, new Vector3(x, y, 0f), Quaternion.identity)
                : CreateAirBubbleObject(new Vector3(x, y, 0f));
            if (bubbleObject.GetComponent<SpriteRenderer>() == null)
                bubbleObject.AddComponent<SpriteRenderer>();
            if (bubbleObject.GetComponent<Collider2D>() == null)
            {
                var collider = bubbleObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.42f;
            }
            if (bubbleObject.GetComponent<Rigidbody2D>() == null)
                bubbleObject.AddComponent<Rigidbody2D>();
            var bubble = bubbleObject.GetComponent<OctopusAirBubble>();
            if (bubble == null) bubble = bubbleObject.AddComponent<OctopusAirBubble>();
            bubble.Initialize(oxygenFraction, large);
            airBubbles.Add(bubble);
        }

        private GameObject CreateAirBubbleObject(Vector3 position)
        {
            var bubble = new GameObject("Boss_Air_Bubble");
            bubble.transform.position = position;
            var renderer = bubble.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateBubbleSprite();
            renderer.color = Color.white;
            var collider = bubble.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;
            bubble.AddComponent<Rigidbody2D>();
            bubble.AddComponent<OctopusAirBubble>();
            return bubble;
        }

        private IEnumerator AttackLoop()
        {
            while (fightActive && !isDead)
            {
                var phase = GetAttackPhase();
                if (player == null) FindPlayer();

                if (phase == AttackPhase.First)
                {
                    if (phaseOneFanNext) FireInkFan();
                    else yield return StartCoroutine(FireAimedInk());
                    phaseOneFanNext = !phaseOneFanNext;
                }
                else if (phase == AttackPhase.Second)
                {
                    int attack = phaseTwoAttackIndex++ % 3;
                    if (attack == 0) FireInkBomb();
                    else if (attack == 1) FireBook();
                    else yield return StartCoroutine(FireAimedInk());
                }
                else
                {
                    if (Random.value < undertowChance)
                    {
                        if (playerController == null) FindPlayer();
                        if (playerController != null)
                            playerController.ApplyUndertow(undertowDuration, undertowDownwardSpeed);
                    }
                    else
                    {
                        int attack = phaseThreeAttackIndex++ % 4;
                        if (attack == 0) FireInkBomb();
                        else if (attack == 1) FireBook();
                        else if (attack == 2) FireInkFan();
                        else yield return StartCoroutine(FireAimedInk());
                    }
                }

                float phaseSpeed = phase == AttackPhase.Third ? phaseThreeAttackSpeedMultiplier : 1f;
                yield return new WaitForSeconds(Random.Range(attackPause.x, attackPause.y) * phaseSpeed);
            }
        }

        private AttackPhase GetAttackPhase()
        {
            float healthFraction = maxHealth <= 0f ? 0f : currentHealth / maxHealth;
            if (healthFraction > 0.66f) return AttackPhase.First;
            return healthFraction > 0.35f ? AttackPhase.Second : AttackPhase.Third;
        }

        private Vector3 GetAttackOrigin()
        {
            return attackSpawnPoint != null ? attackSpawnPoint.position : transform.position + Vector3.left * 1.25f;
        }

        private Vector2 DirectionToPlayer(Vector3 origin)
        {
            if (player == null) FindPlayer();
            return player != null ? ((Vector2)player.position - (Vector2)origin).normalized : Vector2.left;
        }

        private void FireInkFan()
        {
            Vector3 origin = GetAttackOrigin();
            Vector2 aim = DirectionToPlayer(origin);
            int count = Mathf.Max(3, fanProjectileCount);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-fanArcDegrees * 0.5f, fanArcDegrees * 0.5f, t);
                SpawnProjectile(origin, Rotate(aim, angle), inkProjectileSpeed,
                    OctopusProjectile.AttackKind.Ink, null);
            }
        }

        private IEnumerator FireAimedInk()
        {
            Vector3 origin = GetAttackOrigin();
            GameObject telegraph = CreateTelegraph(origin);
            float elapsed = 0f;
            float blinkInterval = 0.12f;
            bool visible = true;
            while (elapsed < aimedInkTelegraphDuration && telegraph != null)
            {
                elapsed += Time.deltaTime;
                if (elapsed % blinkInterval < Time.deltaTime)
                {
                    visible = !visible;
                    var renderer = telegraph.GetComponent<SpriteRenderer>();
                    if (renderer != null) renderer.enabled = visible;
                }
                yield return null;
            }
            if (telegraph != null) Destroy(telegraph);
            SpawnProjectile(GetAttackOrigin(), DirectionToPlayer(GetAttackOrigin()), aimedInkSpeed,
                OctopusProjectile.AttackKind.Ink, null);
        }

        private GameObject CreateTelegraph(Vector3 position)
        {
            var telegraph = new GameObject("Octopus_Aimed_Ink_Telegraph");
            telegraph.transform.position = position;
            telegraph.transform.localScale = Vector3.one * 0.45f;
            var renderer = telegraph.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProjectileSprite();
            renderer.color = new Color(1f, 0.35f, 0.95f, 1f);
            renderer.sortingOrder = 9;
            return telegraph;
        }

        private void FireInkBomb()
        {
            Vector3 origin = GetAttackOrigin();
            SpawnProjectile(origin, DirectionToPlayer(origin), inkBombSpeed,
                OctopusProjectile.AttackKind.InkBomb, null);
        }

        private void FireBook()
        {
            Vector3 origin = GetAttackOrigin();
            Sprite sprite = bookRendererSource != null ? bookRendererSource.sprite : null;
            if (sprite == null && wingedBookPrefab != null)
            {
                var source = wingedBookPrefab.GetComponentInChildren<SpriteRenderer>();
                if (source != null) sprite = source.sprite;
            }
            SpawnProjectile(origin, DirectionToPlayer(origin), bookSpeed,
                OctopusProjectile.AttackKind.Book, sprite);
        }

        private GameObject SpawnProjectile(Vector3 position, Vector2 direction, float speed,
            OctopusProjectile.AttackKind kind, Sprite sprite)
        {
            GameObject projectileObject = projectilePrefab != null
                ? Instantiate(projectilePrefab, position, Quaternion.identity)
                : CreateProjectileObject(position, sprite);
            if (projectileObject.GetComponent<SpriteRenderer>() == null)
                projectileObject.AddComponent<SpriteRenderer>();
            if (projectileObject.GetComponent<Collider2D>() == null)
            {
                var collider = projectileObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.46f;
            }
            if (projectileObject.GetComponent<Rigidbody2D>() == null)
                projectileObject.AddComponent<Rigidbody2D>();
            var projectile = projectileObject.GetComponent<OctopusProjectile>();
            if (projectile == null) projectile = projectileObject.AddComponent<OctopusProjectile>();
            if (sprite == null && kind == OctopusProjectile.AttackKind.Book && bookRendererSource != null)
                sprite = bookRendererSource.sprite;
            projectile.Initialize(direction, speed, projectileDamage, kind, blindnessDuration, sprite);
            projectileObject.transform.localScale = kind == OctopusProjectile.AttackKind.InkBomb
                ? Vector3.one * inkBombScale
                : (kind == OctopusProjectile.AttackKind.Book ? Vector3.one * bookProjectileScale : Vector3.one * inkProjectileScale);
            return projectileObject;
        }

        private GameObject CreateProjectileObject(Vector3 position, Sprite sprite)
        {
            var projectile = new GameObject("Octopus_Attack_Projectile");
            projectile.transform.position = position;
            var renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetProjectileSprite();
            renderer.color = new Color(0.35f, 0.13f, 0.52f, 1f);
            renderer.sortingOrder = 10;
            var collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
            var body = projectile.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            projectile.AddComponent<OctopusProjectile>();
            return projectile;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        public void TakeDamage(float amount, WitchShmup.Combat.ElementType element)
        {
            if (isDead || !fightActive) return;
            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
            if (bodyRenderer != null)
                StartCoroutine(DamageFlash());
            if (hud != null) hud.SetBossHealth(maxHealth <= 0f ? 0f : currentHealth / maxHealth);
            UpdateOverheadHealthBar(maxHealth <= 0f ? 0f : currentHealth / maxHealth);
            if (currentHealth <= 0f) Die();
        }

        private IEnumerator DamageFlash()
        {
            if (bodyRenderer == null) yield break;
            bodyRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.08f);
            if (bodyRenderer != null && !isDead) bodyRenderer.color = normalColor;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            fightActive = false;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            if (hud != null) hud.HideBossBar();
            if (overheadHealthBar != null) overheadHealthBar.SetActive(false);
            if (water != null) water.EndBossFight();
            if (ParallaxBackgroundController.Instance != null)
                ParallaxBackgroundController.Instance.ResumeScrolling();
            if (bossCoreDropPrefab != null) Instantiate(bossCoreDropPrefab, transform.position, Quaternion.identity);
            GameManager.Instance?.AddScore(1500);
            foreach (var bubble in FindObjectsByType<OctopusAirBubble>(FindObjectsSortMode.None))
                if (bubble != null) Destroy(bubble.gameObject);
            foreach (var projectile in FindObjectsByType<OctopusProjectile>(FindObjectsSortMode.None))
                if (projectile != null) Destroy(projectile.gameObject);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(GameManager.Instance.WaitAndTriggerVictory(1.8f));
            }

            gameObject.SetActive(false);
        }

        private static Sprite GetProjectileSprite()
        {
            if (projectileSprite != null) return projectileSprite;
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                if (dx * dx + dy * dy <= 48f)
                    pixels[y * size + x] = ((x + y) % 3 == 0) ? new Color(0.62f, 0.27f, 0.78f) : new Color(0.34f, 0.12f, 0.49f);
                else pixels[y * size + x] = Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            projectileSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            return projectileSprite;
        }

        private static Sprite projectileSprite;
        private static Sprite bubbleSprite;

        private static Sprite CreateBubbleSprite()
        {
            if (bubbleSprite != null) return bubbleSprite;
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                float d = dx * dx + dy * dy;
                pixels[y * size + x] = d >= 29f && d <= 55f ? new Color(0.7f, 0.96f, 1f, 1f) : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            bubbleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            return bubbleSprite;
        }

        private static Sprite CreateOctopusSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            var outline = new Color(0.23f, 0.1f, 0.38f, 1f);
            var body = new Color(0.67f, 0.34f, 0.82f, 1f);
            var light = new Color(0.88f, 0.58f, 0.95f, 1f);

            Vector2[] starts = { new Vector2(19, 31), new Vector2(27, 29), new Vector2(37, 29), new Vector2(45, 31) };
            Vector2[] ends = { new Vector2(7, 4), new Vector2(22, 2), new Vector2(42, 2), new Vector2(57, 5) };
            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 previous = starts[i];
                Vector2 control = new Vector2((starts[i].x + ends[i].x) * 0.5f, 14f);
                for (int step = 1; step <= 12; step++)
                {
                    float t = step / 12f;
                    Vector2 next = (1f - t) * (1f - t) * starts[i] + 2f * (1f - t) * t * control + t * t * ends[i];
                    DrawThickLine(pixels, size, previous, next, 7, outline);
                    previous = next;
                }
            }
            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 previous = starts[i];
                Vector2 control = new Vector2((starts[i].x + ends[i].x) * 0.5f, 14f);
                for (int step = 1; step <= 12; step++)
                {
                    float t = step / 12f;
                    Vector2 next = (1f - t) * (1f - t) * starts[i] + 2f * (1f - t) * t * control + t * t * ends[i];
                    DrawThickLine(pixels, size, previous, next, 4, body);
                    previous = next;
                }
            }

            DrawEllipse(pixels, size, 32, 43, 21, 18, outline);
            DrawEllipse(pixels, size, 32, 44, 18, 15, body);
            DrawEllipse(pixels, size, 32, 51, 12, 5, light);
            DrawDisk(pixels, size, 25, 43, 4, Color.white);
            DrawDisk(pixels, size, 39, 43, 4, Color.white);
            DrawDisk(pixels, size, 26, 42, 2, outline);
            DrawDisk(pixels, size, 40, 42, 2, outline);
            DrawDisk(pixels, size, 27, 43, 1, Color.white);
            DrawDisk(pixels, size, 41, 43, 1, Color.white);
            for (int i = 0; i < 5; i++) DrawDisk(pixels, size, 19 + i * 6, 33, 1, light);

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        }

        private static void DrawDisk(Color[] pixels, int size, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if (x >= 0 && x < size && y >= 0 && y < size && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    pixels[y * size + x] = color;
        }

        private static void DrawEllipse(Color[] pixels, int size, int cx, int cy, int rx, int ry, Color color)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            for (int x = cx - rx; x <= cx + rx; x++)
                if (x >= 0 && x < size && y >= 0 && y < size &&
                    ((x - cx) * (x - cx)) / (float)(rx * rx) + ((y - cy) * (y - cy)) / (float)(ry * ry) <= 1f)
                    pixels[y * size + x] = color;
        }

        private static void DrawThickLine(Color[] pixels, int size, Vector2 a, Vector2 b, int width, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, steps == 0 ? 0f : i / (float)steps);
                DrawDisk(pixels, size, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), width / 2, color);
            }
        }
    }
}
