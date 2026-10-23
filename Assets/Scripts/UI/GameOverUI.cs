using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;
using WitchShmup.Endless;

namespace WitchShmup.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            if (retryButton == null)
            {
                retryButton = GetComponentInChildren<Button>(true);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
                ButtonClickSound.Bind(retryButton);
            }

            if (InfiniteModeSession.IsActive) EnsureInfiniteMenuButton();
        }

        private void OnEnable()
        {
            if (finalScoreText != null && GameManager.Instance != null)
            {
                if (GameManager.Instance.IsInfiniteMode)
                {
                    int wave = InfiniteWaveManager.Instance != null ? InfiniteWaveManager.Instance.CurrentWave : 1;
                    int record = InfiniteModeSession.RecordWave(wave);
                    finalScoreText.text = $"ONDA ALCANÇADA: {wave}\nRECORDE: {record}";
                }
                else
                {
                    finalScoreText.text = $"PONTUAÇÃO FINAL: {GameManager.Instance.CurrentScore:D7}\nRECORDE: {GameManager.Instance.HighScore:D7}";
                }
            }
        }

        private void OnRetryClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        private void EnsureInfiniteMenuButton()
        {
            if (menuButton != null) return;

            var buttonObject = new GameObject("Btn_ReturnToMenu", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 60f);
            rect.anchoredPosition = new Vector2(221f, -95f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.22f, 0.16f, 0.35f, 1f);
            menuButton = buttonObject.GetComponent<Button>();
            menuButton.onClick.AddListener(OnMenuClicked);
            ButtonClickSound.Bind(menuButton);

            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "MENU PRINCIPAL";
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void OnMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMenu();
                return;
            }

            InfiniteModeSession.End();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }
}
