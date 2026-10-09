using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WitchShmup.Combat;
using WitchShmup.Player;

namespace WitchShmup.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerShooting playerShooting;

        [Header("Hearts (Health)")]
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.35f, 0.45f, 1f); // Vibrant Pink/Red
        [SerializeField] private Color emptyHeartColor = new Color(0.25f, 0.2f, 0.25f, 0.5f); // Dim Dark Grey

        [Header("Element Indicator (HUD Buttons)")]
        [SerializeField] private Image iceBadge;
        [SerializeField] private Image fireBadge;
        [SerializeField] private TextMeshProUGUI iceText;
        [SerializeField] private TextMeshProUGUI fireText;

        [Header("Colors for Elements")]
        [SerializeField] private Color activeIceColor = new Color(0.2f, 0.85f, 1f, 1f);
        [SerializeField] private Color inactiveIceColor = new Color(0.1f, 0.35f, 0.45f, 0.5f);
        [SerializeField] private Color activeFireColor = new Color(1f, 0.5f, 0.1f, 1f);
        [SerializeField] private Color inactiveFireColor = new Color(0.45f, 0.25f, 0.05f, 0.5f);

        [Header("Score & Stage Info")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI hiScoreText;

        [Header("Boss Bar")]
        [SerializeField] private GameObject bossBarContainer;
        [SerializeField] private Image bossHealthFill;

        private void Start()
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (playerShooting == null)
            {
                playerShooting = FindFirstObjectByType<PlayerShooting>();
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHearts;
                UpdateHearts(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (playerShooting != null)
            {
                playerShooting.OnElementChanged += UpdateElementUI;
                UpdateElementUI(playerShooting.CurrentElement);
            }

            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHearts;
            }

            if (playerShooting != null)
            {
                playerShooting.OnElementChanged -= UpdateElementUI;
            }
        }

        public void UpdateHearts(int currentHealth, int maxHealth)
        {
            if (heartImages == null || heartImages.Length == 0) return;

            // Suporta expansão dinâmica de corações caso o player compre upgrades de vida
            if (maxHealth > heartImages.Length && heartImages[0] != null)
            {
                var parent = heartImages[0].transform.parent;
                var list = new System.Collections.Generic.List<Image>(heartImages);
                while (list.Count < maxHealth)
                {
                    var newHeart = Instantiate(heartImages[0].gameObject, parent);
                    newHeart.name = $"Heart_{list.Count}";
                    list.Add(newHeart.GetComponent<Image>());
                }
                heartImages = list.ToArray();
            }

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;

                if (i < currentHealth)
                {
                    heartImages[i].color = fullHeartColor;
                    heartImages[i].gameObject.SetActive(true);
                }
                else if (i < maxHealth)
                {
                    heartImages[i].color = emptyHeartColor;
                    heartImages[i].gameObject.SetActive(true);
                }
                else
                {
                    heartImages[i].gameObject.SetActive(false);
                }
            }
        }

        public void UpdateElementUI(ElementType activeElement)
        {
            if (activeElement == ElementType.Ice)
            {
                if (iceBadge != null) iceBadge.color = activeIceColor;
                if (fireBadge != null) fireBadge.color = inactiveFireColor;
                if (iceText != null) iceText.color = Color.white;
                if (fireText != null) fireText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            }
            else if (activeElement == ElementType.Fire)
            {
                if (iceBadge != null) iceBadge.color = inactiveIceColor;
                if (fireBadge != null) fireBadge.color = activeFireColor;
                if (iceText != null) iceText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
                if (fireText != null) fireText.color = Color.white;
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE {score:D7}";
            }
        }

        public void SetStageTitle(string stageTitle)
        {
            if (stageText != null)
            {
                stageText.text = stageTitle;
            }
        }

        public void SetBossHealth(float normalizedHealth)
        {
            if (bossBarContainer != null && !bossBarContainer.activeSelf)
            {
                bossBarContainer.SetActive(true);
            }

            if (bossHealthFill != null)
            {
                bossHealthFill.fillAmount = Mathf.Clamp01(normalizedHealth);
            }
        }

        public void HideBossBar()
        {
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }
        }
    }
}
