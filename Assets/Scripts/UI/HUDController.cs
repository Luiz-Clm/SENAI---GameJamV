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
        [Tooltip("Sprite do coração cheio. Se AMBOS (cheio e vazio) estiverem preenchidos, os sprites são usados no lugar das cores.")]
        [SerializeField] private Sprite fullHeartSprite;
        [Tooltip("Sprite do coração vazio.")]
        [SerializeField] private Sprite emptyHeartSprite;
        [Tooltip("Cores usadas apenas quando NÃO há sprites de coração atribuídos.")]
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.35f, 0.45f, 1f);
        [SerializeField] private Color emptyHeartColor = new Color(0.25f, 0.2f, 0.25f, 0.5f);

        [Header("Element Indicator (HUD Buttons)")]
        [SerializeField] private Image iceBadge;
        [SerializeField] private Image fireBadge;
        [SerializeField] private TextMeshProUGUI iceText;
        [SerializeField] private TextMeshProUGUI fireText;

        [Header("Element Sprites (Equipado / Desequipado)")]
        [Tooltip("Se AMBOS os sprites do gelo estiverem preenchidos, eles são usados no lugar das cores.")]
        [SerializeField] private Sprite iceEquippedSprite;
        [SerializeField] private Sprite iceUnequippedSprite;
        [Tooltip("Se AMBOS os sprites do fogo estiverem preenchidos, eles são usados no lugar das cores.")]
        [SerializeField] private Sprite fireEquippedSprite;
        [SerializeField] private Sprite fireUnequippedSprite;
        [Tooltip("Esconde os textos '1: GELO' / '2: FOGO' quando o sprite do elemento já traz o nome e a tecla desenhados.")]
        [SerializeField] private bool hideElementTextWhenUsingSprites = true;

        [Header("Colors for Elements (usadas só quando NÃO há sprites)")]
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
        [SerializeField] private Image bossBarBackground;
        [SerializeField] private Image bossHealthFill;
        [SerializeField] private TextMeshProUGUI bossNameText;

        [Header("Boss Bar Custom Sprites")]
        [Tooltip("Arraste aqui o Sprite do fundo / moldura da barra de vida do Boss")]
        [SerializeField] private Sprite customBossBarBackgroundSprite;
        [Tooltip("Arraste aqui o Sprite de preenchimento (vida) da barra do Boss")]
        [SerializeField] private Sprite customBossBarFillSprite;

        [Header("Potion Inventory HUD")]
        [SerializeField] private TextMeshProUGUI potionCountText;
        [SerializeField] private GameObject potionIconContainer;

        private static readonly Color InactiveTextColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

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

            if (bossBarBackground == null && bossBarContainer != null)
            {
                foreach (var img in bossBarContainer.GetComponentsInChildren<Image>(true))
                {
                    if (img != bossHealthFill && (img.name.Contains("Back") || img.name.Contains("Bg") || img.name.Contains("Bar")))
                    {
                        bossBarBackground = img;
                        break;
                    }
                }
                if (bossBarBackground == null)
                {
                    foreach (var img in bossBarContainer.GetComponentsInChildren<Image>(true))
                    {
                        if (img != bossHealthFill) { bossBarBackground = img; break; }
                    }
                }
            }

            if (bossNameText == null && bossBarContainer != null)
            {
                bossNameText = bossBarContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            ApplyCustomBossBarSprites();

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

            // Só usa sprites se os dois (cheio e vazio) estiverem atribuídos
            bool useHeartSprites = fullHeartSprite != null && emptyHeartSprite != null;

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;

                if (i < currentHealth)
                {
                    ApplyHeartVisual(heartImages[i], true, useHeartSprites);
                    heartImages[i].gameObject.SetActive(true);
                }
                else if (i < maxHealth)
                {
                    ApplyHeartVisual(heartImages[i], false, useHeartSprites);
                    heartImages[i].gameObject.SetActive(true);
                }
                else
                {
                    heartImages[i].gameObject.SetActive(false);
                }
            }
        }

        private void ApplyHeartVisual(Image heart, bool isFull, bool useSprites)
        {
            if (useSprites)
            {
                heart.sprite = isFull ? fullHeartSprite : emptyHeartSprite;
                heart.color = Color.white; // sem tint, para não alterar as cores do sprite
                heart.preserveAspect = true;
            }
            else
            {
                heart.color = isFull ? fullHeartColor : emptyHeartColor;
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

            if (fireText != null && isFireUnlocked)
            {
                fireText.text = "2: FOGO";
            }

            if (activeElement == ElementType.Ice)
            {
                ApplyElementVisual(iceBadge, iceText, true,
                    iceEquippedSprite, iceUnequippedSprite, activeIceColor, inactiveIceColor);

                if (isFireUnlocked)
                {
                    ApplyElementVisual(fireBadge, fireText, false,
                        fireEquippedSprite, fireUnequippedSprite, activeFireColor, inactiveFireColor);
                }
            }
            else if (activeElement == ElementType.Fire && isFireUnlocked)
            {
                ApplyElementVisual(iceBadge, iceText, false,
                    iceEquippedSprite, iceUnequippedSprite, activeIceColor, inactiveIceColor);

                ApplyElementVisual(fireBadge, fireText, true,
                    fireEquippedSprite, fireUnequippedSprite, activeFireColor, inactiveFireColor);
            }
        }

        /// <summary>
        /// Aplica o visual de um elemento (equipado ou não). Se os dois sprites do elemento
        /// estiverem atribuídos, usa os sprites; caso contrário, cai no comportamento antigo por cor.
        /// </summary>
        private void ApplyElementVisual(Image badge, TextMeshProUGUI label, bool equipped,
            Sprite equippedSprite, Sprite unequippedSprite, Color activeColor, Color inactiveColor)
        {
            bool useSprites = equippedSprite != null && unequippedSprite != null;

            if (badge != null)
            {
                if (useSprites)
                {
                    badge.sprite = equipped ? equippedSprite : unequippedSprite;
                    badge.color = Color.white;
                    badge.preserveAspect = true;
                }
                else
                {
                    badge.color = equipped ? activeColor : inactiveColor;
                }
            }

            if (label != null)
            {
                if (useSprites && hideElementTextWhenUsingSprites)
                {
                    label.gameObject.SetActive(false);
                }
                else
                {
                    label.gameObject.SetActive(true);
                    label.color = equipped ? Color.white : InactiveTextColor;
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

        /// <summary>
        /// Aplica os sprites personalizados da barra de vida do Boss (fundo e fill).
        /// Pode ser chamado via Inspector ou programaticamente.
        /// </summary>
        public void ApplyCustomBossBarSprites()
        {
            if (customBossBarBackgroundSprite != null && bossBarBackground != null)
            {
                bossBarBackground.sprite = customBossBarBackgroundSprite;
                bossBarBackground.color = Color.white;
            }

            if (customBossBarFillSprite != null && bossHealthFill != null)
            {
                bossHealthFill.sprite = customBossBarFillSprite;
                bossHealthFill.type = Image.Type.Filled;
                bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
                bossHealthFill.color = Color.white;
            }
        }
    }
}