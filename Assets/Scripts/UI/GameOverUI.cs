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
            // Keep Inspector references authoritative; use hierarchy names only as a fallback.
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button candidate in buttons)
            {
                if (retryButton == null && candidate.gameObject.name == "Btn_Retry")
                    retryButton = candidate;
                else if (menuButton == null && candidate.gameObject.name == "Btn_MenuPrincipal")
                    menuButton = candidate;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
                ButtonClickSound.Bind(retryButton);
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(OnMenuClicked);
                ButtonClickSound.Bind(menuButton);
            }
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
