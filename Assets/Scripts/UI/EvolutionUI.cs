using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    /// <summary>
    /// EvolutionUI — Árvore de Upgrades (Tab 1) + Evoluir Cajado (Tab 2).
    ///
    /// 10 nós:  vit → vel → foc → des → {esq: pel → ima → seg | dir: rap → dup → per}
    ///
    /// Estados visuais dos nós:
    ///   Cinza   = bloqueado (requisito não atendido)
    ///   Ciano   = disponível para comprar
    ///   Rosa    = comprado
    ///   Dourado = nível máximo
    ///
    /// Hover → anel colorido + card de detalhes à direita.
    /// Click → seleciona + tenta comprar.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(UnityEngine.UI.GraphicRaycaster))]
    public class EvolutionUI : MonoBehaviour
    {
        // ─── Node data ────────────────────────────────────────────────────────
        [Serializable]
        public class NodeData
        {
            public string id;
            public string displayName;
            public string description;
            public string effectText;
            public string requirementId;
            public int    maxLevel;
            public int    cost;
            public string iconText;
            public Vector2 htmlPos;   // coordenadas no espaço HTML (560×500)

            // runtime refs
            [NonSerialized] public GameObject      nodeGo;
            [NonSerialized] public Image           nodeBg;
            [NonSerialized] public Image           nodeRing;
            [NonSerialized] public Image           nodeIcon;
            [NonSerialized] public TextMeshProUGUI levelLabel;
        }

        // ─── Colours ──────────────────────────────────────────────────────────
        static Color C_BG      = new Color(0.05f, 0.04f, 0.09f, 0.97f);
        static Color C_PANEL   = new Color(0.11f, 0.08f, 0.19f, 1f);
        static Color C_DARK    = new Color(0.07f, 0.05f, 0.12f, 1f);
        static Color C_LINE    = new Color(0.29f, 0.21f, 0.47f, 1f);
        static Color C_LOCKED  = new Color(0.14f, 0.11f, 0.20f, 1f);
        static Color C_AVAIL   = new Color(0.37f, 0.90f, 1.00f, 1f);
        static Color C_BOUGHT  = new Color(1.00f, 0.36f, 0.56f, 1f);
        static Color C_GOLD    = new Color(1.00f, 0.78f, 0.23f, 1f);
        static Color C_MUT     = new Color(0.65f, 0.58f, 0.80f, 1f);
        static Color C_CARD    = new Color(0.91f, 0.84f, 0.97f, 1f);
        static Color C_DARK2   = new Color(0.16f, 0.09f, 0.25f, 1f);

        // ─── Node definitions ─────────────────────────────────────────────────
        readonly List<NodeData> NODES = new List<NodeData>
        {
            // Coluna central
            new NodeData { id="vit", displayName="Vitalidade",      description="Seu corpo de mago aguenta mais pancada na masmorra.",           effectText="+1 Coração de Vida",           requirementId=null,  maxLevel=3, cost=1, iconText="♥", htmlPos=new Vector2(280,440) },
            new NodeData { id="vel", displayName="Vassoura Veloz",   description="A vassoura corta o ar mais rápido e fica fácil desviar dos tiros.", effectText="Mais 8% de velocidade",        requirementId="vit", maxLevel=1, cost=2, iconText="⚡", htmlPos=new Vector2(280,370) },
            new NodeData { id="foc", displayName="Foco Arcano",      description="O cristal do cajado brilha mais forte e seus tiros machucam mais.", effectText="Mais 1 de dano",               requirementId="vel", maxLevel=1, cost=2, iconText="⚔", htmlPos=new Vector2(280,300) },
            new NodeData { id="des", displayName="Despertar Arcano", description="Libera os dois caminhos: proteção e ofensiva.",                    effectText="+1 de dano arcano",            requirementId="foc", maxLevel=1, cost=2, iconText="★", htmlPos=new Vector2(280,230) },
            // Ramo Esquerdo (Defesa)
            new NodeData { id="pel", displayName="Pele de Pedra",    description="Você aprendeu um truque com o golem: pancadas doem menos.",       effectText="8% menos dano recebido",       requirementId="des", maxLevel=1, cost=2, iconText="🛡", htmlPos=new Vector2(175,160) },
            new NodeData { id="ima", displayName="Ímã de Almas",     description="Os skill points dropados voam até você de mais longe.",            effectText="Mais 30% de alcance dos drops", requirementId="pel", maxLevel=1, cost=2, iconText="🧲", htmlPos=new Vector2(115,100) },
            new NodeData { id="seg", displayName="Segundo Fôlego",   description="Quando a vida zera, você volta uma vez com um pouco de vida.",     effectText="Revive 1x com 35% de vida",    requirementId="ima", maxLevel=1, cost=5, iconText="☠", htmlPos=new Vector2( 55, 60) },
            // Ramo Direito (Ofensiva)
            new NodeData { id="rap", displayName="Tiro Rápido",      description="O cajado dispara com mais frequência.",                            effectText="Mais 10% de cadência",          requirementId="des", maxLevel=1, cost=2, iconText=">>", htmlPos=new Vector2(385,160) },
            new NodeData { id="per", displayName="Tiro Perfurante",  description="As magias atravessam o primeiro inimigo e acertam o de trás.",    effectText="Tiros atravessam 1 inimigo",    requirementId="rap", maxLevel=1, cost=3, iconText="->", htmlPos=new Vector2(445,100) },
            new NodeData { id="dup", displayName="Fileiras de Tiro", description="Dispara projéteis em fileiras verticais como nos jogos de nave.",    effectText="2 fileiras de tiro verticais",  requirementId="per", maxLevel=2, cost=3, iconText="::", htmlPos=new Vector2(505, 60) },
        };

        // ─── Custom Staff Sprites ─────────────────────────────────────────────
        [Header("Personalização de Sprites (Arraste aqui no Inspector!)")]
        [Tooltip("Sprite do Cajado Inicial (Madeira / Gelo). Arraste seu PNG aqui ou deixe vazio para usar o padrão.")]
        [SerializeField] public Sprite customStaffBasicSprite;
        [Tooltip("Sprite do Cajado Evoluído (Arcano / Fogo). Arraste seu PNG aqui ou deixe vazio para usar o padrão.")]
        [SerializeField] public Sprite customStaffEvolvedSprite;
        [Tooltip("Sprite do Coração de Fogo. Arraste seu PNG aqui ou deixe vazio para usar o padrão.")]
        [SerializeField] public Sprite customFireHeartSprite;
        [Tooltip("Sprite do Núcleo do Boss. Arraste seu PNG aqui ou deixe vazio para usar o padrão.")]
        [SerializeField] public Sprite customBossCoreSprite;

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

        private Sprite GetBasicStaffSprite()
        {
            if (customStaffBasicSprite != null) return customStaffBasicSprite;
            return LoadSpriteResourceOrAsset("Staff_Basic");
        }

        private Sprite GetEvolvedStaffSprite()
        {
            if (customStaffEvolvedSprite != null) return customStaffEvolvedSprite;
            return LoadSpriteResourceOrAsset("Staff_Evolved");
        }

        private Sprite GetFireHeartSprite()
        {
            if (customFireHeartSprite != null) return customFireHeartSprite;
            return LoadSpriteResourceOrAsset("FireHeart");
        }

        private Sprite GetBossCoreSprite()
        {
            if (customBossCoreSprite != null) return customBossCoreSprite;
            var s = LoadSpriteResourceOrAsset("BossCore");
            if (s != null) return s;
            return LoadSpriteResourceOrAsset("cristal_mana_lateral_80x80");
        }

        // ─── Runtime UI refs ──────────────────────────────────────────────────
        GameObject tabTreeContent, tabStaffContent;
        Button     tabTreeBtn, tabStaffBtn;
        Image      tabTreeBtnBg, tabStaffBtnBg;
        Button     closeBtn;
        TextMeshProUGUI spText;

        // Card detalhes (Tab 1)
        TextMeshProUGUI cardName, cardCost, cardDesc, cardEffect, cardStatus;
        Image           cardIconImg;
        GameObject      cardIconBox;
        Transform       cardPipsContainer;
        Button          buyBtn;
        TextMeshProUGUI buyBtnLabel;

        // Staff (Tab 2)
        TextMeshProUGUI curStaffName, curStaffStat;
        Image           curStaffImg;
        TextMeshProUGUI nxtStaffName, nxtStaffStat;
        Image           nxtStaffImg;
        GameObject      nxtStaffBox;
        TextMeshProUGUI needSummary, batCountTxt, coreCountTxt, staffMsg, evolveBtnLabel;
        Button          evolveBtn;
        Transform       batSegCont, coreSegCont;

        string selectedId   = "vit";
        bool   staffTabOpen = false;
        bool   built        = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────
        void Awake()   { EnsureBuilt(); }
        void Start()   { EnsureBuilt(); HookAll(); RefreshUI(); }
        void OnEnable(){ EnsureBuilt(); HookAll(); RefreshUI(); }

        void EnsureBuilt() { if (built) return; Build(); built = true; }

        // ═════════════════════════════════════════════════════════════════════
        // BUILD
        // ═════════════════════════════════════════════════════════════════════
        void Build()
        {
            // Destroy old children
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // ── Configure the Canvas that [RequireComponent] guarantees exists ──
            // overrideSorting breaks free from parent HUD_Canvas that has scale=(0,0,0).
            var selfCanvas = GetComponent<Canvas>();
            selfCanvas.overrideSorting = true;
            selfCanvas.sortingOrder    = 100;

            // Make this RectTransform fill the whole screen
            var selfRt = GetComponent<RectTransform>();
            if (selfRt)
            {
                selfRt.anchorMin       = Vector2.zero;
                selfRt.anchorMax       = Vector2.one;
                selfRt.sizeDelta       = Vector2.zero;
                selfRt.anchoredPosition = Vector2.zero;
                selfRt.localScale      = Vector3.one;   // ensure no inherited zero-scale
            }
            Img(gameObject, C_BG);

            // Central frame — fullscreen (anchors 0→1, delta 0) with a small inset margin
            var frame = Rect("Frame", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-80, -60));
            Img(frame, C_PANEL);

            BuildHeader(frame.transform);

            // Tab 1
            tabTreeContent  = Rect("Tab_Tree",  frame.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-48,-110));
            SetAnchoredPos(tabTreeContent, new Vector2(0,-44));
            BuildTreeTab(tabTreeContent.transform);

            // Tab 2
            tabStaffContent = Rect("Tab_Staff", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-48,-110));
            SetAnchoredPos(tabStaffContent, new Vector2(0,-44));
            BuildStaffTab(tabStaffContent.transform);
            tabStaffContent.SetActive(false);

            // IMPORTANTE: Coloca o Header como último filho para ficar SEMPRE na frente das abas,
            // garantindo que os botões de troca de aba recebam cliques sem bloqueio de raycasts.
            var hdr = frame.transform.Find("Header");
            if (hdr != null)
            {
                hdr.SetAsLastSibling();
            }
        }

        // ─── Header ──────────────────────────────────────────────────────────
        void BuildHeader(Transform parent)
        {
            var hdr = Rect("Header", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-48,62));
            SetAnchoredPos(hdr, new Vector2(0,-16));

            // Tab tree
            var bt = Rect("Btn_Tree",  hdr.transform, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(310,54));
            tabTreeBtnBg = Img(bt, C_BOUGHT);
            tabTreeBtn   = bt.AddComponent<Button>();
            Txt("Text", bt.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, "ÁRVORE DE UPGRADES", 18, Color.white, FontStyles.Bold);

            // Tab staff
            var bs = Rect("Btn_Staff", hdr.transform, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(260,54));
            SetAnchoredPos(bs, new Vector2(324,0));
            tabStaffBtnBg = Img(bs, C_LINE);
            tabStaffBtn   = bs.AddComponent<Button>();
            Txt("Text", bs.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, "EVOLUIR CAJADO", 18, Color.white, FontStyles.Bold);

            // Skill points
            var spBox = Rect("SP_Box", hdr.transform, new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(200,54));
            SetAnchoredPos(spBox, new Vector2(-60,0));
            Img(spBox, new Color(0.04f,0.03f,0.06f,1f));
            spText = Txt("SP", spBox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, "SP: 0", 20, C_GOLD, FontStyles.Bold);
            spText.alignment = TextAlignmentOptions.Center;

            // Close
            var clo = Rect("Btn_Close", hdr.transform, new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(54,54));
            Img(clo, C_BOUGHT);
            closeBtn = clo.AddComponent<Button>();
            Txt("X", clo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, "✕", 26, Color.white, FontStyles.Bold);
        }

        // ─── Tab 1: Tree ──────────────────────────────────────────────────────
        void BuildTreeTab(Transform parent)
        {
            // Subtitle
            var sub = Txt("Sub", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,28),
                "Inimigos dropam skill points. Passe o mouse num nó e clique para desbloquear.",
                18, C_MUT, FontStyles.Normal);
            sub.alignment = TextAlignmentOptions.Center;

            // Left: tree canvas (0..58% width)
            var treeBox = Rect("TreeCanvas", parent, Vector2.zero, new Vector2(0.57f,1), Vector2.zero, new Vector2(0,-34));
            Img(treeBox, C_DARK);

            // Right: detail card (60..100%)
            var card = Rect("Card", parent, new Vector2(0.61f,0), new Vector2(1,1), new Vector2(0,0), new Vector2(0,0));
            Img(card, C_CARD);
            BuildDetailCard(card.transform);

            // Lines behind nodes
            var linesRoot = Rect("Lines", treeBox.transform, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero);
            foreach (var n in NODES)
            {
                if (string.IsNullOrEmpty(n.requirementId)) continue;
                var req = GetNode(n.requirementId);
                if (req == null) continue;
                DrawLine(linesRoot.transform, HtmlToCanvas(req.htmlPos), HtmlToCanvas(n.htmlPos));
            }

            // Nodes
            foreach (var n in NODES)
            {
                Vector2 pos = HtmlToCanvas(n.htmlPos);

                var nodeGo = Rect($"Node_{n.id}", treeBox.transform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(68,68));
                SetAnchoredPos(nodeGo, pos);
                n.nodeGo = nodeGo;

                // Background
                n.nodeBg = Img(nodeGo, C_LOCKED);

                // Hover ring (larger, behind bg via SetAsFirstSibling)
                var ringGo = Rect("Ring", nodeGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), new Vector2(12,12));
                n.nodeRing = Img(ringGo, new Color(C_AVAIL.r,C_AVAIL.g,C_AVAIL.b,0f));
                ringGo.transform.SetAsFirstSibling();

                // Icon (Image renderizada com SkillIconProvider)
                var iconGo = Rect("Icon", nodeGo.transform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(40,40));
                n.nodeIcon = iconGo.AddComponent<Image>();
                n.nodeIcon.sprite = SkillIconProvider.GetIcon(n.id);
                n.nodeIcon.preserveAspect = true;
                n.nodeIcon.raycastTarget = false;

                // Level label (below node)
                n.levelLabel = Txt("Lv", nodeGo.transform, new Vector2(0,0), new Vector2(0,0), new Vector2(0.5f,0), new Vector2(68,22),
                    "", 13, C_MUT, FontStyles.Normal);
                n.levelLabel.alignment = TextAlignmentOptions.Center;
                SetAnchoredPos(n.levelLabel.gameObject, new Vector2(0,-42));

                // Interaction via EventTrigger
                var trig = nodeGo.AddComponent<EventTrigger>();
                AddTrigger(trig, EventTriggerType.PointerEnter, _ => { SelectNode(n.id); PulseRing(n, true);  });
                AddTrigger(trig, EventTriggerType.PointerExit,  _ =>                      PulseRing(n, false) );
                AddTrigger(trig, EventTriggerType.PointerClick, _ => { SelectNode(n.id); TryBuy(n.id);        });
            }

            // Bottom Legend & Cheats
            var leg = Txt("Legend", treeBox.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(-20,24),
                "Cinza = bloqueado  •  Ciano = pode comprar  •  Rosa = comprado  •  Dourado = nível máximo",
                14, C_MUT, FontStyles.Normal);
            SetAnchoredPos(leg.gameObject, new Vector2(0, 10));
            leg.alignment = TextAlignmentOptions.Center;
        }

        void BuildDetailCard(Transform parent)
        {
            // Node name
            cardName = Txt("Name", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(-16,40),
                "Vitalidade", 26, C_DARK2, FontStyles.Bold);
            SetAnchoredPos(cardName.gameObject, new Vector2(16,-12));

            // Cost badge
            var costBx = Rect("CostBox", parent, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1), new Vector2(120,34));
            SetAnchoredPos(costBx, new Vector2(-16,-12));
            Img(costBx, new Color(0.04f,0.03f,0.06f,1f));
            cardCost = Txt("CostTxt", costBx.transform, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero,
                "CUSTO 1 SP", 16, C_GOLD, FontStyles.Bold);
            cardCost.alignment = TextAlignmentOptions.Center;

            // "Descrição:"
            var lblDesc = Txt("LblDesc", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(-16,22),
                "Descrição:", 16, C_MUT, FontStyles.Italic);
            SetAnchoredPos(lblDesc.gameObject, new Vector2(16,-52));

            // Description text
            cardDesc = Txt("Desc", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(-32,60),
                "", 18, C_DARK2, FontStyles.Normal);
            SetAnchoredPos(cardDesc.gameObject, new Vector2(16,-74));
            cardDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // ── Visual Icon Showcase Box ──
            cardIconBox = Rect("IconBox", parent, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(300,120));
            SetAnchoredPos(cardIconBox, new Vector2(0,-140));
            Img(cardIconBox, new Color(0.08f, 0.05f, 0.15f, 1f));

            // Glowing Icon inside showcase box
            var iconPre = Rect("IconPreview", cardIconBox.transform, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(64,64));
            SetAnchoredPos(iconPre, new Vector2(0,12));
            cardIconImg = iconPre.AddComponent<Image>();
            cardIconImg.preserveAspect = true;
            cardIconImg.raycastTarget = false;

            // Level Pips container below the icon
            var pipsGo = Rect("Pips", cardIconBox.transform, new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(160,20));
            SetAnchoredPos(pipsGo, new Vector2(0,10));
            var hlg = pipsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            cardPipsContainer = pipsGo.transform;

            // Effect (rosa em negrito)
            cardEffect = Txt("Effect", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-24,32),
                "", 22, C_BOUGHT, FontStyles.Bold);
            SetAnchoredPos(cardEffect.gameObject, new Vector2(0,-272));
            cardEffect.alignment = TextAlignmentOptions.Center;

            // Status message (above button)
            cardStatus = Txt("Status", parent, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(-24,26),
                "", 16, C_MUT, FontStyles.Italic);
            SetAnchoredPos(cardStatus.gameObject, new Vector2(0,66));
            cardStatus.alignment = TextAlignmentOptions.Center;

            // Buy button
            var btnGo = Rect("Btn_Buy", parent, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(-24,54));
            SetAnchoredPos(btnGo, new Vector2(0,8));
            Img(btnGo, C_BOUGHT);
            buyBtn = btnGo.AddComponent<Button>();
            buyBtnLabel = Txt("Label", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero,
                "DESBLOQUEAR", 20, Color.white, FontStyles.Bold);
            buyBtnLabel.alignment = TextAlignmentOptions.Center;
            buyBtn.onClick.AddListener(() => TryBuy(selectedId));
        }

        // ─── Tab 2: Staff ─────────────────────────────────────────────────────
        void BuildStaffTab(Transform parent)
        {
            // Title
            var title = Txt("Title", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,48),
                "EVOLUIR", 34, C_BOUGHT, FontStyles.Bold);
            SetAnchoredPos(title.gameObject, new Vector2(0,-8));
            title.alignment = TextAlignmentOptions.Center;

            var sub = Txt("Sub", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,30),
                "Troque drops dos inimigos por uma versão mais forte do seu cajado.", 20, C_MUT, FontStyles.Normal);
            SetAnchoredPos(sub.gameObject, new Vector2(0,-58));
            sub.alignment = TextAlignmentOptions.Center;

            // ── Row: ATUAL | → | PRÓXIMO ──
            float rowTop = -96f;

            // Box Atual (left third)
            var boxCur = Rect("Box_Cur", parent, new Vector2(0,1), new Vector2(0.42f,1), new Vector2(0,1), new Vector2(0,-260));
            SetAnchoredPos(boxCur, new Vector2(24, rowTop));
            Img(boxCur, C_DARK);
            var curTag = Txt("Tag", boxCur.transform, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,24),
                "ATUAL", 13, C_MUT, FontStyles.Normal);
            SetAnchoredPos(curTag.gameObject, new Vector2(0,-8));
            curTag.alignment = TextAlignmentOptions.Center;

            curStaffImg = MakeImg("Img", boxCur.transform, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(85,135));
            SetAnchoredPos(curStaffImg.gameObject, new Vector2(0,-32));
            curStaffImg.preserveAspect = true;
            curStaffImg.color = Color.white;
            curStaffImg.sprite = GetBasicStaffSprite();

            curStaffName = Txt("Name", boxCur.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(0,28),
                "Cajado de Madeira", 20, Color.white, FontStyles.Bold);
            SetAnchoredPos(curStaffName.gameObject, new Vector2(0,44));
            curStaffName.alignment = TextAlignmentOptions.Center;

            curStaffStat = Txt("Stat", boxCur.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(0,24),
                "Dano 5", 18, C_GOLD, FontStyles.Normal);
            SetAnchoredPos(curStaffStat.gameObject, new Vector2(0,16));
            curStaffStat.alignment = TextAlignmentOptions.Center;

            // Arrow centre
            var arw = Txt("Arrow", parent, new Vector2(0.42f,1), new Vector2(0.58f,1), new Vector2(0.5f,1), new Vector2(0,80),
                "->", 36, C_BOUGHT, FontStyles.Bold);
            SetAnchoredPos(arw.gameObject, new Vector2(0, rowTop - 80f));
            arw.alignment = TextAlignmentOptions.Center;

            // Box Próximo (right third)
            nxtStaffBox = Rect("Box_Nxt", parent, new Vector2(0.58f,1), new Vector2(1,1), new Vector2(0,1), new Vector2(-24,-260));
            SetAnchoredPos(nxtStaffBox, new Vector2(0, rowTop));
            Img(nxtStaffBox, C_DARK);

            var nxtTag = Txt("Tag", nxtStaffBox.transform, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,24),
                "PRÓXIMO", 13, C_GOLD, FontStyles.Normal);
            SetAnchoredPos(nxtTag.gameObject, new Vector2(0,-8));
            nxtTag.alignment = TextAlignmentOptions.Center;

            nxtStaffImg = MakeImg("Img", nxtStaffBox.transform, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(85,135));
            SetAnchoredPos(nxtStaffImg.gameObject, new Vector2(0,-32));
            nxtStaffImg.preserveAspect = true;
            nxtStaffImg.color = Color.white;
            nxtStaffImg.sprite = GetEvolvedStaffSprite();

            nxtStaffName = Txt("Name", nxtStaffBox.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(0,28),
                "Cajado Arcano", 20, Color.white, FontStyles.Bold);
            SetAnchoredPos(nxtStaffName.gameObject, new Vector2(0,44));
            nxtStaffName.alignment = TextAlignmentOptions.Center;

            nxtStaffStat = Txt("Stat", nxtStaffBox.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), new Vector2(0,24),
                "+3 de dano", 18, C_GOLD, FontStyles.Normal);
            SetAnchoredPos(nxtStaffStat.gameObject, new Vector2(0,16));
            nxtStaffStat.alignment = TextAlignmentOptions.Center;

            // Need summary
            needSummary = Txt("Need", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-40,36),
                "Requer <color=#ffc83a>7</color> Corações de Fogo e <color=#ffc83a>1</color> Núcleo do Boss",
                22, Color.white, FontStyles.Normal);
            SetAnchoredPos(needSummary.gameObject, new Vector2(0, rowTop - 270f));
            needSummary.alignment = TextAlignmentOptions.Center;
            needSummary.richText  = true;

            // Mat rows
            float matY = rowTop - 315f;
            BuildMatRow(parent, new Vector2(0, matY),       "Coração de Fogo", GetFireHeartSprite(), 7, out batSegCont,  out batCountTxt);
            BuildMatRow(parent, new Vector2(0, matY - 72f), "Núcleo do Boss",   GetBossCoreSprite(),  1, out coreSegCont, out coreCountTxt);

            // Evolve button
            var evGo = Rect("Btn_Evolve", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-40,54));
            SetAnchoredPos(evGo, new Vector2(0, matY - 160f));
            Img(evGo, C_BOUGHT);
            evolveBtn      = evGo.AddComponent<Button>();
            evolveBtnLabel = Txt("Label", evGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero,
                "EVOLUIR", 22, Color.white, FontStyles.Bold);
            evolveBtnLabel.alignment = TextAlignmentOptions.Center;
            evolveBtn.onClick.AddListener(OnEvolveClicked);

            staffMsg = Txt("Msg", parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-40,28),
                "Derrote inimigos para coletar os drops que faltam.", 18, C_MUT, FontStyles.Italic);
            SetAnchoredPos(staffMsg.gameObject, new Vector2(0, matY - 202f));
            staffMsg.alignment = TextAlignmentOptions.Center;
        }

        void BuildMatRow(Transform parent, Vector2 anchoredPos, string label, Sprite iconSprite, int total,
                         out Transform segContainer, out TextMeshProUGUI countTxt)
        {
            var box = Rect("Mat_" + label, parent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(-60,58));
            SetAnchoredPos(box, anchoredPos);
            Img(box, C_DARK);

            // Icon Sprite
            if (iconSprite != null)
            {
                var iconGo = Rect("Icon", box.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 36));
                SetAnchoredPos(iconGo, new Vector2(24, 0));
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Label
            var lbl = Txt("Lbl", box.transform, new Vector2(0, 0.5f), new Vector2(0.6f, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 24),
                label, 18, Color.white, FontStyles.Bold);
            SetAnchoredPos(lbl.gameObject, new Vector2(56, 10));

            // Segments
            var segsGo = Rect("Segs", box.transform, new Vector2(0, 0.5f), new Vector2(0.6f, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 10));
            SetAnchoredPos(segsGo, new Vector2(56, -14));
            var hlg = segsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            segContainer = segsGo.transform;
            for (int i = 0; i < total; i++)
            {
                var s = new GameObject($"Seg_{i}"); s.transform.SetParent(segContainer, false);
                s.AddComponent<Image>().color = C_LOCKED;
            }

            // Count (right side)
            countTxt = Txt("Count", box.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(80, 28),
                $"0/{total}", 18, Color.white, FontStyles.Bold);
            SetAnchoredPos(countTxt.gameObject, new Vector2(-16, 0));
            countTxt.alignment = TextAlignmentOptions.MidlineRight;
        }

        // ═════════════════════════════════════════════════════════════════════
        // HOOK + REFRESH
        // ═════════════════════════════════════════════════════════════════════
        void HookAll()
        {
            if (tabTreeBtn  != null) { tabTreeBtn .onClick.RemoveAllListeners(); tabTreeBtn .onClick.AddListener(() => SwitchTab(false)); }
            if (tabStaffBtn != null) { tabStaffBtn.onClick.RemoveAllListeners(); tabStaffBtn.onClick.AddListener(() => SwitchTab(true));  }
            if (closeBtn    != null) { closeBtn   .onClick.RemoveAllListeners(); closeBtn   .onClick.AddListener(OnClose); }
        }

        public void SwitchTab(bool toStaff)
        {
            staffTabOpen = toStaff;
            if (tabTreeContent  != null) tabTreeContent .SetActive(!toStaff);
            if (tabStaffContent != null) tabStaffContent.SetActive(toStaff);
            if (tabTreeBtnBg    != null) tabTreeBtnBg .color = !toStaff ? C_BOUGHT : C_LINE;
            if (tabStaffBtnBg   != null) tabStaffBtnBg.color =  toStaff ? C_BOUGHT : C_LINE;
            RefreshUI();
        }

        public void SelectNode(string id) { selectedId = id; RefreshDetailCard(); }

        public void RefreshUI()
        {
            if (GameManager.Instance == null) return;
            int sp = GameManager.Instance.SkillPoints;
            if (spText != null) spText.text = $"SKILL POINTS: {sp}";
            RefreshNodes();
            if (!staffTabOpen) RefreshDetailCard();
            else               RefreshStaffTab();
        }

        void RefreshNodes()
        {
            if (GameManager.Instance == null) return;
            int sp = GameManager.Instance.SkillPoints;
            foreach (var n in NODES)
            {
                if (n.nodeBg == null) continue;
                int  lvl     = GameManager.Instance.GetSkillLevel(n.id);
                bool reached = IsReachable(n);
                bool maxed   = lvl >= n.maxLevel;
                bool canBuy  = reached && !maxed && sp >= n.cost;

                // Background colour
                if (maxed)         n.nodeBg.color = C_GOLD;
                else if (lvl > 0)  n.nodeBg.color = C_BOUGHT;
                else if (canBuy)   n.nodeBg.color = C_AVAIL;
                else               n.nodeBg.color = C_LOCKED;

                // Ring base colour (alpha preserved from hover)
                if (n.nodeRing != null)
                {
                    Color rc = maxed ? C_GOLD : canBuy ? C_AVAIL : C_BOUGHT;
                    float a  = n.nodeRing.color.a;
                    n.nodeRing.color = new Color(rc.r, rc.g, rc.b, a);
                }

                // Node icon tint
                if (n.nodeIcon != null)
                {
                    if (maxed) n.nodeIcon.color = new Color(1f, 0.95f, 0.8f, 1f);
                    else if (lvl > 0 || canBuy) n.nodeIcon.color = Color.white;
                    else n.nodeIcon.color = new Color(0.6f, 0.55f, 0.7f, 0.40f);
                }

                if (n.levelLabel != null)
                    n.levelLabel.text = reached ? $"{lvl}/{n.maxLevel}" : "";
            }
        }

        void RefreshDetailCard()
        {
            if (GameManager.Instance == null) return;
            NodeData n = GetNode(selectedId);
            if (n == null) return;

            int  sp      = GameManager.Instance.SkillPoints;
            int  lvl     = GameManager.Instance.GetSkillLevel(n.id);
            bool reached = IsReachable(n);
            bool maxed   = lvl >= n.maxLevel;
            bool canBuy  = reached && !maxed && sp >= n.cost;

            if (cardName   != null) cardName.text   = n.displayName;
            if (cardCost   != null) cardCost.text   = maxed ? "CUSTO -" : $"CUSTO {n.cost} SP";
            if (cardDesc   != null) cardDesc.text   = n.description;

            // Ícone do Card
            if (cardIconImg != null)
            {
                cardIconImg.sprite = SkillIconProvider.GetIcon(n.id);
                cardIconImg.color = Color.white;
            }

            // Pips de nível abaixo do ícone no Card
            if (cardPipsContainer != null)
            {
                for (int i = cardPipsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(cardPipsContainer.GetChild(i).gameObject);
                }

                for (int i = 0; i < n.maxLevel; i++)
                {
                    var pip = new GameObject($"Pip_{i}");
                    pip.transform.SetParent(cardPipsContainer, false);
                    var prt = pip.AddComponent<RectTransform>();
                    prt.sizeDelta = new Vector2(16, 12);
                    var pImg = pip.AddComponent<Image>();
                    pImg.color = (i < lvl) ? (maxed ? C_GOLD : C_BOUGHT) : C_LOCKED;
                }
            }

            // Efeito textual dinâmico
            if (cardEffect != null)
            {
                if (n.id == "vit")
                {
                    if (lvl == 0) cardEffect.text = "+1 Coração de Vida (Total: 5)";
                    else if (lvl == 1) cardEffect.text = "+1 Coração de Vida (Total: 6)";
                    else if (lvl == 2) cardEffect.text = "+1 Coração de Vida (Total: 7 - MÁXIMO)";
                    else cardEffect.text = "Vida Máxima Atingida (7 Corações)";
                }
                else if (n.id == "dup")
                {
                    if (lvl == 0) cardEffect.text = "2 Fileiras de Tiro Verticais";
                    else if (lvl == 1) cardEffect.text = "3 Fileiras de Tiro Verticais (MÁXIMO)";
                    else cardEffect.text = "3 Fileiras Verticais Ativas (Nível Máximo)";
                }
                else
                {
                    cardEffect.text = n.effectText;
                }
            }

            string statusMsg = "";
            bool   btnActive = false;
            string btnLabel  = "DESBLOQUEAR";
            Color  btnColor  = new Color(0.35f,0.28f,0.48f,1f);

            if (maxed)
            {
                statusMsg = "Nível máximo alcançado!";
                btnLabel  = "MÁXIMO";
            }
            else if (!reached)
            {
                var req = GetNode(n.requirementId);
                statusMsg = $"Requer: {(req != null ? req.displayName : "anterior")}";
                btnLabel  = "BLOQUEADO";
            }
            else if (sp < n.cost)
            {
                statusMsg = $"Faltam {n.cost - sp} skill point(s)";
                btnLabel  = "SP INSUFICIENTE";
            }
            else
            {
                statusMsg = "Pronto para desbloquear!";
                btnActive = true;
                btnLabel  = "DESBLOQUEAR";
                btnColor  = C_BOUGHT;
            }

            if (cardStatus  != null) cardStatus.text = statusMsg;
            if (buyBtnLabel != null) buyBtnLabel.text = btnLabel;
            if (buyBtn      != null)
            {
                buyBtn.interactable = btnActive;
                var bi = buyBtn.GetComponent<Image>();
                if (bi) bi.color = btnColor;
            }
        }

        void RefreshStaffTab()
        {
            if (GameManager.Instance == null) return;

            bool evolved  = GameManager.Instance.IsStaffEvolved;
            int  bat      = GameManager.Instance.FireHeartsCount;
            int  reqBat   = GameManager.Instance.FireHeartsRequired;
            int  core     = GameManager.Instance.BossCoresCount;
            int  reqCore  = GameManager.Instance.BossCoresRequired;

            if (batCountTxt  != null) { batCountTxt .text  = $"{bat}/{reqBat}";   batCountTxt .color = bat  >= reqBat  ? C_GOLD : Color.white; }
            if (coreCountTxt != null) { coreCountTxt.text  = $"{core}/{reqCore}"; coreCountTxt.color = core >= reqCore ? C_GOLD : Color.white; }

            UpdateSegs(batSegCont,  bat,  reqBat);
            UpdateSegs(coreSegCont, core, reqCore);

            bool canEvolve = !evolved && GameManager.Instance.CanEvolveStaff();

            if (evolved)
            {
                if (curStaffName  != null) curStaffName.text  = "Cajado Arcano";
                if (curStaffStat  != null) curStaffStat.text  = "Dano 8";
                if (curStaffImg   != null) { curStaffImg.sprite = GetEvolvedStaffSprite(); curStaffImg.color = Color.white; }
                if (nxtStaffBox   != null) nxtStaffBox.SetActive(false);
                if (needSummary   != null) needSummary.text   = "Cajado totalmente evoluído!";
                if (staffMsg      != null) staffMsg.text      = "Nenhuma evolução restante.";
                if (evolveBtn     != null) { evolveBtn.interactable = false; var bi = evolveBtn.GetComponent<Image>(); if (bi) bi.color = C_LINE; }
                if (evolveBtnLabel!= null) evolveBtnLabel.text = "MÁXIMO";
            }
            else
            {
                if (curStaffImg   != null) { curStaffImg.sprite = GetBasicStaffSprite(); curStaffImg.color = Color.white; }
                if (nxtStaffImg   != null) { nxtStaffImg.sprite = GetEvolvedStaffSprite(); nxtStaffImg.color = Color.white; }
                if (nxtStaffBox   != null) nxtStaffBox.SetActive(true);
                if (evolveBtn     != null) { evolveBtn.interactable = canEvolve; var bi = evolveBtn.GetComponent<Image>(); if (bi) bi.color = canEvolve ? C_GOLD : new Color(0.35f,0.28f,0.48f,1f); }
                if (evolveBtnLabel!= null) evolveBtnLabel.text = canEvolve ? "EVOLUIR" : "MATERIAIS INSUFICIENTES";
                if (staffMsg      != null) staffMsg.text       = canEvolve ? "Tudo pronto! Pode evoluir." : "Derrote inimigos para coletar os drops que faltam.";
            }
        }

        void UpdateSegs(Transform cont, int have, int req)
        {
            if (cont == null) return;

            while (cont.childCount < req)
            {
                var s = new GameObject($"Seg_{cont.childCount}");
                s.transform.SetParent(cont, false);
                s.AddComponent<Image>().color = C_LOCKED;
            }
            while (cont.childCount > req)
            {
                Destroy(cont.GetChild(cont.childCount - 1).gameObject);
            }

            for (int i = 0; i < cont.childCount; i++)
            {
                var img = cont.GetChild(i).GetComponent<Image>();
                if (img) img.color = i < have ? (have >= req ? C_GOLD : C_BOUGHT) : C_LOCKED;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // ACTIONS
        // ═════════════════════════════════════════════════════════════════════
        void TryBuy(string id)
        {
            if (GameManager.Instance == null) return;
            NodeData n = GetNode(id);
            if (n == null) return;
            int lvl = GameManager.Instance.GetSkillLevel(n.id);
            if (!IsReachable(n) || lvl >= n.maxLevel || GameManager.Instance.SkillPoints < n.cost) return;
            if (GameManager.Instance.UpgradeSkill(n.id, n.cost)) RefreshUI();
        }

        void OnEvolveClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.TryEvolveStaff()) RefreshUI();
        }

        void OnClose()
        {
            if (GameManager.Instance != null) GameManager.Instance.CloseEvolutionPanel();
            else { gameObject.SetActive(false); Time.timeScale = 1f; }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════
        bool IsReachable(NodeData n)
        {
            if (string.IsNullOrEmpty(n.requirementId)) return true;
            return GameManager.Instance != null && GameManager.Instance.GetSkillLevel(n.requirementId) > 0;
        }

        NodeData GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return NODES.Find(x => x.id == id);
        }

        // HTML (560×500, Y-down) → Unity canvas (Y-up)
        Vector2 HtmlToCanvas(Vector2 html)
        {
            float x = (html.x / 560f - 0.5f) * 840f;
            float y = (0.5f - html.y / 500f) * 590f;
            return new Vector2(x, y);
        }

        void DrawLine(Transform parent, Vector2 a, Vector2 b)
        {
            var go  = new GameObject("Line"); go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            var dir = b - a;
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta        = new Vector2(dir.magnitude, 5f);
            rt.localRotation    = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            go.AddComponent<Image>().color = C_LINE;
        }

        void AddTrigger(EventTrigger trig, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trig.triggers.Add(entry);
        }

        void PulseRing(NodeData n, bool on)
        {
            if (n.nodeRing == null) return;
            var c = n.nodeRing.color;
            n.nodeRing.color = new Color(c.r, c.g, c.b, on ? 0.9f : 0f);
        }

        // ── Lightweight builder helpers ────────────────────────────────────────
        // Returns the GameObject (not RectTransform) so .AddComponent<> works.

        GameObject Rect(string name, Transform parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot; rt.sizeDelta = size;
            return go;
        }

        Image Img(GameObject go, Color col)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = col; return img;
        }

        TextMeshProUGUI Txt(string name, Transform parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size,
            string text, float fs, Color col, FontStyles style)
        {
            var go  = Rect(name, parent, ancMin, ancMax, pivot, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text; tmp.fontSize  = fs;
            tmp.color     = col;  tmp.fontStyle = style;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.raycastTarget = false; // Garante que rótulos de texto estáticos nunca bloqueiem cliques
            return tmp;
        }

        Image MakeImg(string name, Transform parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size)
        {
            var go = Rect(name, parent, ancMin, ancMax, pivot, size);
            return go.AddComponent<Image>();
        }

        void SetAnchoredPos(GameObject go, Vector2 pos)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt) rt.anchoredPosition = pos;
        }

        void SetAnchoredPos(TextMeshProUGUI tmp, Vector2 pos)
        {
            if (tmp == null) return;
            SetAnchoredPos(tmp.gameObject, pos);
        }

#if UNITY_EDITOR
        [ContextMenu("★ Gerar UI no Canvas (Preview no Editor) ★")]
        public void EditorBuildUI()
        {
            Build();
            RefreshUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=green>[EvolutionUI] Interface gerada no Canvas com sucesso! Agora você pode visualizar e selecionar cada elemento na Scene View.</color>");
        }

        [ContextMenu("★ Limpar Preview do Canvas ★")]
        public void EditorClearUI()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            built = false;
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("[EvolutionUI] Preview limpo do Canvas.");
        }
#endif
    }
}