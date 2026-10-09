using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Button retryButton;

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
            }
        }

        private void OnEnable()
        {
            if (finalScoreText != null && GameManager.Instance != null)
            {
                finalScoreText.text = $"PONTUAÇÃO FINAL: {GameManager.Instance.CurrentScore:D7}\nRECORDE: {GameManager.Instance.HighScore:D7}";
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
    }
}
