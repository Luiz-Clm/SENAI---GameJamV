using UnityEngine;
using UnityEngine.UI;

namespace SpellGlide
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Painéis de UI")]
        [SerializeField] private GameObject menuRootPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Botões do Menu Principal")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

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

            // Inicia a exibição dos 6 quadrinhos com a história
            if (comicIntroManager != null)
            {
                comicIntroManager.StartStory();
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] ComicIntroManager não encontrado na cena!");
            }
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
    }
}
