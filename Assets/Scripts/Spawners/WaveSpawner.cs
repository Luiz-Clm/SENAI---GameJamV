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
        [SerializeField] private float stageDuration = 35f;
        [SerializeField] private float spawnIntervalMin = 1.2f;
        [SerializeField] private float spawnIntervalMax = 2.5f;

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

        private void Update()
        {
            if (!isSpawning) return;

            stageTimer += Time.deltaTime;
            if (stageTimer >= stageDuration && !bossTriggered)
            {
                TriggerBossSequence();
            }

            // Atalho de debug para Game Jam: Pressionar 'B' chama o Boss imediatamente!
            if (Input.GetKeyDown(KeyCode.B) && !bossTriggered)
            {
                Debug.Log("[WaveSpawner] Debug Boss Trigger ativado via tecla B!");
                TriggerBossSequence();
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(1.5f);

            while (isSpawning)
            {
                SpawnRandomEnemy();

                float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnRandomEnemy()
        {
            float randomY = Random.Range(minY, maxY);
            Vector3 spawnPos = new Vector3(spawnX, randomY, 0f);

            // 50% chance de Rusher, 50% chance de Bat
            bool spawnRusher = (Random.value > 0.5f);

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
