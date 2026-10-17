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
        [Header("Heart Custom Sprites")]
        [Tooltip("Sprite do coração cheio (UI/coracao_cheio_20x20)")]
        [SerializeField] private Sprite fullHeartSprite;
        [Tooltip("Sprite do coração vazio (UI/coracao_vazio_20x20)")]
        [SerializeField] private Sprite emptyHeartSprite;

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

            if (customBossBarBackgroundSprite == null)
            {
                customBossBarBackgroundSprite = LoadSpriteResourceOrAsset("boss_barra_fundo_decorado_192x36");
            }
            if (customBossBarFillSprite == null)
            {
                customBossBarFillSprite = LoadSpriteResourceOrAsset("boss_barra_vida_148x12");
            }
            if (fullHeartSprite == null)
            {
                fullHeartSprite = LoadSpriteResourceOrAsset("coracao_cheio_20x20");
            }
            if (emptyHeartSprite == null)
            {
                emptyHeartSprite = LoadSpriteResourceOrAsset("coracao_vazio_20x20");
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

        private Sprite LoadSpriteResourceOrAsset(string spriteName)
        {
            Sprite s = Resources.Load<Sprite>("Sprites/" + spriteName);
            if (s != null) return s;

            Sprite[] all = Resources.LoadAll<Sprite>("Sprites/" + spriteName);
            if (all != null && all.Length > 0) return all[0];

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(spriteName + " t:Sprite");
            if (guids != null && guids.Length > 0)
            {
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(p);
                foreach (var obj in assets)
                {
                    if (obj is Sprite sp) return sp;
                }
                s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) return s;
            }
#endif
            return null;
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
                    if (fullHeartSprite != null)
                    {
                        heartImages[i].sprite = fullHeartSprite;
                        heartImages[i].color = Color.white;
                    }
                    else
                    {
                        heartImages[i].color = fullHeartColor;
                    }
                    heartImages[i].preserveAspect = true;
                    heartImages[i].gameObject.SetActive(true);
                }
                else if (i < maxHealth)
                {
                    if (emptyHeartSprite != null)
                    {
                        heartImages[i].sprite = emptyHeartSprite;
                        heartImages[i].color = Color.white;
                    }
                    else
                    {
                        heartImages[i].color = emptyHeartColor;
                    }
                    heartImages[i].preserveAspect = true;
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

        /// <summary>
        /// Aplica os sprites personalizados da barra de vida do Boss (fundo e fill).
        /// Pode ser chamado via Inspector ou programaticamente.
        /// </summary>
        public void ApplyCustomBossBarSprites()
        {
            if (customBossBarBackgroundSprite == null)
                customBossBarBackgroundSprite = LoadSpriteResourceOrAsset("boss_barra_fundo_decorado_192x36");
            if (customBossBarFillSprite == null)
                customBossBarFillSprite = LoadSpriteResourceOrAsset("boss_barra_vida_148x12");

            if (customBossBarBackgroundSprite != null && bossBarBackground != null)
            {
                bossBarBackground.sprite = customBossBarBackgroundSprite;
                bossBarBackground.color = Color.white;
                bossBarBackground.preserveAspect = true;

                // Ajusta proporção exata para pixel art 192x36 (escala de 1.5x = 288x54) evitando qualquer esticamento
                var bgRt = bossBarBackground.rectTransform;
                if (bgRt != null)
                {
                    bgRt.anchorMin = new Vector2(0.5f, 0.5f);
                    bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                    bgRt.pivot = new Vector2(0.5f, 0.5f);
                    bgRt.sizeDelta = new Vector2(288f, 54f);
                    bgRt.anchoredPosition = new Vector2(0f, -6f);
                }
            }

            if (bossNameText != null)
            {
                var nameRt = bossNameText.rectTransform;
                if (nameRt != null)
                {
                    nameRt.anchorMin = new Vector2(0.5f, 1f);
                    nameRt.anchorMax = new Vector2(0.5f, 1f);
                    nameRt.pivot = new Vector2(0.5f, 1f);
                    nameRt.anchoredPosition = new Vector2(0f, 0f);
                    nameRt.sizeDelta = new Vector2(300f, 20f);
                    bossNameText.alignment = TextAlignmentOptions.Center;
                    bossNameText.fontSize = 18f;
                }
            }

            if (customBossBarFillSprite != null && bossHealthFill != null)
            {
                bossHealthFill.sprite = customBossBarFillSprite;
                bossHealthFill.type = Image.Type.Filled;
                bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
                bossHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                bossHealthFill.color = Color.white;

                // Encaixe milimétrico do miolo de vida dentro da moldura decorada (192x36 com área útil 148x12)
                var fillRt = bossHealthFill.rectTransform;
                if (fillRt != null)
                {
                    fillRt.anchorMin = new Vector2(22f / 192f, 12f / 36f);
                    fillRt.anchorMax = new Vector2((192f - 22f) / 192f, (36f - 12f) / 36f);
                    fillRt.offsetMin = Vector2.zero;
                    fillRt.offsetMax = Vector2.zero;
                }
            }
        }
    }
}
