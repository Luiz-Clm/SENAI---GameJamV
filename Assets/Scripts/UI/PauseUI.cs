using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class PauseUI : MonoBehaviour
    {
        public static PauseUI Instance { get; private set; }

        [Header("Painel Principal")]
        [SerializeField] private GameObject pausePanel;

        [Header("Botões")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button menuButton;

        [Header("Status Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        // Paleta idêntica ao tema do jogo
        private static readonly Color C_OVERLAY = new Color(0.04f, 0.03f, 0.07f, 0.90f);
        private static readonly Color C_PANEL = new Color(0.12f, 0.09f, 0.20f, 1f);
        private static readonly Color C_BORDER = new Color(0.28f, 0.20f, 0.44f, 1f);
        private static readonly Color C_BTN_PRIMARY = new Color(1.00f, 0.36f, 0.56f, 1f);   // Rosa
        private static readonly Color C_BTN_SECONDARY = new Color(0.22f, 0.16f, 0.35f, 1f); // Roxo escuro
        private static readonly Color C_GOLD = new Color(1.00f, 0.78f, 0.23f, 1f);
        private static readonly Color C_CYAN = new Color(0.37f, 0.90f, 1.00f, 1f);

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

            EnsureUIBuilt();
        }

        private void Start()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Pressionar Escape ou P alterna o pause
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                // Se a tela de evolução estiver aberta, o ESC deve ser tratado para fechar a evolução
                if (GameManager.Instance != null && GameManager.Instance.IsEvolutionOpen)
                {
                    return;
                }

                // Não pausa durante GameOver ou Vitória
                if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory))
                {
                    return;
                }

                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (pausePanel == null)
            {
                EnsureUIBuilt();
            }

            bool newState = !pausePanel.activeSelf;
            pausePanel.SetActive(newState);
            Time.timeScale = newState ? 0f : 1f;

            if (newState && statusFeedbackText != null)
            {
                statusFeedbackText.text = "";
            }
        }

        public void Resume()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            Time.timeScale = 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void SaveGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveCurrentGame(advancePhase: false);
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "★ JOGO SALVO COM SUCESSO! ★";
                statusFeedbackText.color = C_CYAN;
                StopAllCoroutines();
                StartCoroutine(ClearStatusRoutine(3f));
            }
        }

        private IEnumerator ClearStatusRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "";
            }
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMenu();
            }
            else
            {
                // Tenta carregar cena de Menu; se não existir, recarrega a fase
                if (Application.CanStreamedLevelBeLoaded("Menu"))
                {
                    SceneManager.LoadScene("Menu");
                }
                else
                {
                    Debug.LogWarning("[PauseUI] Cena 'Menu' ainda não foi criada. O usuário poderá criá-la manualmente.");
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }

        private void EnsureUIBuilt()
        {
            if (pausePanel != null) return;

            // Encontra Canvas existente ou cria overlay
            var canvas = FindFirstObjectByType<Canvas>();
            Transform canvasTransform = (canvas != null) ? canvas.transform : transform;

            // Fundo escuro fullscreen
            pausePanel = new GameObject("Panel_Pause");
            pausePanel.transform.SetParent(canvasTransform, false);

            var rt = pausePanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var bgImg = pausePanel.AddComponent<Image>();
            bgImg.color = C_OVERLAY;

            // Adiciona Canvas isolado para garantir sorting no topo
            var innerCanvas = pausePanel.AddComponent<Canvas>();
            innerCanvas.overrideSorting = true;
            innerCanvas.sortingOrder = 200;
            if (pausePanel.GetComponent<GraphicRaycaster>() == null)
            {
                pausePanel.AddComponent<GraphicRaycaster>();
            }

            // Card central
            var card = new GameObject("Card");
            card.transform.SetParent(pausePanel.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(440, 520);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = C_PANEL;

            // Borda do card
            var border = new GameObject("Border");
            border.transform.SetParent(card.transform, false);
            var borderRt = border.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(6, 6);
            borderRt.anchoredPosition = Vector2.zero;
            border.transform.SetAsFirstSibling();
            var bImg = border.AddComponent<Image>();
            bImg.color = C_BORDER;

            // Título
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(card.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(0, 60);
            titleRt.anchoredPosition = new Vector2(0, -28);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "JOGO PAUSADO";
            titleTmp.fontSize = 32;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Container de Botões
            var btnsContainer = new GameObject("ButtonsContainer");
            btnsContainer.transform.SetParent(card.transform, false);
            var bcRt = btnsContainer.AddComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.5f);
            bcRt.anchorMax = new Vector2(0.5f, 0.5f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.sizeDelta = new Vector2(360, 320);
            bcRt.anchoredPosition = new Vector2(0, -20);

            var vlg = btnsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // 1. Botão CONTINUAR
            resumeButton = CreateButton(btnsContainer.transform, "Btn_Resume", "CONTINUAR", C_BTN_PRIMARY, Color.white, Resume);

            // 2. Botão SALVAR
            saveButton = CreateButton(btnsContainer.transform, "Btn_Save", "SALVAR JOGO", C_BTN_SECONDARY, C_CYAN, SaveGame);

            // 3. Botão REINICIAR
            restartButton = CreateButton(btnsContainer.transform, "Btn_Restart", "REINICIAR FASE", C_BTN_SECONDARY, Color.white, Restart);

            // 4. Botão MENU
            menuButton = CreateButton(btnsContainer.transform, "Btn_Menu", "MENU PRINCIPAL", C_BTN_SECONDARY, C_GOLD, GoToMenu);

            // Mensagem de Feedback de Save
            var feedbackGo = new GameObject("Txt_Feedback");
            feedbackGo.transform.SetParent(card.transform, false);
            var fRt = feedbackGo.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(0, 30);
            fRt.anchoredPosition = new Vector2(0, 14);
            statusFeedbackText = feedbackGo.AddComponent<TextMeshProUGUI>();
            statusFeedbackText.text = "";
            statusFeedbackText.fontSize = 16;
            statusFeedbackText.fontStyle = FontStyles.Bold;
            statusFeedbackText.alignment = TextAlignmentOptions.Center;

            pausePanel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string name, string label, Color bgColor, Color textColor, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 52);

            var img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
