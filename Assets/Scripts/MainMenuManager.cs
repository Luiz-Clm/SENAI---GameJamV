using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchShmup.Core;

namespace SpellGlide
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Painéis de UI")]
        [SerializeField] private GameObject menuRootPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject phaseSelectPanel;

        [Header("Botões do Menu Principal")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button resetDataButton;

        [Header("Painel de Seleção de Fases")]
        [SerializeField] private Button phase1Button;
        [SerializeField] private Button phase2Button;
        [SerializeField] private Button phase3Button;
        [SerializeField] private Button rewatchStoryButton;
        [SerializeField] private Button closePhaseSelectButton;

        [Header("Opções de Áudio (Painel de Opções)")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button closeOptionsButton;

        [Header("Controladores")]
        [SerializeField] private ComicIntroManager comicIntroManager;
        [SerializeField] private AudioManager audioManager;

        private void Awake()
        {
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
            }

            if (comicIntroManager == null)
            {
                comicIntroManager = FindFirstObjectByType<ComicIntroManager>();
            }

            SetupButtons();
            SetupOptionsSliders();
        }

        private void SetupButtons()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (closeOptionsButton != null)
                closeOptionsButton.onClick.AddListener(OnCloseOptionsClicked);

            if (resetDataButton != null)
                resetDataButton.onClick.AddListener(OnResetDataClicked);
        }

        private void SetupOptionsSliders()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            if (audioManager != null)
            {
                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.value = audioManager.GetMusicVolume();
                    musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.value = audioManager.GetSFXVolume();
                    sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                }
            }
        }

        public void OnPlayClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            // Oculta o menu principal
            if (menuRootPanel != null)
            {
                menuRootPanel.SetActive(false);
            }

            // Oculta o botão de reset (está fora do MenuPanel)
            if (resetDataButton != null)
            {
                resetDataButton.gameObject.SetActive(false);
            }

            // Verifica se o jogador já assistiu à historinha alguma vez
            bool hasSeenStory = SaveSystem.HasSeenStory();

            if (!hasSeenStory)
            {
                // Primeira vez: roda a introdução com os quadrinhos
                if (comicIntroManager != null)
                {
                    comicIntroManager.StartStory(isRewatch: false);
                }
                else
                {
                    Debug.LogWarning("[MainMenuManager] ComicIntroManager não encontrado na cena!");
                }
            }
            else
            {
                // Já viu a história: abre o Seletor de Fases (Fase 1, 2, 3 + Ver História)
                OpenPhaseSelectPanel();
            }
        }

        private void OpenPhaseSelectPanel()
        {
            if (phaseSelectPanel == null)
            {
                BuildPhaseSelectPanelAtRuntime();
            }

            if (phaseSelectPanel != null)
            {
                phaseSelectPanel.transform.SetAsLastSibling();
                phaseSelectPanel.SetActive(true);
                UpdatePhaseSelectButtons();
            }
        }

        private void UpdatePhaseSelectButtons()
        {
            int maxUnlocked = SaveSystem.GetMaxUnlockedPhase();

            // Configura Fase 1 (sempre liberada)
            ConfigurePhaseButton(phase1Button, 1, isUnlocked: true);

            // Configura Fase 2
            ConfigurePhaseButton(phase2Button, 2, isUnlocked: maxUnlocked >= 2);

            // Configura Fase 3
            ConfigurePhaseButton(phase3Button, 3, isUnlocked: maxUnlocked >= 3);
        }

        private void ConfigurePhaseButton(Button btn, int phaseNum, bool isUnlocked)
        {
            if (btn == null) return;

            btn.interactable = isUnlocked;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isUnlocked ? new Color(0.2f, 0.45f, 0.85f, 0.95f) : new Color(0.2f, 0.2f, 0.25f, 0.6f);
            }

            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = isUnlocked ? $"FASE {phaseNum}" : $"FASE {phaseNum}  <size=18>[BLOQUEADA]</size>";
                tmp.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.7f);
            }
        }

        public void OnPhaseSelected(int phaseNumber)
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            string sceneName = (phaseNumber == 1) ? "Fase" : $"Fase{phaseNumber}";
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenuManager] Cena '{sceneName}' não encontrada na build! Carregando Fase padrão.");
                SceneManager.LoadScene(1);
            }
        }

        public void OnRewatchStoryClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            if (phaseSelectPanel != null)
            {
                phaseSelectPanel.SetActive(false);
            }

            if (comicIntroManager != null)
            {
                comicIntroManager.StartStory(isRewatch: true);
            }
        }

        public void OnClosePhaseSelectClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            if (phaseSelectPanel != null)
            {
                phaseSelectPanel.SetActive(false);
            }

            if (menuRootPanel != null)
            {
                menuRootPanel.SetActive(true);
            }

            if (resetDataButton != null)
            {
                resetDataButton.gameObject.SetActive(true);
            }
        }

        private void BuildPhaseSelectPanelAtRuntime()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Painel central de seleção de fases
            phaseSelectPanel = new GameObject("PhaseSelectPanel", typeof(RectTransform), typeof(Image));
            phaseSelectPanel.transform.SetParent(canvas.transform, false);
            phaseSelectPanel.transform.SetAsLastSibling();

            RectTransform panelRt = phaseSelectPanel.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(580f, 540f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.localScale = Vector3.one;

            Image panelImg = phaseSelectPanel.GetComponent<Image>();
            panelImg.color = new Color(0.07f, 0.06f, 0.12f, 0.96f);

            // Título
            CreateTMPLabel(phaseSelectPanel.transform, "Titulo", "SELEÇÃO DE FASES", 30, new Vector2(0, 220), new Color(0.95f, 0.9f, 1f, 1f));

            // Botões de Fases 1, 2 e 3
            phase1Button = CreatePhaseMenuButton(phaseSelectPanel.transform, "Btn_Fase1", "FASE 1", new Vector2(0, 140), () => OnPhaseSelected(1));
            phase2Button = CreatePhaseMenuButton(phaseSelectPanel.transform, "Btn_Fase2", "FASE 2", new Vector2(0, 65),  () => OnPhaseSelected(2));
            phase3Button = CreatePhaseMenuButton(phaseSelectPanel.transform, "Btn_Fase3", "FASE 3", new Vector2(0, -10), () => OnPhaseSelected(3));

            // Botão "Ver História"
            rewatchStoryButton = CreatePhaseMenuButton(phaseSelectPanel.transform, "Btn_VerHistoria", "📖 VER HISTÓRIA", new Vector2(0, -95), OnRewatchStoryClicked);
            var rewatchImg = rewatchStoryButton.GetComponent<Image>();
            if (rewatchImg != null) rewatchImg.color = new Color(0.55f, 0.25f, 0.75f, 0.95f);

            // Botão "Voltar"
            closePhaseSelectButton = CreatePhaseMenuButton(phaseSelectPanel.transform, "Btn_Voltar", "VOLTAR", new Vector2(0, -180), OnClosePhaseSelectClicked);
            var backImg = closePhaseSelectButton.GetComponent<Image>();
            if (backImg != null) backImg.color = new Color(0.3f, 0.3f, 0.35f, 0.95f);
        }

        private Button CreatePhaseMenuButton(Transform parent, string name, string labelText, Vector2 pos, UnityEngine.Events.UnityAction onClickAction)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 55f);
            rt.anchoredPosition = pos;

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.85f, 0.95f);

            Button btn = btnObj.GetComponent<Button>();
            if (onClickAction != null) btn.onClick.AddListener(onClickAction);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 24;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            return btn;
        }

        private TMPro.TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text, float size, Vector2 pos, Color col)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 50f);
            rt.anchoredPosition = pos;

            TMPro.TextMeshProUGUI tmp = obj.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color = col;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            return tmp;
        }

        public void OnOptionsClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
            }
        }

        public void OnCloseOptionsClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        public void OnQuitClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            Debug.Log("[MainMenuManager] Fechando o jogo...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetMusicVolume(value);
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetSFXVolume(value);
            }
        }

        public void OnResetDataClicked()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            SaveSystem.ResetAllData();

            if (phaseSelectPanel != null)
            {
                phaseSelectPanel.SetActive(false);
            }

            if (menuRootPanel != null)
            {
                menuRootPanel.SetActive(true);
            }

            if (resetDataButton != null)
            {
                resetDataButton.gameObject.SetActive(true);
            }

            Debug.Log("[MainMenuManager] Dados do jogo resetados pelo jogador.");
        }
    }
}
