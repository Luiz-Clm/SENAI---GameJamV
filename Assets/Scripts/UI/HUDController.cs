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
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.35f, 0.45f, 1f);
        [SerializeField] private Color emptyHeartColor = new Color(0.25f, 0.2f, 0.25f, 0.5f);

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
        [SerializeField] private TextMeshProUGUI bossNameText;

        [Header("Potion Inventory HUD")]
        [SerializeField] private TextMeshProUGUI potionCountText;
        [SerializeField] private GameObject potionIconContainer;

        private void Awake()
        {
            AutoFindReferences();
        }

        private void AutoFindReferences()
        {
            if (bossBarContainer == null)
            {
                var t = transform.Find("TopBar/BossBar_Wrapper");
                if (t == null) t = transform.Find("TopBar/BossHealthBar");
                if (t == null)
                {
                    foreach (var tr in GetComponentsInChildren<Transform>(true))
                    {
                        if (tr.name.Contains("BossBar") || tr.name.Contains("BossHealth")) { t = tr; break; }
                    }
                }
                if (t != null) bossBarContainer = t.gameObject;
            }

            if (bossHealthFill == null && bossBarContainer != null)
            {
                foreach (var img in bossBarContainer.GetComponentsInChildren<Image>(true))
                {
                    if (img.name.Equals("Fill", System.StringComparison.OrdinalIgnoreCase))
                    {
                        bossHealthFill = img;
                        break;
                    }
                }
            }

            if (bossNameText == null && bossBarContainer != null)
            {
                bossNameText = bossBarContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (potionIconContainer == null)
            {
                var p = transform.Find("BottomBar/PotionInventory");
                if (p != null) potionIconContainer = p.gameObject;
            }

            if (potionCountText == null && potionIconContainer != null)
            {
                potionCountText = potionIconContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void Start()
        {
            AutoFindReferences();

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

            // Garante que a barra do boss começa escondida
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }

            // Inicializa contador de poções
            UpdatePotionCount(0);
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
            bool isFireUnlocked = (playerShooting != null) ? playerShooting.IsFireUnlocked : false;

            // O quadrado de fogo fica COMPLETAMENTE OCULTO até ser desbloqueado no jogo
            if (fireBadge != null)
            {
                fireBadge.gameObject.SetActive(isFireUnlocked);
            }

            if (activeElement == ElementType.Ice)
            {
                if (iceBadge != null) iceBadge.color = activeIceColor;
                if (iceText != null) iceText.color = Color.white;

                if (fireBadge != null && isFireUnlocked)
                {
                    fireBadge.color = inactiveFireColor;
                }
                if (fireText != null && isFireUnlocked)
                {
                    fireText.text = "2: FOGO";
                    fireText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
                }
            }
            else if (activeElement == ElementType.Fire && isFireUnlocked)
            {
                if (iceBadge != null) iceBadge.color = inactiveIceColor;
                if (iceText != null) iceText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);

                if (fireBadge != null) fireBadge.color = activeFireColor;
                if (fireText != null)
                {
                    fireText.text = "2: FOGO";
                    fireText.color = Color.white;
                }
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

        /// <summary>
        /// Ativa e atualiza a barra de vida do boss. Deve ser chamada ANTES de iniciar a luta.
        /// normalizedHealth: valor entre 0 e 1.
        /// </summary>
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

        public void ShowBossBar(string bossName = "GOLEM")
        {
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(true);
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            if (bossHealthFill != null)
            {
                bossHealthFill.fillAmount = 1f;
            }
        }

        public void HideBossBar()
        {
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Atualiza o contador de poções no HUD.
        /// </summary>
        public void UpdatePotionCount(int count)
        {
            if (potionCountText != null)
            {
                potionCountText.text = count > 0 ? $"[F] x{count}" : "";
            }

            if (potionIconContainer != null)
            {
                potionIconContainer.SetActive(count > 0);
            }
        }
    }
}
