using System.Collections;
using UnityEngine;
using WitchShmup.Cutscenes;
using WitchShmup.Enemies;

namespace WitchShmup.Spawners
{
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Prefabs de Inimigos")]
        [SerializeField] private GameObject rusherPrefab;
        [SerializeField] private GameObject batPrefab;

        [Header("Duração da Fase")]
        [Tooltip("Tempo em segundos de ondas antes de chamar o Boss")]
        [SerializeField] private float stageDuration = 150f;

        [Header("Balanceamento de Spawn (Ajuste no Inspector)")]
        [Tooltip("Intervalo mínimo inicial entre spawns")]
        [SerializeField] private float spawnIntervalMin = 0.65f;
        [Tooltip("Intervalo máximo inicial entre spawns")]
        [SerializeField] private float spawnIntervalMax = 1.35f;
        [Tooltip("Intervalo mínimo no final da fase (mais frenético)")]
        [SerializeField] private float endSpawnIntervalMin = 0.35f;
        [Tooltip("Intervalo máximo no final da fase (mais frenético)")]
        [SerializeField] private float endSpawnIntervalMax = 0.75f;

        [Header("Quantidade de Inimigos e Formações")]
        [Tooltip("Mínimo de inimigos por grupo")]
        [SerializeField] private int minEnemiesPerWave = 1;
        [Tooltip("Máximo de inimigos por grupo conforme o tempo passa (escala com a fase)")]
        [SerializeField] private int maxEnemiesPerWave = 4;
        [Tooltip("Chance (0 a 1) de gerar inimigos em enxame/formação alinhada")]
        [Range(0f, 1f)]
        [SerializeField] private float swarmChance = 0.40f;

        [Header("Spawn Position Bounds")]
        [SerializeField] private float spawnX = 11.5f;
        [SerializeField] private float minY = -3.5f;
        [SerializeField] private float maxY = 3.5f;

        [Header("Boss Transition")]
        [SerializeField] private BossIntroCutscene bossCutscene;
        [SerializeField] private float delayBeforeBossIntro = 3f;

        private float stageTimer = 0f;
        private bool isSpawning = true;
        private bool bossTriggered = false;

        private void Start()
        {
            if (bossCutscene == null)
            {
                bossCutscene = FindFirstObjectByType<BossIntroCutscene>();
            }

            StartCoroutine(SpawnLoop());
        }

        private string cheatBuffer = "";
        private const string CHEAT_BOSS = "BOSS";

        private void Update()
        {
            if (!isSpawning) return;

            stageTimer += Time.deltaTime;
            if (stageTimer >= stageDuration && !bossTriggered)
            {
                TriggerBossSequence();
            }

            // Cheat Code estilo GTA: Digitar 'B' - 'O' - 'S' - 'S' em sequência durante a gameplay
            if (!bossTriggered && Input.anyKeyDown)
            {
                foreach (char c in Input.inputString)
                {
                    if (char.IsLetter(c))
                    {
                        cheatBuffer += char.ToUpper(c);
                        if (cheatBuffer.Length > 10)
                        {
                            cheatBuffer = cheatBuffer.Substring(cheatBuffer.Length - 10);
                        }

                        if (cheatBuffer.EndsWith(CHEAT_BOSS))
                        {
                            Debug.Log("<color=yellow>★ [CHEAT ATIVADO] Pular diretamente para o Boss! ★</color>");
                            cheatBuffer = "";
                            ClearAllActiveEnemies();
                            TriggerBossSequence();
                            return;
                        }
                    }
                }
            }

            // Atalho rápido adicional: Pressionar a tecla 'B'
            if (Input.GetKeyDown(KeyCode.B) && !bossTriggered)
            {
                Debug.Log("[WaveSpawner] Atalho tecla 'B': Invocando Boss imediatamente!");
                ClearAllActiveEnemies();
                TriggerBossSequence();
            }
        }

        private void ClearAllActiveEnemies()
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(1.0f);

            while (isSpawning)
            {
                float progress = Mathf.Clamp01(stageTimer / stageDuration);

                // Conforme o tempo avança e o jogador acumula upgrades, aparecem mais inimigos
                int currentMaxEnemies = Mathf.RoundToInt(Mathf.Lerp(minEnemiesPerWave, maxEnemiesPerWave, progress));
                int enemiesToSpawn = Random.Range(minEnemiesPerWave, currentMaxEnemies + 1);

                bool isSwarm = (Random.value < swarmChance && progress > 0.20f && enemiesToSpawn > 1);

                if (isSwarm)
                {
                    // Spawn em formação alinhada verticalmente
                    float stepY = (maxY - minY) / (enemiesToSpawn + 1);
                    for (int i = 0; i < enemiesToSpawn; i++)
                    {
                        float y = Mathf.Clamp(minY + stepY * (i + 1) + Random.Range(-0.3f, 0.3f), minY, maxY);
                        SpawnEnemyAt(new Vector3(spawnX + (i * 0.5f), y, 0f));
                    }
                }
                else
                {
                    for (int i = 0; i < enemiesToSpawn; i++)
                    {
                        SpawnRandomEnemy();
                        if (enemiesToSpawn > 1)
                        {
                            yield return new WaitForSeconds(0.14f);
                        }
                    }
                }

                // Frequência de spawn acelera dinamicamente conforme a fase avança
                float curMin = Mathf.Lerp(spawnIntervalMin, endSpawnIntervalMin, progress);
                float curMax = Mathf.Lerp(spawnIntervalMax, endSpawnIntervalMax, progress);

                float interval = Random.Range(curMin, curMax);
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnRandomEnemy()
        {
            float randomY = Random.Range(minY, maxY);
            SpawnEnemyAt(new Vector3(spawnX, randomY, 0f));
        }

        private void SpawnEnemyAt(Vector3 spawnPos)
        {
            // Progressão: início mais Rushers simples, depois mais Morcegos e formações mistas
            float batChance = (stageTimer < 25f) ? 0.35f : (stageTimer < 60f) ? 0.55f : 0.70f;
            bool spawnRusher = (Random.value > batChance);

            if (spawnRusher)
            {
                if (rusherPrefab != null)
                {
                    Instantiate(rusherPrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    CreateFallbackRusher(spawnPos);
                }
            }
            else
            {
                if (batPrefab != null)
                {
                    Instantiate(batPrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    CreateFallbackBat(spawnPos);
                }
            }
        }

        private void TriggerBossSequence()
        {
            bossTriggered = true;
            isSpawning = false;
            StartCoroutine(WaitAndTriggerBoss());
        }

        private IEnumerator WaitAndTriggerBoss()
        {
            yield return new WaitForSeconds(delayBeforeBossIntro);

            if (bossCutscene != null)
            {
                bossCutscene.PlayCutscene();
            }
        }

        private void CreateFallbackRusher(Vector3 pos)
        {
            GameObject obj = new GameObject("Enemy_Rusher");
            obj.tag = "Enemy";
            obj.transform.position = pos;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.9f, 0.3f, 0.3f, 1f); // Vermelho de perigo
            sr.sortingOrder = 8;

            Texture2D tex = new Texture2D(18, 14, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[18 * 14];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.9f, 0.3f, 0.3f, 1f);
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 18, 14), new Vector2(0.5f, 0.5f), 16f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, 0.8f);

            obj.AddComponent<RusherEnemy>();
        }

        private void CreateFallbackBat(Vector3 pos)
        {
            GameObject obj = new GameObject("Enemy_Bat");
            obj.tag = "Enemy";
            obj.transform.position = pos;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.55f, 0.35f, 0.75f, 1f); // Roxo do morcego
            sr.sortingOrder = 8;

            Texture2D tex = new Texture2D(20, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[20 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.55f, 0.35f, 0.75f, 1f);
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 20, 16), new Vector2(0.5f, 0.5f), 16f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 0.9f);

            obj.AddComponent<BatEnemy>();
        }
    }
}
