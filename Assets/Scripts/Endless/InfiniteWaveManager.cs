using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Boss;
using WitchShmup.Core;
using WitchShmup.Cutscenes;
using WitchShmup.Enemies;
using WitchShmup.Environment;
using WitchShmup.Spawners;
using WitchShmup.UI;

namespace WitchShmup.Endless
{
    /// <summary>Wave flow for the isolated endless run. Campaign WaveSpawner stays unchanged.</summary>
    public class InfiniteWaveManager : MonoBehaviour
    {
        public static InfiniteWaveManager Instance { get; private set; }

        [Header("Wave Rules")]
        [SerializeField, Min(0f)] private float intermissionSeconds = 15f;
        [SerializeField, Min(0f)] private float spawnInterval = 0.65f;
        [SerializeField] private float spawnX = 11.5f;
        [SerializeField] private float minY = -3.5f;
        [SerializeField] private float maxY = 3.5f;

        private readonly Dictionary<GameObject, Queue<EnemyBase>> enemyPools = new Dictionary<GameObject, Queue<EnemyBase>>();
        private readonly Dictionary<EnemyBase, GameObject> enemySources = new Dictionary<EnemyBase, GameObject>();
        private readonly HashSet<EnemyBase> activeEnemies = new HashSet<EnemyBase>();
        private readonly List<InfiniteModeContentPack> contentPacks = new List<InfiniteModeContentPack>();

        private WaveSpawner campaignSpawner;
        private BossIntroCutscene bossCutscene;
        private GolemBoss sceneGolem;
        private HUDController hud;
        private TextMeshProUGUI waveStatusText;
        private GameObject waveStatusCanvas;
        private IInfiniteModeBoss activeBoss;
        private MonoBehaviour activeBossBehaviour;
        private GameObject activeBackground;
        private InfiniteModeContentPack currentPack;
        private Coroutine waveRoutine;
        private int currentWave = 1;
        private bool bossFightActive;

        public int CurrentWave => currentWave;
        public bool IsIntermission => isIntermission;
        public static bool IsRunActive => InfiniteModeSession.IsActive && Instance != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            campaignSpawner = FindFirstObjectByType<WaveSpawner>();
            if (campaignSpawner != null) campaignSpawner.enabled = false;

            bossCutscene = FindFirstObjectByType<BossIntroCutscene>();
            if (bossCutscene != null) sceneGolem = bossCutscene.Boss;
            if (sceneGolem == null) sceneGolem = FindFirstObjectByType<GolemBoss>();

            contentPacks.AddRange(Resources.LoadAll<InfiniteModeContentPack>("InfiniteMode"));
            contentPacks.Sort((a, b) => a.startingWave.CompareTo(b.startingWave));
        }

        private void Start()
        {
            hud = FindFirstObjectByType<HUDController>();
            waveStatusText = CreateWaveStatusText();
            SetStageTitle();
            BeginWave(1);
        }

        private void OnDestroy()
        {
            if (waveStatusCanvas != null) Destroy(waveStatusCanvas);
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (waveStatusText == null || waveRoutine != null || bossFightActive) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // A scaled delta means the countdown pauses with Escape and the evolution screen.
            if (intermissionRemaining > 0f)
            {
                if (InfiniteModeSession.AutoStartNextWave)
                {
                    intermissionRemaining = 0f;
                }
                else
                {
                    intermissionRemaining = Mathf.Max(0f, intermissionRemaining - Time.deltaTime);
                    waveStatusText.text = $"Nova onda em {Mathf.CeilToInt(intermissionRemaining)} segundos";
                }
            }

            if (intermissionRemaining <= 0f && isIntermission)
            {
                isIntermission = false;
                BeginWave(currentWave + 1);
            }
        }

        private float intermissionRemaining;
        private bool isIntermission;

        private void BeginWave(int wave)
        {
            currentWave = wave;
            InfiniteModeSession.RecordWave(currentWave);
            UpdateWaveLabel();
            currentPack = GetContentPackForWave(currentWave);
            ApplyScenarioBackground(currentPack);

            if (currentWave % 10 == 0)
            {
                StartBossWave();
            }
            else
            {
                waveRoutine = StartCoroutine(RunEnemyWave());
            }
        }

        private IEnumerator RunEnemyWave()
        {
            bossFightActive = false;
            isIntermission = false;
            int amountToSpawn = 5 + 2 * currentWave;
            int maxAlive = 10 + currentWave;
            int spawned = 0;

            while (spawned < amountToSpawn)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;

                if (activeEnemies.Count < maxAlive)
                {
                    if (SpawnRandomEnemy()) spawned++;
                }

                if (spawned < amountToSpawn)
                    yield return new WaitForSeconds(spawnInterval);
                else
                    yield return null;
            }

            while (activeEnemies.Count > 0)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
                yield return null;
            }

            waveRoutine = null;
            StartIntermission();
        }

        private void StartBossWave()
        {
            isIntermission = false;
            bossFightActive = true;
            waveStatusText.text = $"ONDA {currentWave} — CHEFE";

            if (currentPack != null && currentPack.bossPrefab != null)
            {
                var bossObject = Instantiate(currentPack.bossPrefab, new Vector3(5.8f, 0f, 0f), Quaternion.identity);
                StartConfiguredBoss(bossObject);
                return;
            }

            if (sceneGolem == null)
            {
                Debug.LogError("[InfiniteWaveManager] Nenhum boss foi configurado para a onda de chefe.");
                bossFightActive = false;
                StartIntermission();
                return;
            }

            sceneGolem.PrepareForInfiniteFight();
            StartConfiguredBoss(sceneGolem.gameObject);
            if (bossCutscene != null && sceneGolem == bossGolemFromCutscene())
            {
                bossCutscene.PlayCutscene();
            }
            else
            {
                activeBoss.StartInfiniteFight();
            }
        }

        private GolemBoss bossGolemFromCutscene() => bossCutscene != null ? bossCutscene.Boss : null;

        private void StartConfiguredBoss(GameObject bossObject)
        {
            activeBoss = bossObject.GetComponent<IInfiniteModeBoss>();
            if (activeBoss == null)
            {
                Debug.LogError($"[InfiniteWaveManager] {bossObject.name} precisa implementar IInfiniteModeBoss.");
                Destroy(bossObject);
                bossFightActive = false;
                StartIntermission();
                return;
            }

            activeBossBehaviour = activeBoss as MonoBehaviour;
            activeBoss.InfiniteBossDefeated += OnBossDefeated;
        }

        private void OnBossDefeated()
        {
            if (activeBoss != null) activeBoss.InfiniteBossDefeated -= OnBossDefeated;
            if (activeBossBehaviour != null && activeBossBehaviour.gameObject != (sceneGolem != null ? sceneGolem.gameObject : null))
            {
                Destroy(activeBossBehaviour.gameObject);
            }

            activeBoss = null;
            activeBossBehaviour = null;
            bossFightActive = false;
            if (GameManager.Instance != null && GameManager.Instance.BGM != null)
                GameManager.Instance.BGM.PlayNormalMusic();
            StartIntermission();
        }

        private void StartIntermission()
        {
            isIntermission = true;
            intermissionRemaining = InfiniteModeSession.AutoStartNextWave ? 0f : intermissionSeconds;
            if (waveStatusText != null)
                waveStatusText.text = InfiniteModeSession.AutoStartNextWave
                    ? $"ONDA {currentWave} concluída"
                    : $"Nova onda em {Mathf.CeilToInt(intermissionRemaining)} segundos";
        }

        private bool SpawnRandomEnemy()
        {
            GameObject[] choices = GetEnemyChoices(currentPack);
            if (choices == null || choices.Length == 0)
            {
                Debug.LogError("[InfiniteWaveManager] Nenhum prefab de inimigo está configurado.");
                return false;
            }

            GameObject prefab = choices[Random.Range(0, choices.Length)];
            if (prefab == null) return false;

            EnemyBase enemy = GetPooledEnemy(prefab);
            if (enemy == null) return false;

            Vector3 spawnPosition = new Vector3(spawnX, Random.Range(minY, maxY), 0f);
            enemy.gameObject.SetActive(true);
            float healthMultiplier = Mathf.Min(4f, 1f + 0.08f * (currentWave - 1));
            float speedMultiplier = Mathf.Min(1.6f, 1f + 0.01f * (currentWave - 1));
            enemy.ActivateFromInfinitePool(spawnPosition, healthMultiplier, speedMultiplier);
            activeEnemies.Add(enemy);
            return true;
        }

        private EnemyBase GetPooledEnemy(GameObject prefab)
        {
            if (!enemyPools.TryGetValue(prefab, out Queue<EnemyBase> pool))
            {
                pool = new Queue<EnemyBase>();
                enemyPools[prefab] = pool;
            }

            while (pool.Count > 0)
            {
                EnemyBase pooled = pool.Dequeue();
                if (pooled != null) return pooled;
            }

            GameObject created = Instantiate(prefab);
            EnemyBase enemy = created.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                Debug.LogError($"[InfiniteWaveManager] O prefab {prefab.name} não possui EnemyBase.");
                Destroy(created);
                return null;
            }

            enemy.InitializeInfinitePoolEntry();
            enemySources[enemy] = prefab;
            created.SetActive(false);
            return enemy;
        }

        public bool ReturnEnemyToPool(EnemyBase enemy)
        {
            if (enemy == null || !enemySources.TryGetValue(enemy, out GameObject prefab)) return false;

            activeEnemies.Remove(enemy);
            if (!enemyPools.TryGetValue(prefab, out Queue<EnemyBase> pool))
            {
                pool = new Queue<EnemyBase>();
                enemyPools[prefab] = pool;
            }

            enemy.PrepareForInfinitePoolReturn();
            enemy.gameObject.SetActive(false);
            pool.Enqueue(enemy);
            return true;
        }

        private GameObject[] GetEnemyChoices(InfiniteModeContentPack pack)
        {
            if (pack != null && pack.enemyPrefabs != null && pack.enemyPrefabs.Length > 0)
                return pack.enemyPrefabs;

            if (campaignSpawner != null)
                return campaignSpawner.GetInfiniteModeEnemyPrefabs();

            return null;
        }

        private InfiniteModeContentPack GetContentPackForWave(int wave)
        {
            InfiniteModeContentPack selected = null;
            for (int i = 0; i < contentPacks.Count; i++)
            {
                if (contentPacks[i] != null && contentPacks[i].startingWave <= wave)
                    selected = contentPacks[i];
            }
            return selected;
        }

        private void ApplyScenarioBackground(InfiniteModeContentPack pack)
        {
            if (pack == null || pack.backgroundPrefab == null) return;

            if (activeBackground != null) Destroy(activeBackground);
            var existing = FindFirstObjectByType<ParallaxBackgroundController>();
            if (existing != null) existing.gameObject.SetActive(false);
            activeBackground = Instantiate(pack.backgroundPrefab, Vector3.zero, Quaternion.identity);
        }

        private void SetStageTitle()
        {
            if (hud == null) hud = FindFirstObjectByType<HUDController>();
            if (hud != null) hud.SetStageTitle("MODO INFINITO");
        }

        private void UpdateWaveLabel()
        {
            SetStageTitle();
            if (waveStatusText != null) waveStatusText.text = $"ONDA {currentWave}";
        }

        private TextMeshProUGUI CreateWaveStatusText()
        {
            // Keep this UI owned by the scene's wave manager. Reusing an arbitrary
            // canvas can parent the label under a DontDestroyOnLoad overlay.
            waveStatusCanvas = new GameObject("InfiniteModeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            waveStatusCanvas.transform.SetParent(transform, false);
            Canvas canvas = waveStatusCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var textObject = new GameObject("InfiniteWaveStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -38f);
            rect.sizeDelta = new Vector2(520f, 42f);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 26f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.86f, 0.38f, 1f);
            text.raycastTarget = false;
            return text;
        }
    }
}
