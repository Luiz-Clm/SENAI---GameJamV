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

        [Header("Staff Evolution Requirements")]
        [SerializeField] private int fireHeartsRequired = 2;
        [SerializeField] private int bossCoresRequired = 1;

        [Header("UI References")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject evolutionPanel;

        // Skill Tree Levels matching HTML sketch
        public int LevelVitality { get; private set; } = 0;     // vit (max 5)
        public int LevelVeloz { get; private set; } = 0;        // vel (max 3)
        public int LevelFoco { get; private set; } = 0;         // foc (max 3)
        public int LevelDespertar { get; private set; } = 0;    // des (max 1)
        public int LevelPele { get; private set; } = 0;         // pel (max 3)
        public int LevelIma { get; private set; } = 0;          // ima (max 2)
        public int LevelSegundoFolego { get; private set; } = 0;// seg (max 1)
        public int LevelRapido { get; private set; } = 0;       // rap (max 4)
        public int LevelDuplo { get; private set; } = 0;        // dup (max 1)
        public int LevelPerfurante { get; private set; } = 0;   // per (max 1)

        public float SoulMagnetMultiplier => 1f + (LevelIma * 0.35f);

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int FireHeartsCount => fireHeartsCount;
        public int BossCoresCount => bossCoresCount;
        public int SkillPoints => skillPoints;
        public bool IsStaffEvolved => isStaffEvolved;
        public int FireHeartsRequired => fireHeartsRequired;
        public int BossCoresRequired => bossCoresRequired;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnSkillPointsChanged;
        public event Action<int, int> OnMaterialsChanged;
        public event Action<string, int> OnSkillUpgraded;
        public event Action OnStaffEvolved;
        public event Action OnGameOver;
        public event Action OnVictory;

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
                default: return 0;
            }
        }

        public bool UpgradeSkill(string id, int cost)
        {
            if (!SpendSkillPoints(cost)) return false;

            var pHealth = FindFirstObjectByType<PlayerHealth>();
            var pShooting = FindFirstObjectByType<PlayerShooting>();
            var pController = FindFirstObjectByType<PlayerController>();

            switch (id)
            {
                case "vit":
                    LevelVitality++;
                    if (pHealth != null) pHealth.IncreaseMaxHealth(1);
                    break;

                case "vel":
                    LevelVeloz++;
                    if (pController != null) pController.MultiplySpeed(1.08f);
                    break;

                case "foc":
                    LevelFoco++;
                    if (pShooting != null) pShooting.AddBonusDamage(0.5f);
                    break;

                case "des":
                    LevelDespertar++;
                    if (pHealth != null) pHealth.IncreaseMaxHealth(1);
                    if (pShooting != null) pShooting.AddBonusDamage(0.5f);
                    break;

                case "pel":
                    LevelPele++;
                    if (pHealth != null) pHealth.SetDamageReduction(LevelPele * 0.08f);
                    break;

                case "ima":
                    LevelIma++;
                    // SoulMagnetMultiplier se atualiza automaticamente
                    break;

                case "seg":
                    LevelSegundoFolego++;
                    if (pHealth != null) pHealth.EnableSecondWind();
                    break;

                case "rap":
                    LevelRapido++;
                    if (pShooting != null) pShooting.AddFireRateBonus(0.10f);
                    break;

                case "dup":
                    LevelDuplo++;
                    if (pShooting != null) pShooting.UnlockDoubleShot();
                    break;

                case "per":
                    LevelPerfurante++;
                    if (pShooting != null) pShooting.AddBonusPierce(1);
                    break;

                default:
                    return false;
            }

            OnSkillUpgraded?.Invoke(id, GetSkillLevel(id));
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
            Time.timeScale = 0f;
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
    }
}
