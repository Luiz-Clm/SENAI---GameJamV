using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WitchShmup.Combat;
using WitchShmup.Core;
using WitchShmup.Player;

namespace WitchShmup.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerShooting playerShooting;
        [SerializeField] private PlayerMana playerMana;

        [Header("Mana Bar (Barra de Mana)")]
        [Tooltip("Container pai da barra de mana")]
        [SerializeField] private GameObject manaBarContainer;
        [Tooltip("Imagem do preenchimento da barra de mana")]
        [SerializeField] private Image manaBarFill;
        [Tooltip("Imagem do fundo da barra de mana")]
        [SerializeField] private Image manaBarBackground;
        [Tooltip("Texto com status da barra de mana (exibe 'MANA' ou 'CANSADO')")]
        [SerializeField] private TextMeshProUGUI manaText;
        [Tooltip("Cor da barra em estado normal (Azul Arcano)")]
        [SerializeField] private Color normalManaColor = new Color(0.2f, 0.65f, 1f, 1f);
        [Tooltip("Cor da barra quando o mago fica cansado (Alerta)")]
        [SerializeField] private Color exhaustedManaColor = new Color(0.95f, 0.3f, 0.25f, 1f);
        [Tooltip("Cor do texto quando exausto")]
        [SerializeField] private Color exhaustedTextColor = new Color(1f, 0.45f, 0.45f, 1f);

        [Header("Hearts (Health)")]
        [SerializeField] private Image[] heartImages;
        [Tooltip("Image used as the template when a health upgrade adds more heart slots.")]
        [SerializeField] private Image heartTemplate;
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

        [Header("Element Slot Custom Sprites (Sprites dos Slots de Magia)")]
        [Tooltip("Sprite do slot de gelo ativo (UI/slot_gelo_ativo_nativo)")]
        [SerializeField] private Sprite slotIceActiveSprite;
        [Tooltip("Sprite do slot de gelo apagado (UI/slot_gelo_apagado_nativo)")]
        [SerializeField] private Sprite slotIceInactiveSprite;
        [Tooltip("Sprite do slot de fogo ativo (UI/slot_fogo_ativo_nativo)")]
        [SerializeField] private Sprite slotFireActiveSprite;
        [Tooltip("Sprite do slot de fogo apagado (UI/slot_fogo_apagado_nativo)")]
        [SerializeField] private Sprite slotFireInactiveSprite;

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

        [Header("Skill Points HUD")]
        [Tooltip("Texto persistente com a quantidade de pontos de habilidade disponíveis")]
        [SerializeField] private TextMeshProUGUI skillPointsCountText;
        [Tooltip("Instrução para abrir a árvore; pisca ao ganhar pontos")]
        [SerializeField] private TextMeshProUGUI skillPointsPromptText;
        [SerializeField] private string skillPointsCountFormat = "Skill Points disponíveis: {0}";
        [SerializeField] private string skillPointsPrompt = "Pressione T para abrir a árvore de habilidades";
        [SerializeField, Min(0.1f)] private float skillPointsPromptBlinkDuration = 4f;
        [SerializeField, Min(0.05f)] private float skillPointsPromptBlinkInterval = 0.3f;

        private GameManager subscribedGameManager;
        private Coroutine skillPointsBlinkCoroutine;
        private int lastDisplayedSkillPoints;
        private bool skillPointsHudInitialized;

        private void Awake()
        {
            AutoFindReferences();
        }

        private void AutoFindReferences()
        {
            // ── Barra do Boss ──
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

            // Sprites: carrega automaticamente APENAS se nao foram atribuidos no Inspector
            if (customBossBarBackgroundSprite == null)
                customBossBarBackgroundSprite = LoadSpriteResourceOrAsset("boss_barra_fundo_decorado_192x36");
            if (customBossBarFillSprite == null)
                customBossBarFillSprite = LoadSpriteResourceOrAsset("boss_barra_vida_148x12");

            // Sprites de coracao: carrega se nao atribuido no inspector
            if (fullHeartSprite == null)
                fullHeartSprite = LoadSpriteResourceOrAsset("coracao_cheio_20x20");
            if (emptyHeartSprite == null)
                emptyHeartSprite = LoadSpriteResourceOrAsset("coracao_vazio_20x20");

            // Sprites dos slots de elemento:
            // NAO sobrescreve se ja foram atribuidos no inspector!
            if (slotIceActiveSprite == null)
                slotIceActiveSprite = LoadSpriteResourceOrAsset("slot_gelo_ativo_nativo");
            if (slotIceInactiveSprite == null)
                slotIceInactiveSprite = LoadSpriteResourceOrAsset("slot_gelo_apagado_nativo");
            if (slotFireActiveSprite == null)
                slotFireActiveSprite = LoadSpriteResourceOrAsset("slot_fogo_ativo_nativo");
            if (slotFireInactiveSprite == null)
                slotFireInactiveSprite = LoadSpriteResourceOrAsset("slot_fogo_apagado_nativo");

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
            InitializeSkillPointsHUD();

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

            if (playerMana == null)
            {
                if (playerShooting != null) playerMana = playerShooting.Mana;
                if (playerMana == null) playerMana = FindFirstObjectByType<PlayerMana>();
            }

            EnsureManaBarSetup();

            if (playerMana != null)
            {
                playerMana.OnManaChanged += UpdateManaUI;
                playerMana.OnExhaustionChanged += HandleExhaustionUI;
                playerMana.OnShootAttemptFailed += FlashManaBarAttempt;
                UpdateManaUI(playerMana.CurrentMana, playerMana.MaxMana);
                HandleExhaustionUI(playerMana.IsExhausted);
            }

            // Garante que a barra do boss começa escondida
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }

            // Inicializa contador de poções
            UpdatePotionCount(0);
        }

        private void InitializeSkillPointsHUD()
        {
            FindSkillPointsTextsIfNeeded();
            subscribedGameManager = GameManager.Instance;
            if (subscribedGameManager == null)
                subscribedGameManager = FindFirstObjectByType<GameManager>();
            if (subscribedGameManager == null)
            {
                RefreshSkillPointsHUD(0);
                return;
            }

            subscribedGameManager.OnSkillPointsChanged += HandleSkillPointsChanged;
            lastDisplayedSkillPoints = subscribedGameManager.SkillPoints;
            skillPointsHudInitialized = true;
            RefreshSkillPointsHUD(lastDisplayedSkillPoints);
            if (lastDisplayedSkillPoints > 0)
                StartSkillPointsPromptBlink();
        }

        private void Update()
        {
            FindSkillPointsTextsIfNeeded();

            if (subscribedGameManager == null)
            {
                subscribedGameManager = GameManager.Instance;
                if (subscribedGameManager == null)
                    subscribedGameManager = FindFirstObjectByType<GameManager>();

                if (subscribedGameManager != null)
                {
                    subscribedGameManager.OnSkillPointsChanged += HandleSkillPointsChanged;
                    lastDisplayedSkillPoints = subscribedGameManager.SkillPoints;
                    skillPointsHudInitialized = true;
                    RefreshSkillPointsHUD(lastDisplayedSkillPoints);
                    if (lastDisplayedSkillPoints > 0)
                        StartSkillPointsPromptBlink();
                }
                return;
            }

            // Keep the HUD in sync even if another system changes the total
            // without raising the notification event.
            int currentTotal = subscribedGameManager.SkillPoints;
            if (!skillPointsHudInitialized)
            {
                lastDisplayedSkillPoints = currentTotal;
                skillPointsHudInitialized = true;
                RefreshSkillPointsHUD(currentTotal);
            }
            else if (currentTotal != lastDisplayedSkillPoints)
            {
                HandleSkillPointsChanged(currentTotal);
            }
        }

        private void FindSkillPointsTextsIfNeeded()
        {
            if (skillPointsCountText != null && skillPointsPromptText != null) return;

            foreach (TextMeshProUGUI candidate in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (skillPointsCountText == null && candidate.gameObject.name == "SkillPointsCountText")
                    skillPointsCountText = candidate;
                else if (skillPointsPromptText == null && candidate.gameObject.name == "SkillPointsPromptText")
                    skillPointsPromptText = candidate;
            }
        }

        private void HandleSkillPointsChanged(int newTotal)
        {
            bool gainedPoints = newTotal > lastDisplayedSkillPoints;
            lastDisplayedSkillPoints = newTotal;
            RefreshSkillPointsHUD(newTotal);

            if (newTotal <= 0)
            {
                if (skillPointsBlinkCoroutine != null)
                    StopCoroutine(skillPointsBlinkCoroutine);
                skillPointsBlinkCoroutine = null;
                SetSkillPointsHUDVisible(false);
            }
            else if (gainedPoints)
            {
                StartSkillPointsPromptBlink();
            }
        }

        private void RefreshSkillPointsHUD(int total)
        {
            bool hasPoints = total > 0;
            if (skillPointsCountText != null)
            {
                skillPointsCountText.text = string.Format(skillPointsCountFormat, total);
                skillPointsCountText.gameObject.SetActive(true);
                skillPointsCountText.enabled = true;
            }

            if (skillPointsPromptText != null)
            {
                skillPointsPromptText.text = skillPointsPrompt;
                skillPointsPromptText.gameObject.SetActive(hasPoints);
                skillPointsPromptText.enabled = true;
            }
        }

        private void SetSkillPointsHUDVisible(bool visible)
        {
            if (skillPointsCountText != null)
            {
                skillPointsCountText.gameObject.SetActive(true);
                skillPointsCountText.enabled = true;
            }
            if (skillPointsPromptText != null) skillPointsPromptText.gameObject.SetActive(visible);
        }

        public void HideSkillPointsHUD()
        {
            if (skillPointsBlinkCoroutine != null)
                StopCoroutine(skillPointsBlinkCoroutine);
            skillPointsBlinkCoroutine = null;
            if (skillPointsCountText != null) skillPointsCountText.gameObject.SetActive(false);
            if (skillPointsPromptText != null) skillPointsPromptText.gameObject.SetActive(false);
        }

        private void StartSkillPointsPromptBlink()
        {
            if (skillPointsBlinkCoroutine != null)
                StopCoroutine(skillPointsBlinkCoroutine);
            skillPointsBlinkCoroutine = StartCoroutine(SkillPointsPromptBlinkRoutine());
        }

        private System.Collections.IEnumerator SkillPointsPromptBlinkRoutine()
        {
            float elapsed = 0f;
            bool visible = true;
            while (elapsed < skillPointsPromptBlinkDuration)
            {
                visible = !visible;
                if (skillPointsPromptText != null) skillPointsPromptText.enabled = visible;
                if (skillPointsCountText != null) skillPointsCountText.enabled = visible;
                yield return new WaitForSeconds(skillPointsPromptBlinkInterval);
                elapsed += skillPointsPromptBlinkInterval;
            }

            if (skillPointsPromptText != null) skillPointsPromptText.enabled = true;
            if (skillPointsCountText != null) skillPointsCountText.enabled = true;
            skillPointsBlinkCoroutine = null;
        }

        private void OnDestroy()
        {
            if (subscribedGameManager != null)
                subscribedGameManager.OnSkillPointsChanged -= HandleSkillPointsChanged;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHearts;
            }

            if (playerShooting != null)
            {
                playerShooting.OnElementChanged -= UpdateElementUI;
            }

            if (playerMana != null)
            {
                playerMana.OnManaChanged -= UpdateManaUI;
                playerMana.OnExhaustionChanged -= HandleExhaustionUI;
                playerMana.OnShootAttemptFailed -= FlashManaBarAttempt;
            }
        }

        public void UpdateHearts(int currentHealth, int maxHealth)
        {
            if (heartImages == null || heartImages.Length == 0) return;

            if (heartTemplate == null) heartTemplate = heartImages[0];
            if (heartTemplate != null && maxHealth > heartImages.Length)
            {
                // The template can be a prefab asset, so its Transform has no scene parent.
                // Put every new icon beside the existing hearts in the HUD layout.
                var parent = heartImages[0].transform.parent;
                var expandedHearts = new System.Collections.Generic.List<Image>(heartImages);
                while (expandedHearts.Count < maxHealth)
                {
                    GameObject newHeart = Instantiate(heartTemplate.gameObject, parent);
                    newHeart.name = $"Heart_{expandedHearts.Count}";
                    expandedHearts.Add(newHeart.GetComponent<Image>());
                }
                heartImages = expandedHearts.ToArray();
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
            bool isFireUnlocked = (playerShooting != null) ? playerShooting.IsFireUnlocked : true;

            // ── GELO ──
            if (iceBadge != null)
            {
                iceBadge.gameObject.SetActive(true);
                if (activeElement == ElementType.Ice)
                {
                    // Slot Ativo: usa sprite do inspector, se nao tiver usa cor solida
                    if (slotIceActiveSprite != null)
                    {
                        iceBadge.sprite = slotIceActiveSprite;
                        iceBadge.color = Color.white;
                    }
                    else
                    {
                        iceBadge.color = activeIceColor;
                    }
                }
                else
                {
                    // Slot Inativo (cinzinha)
                    if (slotIceInactiveSprite != null)
                    {
                        iceBadge.sprite = slotIceInactiveSprite;
                        iceBadge.color = Color.white;
                    }
                    else
                    {
                        iceBadge.color = inactiveIceColor;
                    }
                }
                iceBadge.type = Image.Type.Simple;
                iceBadge.preserveAspect = true;
            }
            // Limpa o texto TMP para nao sobrepor pixel art
            if (iceText != null) iceText.text = "";

            // ── FOGO ──
            if (fireBadge != null)
            {
                fireBadge.gameObject.SetActive(true);

                if (isFireUnlocked && activeElement == ElementType.Fire)
                {
                    if (slotFireActiveSprite != null)
                    {
                        fireBadge.sprite = slotFireActiveSprite;
                        fireBadge.color = Color.white;
                    }
                    else
                    {
                        fireBadge.color = activeFireColor;
                    }
                }
                else if (slotFireInactiveSprite != null)
                {
                    fireBadge.sprite = slotFireInactiveSprite;
                    fireBadge.color = Color.white;
                }
                else
                {
                    fireBadge.color = inactiveFireColor;
                }

                fireBadge.type = Image.Type.Simple;
                fireBadge.preserveAspect = true;
            }
            // Limpa o texto TMP para nao sobrepor pixel art
            if (fireText != null) fireText.text = "";
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE {score:D1}";
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
                bossHealthFill.type = Image.Type.Filled;
                bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
                bossHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
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
        /// Busca a barra de mana existente na hierarquia do Canvas.
        /// NAO cria objetos dinamicamente para nao baguncar o que foi configurado no Inspector.
        /// Use o Context Menu "★ Criar Barra de Mana no Canvas ★" para gerar uma vez.
        /// </summary>
        public void EnsureManaBarSetup()
        {
            // Ja esta configurada no inspector
            if (manaBarContainer != null && manaBarFill != null) return;

            // Busca na hierarquia
            var existing = transform.Find("TopBar/ManaBar_Container");
            if (existing == null) existing = transform.Find("ManaBar_Container");

            // Busca em toda a hierarquia pelo nome
            if (existing == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "ManaBar_Container") { existing = t; break; }
                }
            }

            if (existing != null)
            {
                if (manaBarContainer == null) manaBarContainer = existing.gameObject;
                if (manaBarFill == null)
                {
                    var fillT = existing.Find("Fill");
                    if (fillT != null) manaBarFill = fillT.GetComponent<Image>();
                }
                if (manaBarBackground == null)
                {
                    var bgT = existing.Find("Background");
                    if (bgT != null) manaBarBackground = bgT.GetComponent<Image>();
                }
                if (manaText == null)
                {
                    var txtT = existing.Find("ManaText");
                    if (txtT != null) manaText = txtT.GetComponent<TextMeshProUGUI>();
                }
            }
            // Se nao encontrou, nao faz nada (sem gerar objetos no play)
        }


        public void UpdateManaUI(float current, float max)
        {
            if (manaBarFill == null) return;
            float norm = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            manaBarFill.fillAmount = norm;

            if (manaText != null && (playerMana == null || !playerMana.IsExhausted))
            {
                manaText.text = $"MANA {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        public void HandleExhaustionUI(bool isExhausted)
        {
            if (isExhausted)
            {
                if (manaBarFill != null) manaBarFill.color = exhaustedManaColor;
                if (manaText != null)
                {
                    manaText.text = "CANSADO!";
                    manaText.color = exhaustedTextColor;
                }
            }
            else
            {
                if (manaBarFill != null) manaBarFill.color = normalManaColor;
                if (manaText != null)
                {
                    manaText.color = Color.white;
                    if (playerMana != null)
                    {
                        manaText.text = $"MANA {Mathf.CeilToInt(playerMana.CurrentMana)}/{Mathf.CeilToInt(playerMana.MaxMana)}";
                    }
                    else
                    {
                        manaText.text = "MANA";
                    }
                }
            }
        }

        public void FlashManaBarAttempt()
        {
            if (manaBarFill != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(nameof(FlashManaRoutine));
                StartCoroutine(nameof(FlashManaRoutine));
            }
        }

        private System.Collections.IEnumerator FlashManaRoutine()
        {
            if (manaBarFill == null) yield break;
            Color currentCol = manaBarFill.color;
            manaBarFill.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (manaBarFill != null)
            {
                manaBarFill.color = (playerMana != null && playerMana.IsExhausted) ? exhaustedManaColor : normalManaColor;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("★ Criar Barra de Mana no Canvas (salva na cena) ★")]
        public void EditorCreateManaBar()
        {
            // Remove barra anterior se existir
            var existing = transform.Find("TopBar/ManaBar_Container");
            if (existing == null) existing = transform.Find("ManaBar_Container");
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ManaBar_Container") { existing = t; break; }
            }
            if (existing != null)
            {
                UnityEditor.Undo.DestroyObjectImmediate(existing.gameObject);
            }

            Transform parent = transform.Find("TopBar") ?? transform;

            var root = new GameObject("ManaBar_Container");
            UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Criar ManaBar");
            root.transform.SetParent(parent, false);
            manaBarContainer = root;

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -48f);
            rt.sizeDelta = new Vector2(170f, 14f);

            // Fundo
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
            manaBarBackground = bgGo.AddComponent<Image>();
            manaBarBackground.color = new Color(0.12f, 0.08f, 0.20f, 0.88f);
            manaBarBackground.raycastTarget = false;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero; fillRt.anchoredPosition = Vector2.zero;
            manaBarFill = fillGo.AddComponent<Image>();
            manaBarFill.type = Image.Type.Filled;
            manaBarFill.fillMethod = Image.FillMethod.Horizontal;
            manaBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            manaBarFill.color = normalManaColor;
            manaBarFill.raycastTarget = false;

            // Texto
            var txtGo = new GameObject("ManaText");
            txtGo.transform.SetParent(root.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero; txtRt.anchoredPosition = new Vector2(0f, 0.5f);
            manaText = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            manaText.text = "MANA";
            manaText.fontSize = 9.5f;
            manaText.fontStyle = FontStyles.Bold;
            manaText.alignment = TMPro.TextAlignmentOptions.Center;
            manaText.color = Color.white;
            manaText.raycastTarget = false;

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=cyan>[HUDController] ManaBar_Container criado e salvo na cena com sucesso!</color>");
        }
#endif

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
            }

            if (customBossBarFillSprite != null && bossHealthFill != null)
            {
                bossHealthFill.sprite = customBossBarFillSprite;
                bossHealthFill.type = Image.Type.Filled;
                bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
                bossHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                bossHealthFill.color = Color.white;
            }
        }
    }
}
