using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchShmup.Core;
using WitchShmup.Endless;
using WitchShmup.UI;
using SpellGlide.UI;

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

            var controlRebindingUI = GetComponent<ControlRebindingUI>();
            if (controlRebindingUI == null) controlRebindingUI = gameObject.AddComponent<ControlRebindingUI>();
            controlRebindingUI.Initialize(optionsPanel);
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
            {
                resetDataButton.onClick.RemoveListener(OnNewGameOrResetClicked);
                resetDataButton.onClick.AddListener(OnNewGameOrResetClicked);
            }
        }

        private void RefreshPhaseSelectionUI()
        {
            if (phaseSelectPanel == null) return;
            PhaseSelectionPanelUI panelUI = phaseSelectPanel.GetComponent<PhaseSelectionPanelUI>();
            if (panelUI != null) panelUI.RefreshMenuState();
        }

        private void SetupOptionsSliders()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            if (audioManager != null)
            {
                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.value = PlayerPrefs.GetFloat(AudioManager.MusicVolumePrefsKey, audioManager.GetMusicVolume());
                    musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.value = PlayerPrefs.GetFloat(AudioManager.SfxVolumePrefsKey, audioManager.GetSFXVolume());
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

            OpenPhaseSelectPanel();
        }

        private void OnNewGameOrResetClicked()
        {
            if (SaveSystem.HasStartedGame())
            {
                OnResetDataClicked();
                return;
            }

            if (audioManager != null)
                audioManager.PlayButtonClick();

            SaveSystem.MarkGameStarted();
            RefreshPhaseSelectionUI();
        }

        private void OpenPhaseSelectPanel()
        {
            if (phaseSelectPanel == null)
            {
                Debug.LogError("[MainMenuManager] Associe Panel_Evolution_MenuCopy ao campo Phase Select Panel no Inspector.");
                return;
            }

            phaseSelectPanel.transform.SetAsLastSibling();
            phaseSelectPanel.SetActive(true);
        }

        public void OnPhaseSelected(int phaseNumber)
        {
            if (phaseNumber == 2 && !SaveSystem.HasCompletedPhase1())
            {
                Debug.LogWarning("[MainMenuManager] Vença a Fase 1 para liberar a Fase 2.");
                return;
            }

            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
            }

            string sceneName;
            if (phaseNumber == 1)
                sceneName = "Fase";
            else if (phaseNumber == 2)
                sceneName = "Fase 2";
            else
            {
                Debug.LogWarning($"[MainMenuManager] Fase {phaseNumber} não está disponível.");
                return;
            }

            InfiniteModeSession.End();
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"[MainMenuManager] Cena '{sceneName}' não encontrada ou não habilitada nas Build Settings.");
            }
        }

        public void OnInfiniteModeSelected()
        {
            if (audioManager != null) audioManager.PlayButtonClick();

            const string gameplayScene = "InfiniteMode";
            if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
            {
                Debug.LogError($"[MainMenuManager] A cena '{gameplayScene}' precisa estar habilitada nas Build Settings para iniciar o Modo Infinito.");
                return;
            }

            InfiniteModeSession.Begin();
            SceneManager.LoadScene(gameplayScene);
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

            RefreshPhaseSelectionUI();
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

            RefreshPhaseSelectionUI();

            Debug.Log("[MainMenuManager] Dados do jogo resetados pelo jogador.");
        }
    }
}
