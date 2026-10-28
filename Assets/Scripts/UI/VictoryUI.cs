using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button nextPhaseButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            SetupButtons();
        }

        private bool IsFinalPhase()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase >= 2)
            {
                return true;
            }

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene.IndexOf("2", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private void SetupButtons()
        {
            bool isFinal = IsFinalPhase();
            var existingButtons = GetComponentsInChildren<Button>(true);
            // Referências da cena são a fonte de verdade. O script só conecta ações;
            // não cria botões nem altera textos, posições ou tamanhos do Canvas.
            if (nextPhaseButton == null && existingButtons.Length > 0) nextPhaseButton = existingButtons[0];
            if (restartButton == null)
                foreach (var button in existingButtons)
                    if (button.gameObject.name.Contains("Restart") || button.gameObject.name.Contains("PlayAgain")) { restartButton = button; break; }
            if (menuButton == null)
                foreach (var button in existingButtons)
                    if (button.gameObject.name.Contains("Menu")) { menuButton = button; break; }

            if (isFinal)
            {
                if (nextPhaseButton != null) nextPhaseButton.gameObject.SetActive(false);
                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    restartButton.onClick.RemoveAllListeners();
                    restartButton.onClick.AddListener(OnRestartClicked);
                }
                if (menuButton != null)
                {
                    menuButton.gameObject.SetActive(true);
                    menuButton.onClick.RemoveAllListeners();
                    menuButton.onClick.AddListener(OnMenuClicked);
                }
            }
            else
            {
                if (restartButton != null && restartButton != nextPhaseButton) restartButton.gameObject.SetActive(false);
                if (nextPhaseButton != null)
                {
                    nextPhaseButton.gameObject.SetActive(true);
                    nextPhaseButton.onClick.RemoveAllListeners();
                    nextPhaseButton.onClick.AddListener(OnNextPhaseClicked);
                }
                if (menuButton != null)
                {
                    menuButton.gameObject.SetActive(true);
                    menuButton.onClick.RemoveAllListeners();
                    menuButton.onClick.AddListener(OnMenuClicked);
                }
            }
        }

        private void OnEnable()
        {
            SetupButtons();

            if (GameManager.Instance != null)
            {
                if (finalScoreText != null)
                {
                    finalScoreText.text = $"PONTUAÇÃO FINAL: {GameManager.Instance.CurrentScore:D7}\nRECORDE: {GameManager.Instance.HighScore:D7}";
                }

                if (statsText != null)
                {
                    string staffStatus = GameManager.Instance.IsStaffEvolved ? "Cajado Arcano (Evoluído)" : "Cajado de Madeira";
                    statsText.text = $"Skill Points Restantes: {GameManager.Instance.SkillPoints}\n" +
                                     $"Presas de Morcego: {GameManager.Instance.FireHeartsCount}\n" +
                                     $"Núcleos de Boss: {GameManager.Instance.BossCoresCount}\n" +
                                     $"Poções no Inventário: {GameManager.Instance.HealthPotionsInInventory}\n" +
                                     $"Arma: {staffStatus}";
                }
            }
        }

        private void OnNextPhaseClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvanceToNextPhase();
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMenu();
            }
            else
            {
                Time.timeScale = 1f;
                if (Application.CanStreamedLevelBeLoaded("Menu"))
                {
                    SceneManager.LoadScene("Menu");
                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}
