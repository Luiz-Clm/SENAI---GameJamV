using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button playAgainButton;

        private void Awake()
        {
            if (playAgainButton == null)
            {
                playAgainButton = GetComponentInChildren<Button>(true);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                if (finalScoreText != null)
                {
                    finalScoreText.text = $"PONTUAÇÃO FINAL: {GameManager.Instance.CurrentScore:D7}\nRECORDE: {GameManager.Instance.HighScore:D7}";
                }

                if (statsText != null)
                {
                    statsText.text = $"Skill Points Restantes: {GameManager.Instance.SkillPoints}\nCorações de Fogo Coletados: {GameManager.Instance.FireHeartsCount}";
                }
            }
        }

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
