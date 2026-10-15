using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpellGlide
{
    public class ComicIntroManager : MonoBehaviour
    {
        [Header("Configuração dos Quadrinhos")]
        [Tooltip("As 6 imagens dos quadrinhos (1.png a 6.png)")]
        [SerializeField] private Sprite[] comicSprites = new Sprite[6];

        [Tooltip("Texto narrativo de cada quadrinho")]
        [TextArea(3, 8)]
        [SerializeField] private string[] comicTexts = new string[6]
        {
            "Coloque seu texto aqui",
            "Coloque seu texto aqui",
            "Coloque seu texto aqui",
            "Coloque seu texto aqui",
            "Coloque seu texto aqui",
            "Coloque seu texto aqui"
        };

        [Header("Tempos e Transições")]
        [Tooltip("Tempo de Fade In do quadrinho (segundos)")]
        [SerializeField] private float fadeInDuration = 1.0f;

        [Tooltip("Tempo de Fade Out do quadrinho (segundos)")]
        [SerializeField] private float fadeOutDuration = 0.8f;

        [Tooltip("Tempo de exibição do quadrinho antes do próximo (10 segundos padrão)")]
        [SerializeField] private float panelDisplayDuration = 10.0f;

        [Tooltip("Velocidade de digitação de cada letra (segundos)")]
        [SerializeField] private float typewriterSpeed = 0.04f;

        [Tooltip("Nome da cena do jogo a ser carregada após o último quadrinho")]
        [SerializeField] private string nextSceneName = "Fase";

        [Header("Referências de Interface")]
        [SerializeField] private GameObject comicRootPanel;
        [SerializeField] private Image comicImage;
        [SerializeField] private CanvasGroup comicContentCanvasGroup;
        [SerializeField] private TMP_Text storyTmpText;
        [SerializeField] private Text storyUiText;
        [SerializeField] private TMP_Text skipPromptTmpText;
        [SerializeField] private Text skipPromptUiText;
        [SerializeField] private CanvasGroup screenFader;

        [Header("Áudio")]
        [SerializeField] private AudioManager audioManager;

        // Estado interno
        private int currentPanelIndex = 0;
        private bool isStoryRunning = false;
        private bool isTypewriterRunning = false;
        private bool requestSkipTypewriter = false;
        private bool requestAdvancePanel = false;
        private Coroutine comicSequenceCoroutine;

        private void Awake()
        {
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
            }

            // Garante que o texto de pular esteja com o texto pedido
            SetSkipPromptText("Aperte qualquer tecla para pular");

            // Inicializa painel desativado ou transparente
            if (comicRootPanel != null)
            {
                comicRootPanel.SetActive(false);
            }

            if (comicContentCanvasGroup != null)
            {
                comicContentCanvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (!isStoryRunning) return;

            if (CheckAnyInputDown())
            {
                HandlePlayerSkipInput();
            }
        }

        public void StartStory()
        {
            if (isStoryRunning) return;

            isStoryRunning = true;
            currentPanelIndex = 0;

            if (comicRootPanel != null)
            {
                comicRootPanel.SetActive(true);
            }

            comicSequenceCoroutine = StartCoroutine(ComicStoryRoutine());
        }

        private IEnumerator ComicStoryRoutine()
        {
            // Diminui/para a música do menu suavemente ao começar a história
            if (audioManager != null)
            {
                audioManager.StopMenuMusic(1.5f);
            }

            int totalPanels = Mathf.Min(comicSprites != null ? comicSprites.Length : 0, 6);
            if (totalPanels == 0) totalPanels = 6;

            for (int i = 0; i < totalPanels; i++)
            {
                currentPanelIndex = i;
                requestAdvancePanel = false;
                requestSkipTypewriter = false;

                // 1. Configura a imagem do quadrinho
                if (comicImage != null && comicSprites != null && i < comicSprites.Length && comicSprites[i] != null)
                {
                    comicImage.sprite = comicSprites[i];
                    comicImage.enabled = true;
                }

                // Limpa o texto da história
                SetStoryText("");

                // 2. Fade In do quadrinho e do container de texto
                yield return StartCoroutine(FadeCanvasGroup(comicContentCanvasGroup, 0f, 1f, fadeInDuration));

                // 3. Efeito Máquina de Escrever (Typewriter) com áudio
                string textToDisplay = GetPanelText(i);
                yield return StartCoroutine(TypewriterRoutine(textToDisplay));

                // 4. Aguarda 10 segundos OU até o jogador apertar para pular
                float timer = 0f;
                while (timer < panelDisplayDuration && !requestAdvancePanel)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                // 5. Fade Out do quadrinho e texto antes de ir para o próximo
                yield return StartCoroutine(FadeCanvasGroup(comicContentCanvasGroup, 1f, 0f, fadeOutDuration));

                // Pequeno intervalo limpo entre quadrinhos
                yield return new WaitForSeconds(0.2f);
            }

            // Final da HQ: Fade out para preto e carrega a cena do jogo
            if (screenFader != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(screenFader, 0f, 1f, 1.0f));
            }

            LoadGameplayScene();
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            isTypewriterRunning = true;
            requestSkipTypewriter = false;

            int charCount = fullText.Length;
            for (int i = 0; i <= charCount; i++)
            {
                if (requestSkipTypewriter)
                {
                    // Jogador pulou a digitação: completa o texto na hora!
                    SetStoryText(fullText);
                    break;
                }

                string currentSubstr = fullText.Substring(0, i);
                SetStoryText(currentSubstr);

                // Toca som de digitação para caracteres visíveis (não espaços vazios)
                if (i > 0 && i < charCount && !char.IsWhiteSpace(fullText[i - 1]))
                {
                    if (audioManager != null)
                    {
                        audioManager.PlayTypewriterSound();
                    }
                }

                yield return new WaitForSeconds(typewriterSpeed);
            }

            isTypewriterRunning = false;
            requestSkipTypewriter = false;
        }

        private void HandlePlayerSkipInput()
        {
            if (audioManager != null)
            {
                audioManager.PlaySkipSound();
            }

            if (isTypewriterRunning)
            {
                // Primeiro clique: completa o texto imediatamente
                requestSkipTypewriter = true;
            }
            else
            {
                // Texto já está completo: avança para o próximo quadrinho
                requestAdvancePanel = true;
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
        {
            if (cg == null) yield break;

            cg.alpha = startAlpha;
            if (duration <= 0f)
            {
                cg.alpha = endAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            cg.alpha = endAlpha;
        }

        private string GetPanelText(int index)
        {
            if (comicTexts != null && index >= 0 && index < comicTexts.Length && !string.IsNullOrEmpty(comicTexts[index]))
            {
                return comicTexts[index];
            }
            return "Coloque seu texto aqui";
        }

        private void SetStoryText(string text)
        {
            if (storyTmpText != null) storyTmpText.text = text;
            if (storyUiText != null) storyUiText.text = text;
        }

        private void SetSkipPromptText(string text)
        {
            if (skipPromptTmpText != null) skipPromptTmpText.text = text;
            if (skipPromptUiText != null) skipPromptUiText.text = text;
        }

        private bool CheckAnyInputDown()
        {
            // Clique do mouse
            if (Input.GetMouseButtonDown(0)) return true;

            // Teclado legado
            try
            {
                if (Input.anyKeyDown) return true;
            }
            catch
            {
                // Caso legado não esteja ativo
            }

            // Novo Input System (se ativo no projeto)
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            #endif

            return false;
        }

        private void LoadGameplayScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Fallback: carrega a cena 1 da build
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
            }
        }
    }
}
