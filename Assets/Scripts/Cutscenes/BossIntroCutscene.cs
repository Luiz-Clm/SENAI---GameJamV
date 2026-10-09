using System;
using System.Collections;
using UnityEngine;
using WitchShmup.Boss;
using WitchShmup.CameraSystem;
using WitchShmup.Enemies;
using WitchShmup.Environment;
using WitchShmup.Player;
using WitchShmup.UI;

namespace WitchShmup.Cutscenes
{
    public class BossIntroCutscene : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GolemBoss golemBoss;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerShooting playerShooting;
        [SerializeField] private Transform playerTransform;

        [Header("Scared Wizard FX")]
        [SerializeField] private GameObject scaredEmotePrefab;
        [SerializeField] private float wizardTrembleIntensity = 0.08f;

        [Header("Boss Entrance")]
        [SerializeField] private Vector3 bossOffscreenPos = new Vector3(14f, 0f, 0f);
        [SerializeField] private Vector3 bossArenaPos = new Vector3(5.8f, 0f, 0f);
        [SerializeField] private float bossSlideSpeed = 4f;

        [Header("Roar / Scream FX")]
        [SerializeField] private float roarDuration = 1.8f;
        [SerializeField] private float roarShakeIntensity = 0.45f;
        [SerializeField] private GameObject roarShockwavePrefab;

        private GameObject spawnedEmote;
        private bool isCutsceneRunning = false;

        public event Action OnCutsceneFinished;

        private void Start()
        {
            if (playerController == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null)
                {
                    playerController = p.GetComponent<PlayerController>();
                    playerShooting = p.GetComponent<PlayerShooting>();
                    playerTransform = p.transform;
                }
            }

            if (golemBoss == null)
            {
                golemBoss = FindFirstObjectByType<GolemBoss>();
            }

            // Garante que o Boss comece fora da tela antes da cutscene
            if (golemBoss != null && !isCutsceneRunning)
            {
                golemBoss.transform.position = bossOffscreenPos;
            }
        }

        public void PlayCutscene()
        {
            if (isCutsceneRunning) return;
            StartCoroutine(CutsceneSequence());
        }

        private IEnumerator CutsceneSequence()
        {
            isCutsceneRunning = true;

            // 1. Pausa o scrolling do cenário
            if (ParallaxBackgroundController.Instance != null)
            {
                ParallaxBackgroundController.Instance.TransitionScrollSpeed(0f, 1.2f);
            }

            // 2. Protege o player contra qualquer dano durante a cutscene
            PlayerHealth playerHealth = (playerTransform != null) ? playerTransform.GetComponent<PlayerHealth>() : null;
            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(true);
            }

            // 3. Limpa todos os inimigos menores e projéteis da tela: a arena fica livre pro Boss!
            EnemyBase[] activeEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && !enemy.CompareTag("Boss"))
                {
                    enemy.enabled = false;
                    Destroy(enemy.gameObject);
                }
            }

            WitchShmup.Combat.Projectile[] activeProjectiles = FindObjectsByType<WitchShmup.Combat.Projectile>(FindObjectsSortMode.None);
            foreach (var proj in activeProjectiles)
            {
                if (proj != null)
                {
                    Destroy(proj.gameObject);
                }
            }

            // 4. Trava controles do jogador temporariamente
            if (playerController != null) playerController.enabled = false;
            if (playerShooting != null) playerShooting.enabled = false;

            // 5. Move o jogador suavemente para a posição segura à esquerda
            if (playerTransform != null)
            {
                Vector3 startPlayerPos = playerTransform.position;
                Vector3 targetPlayerPos = new Vector3(-6f, 0f, startPlayerPos.z);
                float moveT = 0f;
                while (moveT < 1f)
                {
                    moveT += Time.deltaTime * 2f;
                    playerTransform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, moveT);
                    yield return null;
                }
            }

            // 4. Mago assustado: surge emote de medo / "!" e ele treme!
            Vector3 wizardBasePos = (playerTransform != null) ? playerTransform.position : Vector3.zero;
            if (scaredEmotePrefab != null && playerTransform != null)
            {
                spawnedEmote = Instantiate(scaredEmotePrefab, playerTransform.position + new Vector3(0f, 1.2f, 0f), Quaternion.identity, playerTransform);
            }
            else if (playerTransform != null)
            {
                spawnedEmote = CreateFallbackScaredEmote(playerTransform);
            }

            // 5. O Golem surge lentamente vindo da direita
            if (golemBoss != null)
            {
                golemBoss.transform.position = bossOffscreenPos;
                golemBoss.gameObject.SetActive(true);

                // O mago treme enquanto o Golem entra
                float distance = Vector3.Distance(bossOffscreenPos, bossArenaPos);
                while (Vector3.Distance(golemBoss.transform.position, bossArenaPos) > 0.05f)
                {
                    golemBoss.transform.position = Vector3.MoveTowards(golemBoss.transform.position, bossArenaPos, bossSlideSpeed * Time.deltaTime);

                    // Mago tremendo de medo
                    if (playerTransform != null)
                    {
                        Vector2 tremble = UnityEngine.Random.insideUnitCircle * wizardTrembleIntensity;
                        playerTransform.position = wizardBasePos + new Vector3(tremble.x, tremble.y, 0f);
                    }

                    yield return null;
                }
                golemBoss.transform.position = bossArenaPos;
            }

            // 6. O Boss Ruge / Grita! (Screen Shake intenso)
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(roarDuration, roarShakeIntensity);
            }

            if (roarShockwavePrefab != null && golemBoss != null)
            {
                Instantiate(roarShockwavePrefab, golemBoss.transform.position, Quaternion.identity);
            }

            // Mago ainda tremendo durante o rugido
            float roarTimer = 0f;
            while (roarTimer < roarDuration)
            {
                if (playerTransform != null)
                {
                    Vector2 tremble = UnityEngine.Random.insideUnitCircle * (wizardTrembleIntensity * 1.5f);
                    playerTransform.position = wizardBasePos + new Vector3(tremble.x, tremble.y, 0f);
                }
                roarTimer += Time.deltaTime;
                yield return null;
            }

            if (playerTransform != null)
            {
                playerTransform.position = wizardBasePos;
            }

            // 7. Remove emote de medo e devolve o controle ao jogador
            if (spawnedEmote != null)
            {
                Destroy(spawnedEmote);
            }

            if (playerController != null) playerController.enabled = true;
            if (playerShooting != null) playerShooting.enabled = true;

            // 8. Inicia a luta contra o Boss!
            if (golemBoss != null)
            {
                golemBoss.StartFight();
            }

            // Breve intervalo de graça (0.5s) antes de remover a invulnerabilidade
            yield return new WaitForSeconds(0.5f);
            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(false);
            }

            isCutsceneRunning = false;
            OnCutsceneFinished?.Invoke();
        }

        private GameObject CreateFallbackScaredEmote(Transform parent)
        {
            GameObject emote = new GameObject("Scared_Emote");
            emote.transform.SetParent(parent);
            emote.transform.localPosition = new Vector3(0.4f, 1.2f, 0f);

            var sr = emote.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.9f, 0.2f, 1f); // Amarelo brilhante
            sr.sortingOrder = 15;

            // Pixel art "!" de susto com gotas de suor
            Texture2D tex = new Texture2D(12, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[12 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

            Color alert = new Color(1f, 0.85f, 0.1f, 1f);
            Color outline = new Color(0.2f, 0.1f, 0f, 1f);

            // Exclamação
            for (int y = 5; y <= 14; y++)
            {
                for (int x = 4; x <= 6; x++)
                {
                    cols[y * 12 + x] = alert;
                }
            }
            // Ponto
            for (int y = 1; y <= 3; y++)
            {
                for (int x = 4; x <= 6; x++)
                {
                    cols[y * 12 + x] = alert;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 12, 16), new Vector2(0.5f, 0.5f), 16f);

            return emote;
        }
    }
}
