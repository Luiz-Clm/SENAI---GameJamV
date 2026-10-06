using System;
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
        [SerializeField] private int skillPoints = 0;
        [SerializeField] private bool isStaffEvolved = false;

        [Header("Staff Evolution Requirements")]
        [SerializeField] private int fireHeartsRequired = 2;
        [SerializeField] private int bossCoresRequired = 1;

        [Header("UI References")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject evolutionPanel;

        // Skill Tree Unlocks
        public bool SkillVitalityUnlocked { get; private set; } = false;
        public bool SkillAttackSpeedUnlocked { get; private set; } = false;
        public bool SkillIcePowerUnlocked { get; private set; } = false;
        public bool SkillFirePowerUnlocked { get; private set; } = false;
        public bool SkillFlightSpeedUnlocked { get; private set; } = false;

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
        public event Action OnStaffEvolved;
        public event Action OnGameOver;

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

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
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
                // Aumenta poder de fogo e dano elemental do jogador
                shooting.UpgradeStaffBonus(1.6f);
            }
        }

        public bool UnlockSkill(string skillId)
        {
            switch (skillId)
            {
                case "Vitality":
                    if (!SkillVitalityUnlocked && SpendSkillPoints(1))
                    {
                        SkillVitalityUnlocked = true;
                        var pHealth = FindFirstObjectByType<PlayerHealth>();
                        if (pHealth != null) pHealth.IncreaseMaxHealth(1);
                        return true;
                    }
                    break;

                case "AttackSpeed":
                    if (!SkillAttackSpeedUnlocked && SpendSkillPoints(1))
                    {
                        SkillAttackSpeedUnlocked = true;
                        var pShooting = FindFirstObjectByType<PlayerShooting>();
                        if (pShooting != null) pShooting.MultiplyFireRate(0.8f); // 20% mais rápido
                        return true;
                    }
                    break;

                case "IcePower":
                    if (!SkillIcePowerUnlocked && SpendSkillPoints(1))
                    {
                        SkillIcePowerUnlocked = true;
                        var pShooting = FindFirstObjectByType<PlayerShooting>();
                        if (pShooting != null) pShooting.BoostElementDamage(Combat.ElementType.Ice, 1.5f);
                        return true;
                    }
                    break;

                case "FirePower":
                    if (!SkillFirePowerUnlocked && SpendSkillPoints(1))
                    {
                        SkillFirePowerUnlocked = true;
                        var pShooting = FindFirstObjectByType<PlayerShooting>();
                        if (pShooting != null) pShooting.BoostElementDamage(Combat.ElementType.Fire, 1.5f);
                        return true;
                    }
                    break;

                case "FlightSpeed":
                    if (!SkillFlightSpeedUnlocked && SpendSkillPoints(1))
                    {
                        SkillFlightSpeedUnlocked = true;
                        var pController = FindFirstObjectByType<PlayerController>();
                        if (pController != null) pController.MultiplySpeed(1.2f); // +20% velocidade
                        return true;
                    }
                    break;
            }
            return false;
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
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            OnGameOver?.Invoke();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
