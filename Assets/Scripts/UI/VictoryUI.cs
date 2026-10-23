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
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            // Se nextPhaseButton não estiver assinalado, tenta usar o primeiro botão encontrado (antigo playAgainButton)
            if (nextPhaseButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                {
                    nextPhaseButton = buttons[0];
                }
            }

            if (nextPhaseButton != null)
            {
                nextPhaseButton.onClick.RemoveAllListeners();
                nextPhaseButton.onClick.AddListener(OnNextPhaseClicked);
                ButtonClickSound.Bind(nextPhaseButton);

                var txt = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = "PRÓXIMA FASE";
                }
            }

            // Procura ou cria o botão de Menu
            if (menuButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 1)
                {
                    menuButton = buttons[1];
                }
                else if (nextPhaseButton != null)
                {
                    // Duplica o botão de Próxima Fase para manter o estilo visual
                    var menuGo = Instantiate(nextPhaseButton.gameObject, nextPhaseButton.transform.parent);
                    menuGo.name = "Btn_Menu";
                    menuButton = menuGo.GetComponent<Button>();

                    var rect = menuGo.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        var origRt = nextPhaseButton.GetComponent<RectTransform>();
                        if (origRt != null)
                        {
                            rect.anchoredPosition = new Vector2(origRt.anchoredPosition.x, origRt.anchoredPosition.y - 65f);
                        }
                    }
                }
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(OnMenuClicked);
                ButtonClickSound.Bind(menuButton);

                var txt = menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = "MENU";
                }
            }
        }

        private void OnEnable()
        {
            SetupButtons();

            if (GameManager.Instance != null)
            {
                int phase = GameManager.Instance.CurrentPhase;
                if (victoryTitleText != null)
                {
                    victoryTitleText.text = $"VITÓRIA!\nFASE {phase} CONCLUÍDA";
                }

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
