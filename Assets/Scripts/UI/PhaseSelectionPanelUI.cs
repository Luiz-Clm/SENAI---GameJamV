using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Endless;
using WitchShmup.UI;

namespace SpellGlide.UI
{
    public sealed class PhaseSelectionPanelUI : MonoBehaviour
    {
        [Serializable]
        public sealed class PhaseOption
        {
            public int phaseNumber = 1;
            public string title = "Fase";
            [TextArea(2, 6)] public string description = "Descrição da fase.";
            public Sprite previewImage;
            public Button selectButton;
        }

        [Header("Páginas e abas")]
        [SerializeField] private GameObject phasesPage;
        [SerializeField] private GameObject infinitePage;
        [SerializeField] private Button phasesTabButton;
        [SerializeField] private Button infiniteTabButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = new Color(0.58f, 0.58f, 0.58f, 1f);

        [Header("Seleção de fases")]
        [SerializeField] private PhaseOption[] phases = Array.Empty<PhaseOption>();
        [SerializeField] private GameObject phaseDetailsRoot;
        [SerializeField] private Image phasePreviewImage;
        [SerializeField] private TMP_Text phaseTitleText;
        [SerializeField] private TMP_Text phaseDescriptionText;
        [SerializeField] private Button phasePlayButton;
        [SerializeField] private Button storyButton;

        [Header("Modo infinito")]
        [SerializeField] private Image infinitePreviewImage;
        [SerializeField] private TMP_Text infiniteDescriptionText;
        [SerializeField] private TMP_Text bestWaveText;
        [SerializeField] private Button infinitePlayButton;

        [Header("Controlador do menu")]
        [SerializeField] private MainMenuManager mainMenu;

        private int selectedPhaseNumber;
        private bool initialized;
        private Image phasesTabIndicator;
        private Image infiniteTabIndicator;
        private Sprite[] tabIndicatorFrames;
        private Coroutine tabIndicatorAnimation;
        private Image activeTabIndicator;
        private Vector3 activeTabIndicatorScale;
        private Coroutine tabButtonPulse;
        private Button pulsingTabButton;
        private Vector3 pulsingTabOriginalScale;

        private void Awake()
        {
            if (mainMenu == null)
                mainMenu = FindFirstObjectByType<MainMenuManager>();

            phasesTabIndicator = FindTabIndicator(phasesTabButton);
            infiniteTabIndicator = FindTabIndicator(infiniteTabButton);
            tabIndicatorFrames = new[]
            {
                Resources.Load<Sprite>("TravelBookEffects/UI_TravelBook_FrameSelect01a"),
                Resources.Load<Sprite>("TravelBookEffects/UI_TravelBook_FrameSelect01b")
            };
            ConfigureTabHoverAnimation(phasesTabButton);
            ConfigureTabHoverAnimation(infiniteTabButton);

            if (phasesTabButton != null)
            {
                ButtonClickSound.Bind(phasesTabButton);
                phasesTabButton.onClick.AddListener(ShowPhasesPage);
                phasesTabButton.onClick.AddListener(() => PulseTabButton(phasesTabButton));
            }
            if (infiniteTabButton != null)
            {
                ButtonClickSound.Bind(infiniteTabButton);
                infiniteTabButton.onClick.AddListener(ShowInfinitePage);
                infiniteTabButton.onClick.AddListener(() => PulseTabButton(infiniteTabButton));
            }
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);
            if (phasePlayButton != null)
                phasePlayButton.onClick.AddListener(PlaySelectedPhase);
            if (storyButton != null)
                storyButton.onClick.AddListener(ReplayStory);
            if (infinitePlayButton != null)
                infinitePlayButton.onClick.AddListener(PlayInfiniteMode);

            if (phases != null)
            {
                foreach (PhaseOption phase in phases)
                {
                    if (phase == null || phase.selectButton == null) continue;
                    TMP_Text buttonLabel = phase.selectButton.GetComponentInChildren<TMP_Text>(true);
                    if (buttonLabel != null) buttonLabel.text = phase.title;
                    ButtonClickSound.Bind(phase.selectButton);
                    int number = phase.phaseNumber;
                    phase.selectButton.onClick.AddListener(() => SelectPhase(number));
                }
            }

            initialized = true;
            RefreshBestWave();
            ShowPhasesPage();
        }

        private void OnEnable()
        {
            RemoveOldButtonBackgrounds();
            if (!initialized) return;
            RefreshBestWave();
            ShowPhasesPage();
        }

        private void RemoveOldButtonBackgrounds()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;

                Transform oldBackground = button.transform.Find("TravelBookButtonAnimation");
                if (oldBackground != null) Destroy(oldBackground.gameObject);

                TravelBookSpriteButtonAnimation oldAnimation = button.GetComponent<TravelBookSpriteButtonAnimation>();
                if (oldAnimation != null) Destroy(oldAnimation);
            }
        }

        public void ShowPhasesPage()
        {
            if (phasesPage != null) phasesPage.SetActive(true);
            if (infinitePage != null) infinitePage.SetActive(false);
            if (phaseDetailsRoot != null) phaseDetailsRoot.SetActive(false);
            UpdateTabColors(phasesActive: true);
            AnimateTabIndicator(phasesTabIndicator, infiniteTabIndicator);
        }

        public void ShowInfinitePage()
        {
            if (phasesPage != null) phasesPage.SetActive(false);
            if (infinitePage != null) infinitePage.SetActive(true);
            RefreshBestWave();
            UpdateTabColors(phasesActive: false);
            AnimateTabIndicator(infiniteTabIndicator, phasesTabIndicator);
        }

        public void SelectPhase(int phaseNumber)
        {
            if (phases == null) return;
            foreach (PhaseOption phase in phases)
            {
                if (phase == null || phase.phaseNumber != phaseNumber) continue;

                selectedPhaseNumber = phaseNumber;
                if (phaseDetailsRoot != null) phaseDetailsRoot.SetActive(true);
                if (phaseTitleText != null) phaseTitleText.text = phase.title;
                if (phaseDescriptionText != null) phaseDescriptionText.text = phase.description;
                if (phasePreviewImage != null)
                {
                    phasePreviewImage.sprite = phase.previewImage;
                    phasePreviewImage.enabled = true;
                }
                return;
            }
        }

        private void RefreshBestWave()
        {
            if (bestWaveText != null)
                bestWaveText.text = InfiniteModeSession.BestWave.ToString();
        }

        private void PlaySelectedPhase()
        {
            if (mainMenu != null && selectedPhaseNumber > 0)
                mainMenu.OnPhaseSelected(selectedPhaseNumber);
        }

        private void PlayInfiniteMode()
        {
            if (mainMenu != null)
                mainMenu.OnInfiniteModeSelected();
        }

        private void ReplayStory()
        {
            if (mainMenu != null)
                mainMenu.OnRewatchStoryClicked();
        }

        private void ClosePanel()
        {
            if (mainMenu != null)
                mainMenu.OnClosePhaseSelectClicked();
            else
                gameObject.SetActive(false);
        }

        private static Image FindTabIndicator(Button tabButton)
        {
            if (tabButton == null) return null;
            return tabButton.transform.Find("Indicator")?.GetComponent<Image>();
        }

        private void UpdateTabColors(bool phasesActive)
        {
            SetTabColor(phasesTabButton, phasesActive ? activeTabColor : inactiveTabColor);
            SetTabColor(infiniteTabButton, phasesActive ? inactiveTabColor : activeTabColor);
        }

        private static void SetTabColor(Button button, Color tint)
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            colors.normalColor = tint;
            colors.highlightedColor = Color.Lerp(tint, Color.white, 0.12f);
            colors.selectedColor = tint;
            colors.pressedColor = Color.Lerp(tint, Color.black, 0.12f);
            colors.disabledColor = tint;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static void ConfigureTabHoverAnimation(Button tabButton)
        {
            if (tabButton == null) return;

            var frames = new Sprite[4][];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                frames[frame] = Resources.LoadAll<Sprite>($"TravelBookEffects/UI_TravelBook_SlotCursor01a_{frame + 1}");
                Array.Sort(frames[frame], (left, right) => string.CompareOrdinal(left.name, right.name));
                if (frames[frame].Length < 4) return;
            }

            var animation = tabButton.GetComponent<TravelBookTabHoverAnimation>();
            if (animation == null) animation = tabButton.gameObject.AddComponent<TravelBookTabHoverAnimation>();
            animation.Configure(frames);
        }

        private void AnimateTabIndicator(Image active, Image inactive)
        {
            if (activeTabIndicator != null)
                activeTabIndicator.rectTransform.localScale = activeTabIndicatorScale;

            if (tabIndicatorAnimation != null)
            {
                StopCoroutine(tabIndicatorAnimation);
                tabIndicatorAnimation = null;
            }
            activeTabIndicator = null;

            if (inactive != null) inactive.enabled = false;
            if (active == null) return;

            active.enabled = true;
            active.raycastTarget = false;
            active.color = Color.white;
            activeTabIndicator = active;
            activeTabIndicatorScale = active.rectTransform.localScale;

            if (tabIndicatorFrames == null || tabIndicatorFrames.Length == 0 || tabIndicatorFrames[0] == null)
                return;

            tabIndicatorAnimation = StartCoroutine(AnimateTabIndicatorLoop(active));
        }

        private IEnumerator AnimateTabIndicatorLoop(Image indicator)
        {
            int frame = 0;
            while (indicator != null && indicator == activeTabIndicator && indicator.enabled)
            {
                if (tabIndicatorFrames[frame] != null)
                    indicator.sprite = tabIndicatorFrames[frame];

                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.08f;
                indicator.rectTransform.localScale = activeTabIndicatorScale * pulse;
                frame = (frame + 1) % tabIndicatorFrames.Length;
                yield return new WaitForSecondsRealtime(0.16f);
            }

            if (indicator != null) indicator.rectTransform.localScale = activeTabIndicatorScale;
            tabIndicatorAnimation = null;
        }

        private void OnDisable()
        {
            if (tabIndicatorAnimation != null) StopCoroutine(tabIndicatorAnimation);
            tabIndicatorAnimation = null;
            if (activeTabIndicator != null) activeTabIndicator.rectTransform.localScale = activeTabIndicatorScale;
            activeTabIndicator = null;
            if (tabButtonPulse != null) StopCoroutine(tabButtonPulse);
            tabButtonPulse = null;
            if (pulsingTabButton != null) pulsingTabButton.transform.localScale = pulsingTabOriginalScale;
            pulsingTabButton = null;
        }

        private void PulseTabButton(Button button)
        {
            if (button == null) return;
            if (tabButtonPulse != null) StopCoroutine(tabButtonPulse);
            if (pulsingTabButton != null) pulsingTabButton.transform.localScale = pulsingTabOriginalScale;

            pulsingTabButton = button;
            pulsingTabOriginalScale = button.transform.localScale;
            tabButtonPulse = StartCoroutine(PulseTabButtonAnimation(button, pulsingTabOriginalScale));
        }

        private IEnumerator PulseTabButtonAnimation(Button button, Vector3 originalScale)
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration && button != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * 0.08f;
                button.transform.localScale = originalScale * scale;
                yield return null;
            }

            if (button != null) button.transform.localScale = originalScale;
            tabButtonPulse = null;
        }
    }
}
