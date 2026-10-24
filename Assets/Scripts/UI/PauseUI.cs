using System.Collections;
using TMPro;
using SpellGlide;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchShmup.Core;
using WitchShmup.Endless;

namespace WitchShmup.UI
{
    public class PauseUI : MonoBehaviour
    {
        public static PauseUI Instance { get; private set; }

        [Header("Painel Principal")]
        public GameObject pausePanel;

        [Header("Botões")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button settingsButton;

        [Header("Painel de Configurações")]
        [SerializeField] private GameObject pauseCard;
        [SerializeField] private GameObject settingsCard;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle autoWavesToggle;

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
            bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
            bool pausePressed = Input.GetKeyDown(KeyCode.P);
            if (!escapePressed && !pausePressed) return;

            if (GameManager.Instance != null && GameManager.Instance.IsEvolutionOpen)
            {
                // O ESC fecha a árvore primeiro; não tenta abrir o pause no mesmo quadro.
                if (escapePressed) GameManager.Instance.CloseEvolutionPanel();
                return;
            }

            // Não pausa durante GameOver ou Vitória.
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory))
                return;

            TogglePause();
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

            if (newState)
            {
                if (pauseCard != null) pauseCard.SetActive(true);
                if (settingsCard != null) settingsCard.SetActive(false);
            }

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
            if (settingsCard != null) settingsCard.SetActive(false);
            if (pauseCard != null) pauseCard.SetActive(true);
            Time.timeScale = 1f;
        }

        private void OpenSettings()
        {
            if (pauseCard != null) pauseCard.SetActive(false);
            if (settingsCard != null) settingsCard.SetActive(true);

            float musicValue = AudioManager.Instance != null
                ? AudioManager.Instance.GetMusicVolume()
                : PlayerPrefs.GetFloat(AudioManager.MusicVolumePrefsKey, 0.5f);
            float sfxValue = AudioManager.Instance != null
                ? AudioManager.Instance.GetSFXVolume()
                : PlayerPrefs.GetFloat(AudioManager.SfxVolumePrefsKey, 0.7f);

            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(musicValue);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sfxValue);
            if (autoWavesToggle != null)
            {
                autoWavesToggle.gameObject.SetActive(InfiniteModeSession.IsActive);
                autoWavesToggle.SetIsOnWithoutNotify(InfiniteModeSession.AutoStartNextWave);
            }
        }

        private void CloseSettings()
        {
            if (settingsCard != null) settingsCard.SetActive(false);
            if (pauseCard != null) pauseCard.SetActive(true);
        }

        private void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(AudioManager.MusicVolumePrefsKey, value);
            PlayerPrefs.Save();
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
            else if (WitchShmup.Audio.BGMManager.Instance != null)
                WitchShmup.Audio.BGMManager.Instance.MusicVolume = value;
        }

        private void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(AudioManager.SfxVolumePrefsKey, value);
            PlayerPrefs.Save();
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
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
            if (pausePanel != null)
            {
                BindExistingUI();
                return;
            }

            // Canvas overlay próprio: evita congelar o jogo com o painel dentro de um Canvas oculto.
            pausePanel = new GameObject("Canvas_Pause", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            var rt = pausePanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var bgImg = pausePanel.GetComponent<Image>();
            bgImg.color = C_OVERLAY;

            var pauseCanvas = pausePanel.GetComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.overrideSorting = true;
            pauseCanvas.sortingOrder = 500;

            var scaler = pausePanel.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Card central
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(pausePanel.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(440, 590);
            pauseCard = card;
            var cardImg = card.GetComponent<Image>();
            cardImg.color = C_PANEL;

            // Borda do card
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(card.transform, false);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(6, 6);
            borderRt.anchoredPosition = Vector2.zero;
            border.transform.SetAsFirstSibling();
            var bImg = border.GetComponent<Image>();
            bImg.color = C_BORDER;

            // Título
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(card.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(0, 60);
            titleRt.anchoredPosition = new Vector2(0, -28);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "JOGO PAUSADO";
            titleTmp.fontSize = 32;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Container de Botões
            var btnsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
            btnsContainer.transform.SetParent(card.transform, false);
            var bcRt = btnsContainer.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.5f);
            bcRt.anchorMax = new Vector2(0.5f, 0.5f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.sizeDelta = new Vector2(360, 380);
            bcRt.anchoredPosition = new Vector2(0, -25);

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
            saveButton.gameObject.SetActive(!InfiniteModeSession.IsActive);

            // 3. Botão CONFIGURAÇÕES
            settingsButton = CreateButton(btnsContainer.transform, "Btn_Settings", "CONFIGURAÇÕES", C_BTN_SECONDARY, C_CYAN, OpenSettings);

            // 4. Botão REINICIAR
            restartButton = CreateButton(btnsContainer.transform, "Btn_Restart", InfiniteModeSession.IsActive ? "REINICIAR PARTIDA" : "REINICIAR FASE", C_BTN_SECONDARY, Color.white, Restart);

            // 5. Botão MENU
            menuButton = CreateButton(btnsContainer.transform, "Btn_Menu", "MENU PRINCIPAL", C_BTN_SECONDARY, C_GOLD, GoToMenu);

            // Mensagem de Feedback de Save
            var feedbackGo = new GameObject("Txt_Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            feedbackGo.transform.SetParent(card.transform, false);
            var fRt = feedbackGo.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(0, 30);
            fRt.anchoredPosition = new Vector2(0, 14);
            statusFeedbackText = feedbackGo.GetComponent<TextMeshProUGUI>();
            statusFeedbackText.text = "";
            statusFeedbackText.fontSize = 16;
            statusFeedbackText.fontStyle = FontStyles.Bold;
            statusFeedbackText.alignment = TextAlignmentOptions.Center;

            BuildSettingsCard(pausePanel.transform);

            pausePanel.SetActive(false);
        }

        private void BindExistingUI()
        {
            if (pausePanel == null) return;

            pauseCard = FindChild("Card");
            settingsCard = FindChild("Panel_PauseSettings");
            resumeButton = FindButton("Card/ButtonsContainer/Btn_Resume");
            saveButton = FindButton("Card/ButtonsContainer/Btn_Save");
            settingsButton = FindButton("Card/ButtonsContainer/Btn_Settings");
            restartButton = FindButton("Card/ButtonsContainer/Btn_Restart");
            menuButton = FindButton("Card/ButtonsContainer/Btn_Menu");
            statusFeedbackText = FindText("Card/Txt_Feedback");
            musicVolumeSlider = FindSlider("Panel_PauseSettings/MusicVolumeSlider");
            sfxVolumeSlider = FindSlider("Panel_PauseSettings/SfxVolumeSlider");
            autoWavesToggle = FindToggle("Panel_PauseSettings/AutoStartNextWaveToggle");

            BindButton(resumeButton, Resume);
            BindButton(saveButton, SaveGame);
            BindButton(settingsButton, OpenSettings);
            BindButton(restartButton, Restart);
            BindButton(menuButton, GoToMenu);
            BindButton(FindButton("Panel_PauseSettings/Btn_SettingsBack"), CloseSettings);

            if (restartButton != null)
            {
                var label = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = InfiniteModeSession.IsActive ? "REINICIAR PARTIDA" : "REINICIAR FASE";
            }
            if (saveButton != null) saveButton.gameObject.SetActive(!InfiniteModeSession.IsActive);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
                float value = AudioManager.Instance != null
                    ? AudioManager.Instance.GetMusicVolume()
                    : PlayerPrefs.GetFloat(AudioManager.MusicVolumePrefsKey, 0.5f);
                musicVolumeSlider.SetValueWithoutNotify(value);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
                sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
                float value = AudioManager.Instance != null
                    ? AudioManager.Instance.GetSFXVolume()
                    : PlayerPrefs.GetFloat(AudioManager.SfxVolumePrefsKey, 0.7f);
                sfxVolumeSlider.SetValueWithoutNotify(value);
            }
            if (autoWavesToggle != null)
            {
                autoWavesToggle.gameObject.SetActive(InfiniteModeSession.IsActive);
                autoWavesToggle.onValueChanged.RemoveListener(SetAutoStartNextWave);
                autoWavesToggle.onValueChanged.RemoveListener(PlayToggleClickSound);
                autoWavesToggle.onValueChanged.AddListener(SetAutoStartNextWave);
                autoWavesToggle.onValueChanged.AddListener(PlayToggleClickSound);
                autoWavesToggle.SetIsOnWithoutNotify(InfiniteModeSession.AutoStartNextWave);
            }
        }

        private GameObject FindChild(string path)
        {
            Transform child = pausePanel != null ? pausePanel.transform.Find(path) : null;
            return child != null ? child.gameObject : null;
        }

        private Button FindButton(string path)
        {
            GameObject child = FindChild(path);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private TextMeshProUGUI FindText(string path)
        {
            GameObject child = FindChild(path);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Slider FindSlider(string path)
        {
            GameObject child = FindChild(path);
            return child != null ? child.GetComponent<Slider>() : null;
        }

        private Toggle FindToggle(string path)
        {
            GameObject child = FindChild(path);
            return child != null ? child.GetComponent<Toggle>() : null;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            ButtonClickSound.Bind(button);
        }

#if UNITY_EDITOR
        public void BuildEditablePanelInEditor()
        {
            EnsureUIBuilt();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        private void BuildSettingsCard(Transform parent)
        {
            settingsCard = new GameObject("Panel_PauseSettings", typeof(RectTransform), typeof(Image));
            settingsCard.transform.SetParent(parent, false);
            var settingsRt = settingsCard.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRt.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRt.pivot = new Vector2(0.5f, 0.5f);
            settingsRt.sizeDelta = new Vector2(440, 430);
            settingsCard.GetComponent<Image>().color = C_PANEL;

            CreateSettingsLabel(settingsCard.transform, "CONFIGURAÇÕES", 30, new Vector2(0, 155));
            CreateSettingsLabel(settingsCard.transform, "Volume da música", 18, new Vector2(0, 88));
            musicVolumeSlider = CreateSettingsSlider(settingsCard.transform, "MusicVolumeSlider", new Vector2(0, 55), 0.5f, SetMusicVolume);
            CreateSettingsLabel(settingsCard.transform, "Volume dos efeitos", 18, new Vector2(0, 5));
            sfxVolumeSlider = CreateSettingsSlider(settingsCard.transform, "SfxVolumeSlider", new Vector2(0, -28), 0.7f, SetSfxVolume);
            autoWavesToggle = CreateAutoWavesToggle(settingsCard.transform, new Vector2(0, -88));
            CreateButton(settingsCard.transform, "Btn_SettingsBack", "VOLTAR", C_BTN_SECONDARY, Color.white, CloseSettings)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -160);

            settingsCard.SetActive(false);
        }

        private TextMeshProUGUI CreateSettingsLabel(Transform parent, string label, float size, Vector2 position)
        {
            var go = new GameObject("Label_" + label.Replace(' ', '_'), typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 34);
            rt.anchoredPosition = position;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private Slider CreateSettingsSlider(Transform parent, string name, Vector2 position, float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 22);
            rt.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.color = C_BORDER;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(go.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0f, 1f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(18, 30);
            handle.GetComponent<Image>().color = C_GOLD;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = image;
            slider.handleRect = handleRt;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private Toggle CreateAutoWavesToggle(Transform parent, Vector2 position)
        {
            var go = new GameObject("AutoStartNextWaveToggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350, 42);
            rt.anchoredPosition = position;

            var box = new GameObject("CheckBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(go.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0, 0.5f);
            boxRt.anchorMax = new Vector2(0, 0.5f);
            boxRt.sizeDelta = new Vector2(28, 28);
            boxRt.anchoredPosition = new Vector2(8, 0);
            var boxImage = box.GetComponent<Image>();
            boxImage.color = C_BORDER;

            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(5, 5);
            checkRt.offsetMax = new Vector2(-5, -5);
            check.GetComponent<Image>().color = C_GOLD;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(48, 0);
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "Pular ondas automaticamente";
            label.fontSize = 17;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = InfiniteModeSession.AutoStartNextWave;
            toggle.onValueChanged.AddListener(SetAutoStartNextWave);
            toggle.onValueChanged.AddListener(PlayToggleClickSound);
            return toggle;
        }

        private void SetAutoStartNextWave(bool value) => InfiniteModeSession.AutoStartNextWave = value;
        private void PlayToggleClickSound(bool _) => ButtonClickSound.PlayClickSound();

        private Button CreateButton(Transform parent, string name, string label, Color bgColor, Color textColor, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 52);

            var img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(action);
            ButtonClickSound.Bind(btn);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
