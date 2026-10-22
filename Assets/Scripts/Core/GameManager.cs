using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WitchShmup.Player;
using WitchShmup.UI;

namespace WitchShmup.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Score Stats")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;

        [Header("Evolution Materials")]
        [SerializeField] private int fireHeartsCount = 0;
        [SerializeField] private int bossCoresCount = 0;
        [SerializeField] private int skillPoints = 2; // Começa com 2 pontos como no esboço HTML
        [SerializeField] private bool isStaffEvolved = false;

        [Header("Inventário")]
        [SerializeField] private int healthPotionsInInventory = 0;

        [Header("Staff Evolution Requirements")]
        [SerializeField] private int fireHeartsRequired = 5;
        [SerializeField] private int bossCoresRequired = 1;

        [Header("UI References")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject evolutionPanel;

        [Header("Audio & BGM")]
        [SerializeField] private WitchShmup.Audio.BGMManager bgmManager;
        public WitchShmup.Audio.BGMManager BGM => bgmManager;

        // Skill Tree Levels
        public int LevelVitality { get; private set; } = 0;     // vit (max 5)
        public int LevelVeloz { get; private set; } = 0;        // vel (max 3)
        public int LevelFoco { get; private set; } = 0;         // foc (max 3)
        public int LevelDespertar { get; private set; } = 0;    // des (max 1)
        public int LevelPele { get; private set; } = 0;         // pel (max 3)
        public int LevelIma { get; private set; } = 0;          // ima (max 2)
        public int LevelSegundoFolego { get; private set; } = 0;// seg (max 1)
        public int LevelRapido { get; private set; } = 0;       // rap (max 4)
        public int LevelDuplo { get; private set; } = 0;        // dup (max 2)
        public int LevelPerfurante { get; private set; } = 0;   // per (max 1)
        public int LevelVelTiro { get; private set; } = 0;      // vel_tiro (max 3)
        public int LevelQtdTiro { get; private set; } = 0;      // qtd_tiro (max 2)
        public int LevelPodGelo { get; private set; } = 0;      // pod_gelo (max 3)
        public int LevelPodFogo { get; private set; } = 0;      // pod_fogo (max 3)

        [Header("Phase Progression")]
        [SerializeField] private int currentPhase = 1;
        [SerializeField] private int maxUnlockedPhase = 1;

        public bool IsGameOver { get; private set; } = false;
        public bool IsVictory { get; private set; } = false;
        public bool IsEvolutionOpen => evolutionPanel != null && evolutionPanel.activeSelf;
        public int CurrentPhase => currentPhase;
        public int MaxUnlockedPhase => maxUnlockedPhase;

        public float SoulMagnetMultiplier => 1f + (LevelIma * 0.30f);

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int FireHeartsCount => fireHeartsCount;
        public int BossCoresCount => bossCoresCount;
        public int SkillPoints => skillPoints;
        public bool IsStaffEvolved => isStaffEvolved;
        public int FireHeartsRequired => fireHeartsRequired;
        public int BossCoresRequired => bossCoresRequired;
        public int HealthPotionsInInventory => healthPotionsInInventory;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnSkillPointsChanged;
        public event Action<int, int> OnMaterialsChanged;
        public event Action<string, int> OnSkillUpgraded;
        public event Action OnStaffEvolved;
        public event Action OnGameOver;
        public event Action OnVictory;
        public event Action<int> OnHealthPotionsChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            highScore = PlayerPrefs.GetInt("WitchShmup_HighScore", 0);

            // Identifica a fase pela cena ativa
            DetectCurrentPhaseFromScene();

            // Carrega dados salvos
            LoadSavedData();

            // Garante que a cena possua um EventSystem ativo para cliques de botões funcionarem
            if (UnityEngine.EventSystems.EventSystem.current == null && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // Garante que o PauseUI exista na cena
            if (FindFirstObjectByType<PauseUI>() == null)
            {
                var pauseObj = new GameObject("PauseUI_Manager");
                pauseObj.AddComponent<PauseUI>();
            }

            // Garante que o BGMManager exista na cena
            if (bgmManager == null)
            {
                bgmManager = FindFirstObjectByType<WitchShmup.Audio.BGMManager>();
                if (bgmManager == null)
                {
                    var bgmObj = new GameObject("BGM_Manager");
                    bgmManager = bgmObj.AddComponent<WitchShmup.Audio.BGMManager>();
                }
            }
        }

        private void DetectCurrentPhaseFromScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.Equals("Fase", StringComparison.OrdinalIgnoreCase))
            {
                currentPhase = 1;
            }
            else if (sceneName.StartsWith("Fase", StringComparison.OrdinalIgnoreCase))
            {
                string numStr = sceneName.Substring(4);
                if (int.TryParse(numStr, out int p))
                {
                    currentPhase = p;
                }
            }
        }

        private void LoadSavedData()
        {
            var data = SaveSystem.LoadGame();
            if (data != null)
            {
                skillPoints = data.skillPoints;
                fireHeartsCount = data.fireHeartsCount;
                bossCoresCount = data.bossCoresCount;
                healthPotionsInInventory = data.healthPotionsInInventory;
                isStaffEvolved = data.isStaffEvolved;
                maxUnlockedPhase = Mathf.Max(currentPhase, data.maxUnlockedPhase);

                // Carrega níveis da árvore
                LevelVitality = data.levelVit;
                LevelVeloz = data.levelVel;
                LevelVelTiro = data.levelVel;
                LevelFoco = data.levelFoc;
                LevelPodGelo = data.levelFoc;
                LevelDespertar = data.levelDes;
                LevelPele = data.levelPel;
                LevelIma = data.levelIma;
                LevelSegundoFolego = data.levelSeg;
                LevelRapido = data.levelRap;
                LevelDuplo = data.levelDup;
                LevelQtdTiro = data.levelDup;
                LevelPerfurante = data.levelPer;
                LevelPodFogo = data.levelPer;

                if (data.highScore > highScore)
                {
                    highScore = data.highScore;
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<HUDController>();
            }

            if (hudController != null)
            {
                hudController.SetScore(currentScore);
                hudController.UpdatePotionCount(healthPotionsInInventory);
                hudController.SetStageTitle($"FASE {currentPhase} - RUÍNAS DE PEDRA");
            }

            var playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDeath += TriggerGameOver;
            }

            // Auto-associa painéis caso a referência no inspector esteja vazia
            if (gameOverPanel == null)
            {
                var goUi = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
                if (goUi != null) gameOverPanel = goUi.gameObject;
            }
            if (victoryPanel == null)
            {
                var vicUi = FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
                if (vicUi != null) victoryPanel = vicUi.gameObject;
            }
            if (evolutionPanel == null)
            {
                var evoUi = FindFirstObjectByType<EvolutionUI>(FindObjectsInactive.Include);
                if (evoUi != null) evolutionPanel = evoUi.gameObject;
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (evolutionPanel != null) evolutionPanel.SetActive(false);

            // Aplica bônus de habilidades carregadas ao Player
            ApplyLoadedSkillsToPlayer();
        }

        private void ApplyLoadedSkillsToPlayer()
        {
            var pHealth = FindFirstObjectByType<PlayerHealth>();
            var pShooting = FindFirstObjectByType<PlayerShooting>();
            var pController = FindFirstObjectByType<PlayerController>();

            if (pHealth != null)
            {
                // Bônus de Vitalidade (+5 por nível)
                if (LevelVitality > 0) pHealth.IncreaseMaxHealth(LevelVitality * 5);
                // Bônus de Despertar (+3 de vida)
                if (LevelDespertar > 0) pHealth.IncreaseMaxHealth(LevelDespertar * 3);
                // Pele de pedra
                if (LevelPele > 0) pHealth.SetDamageReduction(0.08f * LevelPele);
                // Segundo fôlego
                if (LevelSegundoFolego > 0) pHealth.EnableSecondWind();
            }

            if (pController != null && LevelVeloz > 0)
            {
                pController.MultiplySpeed(Mathf.Pow(1.08f, LevelVeloz));
            }

            if (pShooting != null)
            {
                if (LevelVeloz > 0) pShooting.AddProjectileSpeedBonus(0.15f * LevelVeloz);
                if (LevelFoco > 0) pShooting.AddBonusDamage(1f * LevelFoco);
                if (LevelDespertar > 0) pShooting.AddBonusDamage(1f * LevelDespertar);
                if (LevelRapido > 0) pShooting.AddFireRateBonus(0.10f * LevelRapido);
                if (LevelDuplo > 0) pShooting.SetMultiShotLevel(LevelDuplo);
                if (LevelPerfurante > 0) pShooting.AddBonusPierce(LevelPerfurante);

                if (isStaffEvolved)
                {
                    ApplyStaffEvolutionBonus();
                }
            }
        }

        private void Update()
        {
            // Pressionar tecla 'E' abre/fecha a tela de evolução
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleEvolutionPanel();
            }

            // Tecla ESC fecha a tela de evolução se estiver aberta
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (evolutionPanel != null && evolutionPanel.activeSelf)
                {
                    ToggleEvolutionPanel();
                }
            }

            // Tecla 'F' usa poção do inventário
            if (Input.GetKeyDown(KeyCode.F))
            {
                UseHealthPotionFromInventory();
            }
        }

        public void AddScore(int points)
        {
            currentScore += points;
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt("WitchShmup_HighScore", highScore);
            }

            if (hudController != null)
            {
                hudController.SetScore(currentScore);
            }

            OnScoreChanged?.Invoke(currentScore);
        }

        public void AddFireHeart(int amount = 1)
        {
            fireHeartsCount += amount;
            OnMaterialsChanged?.Invoke(fireHeartsCount, bossCoresCount);
        }

        public void AddBossCore(int amount = 1)
        {
            bossCoresCount += amount;
            OnMaterialsChanged?.Invoke(fireHeartsCount, bossCoresCount);
        }

        public void AddSkillPoints(int amount = 1)
        {
            skillPoints += amount;
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        /// <summary>
        /// Adiciona poção ao inventário (chamado quando o player está com vida cheia).
        /// </summary>
        public void AddHealthPotionToInventory(int amount = 1)
        {
            healthPotionsInInventory += amount;
            OnHealthPotionsChanged?.Invoke(healthPotionsInInventory);

            if (hudController != null)
            {
                hudController.UpdatePotionCount(healthPotionsInInventory);
            }

            Debug.Log($"[GameManager] Poção guardada no inventário! Total: {healthPotionsInInventory}");
        }

        /// <summary>
        /// Usa uma poção do inventário (tecla F).
        /// </summary>
        public bool UseHealthPotionFromInventory()
        {
            if (healthPotionsInInventory <= 0) return false;

            var playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return false;

            // Não desperdiça se já estiver com vida cheia
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            {
                Debug.Log("[GameManager] Vida já cheia, poção não usada.");
                return false;
            }

            healthPotionsInInventory--;
            playerHealth.Heal(1);
            OnHealthPotionsChanged?.Invoke(healthPotionsInInventory);

            if (hudController != null)
            {
                hudController.UpdatePotionCount(healthPotionsInInventory);
            }

            Debug.Log($"[GameManager] Poção usada! Restantes: {healthPotionsInInventory}");
            return true;
        }

        public bool SpendSkillPoints(int amount)
        {
            if (skillPoints >= amount)
            {
                skillPoints -= amount;
                OnSkillPointsChanged?.Invoke(skillPoints);
                return true;
            }
            return false;
        }

        public int GetSkillLevel(string id)
        {
            switch (id)
            {
                case "vit": return LevelVitality;
                case "vel": return LevelVeloz;
                case "foc": return LevelFoco;
                case "des": return LevelDespertar;
                case "pel": return LevelPele;
                case "ima": return LevelIma;
                case "seg": return LevelSegundoFolego;
                case "rap": return LevelRapido;
                case "dup": return LevelDuplo;
                case "per": return LevelPerfurante;
                case "vel_tiro": return LevelVelTiro;
                case "qtd_tiro": return LevelQtdTiro;
                case "pod_gelo": return LevelPodGelo;
                case "pod_fogo": return LevelPodFogo;
                default: return 0;
            }
        }

        public bool UpgradeSkill(string id, int cost)
        {
            if (!SpendSkillPoints(cost)) return false;

            var pHealth     = FindFirstObjectByType<PlayerHealth>();
            var pShooting   = FindFirstObjectByType<PlayerShooting>();
            var pController = FindFirstObjectByType<PlayerController>();

            switch (id)
            {
                case "vit":
                    LevelVitality++;
                    // +10 de vida (5 corações de 2 HP cada, baseado em maxHealth=4)
                    if (pHealth != null) pHealth.IncreaseMaxHealth(5);
                    break;

                case "vel":
                    LevelVeloz++;
                    LevelVelTiro = LevelVeloz;
                    // +8% velocidade de movimento e +15% velocidade dos projéteis
                    if (pController != null) pController.MultiplySpeed(1.08f);
                    if (pShooting  != null) pShooting.AddProjectileSpeedBonus(0.15f);
                    break;

                case "foc":
                    LevelFoco++;
                    LevelPodGelo = LevelFoco;
                    // +1 de dano fixo em ambos elementos
                    if (pShooting != null) pShooting.AddBonusDamage(1f);
                    break;

                case "des":
                    LevelDespertar++;
                    // +5 de vida e +1 de dano
                    if (pHealth   != null) pHealth.IncreaseMaxHealth(3);
                    if (pShooting != null) pShooting.AddBonusDamage(1f);
                    break;

                // ── Ramo Esquerdo ─────────────────────────────────────────────
                case "pel":
                    LevelPele++;
                    // 8% menos dano recebido
                    if (pHealth != null) pHealth.SetDamageReduction(0.08f);
                    break;

                case "ima":
                    LevelIma++;
                    // +30% alcance magnético de drops (SoulMagnetMultiplier auto-calcula)
                    break;

                case "seg":
                    LevelSegundoFolego++;
                    // Revive 1x com ~30% de vida
                    if (pHealth != null) pHealth.EnableSecondWind();
                    break;

                // ── Ramo Direito ──────────────────────────────────────────────
                case "rap":
                    LevelRapido++;
                    // +10% cadência de disparo
                    if (pShooting != null) pShooting.AddFireRateBonus(0.10f);
                    break;

                case "dup":
                    LevelDuplo++;
                    LevelQtdTiro = LevelDuplo;
                    // +1 projétil por disparo
                    if (pShooting != null) pShooting.SetMultiShotLevel(LevelQtdTiro);
                    break;

                case "per":
                    LevelPerfurante++;
                    LevelPodFogo = LevelPerfurante;
                    // Tiros atravessam 1 inimigo
                    if (pShooting != null) pShooting.AddBonusPierce(1);
                    break;

                default:
                    return false;
            }

            OnSkillUpgraded?.Invoke(id, GetSkillLevel(id));
            SaveCurrentGame(advancePhase: false);
            return true;
        }

        public bool CanEvolveStaff()
        {
            return (!isStaffEvolved && fireHeartsCount >= fireHeartsRequired && bossCoresCount >= bossCoresRequired);
        }

        public bool TryEvolveStaff()
        {
            if (!CanEvolveStaff()) return false;

            fireHeartsCount -= fireHeartsRequired;
            bossCoresCount -= bossCoresRequired;
            isStaffEvolved = true;

            ApplyStaffEvolutionBonus();

            OnMaterialsChanged?.Invoke(fireHeartsCount, bossCoresCount);
            OnStaffEvolved?.Invoke();

            SaveCurrentGame(advancePhase: false);
            return true;
        }

        private void ApplyStaffEvolutionBonus()
        {
            var shooting = FindFirstObjectByType<PlayerShooting>();
            if (shooting != null)
            {
                shooting.UpgradeStaffBonus(1.6f);
            }
        }

        public void ToggleEvolutionPanel()
        {
            if (evolutionPanel == null) return;

            bool isOpening = !evolutionPanel.activeSelf;
            evolutionPanel.SetActive(isOpening);

            // Pausa o jogo quando a tela de evolução abre!
            Time.timeScale = isOpening ? 0f : 1f;
        }

        public void CloseEvolutionPanel()
        {
            if (evolutionPanel != null)
            {
                evolutionPanel.SetActive(false);
                Time.timeScale = 1f;
            }
        }

        public void TriggerGameOver()
        {
            IsGameOver = true;
            Time.timeScale = 0f;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            OnGameOver?.Invoke();
        }

        public IEnumerator WaitAndTriggerVictory(float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerVictory();
        }

        public void TriggerVictory()
        {
            IsVictory = true;
            Time.timeScale = 0f;

            // Salva jogo e desbloqueia próxima fase
            SaveCurrentGame(advancePhase: true);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
            OnVictory?.Invoke();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Salva o estado atual do jogo no disco e em PlayerPrefs.
        /// </summary>
        public void SaveCurrentGame(bool advancePhase = false)
        {
            var data = new SaveData
            {
                currentPhase = currentPhase,
                maxUnlockedPhase = maxUnlockedPhase,
                currentScore = currentScore,
                highScore = highScore,
                skillPoints = skillPoints,
                fireHeartsCount = fireHeartsCount,
                bossCoresCount = bossCoresCount,
                healthPotionsInInventory = healthPotionsInInventory,
                isStaffEvolved = isStaffEvolved,
                levelVit = LevelVitality,
                levelVel = LevelVeloz,
                levelFoc = LevelFoco,
                levelDes = LevelDespertar,
                levelPel = LevelPele,
                levelIma = LevelIma,
                levelSeg = LevelSegundoFolego,
                levelRap = LevelRapido,
                levelDup = LevelDuplo,
                levelPer = LevelPerfurante,
                hasSeenStory = true
            };

            if (advancePhase)
            {
                int next = currentPhase + 1;
                data.currentPhase = next;
                if (next > data.maxUnlockedPhase)
                {
                    data.maxUnlockedPhase = next;
                }
                maxUnlockedPhase = data.maxUnlockedPhase;
            }

            SaveSystem.SaveGame(data);
        }

        /// <summary>
        /// Avança para a próxima fase repetida (ex: Fase 1 -> Fase 2 -> Fase 3).
        /// </summary>
        public void AdvanceToNextPhase()
        {
            Time.timeScale = 1f;
            int nextPhaseNum = currentPhase + 1;

            // Salva o avanço
            SaveCurrentGame(advancePhase: true);

            // Os arquivos de fase usam espaço no nome ("Fase 2", "Fase 3").
            string nextSceneName = nextPhaseNum == 1 ? "Fase" : $"Fase {nextPhaseNum}";

            // Se a cena existir nas Build Settings, carrega
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Se não houver mais fases além da 3, recarrega Fase 1 ou reinicia
                Debug.LogError($"[GameManager] Cena '{nextSceneName}' não está nas Build Settings. Verifique Assets/Scenes e EditorBuildSettings.asset.");
            }
        }

        /// <summary>
        /// Retorna ao Menu principal (salvando progresso).
        /// </summary>
        public void GoToMenu()
        {
            Time.timeScale = 1f;
            SaveCurrentGame(advancePhase: false);

            if (Application.CanStreamedLevelBeLoaded("Menu"))
            {
                SceneManager.LoadScene("Menu");
            }
            else
            {
                Debug.LogWarning("[GameManager] Cena 'Menu' ainda não existe no projeto. O usuário criará o menu manualmente. Recarregando a fase atual.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
