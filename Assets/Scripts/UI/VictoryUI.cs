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
            // Quando o painel final já tem os dois botões salvos na cena,
            // mantém seus rótulos editáveis no Inspector.
            bool hasStaticFinalButtons = isFinal && restartButton != null && menuButton != null;

            // Localiza botões filhos caso não estejam assinalados no Inspector
            var existingButtons = GetComponentsInChildren<Button>(true);

            // Se for Fase 2 (final): exibe APENAS Reiniciar Fase e Voltar ao Menu
            if (isFinal)
            {
                // Se o nextPhaseButton existir, esconde-o
                if (nextPhaseButton != null)
                {
                    nextPhaseButton.gameObject.SetActive(false);
                }

                // 1. Botão Reiniciar Fase
                if (restartButton == null)
                {
                    foreach (var b in existingButtons)
                    {
                        if (b != null && (b.gameObject.name.Contains("Restart") || b.gameObject.name.Contains("PlayAgain")))
                        {
                            restartButton = b;
                            break;
                        }
                    }

                    if (restartButton == null && existingButtons.Length > 0)
                    {
                        restartButton = existingButtons[0];
                    }
                }

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    restartButton.onClick.RemoveAllListeners();
                    restartButton.onClick.AddListener(OnRestartClicked);

                    var txt = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && !hasStaticFinalButtons)
                    {
                        txt.text = "REINICIAR FASE";
                    }

                    if (!hasStaticFinalButtons)
                    {
                        var rt = restartButton.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(0f, -240f);
                            rt.sizeDelta = new Vector2(300f, 56f);
                        }
                    }

                }

                // 2. Botão Voltar ao Menu
                if (menuButton == null)
                {
                    foreach (var b in existingButtons)
                    {
                        if (b != null && b.gameObject.name.Contains("Menu") && b != restartButton)
                        {
                            menuButton = b;
                            break;
                        }
                    }

                    if (menuButton == null && restartButton != null)
                    {
                        var menuGo = Instantiate(restartButton.gameObject, restartButton.transform.parent);
                        menuGo.name = "Btn_Menu";
                        menuButton = menuGo.GetComponent<Button>();
                    }
                }

                if (menuButton != null)
                {
                    menuButton.gameObject.SetActive(true);
                    menuButton.onClick.RemoveAllListeners();
                    menuButton.onClick.AddListener(OnMenuClicked);

                    var txt = menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && !hasStaticFinalButtons)
                    {
                        txt.text = "VOLTAR AO MENU";
                    }

                    if (!hasStaticFinalButtons)
                    {
                        var rt = menuButton.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(0f, -315f);
                            rt.sizeDelta = new Vector2(300f, 56f);
                        }
                    }

                }
            }
            else
            {
                // Fase 1: Próxima Fase + Menu
                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(false);
                }

                if (nextPhaseButton == null && existingButtons.Length > 0)
                {
                    nextPhaseButton = existingButtons[0];
                }

                if (nextPhaseButton != null)
                {
                    nextPhaseButton.gameObject.SetActive(true);
                    nextPhaseButton.onClick.RemoveAllListeners();
                    nextPhaseButton.onClick.AddListener(OnNextPhaseClicked);

                    var txt = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = "PRÓXIMA FASE";
                    }

                    var rt = nextPhaseButton.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, -240f);
                        rt.sizeDelta = new Vector2(300f, 56f);
                    }

                }

                if (menuButton == null)
                {
                    if (existingButtons.Length > 1 && existingButtons[1] != nextPhaseButton)
                    {
                        menuButton = existingButtons[1];
                    }
                    else if (nextPhaseButton != null)
                    {
                        var menuGo = Instantiate(nextPhaseButton.gameObject, nextPhaseButton.transform.parent);
                        menuGo.name = "Btn_Menu";
                        menuButton = menuGo.GetComponent<Button>();
                    }
                }

                if (menuButton != null)
                {
                    menuButton.gameObject.SetActive(true);
                    menuButton.onClick.RemoveAllListeners();
                    menuButton.onClick.AddListener(OnMenuClicked);

                    var txt = menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = "MENU";
                    }

                    var rt = menuButton.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, -315f);
                        rt.sizeDelta = new Vector2(300f, 56f);
                    }

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
                    if (IsFinalPhase())
                    {
                        victoryTitleText.text = "VITÓRIA!\nFASE 2 CONCLUÍDA";
                    }
                    else
                    {
                        victoryTitleText.text = $"VITÓRIA!\nFASE {phase} CONCLUÍDA";
                    }
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
