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
    /// EvolutionUI — Repaginado com estilo gótico/arcano em pixel-art.
    /// Suporte total à personalização sem resetar modificações do usuário:
    ///   - Liberdade total para mudar tamanhos globais ou individuais dos nós.
    ///   - Posições editáveis no Inspector e na Scene View sem serem sobrescritas.
    ///   - Não destrói os GameObjects ao reabrir a janela, preservando ajustes finos.
    ///   - Navegação interativa com arrasto do mouse e zoom com a roda.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(UnityEngine.UI.GraphicRaycaster))]
    public class EvolutionUI : MonoBehaviour
    {
        // ─── Node Data Configurável no Inspector ──────────────────────────────
        [Serializable]
        public class NodeData
        {
            public string id;
            public string displayName;
            [TextArea(2, 4)]
            public string description;
            public string effectText;
            [Tooltip("ID do nó que é pré-requisito para este. Deixe vazio para nós iniciais.")]
            public string requirementId;
            public int    maxLevel = 1;
            public int    cost = 1;

            [Header("Customização Visual")]
            [Tooltip("Arraste aqui um Sprite personalizado para este nó. Se deixar vazio, usa o ícone padrão.")]
            public Sprite customIcon;

            [Tooltip("Posição livre do nó (X, Y) no Canvas da Árvore. Edite livremente para mudar o formato!")]
            public Vector2 canvasPos;

            [Tooltip("Tamanho personalizado deste nó circular (ex: 80, 80). Se deixar (0,0), usa o tamanho padrão global.")]
            public Vector2 customNodeSize = Vector2.zero;

            [Tooltip("Tamanho personalizado do ícone dentro deste nó. Se deixar (0,0), usa o tamanho padrão global.")]
            public Vector2 customIconSize = Vector2.zero;

            // runtime refs
            [NonSerialized] public GameObject      nodeGo;
            [NonSerialized] public Image           nodeBg;
            [NonSerialized] public Image           nodeRing;
            [NonSerialized] public Image           nodeIcon;
            [NonSerialized] public TextMeshProUGUI levelLabel;
        }

        // ─── Configurações da Árvore no Inspector ────────────────────────────
        [Header("Tamanhos Globais da Árvore (Mude livremente!)")]
        [Tooltip("Tamanho padrão de cada nó circular da árvore.")]
        public Vector2 defaultNodeSize = new Vector2(68, 68);

        [Tooltip("Tamanho padrão do ícone interno dentro do nó.")]
        public Vector2 defaultIconSize = new Vector2(40, 40);

        [Tooltip("Espessura das linhas que conectam os nós da árvore.")]
        public float lineThickness = 5f;

        [Tooltip("Tamanho da área navegável da árvore.")]
        public Vector2 treeAreaSize = new Vector2(1500, 1800);

        [Header("Árvore de Habilidades (Customização Livre)")]
        [Tooltip("Lista de nós da árvore. Modifique posições (canvasPos), ícones (customIcon) e requisitos livremente!")]
        [SerializeField] public List<NodeData> treeNodes = new List<NodeData>();

        [Header("Navegação Interativa (Pan / Arraste)")]
        [Tooltip("Se ativado: segurar o mouse e arrastar para baixo move a árvore para cima (e vice-versa).")]
        [SerializeField] public bool invertDrag = false;
        [Tooltip("Sensibilidade do movimento de arraste.")]
        [SerializeField] public float dragSensitivity = 1.0f;
        [Tooltip("Habilita zoom suave com a roda do mouse.")]
        [SerializeField] public bool enableZoom = true;

        // ─── Cores Repaginadas da Paleta Arcana ──────────────────────────────────
        static readonly Color C_BG_OVERLAY   = new Color(0.04f, 0.03f, 0.08f, 0.96f);
        static readonly Color C_PANEL_BG     = new Color(0.09f, 0.06f, 0.16f, 1.00f);
        static readonly Color C_PANEL_BORDER = new Color(0.32f, 0.22f, 0.50f, 1.00f);
        static readonly Color C_DARK_BOX     = new Color(0.06f, 0.04f, 0.11f, 1.00f);
        static readonly Color C_DARK_BOX_HL  = new Color(0.12f, 0.08f, 0.20f, 1.00f);
        static readonly Color C_LINE         = new Color(0.40f, 0.28f, 0.60f, 0.90f);
        static readonly Color C_LOCKED       = new Color(0.16f, 0.12f, 0.24f, 1.00f);
        static readonly Color C_AVAIL        = new Color(0.22f, 0.82f, 0.98f, 1.00f);
        static readonly Color C_BOUGHT       = new Color(0.92f, 0.22f, 0.52f, 1.00f);
        static readonly Color C_GOLD         = new Color(0.98f, 0.76f, 0.20f, 1.00f);
        static readonly Color C_MUTED        = new Color(0.70f, 0.64f, 0.82f, 1.00f);
        static readonly Color C_CARD_BG      = new Color(0.95f, 0.91f, 0.98f, 1.00f);
        static readonly Color C_CARD_DARK    = new Color(0.14f, 0.09f, 0.22f, 1.00f);
        static readonly Color C_CYAN_ICE     = new Color(0.30f, 0.78f, 1.00f, 1.00f);
        static readonly Color C_FIRE_RED     = new Color(0.95f, 0.30f, 0.18f, 1.00f);

        // ─── Cores Configuráveis no Inspector (Não resetam no Play!) ──────────
        [Header("Cores dos Botões das Abas (Árvore e Evolução)")]
        [Tooltip("Cor de fundo da aba quando selecionada (ativa).")]
        public Color tabActiveColor = new Color(0.92f, 0.22f, 0.52f, 1.00f);
        [Tooltip("Cor de fundo da aba quando desselecionada (inativa).")]
        public Color tabInactiveColor = new Color(0.12f, 0.08f, 0.20f, 1.00f);
        [Tooltip("Cor do texto da aba selecionada.")]
        public Color tabActiveTextColor = Color.white;
        [Tooltip("Cor do texto da aba inativa.")]
        public Color tabInactiveTextColor = new Color(0.70f, 0.64f, 0.82f, 1.00f);
        [Tooltip("Cor da barrinha indicadora sob a aba ativa.")]
        public Color tabIndicatorColor = new Color(0.98f, 0.76f, 0.20f, 1.00f);

        [Header("Cores dos Nós da Árvore de Habilidades")]
        [Tooltip("Cor do nó quando bloqueado (pré-requisito não cumprido).")]
        public Color nodeLockedColor = new Color(0.16f, 0.12f, 0.24f, 1.00f);
        [Tooltip("Cor do nó quando disponível para comprar.")]
        public Color nodeAvailableColor = new Color(0.22f, 0.82f, 0.98f, 1.00f);
        [Tooltip("Cor do nó quando já foi comprado.")]
        public Color nodeBoughtColor = new Color(0.92f, 0.22f, 0.52f, 1.00f);
        [Tooltip("Cor do nó quando atingiu o nível máximo.")]
        public Color nodeMaxedColor = new Color(0.98f, 0.76f, 0.20f, 1.00f);

        [Header("Cores dos Botões de Ação")]
        [Tooltip("Cor do botão Desbloquear / Comprar Upgrade.")]
        public Color buyButtonColor = new Color(0.92f, 0.22f, 0.52f, 1.00f);
        [Tooltip("Cor do botão Evoluir Cajado quando liberado.")]
        public Color evolveButtonColor = new Color(0.98f, 0.76f, 0.20f, 1.00f);

        // ─── Custom Staff Sprites ─────────────────────────────────────────────
        [Header("Personalização de Sprites do Cajado")]
        [SerializeField] public Sprite customStaffBasicSprite;
        [SerializeField] public Sprite customStaffEvolvedSprite;
        [SerializeField] public Sprite customFireHeartSprite;
        [SerializeField] public Sprite customBossCoreSprite;

        // ─── Custom Book Background ───────────────────────────────────────────
        [Header("Personalização do Fundo do Painel (Livro Travel Book)")]
        [Tooltip("Sprite do livro aberto como fundo. Se vazio, carrega automaticamente o livro montado do Travel Book!")]
        [SerializeField] public Sprite customBookSprite;
        [Tooltip("Tamanho do Livro no Canvas. Padrão 1344x960 (escala perfeita 6x do pixel-art sem distorção).")]
        public Vector2 bookSize = new Vector2(1344, 960);

        public Sprite GetBookSprite()
        {
            if (customBookSprite != null) return customBookSprite;
            var s = LoadSpriteResourceOrAsset("UI_TravelBook_BookAssembled_6x");
            if (s != null) return s;
            s = LoadSpriteResourceOrAsset("UI_TravelBook_BookAssembled_4x");
            if (s != null) return s;
            s = LoadSpriteResourceOrAsset("UI_TravelBook_BookAssembled_Full");
            return s;
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
        Image      tabTreeIndicator, tabStaffIndicator;
        TextMeshProUGUI tabTreeTxt, tabStaffTxt;
        Button     closeBtn;
        TextMeshProUGUI spText;

        // Container interativo da árvore
        RectTransform    treeContentRt;
        TreeDraggablePan panController;
        readonly HashSet<string> visibleNodeIds = new HashSet<string>();

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

        // ─── Lifecycle ────────────────────────────────────────────────────────
        void Awake()
        {
            EnsureDefaultNodes();
            EnsureBuilt();
        }

        void Start()
        {
            EnsureDefaultNodes();
            EnsureBuilt();
            HookAll();
            RefreshUI();
        }

        void OnEnable()
        {
            EnsureDefaultNodes();
            EnsureBuilt();
            HookAll();
            RefreshUI();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
            {
                OnClose();
            }
        }

        /// <summary>
        /// Se os elementos já existirem na cena, vincula as referências sem destruir nada!
        /// Se não existirem, constrói a UI inicial.
        /// </summary>
        void EnsureBuilt()
        {
            if (HasExistingUI())
            {
                BindExistingUI();
                return;
            }
            Build();
        }

        public bool HasExistingUI()
        {
            return transform.Find("FrameBorder/Frame/Tab_Tree") != null;
        }

        public void EnsureDefaultNodes()
        {
            if (treeNodes != null && treeNodes.Count >= 20)
            {
                EnsureManaNodes();
                return;
            }

            treeNodes = new List<NodeData>
            {
                // ── 1. Tronco Base ──────────────────────────────────────────────
                new NodeData { id="vit", displayName="1. Coração Reforçado", description="Cada nível aumenta sua vida máxima em um coração.", effectText="+1 Coração de Vida Máxima por nível", requirementId=null, maxLevel=5, cost=1, canvasPos=new Vector2(0, -320) },
                new NodeData { id="vel", displayName="2. Vassoura Leve", description="A vassoura corta o ar com leveza, facilitando esquivas.", effectText="+10% Velocidade de Movimento", requirementId="vit", maxLevel=1, cost=2, canvasPos=new Vector2(0, -220) },

                // ── 2. Ramo de Fogo (Coluna Interna Esquerda) ───────────────────
                new NodeData { id="f_cha", displayName="3. Chama Viva", description="Intensifica o fogo mágico disparado pelo cajado.", effectText="+15% de Dano de Fogo", requirementId="vel", maxLevel=1, cost=2, canvasPos=new Vector2(-90, -110) },
                new NodeData { id="f_que", displayName="4. Brasa Contínua", description="Inimigos atingidos ardem continuamente ao longo de 2s.", effectText="Dano Contínuo por 2s (DoT)", requirementId="f_cha", maxLevel=1, cost=3, canvasPos=new Vector2(-90, 0) },
                new NodeData { id="f_are", displayName="5. Impacto Térmico", description="O impacto do fogo causa uma detonação curta ao redor.", effectText="Dano em Área Residual (AoE)", requirementId="f_que", maxLevel=1, cost=4, canvasPos=new Vector2(-90, 110) },
                new NodeData { id="f_sup", displayName="6. Combustão Espalhada", description="Se o monstro queimar e morrer, espalha brasa aos vizinhos.", effectText="Espalha Chamas em Cadeia", requirementId="f_are", maxLevel=1, cost=4, canvasPos=new Vector2(-70, 220) },

                // ── 3. Asa de Tiro (Coluna Externa Esquerda) ───────────────────
                new NodeData { id="t_cad", displayName="7. Cadência Constante", description="Acelera a velocidade de recarga e disparo do mago.", effectText="+12% de Velocidade de Ataque", requirementId="f_cha", maxLevel=1, cost=2, canvasPos=new Vector2(-240, -40) },
                new NodeData { id="t_hit", displayName="8. Cajado Ampliado", description="Aumenta a área de contato de todos os projéteis.", effectText="+20% no Tamanho dos Projéteis", requirementId="t_cad", maxLevel=1, cost=3, canvasPos=new Vector2(-280, 70) },
                new NodeData { id="t_dup", displayName="9. Tiro Duplo Angular", description="A cada 3 disparos, solta um segundo tiro em leque de 15º.", effectText="Disparo Duplo a cada 3 tiros", requirementId="t_hit", maxLevel=1, cost=4, canvasPos=new Vector2(-280, 180) },
                new NodeData { id="t_rit", displayName="10. Ritmo Bélico", description="Atirar continuamente por 2s concede um surto de rapidez.", effectText="+10% de Cadência em Combate", requirementId="t_dup", maxLevel=1, cost=4, canvasPos=new Vector2(-210, 280) },

                // ── 4. Ramo de Gelo (Coluna Interna Direita) ────────────────────
                new NodeData { id="g_lan", displayName="11. Frio Cortante", description="O tiro de gelo penetra o ar com lâminas afiadas.", effectText="+15% de Dano de Gelo", requirementId="vel", maxLevel=1, cost=2, canvasPos=new Vector2(90, -110) },
                new NodeData { id="g_con", displayName="12. Resfriamento", description="O gelo arrefece os movimentos dos monstros inimigos.", effectText="Lentidão de 20% por 1,5s", requirementId="g_lan", maxLevel=1, cost=3, canvasPos=new Vector2(90, 0) },
                new NodeData { id="g_que", displayName="13. Quebra-Gelo", description="Causa dano devastador contra alvos que já estejam lentos.", effectText="+25% de Dano em Alvos Lentos", requirementId="g_con", maxLevel=1, cost=4, canvasPos=new Vector2(90, 110) },
                new NodeData { id="g_per", displayName="14. Gelo Perfurante", description="Se o tiro de gelo abater o primeiro monstro, ele segue em frente.", effectText="Perfuração em Golpe Letal", requirementId="g_que", maxLevel=1, cost=4, canvasPos=new Vector2(70, 220) },

                // ── 5. Asa de Vida (Coluna Externa Direita) ────────────────────
                new NodeData { id="v_inv", displayName="15. Recuperação Rápida", description="Aumenta o tempo que o mago fica imune após sofrer dano.", effectText="+0,5s de Invulnerabilidade", requirementId="g_lan", maxLevel=1, cost=2, canvasPos=new Vector2(240, -40) },
                new NodeData { id="v_cor", displayName="16. Coração Adicional", description="Fortalece permanentemente a essência vital do feiticeiro.", effectText="+1 Coração de Vida Máxima", requirementId="v_inv", maxLevel=1, cost=4, canvasPos=new Vector2(280, 70) },
                new NodeData { id="v_pul", displayName="17. Pulso Protetor", description="Ao levar um golpe, dissipa os projéteis inimigos colados em você.", effectText="Apaga Balas ao Sofrer Dano", requirementId="v_cor", maxLevel=1, cost=4, canvasPos=new Vector2(280, 180) },
                new NodeData { id="v_seg", displayName="18. Preservação Mágica", description="Ao beirar a derrota, ganha 2 segundos invulneráveis para escapar.", effectText="Sobrevida de 2s em Risco Letal", requirementId="v_pul", maxLevel=1, cost=5, canvasPos=new Vector2(210, 280) },

                // ── 6. Tronco Topo (Centro Superior & Ápice) ───────────────────
                new NodeData { id="ima", displayName="19. Ímã de Mana", description="Atrai os Skill Points e orbes dropados de muito mais longe.", effectText="+50% de Alcance de Coleta", requirementId="f_sup", maxLevel=1, cost=3, canvasPos=new Vector2(0, 280) },
                new NodeData { id="fus", displayName="20. Harmonia Elemental", description="Alternar entre Gelo e Fogo fortalece o próximo tiro com faíscas.", effectText="+25% de Dano ao Alternar Elemento", requirementId="ima", maxLevel=1, cost=5, canvasPos=new Vector2(0, 390) }
            };

            EnsureManaNodes();
        }

        void EnsureManaNodes()
        {
            if (treeNodes == null) treeNodes = new List<NodeData>();

            if (GetNode("mana_reservoir") == null)
                treeNodes.Add(new NodeData { id="mana_reservoir", displayName="Reservatório Arcano", description="Aumenta a quantidade máxima de mana.", effectText="Mana máxima: 100 → 125", requirementId="vit", maxLevel=1, cost=1, canvasPos=new Vector2(0, -430) });
            if (GetNode("mana_flow") == null)
                treeNodes.Add(new NodeData { id="mana_flow", displayName="Fluxo de Mana", description="A mana recarrega mais rápido após disparar.", effectText="Recarga: 22 → 30/s • Atraso: 0,45 → 0,35s", requirementId="mana_reservoir", maxLevel=1, cost=2, canvasPos=new Vector2(0, -530) });
            if (GetNode("mana_breath") == null)
                treeNodes.Add(new NodeData { id="mana_breath", displayName="Fôlego do Mago", description="Recupera a capacidade de disparar com menos mana.", effectText="Limite de recuperação da fadiga: 30% → 15%", requirementId="mana_flow", maxLevel=1, cost=3, canvasPos=new Vector2(0, -630) });
            if (GetNode("mana_efficiency") == null)
                treeNodes.Add(new NodeData { id="mana_efficiency", displayName="Eficiência Arcana", description="Reduz o custo de mana de cada disparo.", effectText="Custo por disparo: 5 → 4 de mana", requirementId="mana_breath", maxLevel=1, cost=4, canvasPos=new Vector2(0, -730) });
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else                       DestroyImmediate(go);
        }

        // ═════════════════════════════════════════════════════════════════════
        // APLICAÇÃO VISUAL DO LIVRO (FUNDO DO PAINEL)
        // ═════════════════════════════════════════════════════════════════════
        public void EnsureBookVisualsApplied(Transform frameBorder, Transform frame)
        {
            if (frameBorder == null || frame == null) return;

            var fbRt = (RectTransform)frameBorder;
            if (fbRt != null)
            {
                fbRt.anchorMin = new Vector2(0.5f, 0.5f);
                fbRt.anchorMax = new Vector2(0.5f, 0.5f);
                fbRt.pivot     = new Vector2(0.5f, 0.5f);
                fbRt.sizeDelta = bookSize;
                fbRt.anchoredPosition = Vector2.zero;
            }
            var fbImg = frameBorder.GetComponent<Image>();
            if (fbImg != null) fbImg.color = Color.clear;

            var fRt = (RectTransform)frame;
            if (fRt != null)
            {
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = Vector2.one;
                fRt.sizeDelta = Vector2.zero;
                fRt.anchoredPosition = Vector2.zero;
            }
            var fImg = frame.GetComponent<Image>();
            if (fImg != null)
            {
                var bookSp = GetBookSprite();
                if (bookSp != null)
                {
                    fImg.sprite = bookSp;
                    fImg.color = Color.white;
                    fImg.preserveAspect = true;
                    fImg.type = Image.Type.Simple;
                }
            }

            var treeCanvas = frame.Find("Tab_Tree/TreeBorder/TreeCanvas");
            if (treeCanvas != null)
            {
                var tcImg = treeCanvas.GetComponent<Image>();
                if (tcImg != null) tcImg.color = Color.clear;
            }

            var treeBorder = frame.Find("Tab_Tree/TreeBorder");
            if (treeBorder != null)
            {
                var tbImg = treeBorder.GetComponent<Image>();
                if (tbImg != null) tbImg.color = Color.clear;

                var tbRt = (RectTransform)treeBorder;
                tbRt.anchorMin = new Vector2(0.045f, 0.055f);
                tbRt.anchorMax = new Vector2(0.485f, 0.88f);
                tbRt.sizeDelta = Vector2.zero;
                tbRt.anchoredPosition = Vector2.zero;
            }

            var cardBorder = frame.Find("Tab_Tree/CardBorder");
            if (cardBorder != null)
            {
                var cbImg = cardBorder.GetComponent<Image>();
                if (cbImg != null) cbImg.color = Color.clear;

                var cbRt = (RectTransform)cardBorder;
                cbRt.anchorMin = new Vector2(0.515f, 0.055f);
                cbRt.anchorMax = new Vector2(0.955f, 0.88f);
                cbRt.sizeDelta = Vector2.zero;
                cbRt.anchoredPosition = Vector2.zero;
            }

            var card = frame.Find("Tab_Tree/CardBorder/Card");
            if (card != null)
            {
                var cImg = card.GetComponent<Image>();
                if (cImg != null) cImg.color = Color.clear;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // BINDING DE ELEMENTOS EXISTENTES (NÃO DESTROI MODIFICAÇÕES DO USUÁRIO)
        // ═════════════════════════════════════════════════════════════════════
        public void BindExistingUI()
        {
            var frameBorder = transform.Find("FrameBorder");
            if (frameBorder == null) return;
            var frame = frameBorder.Find("Frame");
            if (frame == null) return;

            EnsureBookVisualsApplied(frameBorder, frame);

            var hdr = frame.Find("Header");
            if (hdr != null)
            {
                var bt = hdr.Find("Btn_Tree");
                if (bt != null)
                {
                    tabTreeBtn = bt.GetComponent<Button>();
                    tabTreeBtnBg = bt.GetComponent<Image>();
                    tabTreeTxt = bt.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    tabTreeIndicator = bt.Find("Indicator")?.GetComponent<Image>();
                }

                var bs = hdr.Find("Btn_Staff");
                if (bs != null)
                {
                    tabStaffBtn = bs.GetComponent<Button>();
                    tabStaffBtnBg = bs.GetComponent<Image>();
                    tabStaffTxt = bs.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    tabStaffIndicator = bs.Find("Indicator")?.GetComponent<Image>();
                }

                var spBx = hdr.Find("SP_Border/SP_Box");
                if (spBx != null) spText = spBx.Find("SP")?.GetComponent<TextMeshProUGUI>();

                var clo = hdr.Find("Btn_Close");
                if (clo != null) closeBtn = clo.GetComponent<Button>();
            }

            var tTree = frame.Find("Tab_Tree");
            if (tTree != null)
            {
                tabTreeContent = tTree.gameObject;
                var treeCanvas = tTree.Find("TreeBorder/TreeCanvas");
                if (treeCanvas != null)
                {
                    panController = treeCanvas.GetComponent<TreeDraggablePan>()
                        ?? treeCanvas.gameObject.AddComponent<TreeDraggablePan>();
                    var tContent = treeCanvas.Find("TreeContent");
                    if (tContent != null)
                    {
                        treeContentRt = tContent.GetComponent<RectTransform>();
                        treeContentRt.sizeDelta = treeAreaSize;
                        panController.SetContent(treeContentRt);
                        panController.dragSensitivity = dragSensitivity;
                        panController.invertDrag = invertDrag;
                        panController.enableZoom = enableZoom;
                        panController.minZoom = 0.25f;

                        // Reconecta os nós existentes e respeita posições/tamanhos editados na Scene View!
                        foreach (var n in treeNodes)
                        {
                            var nTr = tContent.Find($"Node_{n.id}");
                            if (nTr == null)
                            {
                                BuildTreeNode(n);
                                continue;
                            }
                            else
                            {
                                n.nodeGo = nTr.gameObject;
                                n.nodeBg = nTr.GetComponent<Image>();
                                n.nodeRing = nTr.Find("Ring")?.GetComponent<Image>();
                                n.nodeIcon = nTr.Find("Icon")?.GetComponent<Image>();
                                n.levelLabel = nTr.Find("Lv")?.GetComponent<TextMeshProUGUI>();

                                BindNodeInteractions(n, nTr.gameObject);
                                if (n.nodeRing != null) n.nodeRing.raycastTarget = false;
                                if (n.nodeIcon != null) n.nodeIcon.raycastTarget = false;

                                // Atualiza sprite se houver customIcon
                                if (n.nodeIcon != null) n.nodeIcon.sprite = GetNodeSprite(n);

                                var rt = (RectTransform)nTr;
                                n.canvasPos = rt.anchoredPosition;

                                if (n.customNodeSize == Vector2.zero && rt.sizeDelta != defaultNodeSize)
                                {
                                    n.customNodeSize = rt.sizeDelta;
                                }
                            }
                        }
                    }
                }

                var cardTr = tTree.Find("CardBorder/Card");
                if (cardTr != null)
                {
                    cardName = cardTr.Find("Name")?.GetComponent<TextMeshProUGUI>();
                    cardCost = cardTr.Find("CostBorder/CostBox/CostTxt")?.GetComponent<TextMeshProUGUI>();
                    cardIconBox = cardTr.Find("IconBox")?.gameObject;
                    if (cardIconBox != null)
                    {
                        cardIconImg = cardIconBox.transform.Find("IconPreview")?.GetComponent<Image>();
                        cardPipsContainer = cardIconBox.transform.Find("Pips");
                    }
                    cardDesc = cardTr.Find("DescBox/Desc")?.GetComponent<TextMeshProUGUI>();
                    cardEffect = cardTr.Find("EffectBox/Effect")?.GetComponent<TextMeshProUGUI>();
                    cardStatus = cardTr.Find("Status")?.GetComponent<TextMeshProUGUI>();
                    var bBuy = cardTr.Find("Btn_Buy");
                    if (bBuy != null)
                    {
                        buyBtn = bBuy.GetComponent<Button>();
                        buyBtnLabel = bBuy.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            var tStaff = frame.Find("Tab_Staff");
            if (tStaff != null)
            {
                tabStaffContent = tStaff.gameObject;
                var curBox = tStaff.Find("BoxCur_Border/Box_Cur");
                if (curBox != null)
                {
                    curStaffImg = curBox.Find("Img")?.GetComponent<Image>();
                    curStaffName = curBox.Find("Name")?.GetComponent<TextMeshProUGUI>();
                    curStaffStat = curBox.Find("Stat")?.GetComponent<TextMeshProUGUI>();
                }
                var nxtBox = tStaff.Find("BoxNxt_Border/Box_Nxt") ?? tStaff.Find("Box_Nxt");
                if (nxtBox != null)
                {
                    nxtStaffBox = nxtBox.gameObject;
                    nxtStaffImg = nxtBox.Find("Img")?.GetComponent<Image>();
                    nxtStaffName = nxtBox.Find("Name")?.GetComponent<TextMeshProUGUI>();
                    nxtStaffStat = nxtBox.Find("Stat")?.GetComponent<TextMeshProUGUI>();
                }
                var matP = tStaff.Find("BoxNxt_Border/MatPanel") ?? tStaff.Find("MatPanel");
                if (matP != null)
                {
                    needSummary = matP.Find("Need")?.GetComponent<TextMeshProUGUI>();
                    var mBat = matP.Find("Mat_Coração de Fogo");
                    if (mBat != null)
                    {
                        batSegCont = mBat.Find("Segs");
                        batCountTxt = mBat.Find("Count")?.GetComponent<TextMeshProUGUI>();
                    }
                    var mCore = matP.Find("Mat_Núcleo do Boss");
                    if (mCore != null)
                    {
                        coreSegCont = mCore.Find("Segs");
                        coreCountTxt = mCore.Find("Count")?.GetComponent<TextMeshProUGUI>();
                    }
                }
                var ev = tStaff.Find("BoxNxt_Border/Btn_Evolve") ?? tStaff.Find("Btn_Evolve");
                if (ev != null)
                {
                    evolveBtn = ev.GetComponent<Button>();
                    evolveBtnLabel = ev.Find("Label")?.GetComponent<TextMeshProUGUI>();
                }
                var msgTr = tStaff.Find("BoxNxt_Border/Msg") ?? tStaff.Find("Msg");
                if (msgTr != null) staffMsg = msgTr.GetComponent<TextMeshProUGUI>();
            }

            HookAll();
            RefreshUI();
        }

        // ═════════════════════════════════════════════════════════════════════
        // BUILD PROCEDURAL DA INTERFACE (CHAMADO APENAS QUANDO A UI NÃO EXISTE OU AO FORÇAR)
        // ═════════════════════════════════════════════════════════════════════
        public void Build()
        {
            EnsureDefaultNodes();

            for (int i = transform.childCount - 1; i >= 0; i--)
                SafeDestroy(transform.GetChild(i).gameObject);

            var selfCanvas = GetComponent<Canvas>();
            selfCanvas.overrideSorting = true;
            selfCanvas.sortingOrder    = 100;

            var selfRt = GetComponent<RectTransform>();
            if (selfRt)
            {
                selfRt.anchorMin        = Vector2.zero;
                selfRt.anchorMax        = Vector2.one;
                selfRt.sizeDelta        = Vector2.zero;
                selfRt.anchoredPosition = Vector2.zero;
                selfRt.localScale       = Vector3.one;
            }
            Img(gameObject, C_BG_OVERLAY);

            // Container central do livro no formato 1.4:1 (pixel-perfect 1344x960)
            var frameBorder = Rect("FrameBorder", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), bookSize);
            Img(frameBorder, Color.clear);

            // O Frame agora exibe o Sprite completo do Livro Aberto montado do Travel Book!
            var frame = Rect("Frame", frameBorder.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            var frameImg = Img(frame, Color.white);
            frameImg.sprite = GetBookSprite();
            frameImg.preserveAspect = true;
            frameImg.type = Image.Type.Simple;

            BuildHeader(frame.transform);

            // Tab 1: Árvore de Habilidades (Página Esquerda = Árvore; Página Direita = Card de Detalhes)
            tabTreeContent = Rect("Tab_Tree", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildTreeTab(tabTreeContent.transform);

            // Tab 2: Evolução do Cajado (Página Esquerda = Cajado Atual; Página Direita = Próximo + Materiais)
            tabStaffContent = Rect("Tab_Staff", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildStaffTab(tabStaffContent.transform);
            tabStaffContent.SetActive(false);

            var hdr = frame.transform.Find("Header");
            if (hdr != null) hdr.SetAsLastSibling();

            HookAll();
            SwitchTab(false);
        }

        // ─── Header: 2 Botões no Topo (Tabs Marcadores) + SP + Fechar ────────
        void BuildHeader(Transform parent)
        {
            var hdr = Rect("Header", parent, new Vector2(0.045f, 0.88f), new Vector2(0.955f, 0.98f), new Vector2(0.5f, 1f), Vector2.zero);
            SetAnchoredPos(hdr, Vector2.zero);

            var navBg = Rect("NavBg", hdr.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            Img(navBg, Color.clear);

            // Botão 1: Árvore (Aba Marcador Esquerdo)
            var bt = Rect("Btn_Tree", hdr.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(280, 48));
            SetAnchoredPos(bt, new Vector2(10, 0));
            tabTreeBtnBg = Img(bt, tabActiveColor);
            tabTreeBtn   = bt.AddComponent<Button>();
            tabTreeTxt   = Txt("Text", bt.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, 
                               "✦ ÁRVORE DE HABILIDADES", 16, tabActiveTextColor, FontStyles.Bold);
            tabTreeTxt.alignment = TextAlignmentOptions.Center;

            var treeIndGo = Rect("Indicator", bt.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 4));
            tabTreeIndicator = Img(treeIndGo, tabIndicatorColor);

            // Botão 2: Cajado (Aba Marcador Direito)
            var bs = Rect("Btn_Staff", hdr.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(240, 48));
            SetAnchoredPos(bs, new Vector2(300, 0));
            tabStaffBtnBg = Img(bs, tabInactiveColor);
            tabStaffBtn   = bs.AddComponent<Button>();
            tabStaffTxt   = Txt("Text", bs.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, 
                                "⚡ EVOLUIR CAJADO", 16, tabInactiveTextColor, FontStyles.Bold);
            tabStaffTxt.alignment = TextAlignmentOptions.Center;

            var staffIndGo = Rect("Indicator", bs.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 4));
            tabStaffIndicator = Img(staffIndGo, Color.clear);

            // Caixa de SP no canto superior direito do livro
            var spBorder = Rect("SP_Border", hdr.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(210, 44));
            SetAnchoredPos(spBorder, new Vector2(-65, 0));
            Img(spBorder, C_GOLD);

            var spBox = Rect("SP_Box", spBorder.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4, -4));
            Img(spBox, C_DARK_BOX);

            spText = Txt("SP", spBox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, 
                         "★ SP: 0", 17, C_GOLD, FontStyles.Bold);
            spText.alignment = TextAlignmentOptions.Center;

            // Botão Fechar [X] no canto superior direito do livro
            var clo = Rect("Btn_Close", hdr.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(46, 44));
            SetAnchoredPos(clo, new Vector2(-10, 0));
            Img(clo, new Color(0.75f, 0.15f, 0.28f, 1f));
            closeBtn = clo.AddComponent<Button>();

            var cloTxt = Txt("X", clo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one, 
                             "✕", 22, Color.white, FontStyles.Bold);
            cloTxt.alignment = TextAlignmentOptions.Center;
        }

        // ─── Tab 1: Árvore Interativa (Página Esquerda = Árvore; Direita = Detalhes) ──
        void BuildTreeTab(Transform parent)
        {
            // Página Esquerda do Livro: Área da Árvore
            var treeBorder = Rect("TreeBorder", parent, new Vector2(0.045f, 0.055f), new Vector2(0.485f, 0.88f), Vector2.zero, Vector2.zero);
            Img(treeBorder, Color.clear);

            // TreeCanvas com fundo transparente para o pergaminho do livro aparecer!
            var treeCanvas = Rect("TreeCanvas", treeBorder.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(treeCanvas, Color.clear);
            treeCanvas.AddComponent<RectMask2D>();

            panController = treeCanvas.AddComponent<TreeDraggablePan>();
            panController.dragSensitivity = dragSensitivity;
            panController.invertDrag = invertDrag;
            panController.enableZoom = enableZoom;

            // Container interno móvel com tamanho configurável
            var contentGo = Rect("TreeContent", treeCanvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), treeAreaSize);
            treeContentRt = contentGo.GetComponent<RectTransform>();
            panController.SetContent(treeContentRt);

            // Página Direita do Livro: Card de Detalhes
            var cardBorder = Rect("CardBorder", parent, new Vector2(0.515f, 0.055f), new Vector2(0.955f, 0.88f), Vector2.zero, Vector2.zero);
            Img(cardBorder, Color.clear);

            var card = Rect("Card", cardBorder.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(card, Color.clear);
            BuildDetailCard(card.transform);

            RedrawLines();

            // Constrói cada Nó com suporte aos tamanhos personalizados
            foreach (var n in treeNodes) BuildTreeNode(n);

            // Botão "Resetar / Centralizar Visão [⟲]" na página esquerda
            var resetGo = Rect("Btn_ResetView", treeCanvas.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(110, 30));
            SetAnchoredPos(resetGo, new Vector2(-12, 12));
            Img(resetGo, new Color(0.20f, 0.14f, 0.32f, 0.85f));
            var rBtn = resetGo.AddComponent<Button>();
            rBtn.onClick.AddListener(() => {
                if (panController != null) panController.ResetView();
            });
            var rTxt = Txt("Txt", resetGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                           "⟲ Centralizar", 12, C_GOLD, FontStyles.Bold);
            rTxt.alignment = TextAlignmentOptions.Center;

            var leg = Txt("Legend", treeCanvas.transform, new Vector2(0, 0), new Vector2(0.75f, 0), new Vector2(0, 0), new Vector2(0, 24),
                "● Bloqueado  ● Disponível  ● Comprado  ★ Máximo",
                12, new Color(0.35f, 0.25f, 0.18f, 0.95f), FontStyles.Bold);
            SetAnchoredPos(leg.gameObject, new Vector2(12, 10));
        }

        GameObject BuildTreeNode(NodeData n)
        {
            Vector2 nodeSize = n.customNodeSize != Vector2.zero ? n.customNodeSize : defaultNodeSize;
            Vector2 iconSize = n.customIconSize != Vector2.zero ? n.customIconSize : defaultIconSize;
            var nodeGo = Rect($"Node_{n.id}", treeContentRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), nodeSize);
            SetAnchoredPos(nodeGo, n.canvasPos);
            n.nodeGo = nodeGo;
            n.nodeBg = Img(nodeGo, C_LOCKED);

            var ringGo = Rect("Ring", nodeGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 12));
            n.nodeRing = Img(ringGo, new Color(C_AVAIL.r, C_AVAIL.g, C_AVAIL.b, 0f));
            n.nodeRing.raycastTarget = false;
            ringGo.transform.SetAsFirstSibling();

            var iconGo = Rect("Icon", nodeGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), iconSize);
            n.nodeIcon = iconGo.AddComponent<Image>();
            n.nodeIcon.sprite = GetNodeSprite(n);
            n.nodeIcon.preserveAspect = true;
            n.nodeIcon.raycastTarget = false;

            n.levelLabel = Txt("Lv", nodeGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 22),
                "", 13, C_MUTED, FontStyles.Bold);
            n.levelLabel.alignment = TextAlignmentOptions.Center;
            SetAnchoredPos(n.levelLabel.gameObject, new Vector2(0, -(nodeSize.y * 0.5f + 12f)));
            BindNodeInteractions(n, nodeGo);
            return nodeGo;
        }

        public void RedrawLines()
        {
            if (treeContentRt == null) return;

            var linesObj = treeContentRt.Find("Lines");
            if (linesObj != null)
            {
                SafeDestroy(linesObj.gameObject);
            }

            var linesRoot = Rect("Lines", treeContentRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            linesRoot.transform.SetAsFirstSibling();

            foreach (var n in treeNodes)
            {
                if (string.IsNullOrEmpty(n.requirementId)) continue;
                var req = GetNode(n.requirementId);
                if (req == null || !visibleNodeIds.Contains(n.id) || !visibleNodeIds.Contains(req.id)) continue;
                DrawLine(linesRoot.transform, req.canvasPos, n.canvasPos);
            }
        }

        public Sprite GetNodeSprite(NodeData n)
        {
            if (n.customIcon != null) return n.customIcon;
            return SkillIconProvider.GetIcon(n.id);
        }

        // ─── Card de Detalhes Repaginado (Estilo Manuscrito Medieval) ──────────
        void BuildDetailCard(Transform parent)
        {
            cardName = Txt("Name", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-160, 44),
                "Vitalidade Arcana", 23, C_CARD_DARK, FontStyles.Bold);
            SetAnchoredPos(cardName.gameObject, new Vector2(18, -14));

            var costBorder = Rect("CostBorder", parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(130, 36));
            SetAnchoredPos(costBorder, new Vector2(-18, -14));
            Img(costBorder, C_GOLD);

            var costBx = Rect("CostBox", costBorder.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4, -4));
            Img(costBx, C_DARK_BOX);

            cardCost = Txt("CostTxt", costBx.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                "CUSTO 1 SP", 15, C_GOLD, FontStyles.Bold);
            cardCost.alignment = TextAlignmentOptions.Center;

            cardIconBox = Rect("IconBox", parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(340, 115));
            SetAnchoredPos(cardIconBox, new Vector2(0, -68));
            Img(cardIconBox, C_DARK_BOX);

            var iconPre = Rect("IconPreview", cardIconBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64, 64));
            SetAnchoredPos(iconPre, new Vector2(0, 12));
            cardIconImg = iconPre.AddComponent<Image>();
            cardIconImg.preserveAspect = true;
            cardIconImg.raycastTarget = false;

            var pipsGo = Rect("Pips", cardIconBox.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(180, 20));
            SetAnchoredPos(pipsGo, new Vector2(0, 12));
            var hlg = pipsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            cardPipsContainer = pipsGo.transform;

            var descBox = Rect("DescBox", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-36, 92));
            SetAnchoredPos(descBox, new Vector2(0, -196));
            Img(descBox, new Color(0.92f, 0.88f, 0.82f, 0.70f));

            var lblDesc = Txt("LblDesc", descBox.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-16, 22),
                "DESCRIÇÃO:", 13, new Color(0.40f, 0.30f, 0.52f, 1f), FontStyles.Bold);
            SetAnchoredPos(lblDesc.gameObject, new Vector2(12, -6));

            cardDesc = Txt("Desc", descBox.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-24, -30),
                           "", 15, C_CARD_DARK, FontStyles.Normal);
            SetAnchoredPos(cardDesc.gameObject, new Vector2(0, -8));
            cardDesc.textWrappingMode = TextWrappingModes.Normal;

            var effectBox = Rect("EffectBox", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-36, 50));
            SetAnchoredPos(effectBox, new Vector2(0, -298));
            Img(effectBox, new Color(0.20f, 0.12f, 0.30f, 0.95f));

            cardEffect = Txt("Effect", effectBox.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                "+1 Coração de Vida", 17, C_GOLD, FontStyles.Bold);
            cardEffect.alignment = TextAlignmentOptions.Center;

            cardStatus = Txt("Status", parent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-36, 26),
                "", 14, new Color(0.35f, 0.25f, 0.45f, 1f), FontStyles.Italic);
            SetAnchoredPos(cardStatus.gameObject, new Vector2(0, 72));
            cardStatus.alignment = TextAlignmentOptions.Center;

            var btnGo = Rect("Btn_Buy", parent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-36, 52));
            SetAnchoredPos(btnGo, new Vector2(0, 14));
            Img(btnGo, C_BOUGHT);
            buyBtn = btnGo.AddComponent<Button>();

            buyBtnLabel = Txt("Label", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                "DESBLOQUEAR", 19, Color.white, FontStyles.Bold);
            buyBtnLabel.alignment = TextAlignmentOptions.Center;
            buyBtn.onClick.AddListener(() => TryBuy(selectedId));
        }

        // ─── Tab 2: Evolução do Cajado (Página Esquerda = Atual; Página Direita = Próximo + Materiais) ──
        void BuildStaffTab(Transform parent)
        {
            // Página Esquerda: Cajado Atual
            var boxCurBorder = Rect("BoxCur_Border", parent, new Vector2(0.045f, 0.055f), new Vector2(0.485f, 0.88f), Vector2.zero, Vector2.zero);
            Img(boxCurBorder, Color.clear);

            var boxCur = Rect("Box_Cur", boxCurBorder.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            Img(boxCur, Color.clear);

            var curTag = Txt("Tag", boxCur.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 30),
                "● EM USO", 16, C_CYAN_ICE, FontStyles.Bold);
            SetAnchoredPos(curTag.gameObject, new Vector2(0, -18));
            curTag.alignment = TextAlignmentOptions.Center;

            curStaffImg = MakeImg("Img", boxCur.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140, 180));
            SetAnchoredPos(curStaffImg.gameObject, new Vector2(0, 40));
            curStaffImg.preserveAspect = true;
            curStaffImg.color = Color.white;
            curStaffImg.sprite = GetBasicStaffSprite();

            curStaffName = Txt("Name", boxCur.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 32),
                "Cajado de Gelo", 22, C_CARD_DARK, FontStyles.Bold);
            SetAnchoredPos(curStaffName.gameObject, new Vector2(0, -85));
            curStaffName.alignment = TextAlignmentOptions.Center;

            curStaffStat = Txt("Stat", boxCur.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 26),
                "Dano Base: 1.0 (Orbe Glacial)", 15, new Color(0.15f, 0.45f, 0.70f, 1f), FontStyles.Bold);
            SetAnchoredPos(curStaffStat.gameObject, new Vector2(0, -120));
            curStaffStat.alignment = TextAlignmentOptions.Center;

            var curDesc = Txt("CurDesc", boxCur.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40, 60),
                "Canaliza energia pura de gelo. Seus projéteis desaceleram os monstros da masmorra.", 14, new Color(0.35f, 0.28f, 0.22f, 0.9f), FontStyles.Italic);
            SetAnchoredPos(curDesc.gameObject, new Vector2(0, -175));
            curDesc.alignment = TextAlignmentOptions.Center;

            // Página Direita: Próximo Cajado + Painel de Materiais + Botão de Evolução
            var boxNxtBorder = Rect("BoxNxt_Border", parent, new Vector2(0.515f, 0.055f), new Vector2(0.955f, 0.88f), Vector2.zero, Vector2.zero);
            Img(boxNxtBorder, Color.clear);

            var boxNxt = Rect("Box_Nxt", boxNxtBorder.transform, new Vector2(0, 0.58f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero);
            Img(boxNxt, Color.clear);
            nxtStaffBox = boxNxt;

            var nxtTag = Txt("Tag", boxNxt.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 26),
                "★ PRÓXIMO NÍVEL", 15, C_GOLD, FontStyles.Bold);
            SetAnchoredPos(nxtTag.gameObject, new Vector2(0, -10));
            nxtTag.alignment = TextAlignmentOptions.Center;

            nxtStaffImg = MakeImg("Img", boxNxt.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90, 120));
            SetAnchoredPos(nxtStaffImg.gameObject, new Vector2(0, 12));
            nxtStaffImg.preserveAspect = true;
            nxtStaffImg.color = Color.white;
            nxtStaffImg.sprite = GetEvolvedStaffSprite();

            nxtStaffName = Txt("Name", boxNxt.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 28),
                "Cajado de Fogo", 20, C_CARD_DARK, FontStyles.Bold);
            SetAnchoredPos(nxtStaffName.gameObject, new Vector2(0, 30));
            nxtStaffName.alignment = TextAlignmentOptions.Center;

            nxtStaffStat = Txt("Stat", boxNxt.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 24),
                "+60% de Dano (1.6 / Tiro) • Dano Contínuo", 14, C_FIRE_RED, FontStyles.Bold);
            SetAnchoredPos(nxtStaffStat.gameObject, new Vector2(0, 6));
            nxtStaffStat.alignment = TextAlignmentOptions.Center;

            // Painel de Materiais dentro da página direita
            var matPanel = Rect("MatPanel", boxNxtBorder.transform, new Vector2(0, 0.16f), new Vector2(1, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Img(matPanel, new Color(0.12f, 0.08f, 0.18f, 0.92f));

            needSummary = Txt("Need", matPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-24, 28),
                "MATERIAIS NECESSÁRIOS: <color=#ffc83a>7</color> Corações e <color=#ffc83a>1</color> Núcleo",
                15, Color.white, FontStyles.Bold);
            SetAnchoredPos(needSummary.gameObject, new Vector2(0, -8));
            needSummary.alignment = TextAlignmentOptions.Center;
            needSummary.richText  = true;

            BuildMatRow(matPanel.transform, new Vector2(0, -40), "Coração de Fogo", GetFireHeartSprite(), 7, out batSegCont, out batCountTxt);
            BuildMatRow(matPanel.transform, new Vector2(0, -100), "Núcleo do Boss",   GetBossCoreSprite(),  1, out coreSegCont, out coreCountTxt);

            var evGo = Rect("Btn_Evolve", boxNxtBorder.transform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.14f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Img(evGo, C_BOUGHT);
            evolveBtn = evGo.AddComponent<Button>();

            evolveBtnLabel = Txt("Label", evGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                "★ EVOLUIR CAJADO ★", 20, Color.white, FontStyles.Bold);
            evolveBtnLabel.alignment = TextAlignmentOptions.Center;
            evolveBtn.onClick.AddListener(OnEvolveClicked);

            staffMsg = Txt("Msg", boxNxtBorder.transform, new Vector2(0, 0.005f), new Vector2(1, 0.045f), new Vector2(0.5f, 0.5f), Vector2.zero,
                "Derrote morcegos de fogo e chefes para recolher materiais.", 13, new Color(0.35f, 0.28f, 0.22f, 0.9f), FontStyles.Italic);
            staffMsg.alignment = TextAlignmentOptions.Center;
        }

        void BuildMatRow(Transform parent, Vector2 anchoredPos, string label, Sprite iconSprite, int total,
                         out Transform segContainer, out TextMeshProUGUI countTxt)
        {
            var box = Rect("Mat_" + label, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-36, 54));
            SetAnchoredPos(box, anchoredPos);
            Img(box, C_DARK_BOX_HL);

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

            var lbl = Txt("Lbl", box.transform, new Vector2(0, 0.5f), new Vector2(0.45f, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 24),
                label, 17, Color.white, FontStyles.Bold);
            SetAnchoredPos(lbl.gameObject, new Vector2(56, 8));

            var segsGo = Rect("Segs", box.transform, new Vector2(0, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 12));
            SetAnchoredPos(segsGo, new Vector2(56, -12));
            var hlg = segsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; 
            hlg.childForceExpandWidth = true; 
            hlg.childForceExpandHeight = true;
            segContainer = segsGo.transform;

            for (int i = 0; i < total; i++)
            {
                var s = new GameObject($"Seg_{i}"); 
                s.transform.SetParent(segContainer, false);
                s.AddComponent<Image>().color = C_LOCKED;
            }

            countTxt = Txt("Count", box.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(100, 28),
                $"0/{total}", 18, Color.white, FontStyles.Bold);
            SetAnchoredPos(countTxt.gameObject, new Vector2(-16, 0));
            countTxt.alignment = TextAlignmentOptions.MidlineRight;
        }

        // ═════════════════════════════════════════════════════════════════════
        // HOOKS E ALTERNÂNCIA DE ABAS
        // ═════════════════════════════════════════════════════════════════════
        void HookAll()
        {
            if (tabTreeBtn  != null) { tabTreeBtn.onClick.RemoveAllListeners();  tabTreeBtn.onClick.AddListener(() => SwitchTab(false)); }
            if (tabStaffBtn != null) { tabStaffBtn.onClick.RemoveAllListeners(); tabStaffBtn.onClick.AddListener(() => SwitchTab(true));  }
            if (closeBtn    != null) { closeBtn.onClick.RemoveAllListeners();    closeBtn.onClick.AddListener(OnClose); }
        }

        public void SwitchTab(bool toStaff)
        {
            staffTabOpen = toStaff;

            if (tabTreeContent  != null) tabTreeContent.SetActive(!toStaff);
            if (tabStaffContent != null) tabStaffContent.SetActive(toStaff);

            if (tabTreeBtnBg != null) tabTreeBtnBg.color = !toStaff ? tabActiveColor : tabInactiveColor;
            if (tabStaffBtnBg != null) tabStaffBtnBg.color = toStaff ? tabActiveColor : tabInactiveColor;

            if (tabTreeTxt != null) tabTreeTxt.color = !toStaff ? tabActiveTextColor : tabInactiveTextColor;
            if (tabStaffTxt != null) tabStaffTxt.color = toStaff ? tabActiveTextColor : tabInactiveTextColor;

            if (tabTreeIndicator != null) tabTreeIndicator.color = !toStaff ? tabIndicatorColor : Color.clear;
            if (tabStaffIndicator != null) tabStaffIndicator.color = toStaff ? tabIndicatorColor : Color.clear;

            RefreshUI();
        }

        public void SelectNode(string id) { selectedId = id; RefreshDetailCard(); }

        public void RefreshUI()
        {
            if (GameManager.Instance == null) return;
            int sp = GameManager.Instance.SkillPoints;
            if (spText != null) spText.text = $"★ SP: {sp}";

            RefreshNodes();
            if (!staffTabOpen) RefreshDetailCard();
            else               RefreshStaffTab();
        }

        void RefreshNodes()
        {
            if (GameManager.Instance == null) return;
            UpdateNodeVisibility();
            int sp = GameManager.Instance.SkillPoints;
            foreach (var n in treeNodes)
            {
                if (n.nodeBg == null) continue;
                int  lvl     = GameManager.Instance.GetSkillLevel(n.id);
                bool reached = IsReachable(n);
                bool maxed   = lvl >= n.maxLevel;
                int cost = GetNodeCost(n, lvl);
                bool canBuy  = reached && !maxed && sp >= cost;

                if (maxed)        n.nodeBg.color = nodeMaxedColor;
                else if (lvl > 0) n.nodeBg.color = nodeBoughtColor;
                else if (canBuy)  n.nodeBg.color = nodeAvailableColor;
                else              n.nodeBg.color = nodeLockedColor;

                if (n.nodeRing != null)
                {
                    Color rc = maxed ? nodeMaxedColor : canBuy ? nodeAvailableColor : nodeBoughtColor;
                    float a  = n.nodeRing.color.a;
                    n.nodeRing.color = new Color(rc.r, rc.g, rc.b, a);
                }

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
            int  cost    = GetNodeCost(n, lvl);
            bool canBuy  = reached && !maxed && sp >= cost;

            if (cardName != null) cardName.text = n.displayName;
            if (cardCost != null) cardCost.text = maxed ? "CUSTO: —" : $"CUSTO: {cost} SP";
            if (cardDesc != null) cardDesc.text = n.description;

            if (cardIconImg != null)
            {
                cardIconImg.sprite = GetNodeSprite(n);
                cardIconImg.color  = Color.white;
            }

            if (cardPipsContainer != null)
            {
                for (int i = cardPipsContainer.childCount - 1; i >= 0; i--)
                    SafeDestroy(cardPipsContainer.GetChild(i).gameObject);

                for (int i = 0; i < n.maxLevel; i++)
                {
                    var pip = new GameObject($"Pip_{i}");
                    pip.transform.SetParent(cardPipsContainer, false);
                    var prt = pip.AddComponent<RectTransform>();
                    prt.sizeDelta = new Vector2(18, 12);
                    var pImg = pip.AddComponent<Image>();
                    pImg.color = (i < lvl) ? (maxed ? nodeMaxedColor : nodeBoughtColor) : nodeLockedColor;
                }
            }

            if (cardEffect != null)
            {
                if (n.id == "vit")
                {
                    cardEffect.text = n.effectText;
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
            Color  btnColor  = new Color(0.35f, 0.28f, 0.48f, 1f);

            if (maxed)
            {
                statusMsg = "Habilidade no nível máximo!";
                btnLabel  = "NÍVEL MÁXIMO";
            }
            else if (!reached)
            {
                var req = GetNode(n.requirementId);
                statusMsg = $"Requer: {(req != null ? req.displayName : "habilidade anterior")}";
                btnLabel  = "BLOQUEADO";
            }
            else if (sp < cost)
            {
                statusMsg = $"Faltam {cost - sp} Skill Point(s)";
                btnLabel  = "SP INSUFICIENTE";
            }
            else
            {
                statusMsg = "Pronto para desbloquear!";
                btnActive = true;
                btnLabel  = "DESBLOQUEAR";
                btnColor  = buyButtonColor;
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

            bool evolved = GameManager.Instance.IsStaffEvolved;
            int  bat     = GameManager.Instance.FireHeartsCount;
            int  reqBat  = GameManager.Instance.FireHeartsRequired;
            int  core    = GameManager.Instance.BossCoresCount;
            int  reqCore = GameManager.Instance.BossCoresRequired;

            if (batCountTxt  != null) { batCountTxt.text  = $"{bat}/{reqBat}";   batCountTxt.color  = bat  >= reqBat  ? C_GOLD : Color.white; }
            if (coreCountTxt != null) { coreCountTxt.text = $"{core}/{reqCore}"; coreCountTxt.color = core >= reqCore ? C_GOLD : Color.white; }

            UpdateSegs(batSegCont,  bat,  reqBat);
            UpdateSegs(coreSegCont, core, reqCore);

            bool canEvolve = !evolved && GameManager.Instance.CanEvolveStaff();

            if (evolved)
            {
                if (curStaffName != null) curStaffName.text = "Cajado Arcano das Chamas";
                if (curStaffStat != null) curStaffStat.text = "+60% de Dano (1.6 / Tiro) • Fogo";
                if (curStaffImg  != null) { curStaffImg.sprite = GetEvolvedStaffSprite(); curStaffImg.color = Color.white; }
                if (nxtStaffBox  != null) nxtStaffBox.SetActive(false);
                if (needSummary  != null) needSummary.text = "Cajado no ápice de poder!";
                if (staffMsg     != null) staffMsg.text = "Todas as evoluções do cajado foram concluídas.";
                if (evolveBtn    != null) { evolveBtn.interactable = false; var bi = evolveBtn.GetComponent<Image>(); if (bi) bi.color = C_LINE; }
                if (evolveBtnLabel != null) evolveBtnLabel.text = "NÍVEL MÁXIMO";
            }
            else
            {
                if (curStaffName != null) curStaffName.text = "Cajado de Gelo";
                if (curStaffStat != null) curStaffStat.text = "Dano Base: 1.0 (Orbe Glacial)";
                if (curStaffImg  != null) { curStaffImg.sprite = GetBasicStaffSprite(); curStaffImg.color = Color.white; }
                if (nxtStaffName != null) nxtStaffName.text = "Cajado de Fogo";
                if (nxtStaffStat != null) nxtStaffStat.text = "+60% de Dano (1.6 / Tiro) • Fogo";
                if (nxtStaffImg  != null) { nxtStaffImg.sprite = GetEvolvedStaffSprite(); nxtStaffImg.color = Color.white; }
                if (nxtStaffBox  != null) nxtStaffBox.SetActive(true);
                if (evolveBtn    != null) { evolveBtn.interactable = canEvolve; var bi = evolveBtn.GetComponent<Image>(); if (bi) bi.color = canEvolve ? evolveButtonColor : new Color(0.35f, 0.28f, 0.48f, 1f); }
                if (evolveBtnLabel != null) evolveBtnLabel.text = canEvolve ? "★ EVOLUIR CAJADO ★" : "MATERIAIS INSUFICIENTES";
                if (staffMsg     != null) staffMsg.text = canEvolve ? "Tudo pronto! Pressione o botão para evoluir." : "Derrote morcegos de fogo e chefes para recolher materiais.";
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
                SafeDestroy(cont.GetChild(cont.childCount - 1).gameObject);
            }

            for (int i = 0; i < cont.childCount; i++)
            {
                var img = cont.GetChild(i).GetComponent<Image>();
                if (img) img.color = i < have ? (have >= req ? C_GOLD : C_BOUGHT) : C_LOCKED;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // AÇÕES
        // ═════════════════════════════════════════════════════════════════════
        void TryBuy(string id)
        {
            if (GameManager.Instance == null) return;
            NodeData n = GetNode(id);
            if (n == null) return;
            int lvl = GameManager.Instance.GetSkillLevel(n.id);
            int cost = GetNodeCost(n, lvl);
            if (!IsReachable(n) || lvl >= n.maxLevel || GameManager.Instance.SkillPoints < cost) return;
            if (GameManager.Instance.UpgradeSkill(n.id, cost)) RefreshUI();
        }

        int GetNodeCost(NodeData node, int currentLevel)
        {
            return node.id == "vit" ? node.cost + currentLevel : node.cost;
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

        bool IsReachable(NodeData n)
        {
            if (string.IsNullOrEmpty(n.requirementId)) return true;
            return GameManager.Instance != null && GameManager.Instance.GetSkillLevel(n.requirementId) > 0;
        }

        NodeData GetNode(string id)
        {
            if (string.IsNullOrEmpty(id) || treeNodes == null) return null;
            return treeNodes.Find(x => x.id == id);
        }

        void DrawLine(Transform parent, Vector2 a, Vector2 b)
        {
            var go  = new GameObject("Line"); 
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            var dir = b - a;
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta        = new Vector2(dir.magnitude, lineThickness);
            rt.localRotation    = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var image = go.AddComponent<Image>();
            image.color = C_LINE;
            image.raycastTarget = false;
        }

        void BindNodeInteractions(NodeData n, GameObject nodeGo)
        {
            if (n == null || nodeGo == null) return;
            var trig = nodeGo.GetComponent<EventTrigger>() ?? nodeGo.AddComponent<EventTrigger>();
            // Scene-authored EventTriggers have empty serialized callbacks. Rebind them at runtime.
            trig.triggers.Clear();
            Vector2 pointerDownPos = Vector2.zero;

            AddTrigger(trig, EventTriggerType.PointerDown, data =>
                pointerDownPos = ((PointerEventData)data).position);
            AddTrigger(trig, EventTriggerType.PointerEnter, _ =>
            {
                SelectNode(n.id);
                PulseRing(n, true);
            });
            AddTrigger(trig, EventTriggerType.PointerExit, _ => PulseRing(n, false));
            AddTrigger(trig, EventTriggerType.PointerClick, data =>
            {
                var pointer = (PointerEventData)data;
                if (Vector2.Distance(pointer.position, pointerDownPos) < 10f)
                {
                    SelectNode(n.id);
                    TryBuy(n.id);
                }
            });
        }

        void UpdateNodeVisibility()
        {
            var nextVisible = new HashSet<string>();
            var distance = new Dictionary<string, int>();
            var queue = new Queue<NodeData>();

            // Revele a raiz e os dois próximos passos até o jogador comprar sua primeira habilidade.
            bool hasPurchased = false;
            foreach (var node in treeNodes)
            {
                if (GameManager.Instance.GetSkillLevel(node.id) > 0)
                {
                    hasPurchased = true;
                    nextVisible.Add(node.id);
                    if (!distance.ContainsKey(node.id))
                    {
                        distance[node.id] = 0;
                        queue.Enqueue(node);
                    }
                }
            }

            if (!hasPurchased)
            {
                foreach (var node in treeNodes)
                {
                    if (string.IsNullOrEmpty(node.requirementId))
                    {
                        nextVisible.Add(node.id);
                        distance[node.id] = 0;
                        queue.Enqueue(node);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentDistance = distance[current.id];
                if (currentDistance >= 2) continue;

                foreach (var child in treeNodes)
                {
                    if (child.requirementId != current.id) continue;
                    int childDistance = currentDistance + 1;
                    if (distance.TryGetValue(child.id, out int knownDistance) && knownDistance <= childDistance) continue;
                    distance[child.id] = childDistance;
                    nextVisible.Add(child.id);
                    queue.Enqueue(child);
                }
            }

            bool changed = !visibleNodeIds.SetEquals(nextVisible);
            foreach (var node in treeNodes)
            {
                bool visible = nextVisible.Contains(node.id);
                if (node.nodeGo != null && node.nodeGo.activeSelf != visible)
                {
                    node.nodeGo.SetActive(visible);
                    changed = true;
                }
            }

            visibleNodeIds.Clear();
            visibleNodeIds.UnionWith(nextVisible);
            if (changed) RedrawLines();
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

        GameObject Rect(string name, Transform parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(name); 
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot; rt.sizeDelta = size;
            return go;
        }

        Image Img(GameObject go, Color col)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = col; 
            return img;
        }

        TextMeshProUGUI Txt(string name, Transform parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size,
            string text, float fs, Color col, FontStyles style)
        {
            var go  = Rect(name, parent, ancMin, ancMax, pivot, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text; 
            tmp.fontSize  = fs;
            tmp.color     = col;  
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
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
        private void OnValidate()
        {
            if (curStaffImg != null)
            {
                curStaffImg.sprite = GetBasicStaffSprite();
                curStaffImg.color = Color.white;
            }
            if (nxtStaffImg != null)
            {
                nxtStaffImg.sprite = GetEvolvedStaffSprite();
                nxtStaffImg.color = Color.white;
            }
        }

        [ContextMenu("★ Gerar UI no Canvas (Preview no Editor) ★")]
        public void EditorBuildUI()
        {
            if (HasExistingUI())
            {
                BindExistingUI();
                EditorApplyInspectorToScene();
            }
            else
            {
                Build();
                RefreshUI();
            }
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=green>[EvolutionUI] Interface sincronizada no Canvas sem perder modificações!</color>");
        }

        [ContextMenu("★ Aplicar Posições e Tamanhos do Inspector na Cena ★")]
        public void EditorApplyInspectorToScene()
        {
            if (treeContentRt == null && HasExistingUI())
            {
                BindExistingUI();
            }

            if (treeContentRt == null)
            {
                Debug.LogWarning("[EvolutionUI] A hierarquia ainda não existe no Canvas. Use 'Forçar Reconstrução' primeiro.");
                return;
            }

            // Atualiza tamanho do container
            treeContentRt.sizeDelta = treeAreaSize;

            foreach (var n in treeNodes)
            {
                var nTr = treeContentRt.Find($"Node_{n.id}");
                if (nTr != null)
                {
                    var rt = (RectTransform)nTr;
                    rt.anchoredPosition = n.canvasPos;

                    Vector2 nodeSize = (n.customNodeSize != Vector2.zero) ? n.customNodeSize : defaultNodeSize;
                    rt.sizeDelta = nodeSize;

                    var iconTr = nTr.Find("Icon");
                    if (iconTr != null)
                    {
                        var iconRt = (RectTransform)iconTr;
                        Vector2 iconSize = (n.customIconSize != Vector2.zero) ? n.customIconSize : defaultIconSize;
                        iconRt.sizeDelta = iconSize;
                        var img = iconTr.GetComponent<Image>();
                        if (img != null) img.sprite = GetNodeSprite(n);
                    }

                    var lvTr = nTr.Find("Lv");
                    if (lvTr != null)
                    {
                        ((RectTransform)lvTr).anchoredPosition = new Vector2(0, - (nodeSize.y * 0.5f + 12f));
                    }
                }
            }

            RedrawLines();
            RefreshUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=green>[EvolutionUI] Posições, tamanhos e linhas atualizados com sucesso no Canvas!</color>");
        }

        [ContextMenu("★ Salvar Posições e Tamanhos da Scene View para o Inspector ★")]
        public void EditorSaveSceneToInspector()
        {
            if (treeContentRt == null && HasExistingUI())
            {
                BindExistingUI();
            }

            if (treeContentRt == null)
            {
                Debug.LogWarning("[EvolutionUI] A hierarquia da árvore não foi encontrada na cena.");
                return;
            }

            int count = 0;
            foreach (var n in treeNodes)
            {
                var nTr = treeContentRt.Find($"Node_{n.id}");
                if (nTr != null)
                {
                    var rt = (RectTransform)nTr;
                    n.canvasPos = rt.anchoredPosition;

                    if (rt.sizeDelta != defaultNodeSize)
                    {
                        n.customNodeSize = rt.sizeDelta;
                    }

                    var iconTr = nTr.Find("Icon");
                    if (iconTr != null)
                    {
                        var iconRt = (RectTransform)iconTr;
                        if (iconRt.sizeDelta != defaultIconSize)
                        {
                            n.customIconSize = iconRt.sizeDelta;
                        }
                    }
                    count++;
                }
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=cyan>[EvolutionUI] {count} nós sincronizados da Scene View para o Inspector com sucesso!</color>");
        }

        [ContextMenu("★ Aplicar Fundo do Livro no Painel ★")]
        public void EditorApplyBookBackground()
        {
            var frameBorder = transform.Find("FrameBorder");
            if (frameBorder == null)
            {
                Build();
                RefreshUI();
                UnityEditor.EditorUtility.SetDirty(gameObject);
                Debug.Log("<color=green>[EvolutionUI] Painel construído com fundo do Livro!</color>");
                return;
            }

            EnsureBookVisualsApplied(frameBorder, frameBorder.Find("Frame"));
            BindExistingUI();
            RefreshUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=green>[EvolutionUI] Fundo do Livro aplicado no Canvas com sucesso!</color>");
        }

        [ContextMenu("★ Forçar Reconstrução Completa (Recriar Tudo do Zero) ★")]
        public void EditorForceRebuildUI()
        {
            Build();
            RefreshUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=yellow>[EvolutionUI] Interface reconstruída do zero com sucesso!</color>");
        }

        [ContextMenu("★ Limpar Preview do Canvas ★")]
        public void EditorClearUI()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("[EvolutionUI] Preview limpo do Canvas.");
        }

        [ContextMenu("★ Restaurar Nós e Posições Padrão ★")]
        public void EditorResetDefaultNodes()
        {
            treeNodes.Clear();
            EnsureDefaultNodes();
            Build();
            RefreshUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=yellow>[EvolutionUI] Nós restaurados para o padrão de 20 nós.</color>");
        }
#endif
    }

    /// <summary>
    /// Permite arrastar (pan) e dar zoom interativo na área da Árvore de Habilidades com o mouse.
    /// Funciona com clique e arraste (M1 ou M3) e roda do mouse para zoom.
    /// </summary>
    public class TreeDraggablePan : MonoBehaviour
    {
        [Header("Alvo do Movimento")]
        [Tooltip("Container (RectTransform) onde estão os nós e linhas da árvore.")]
        public RectTransform contentToMove;

        [Header("Sensibilidade e Direção")]
        [Tooltip("Sensibilidade do arrasto do mouse.")]
        public float dragSensitivity = 1.0f;

        [Tooltip("Se ativado: segurar o mouse e arrastar para baixo move a árvore para cima (e vice-versa).")]
        public bool invertDrag = true;

        [Header("Zoom com a Roda do Mouse")]
        public bool enableZoom = true;
        public float zoomSensitivity = 0.08f;
        public float minZoom = 0.25f;
        public float maxZoom = 1.8f;

        [Header("Limites de Movimento (Clamp)")]
        public bool clampMovement = true;
        public Vector2 minBounds = new Vector2(-1200, -1200);
        public Vector2 maxBounds = new Vector2(1200, 1200);

        private Vector2 defaultPosition = Vector2.zero;
        private Vector3 defaultScale = Vector3.one;
        private bool isDragging = false;
        private Vector2 lastMousePosition;

        public bool IsDragging => isDragging;

        private void OnEnable()
        {
            isDragging = false;
            ConfigureCanvasCamera();
            TryBindContent();
        }

        private void Update()
        {
            if (contentToMove == null && !TryBindContent()) return;

            // Pan directly from mouse state instead of relying on UI drag
            // events, which are unreliable on this World Space canvas.
            // The project has Both input backends enabled; use Unity's legacy
            // mouse polling here so this remains independent of the UI module.
            Vector2 mousePosition = Input.mousePosition;
            bool pointerInsideTree = IsPointerInsideTree(mousePosition);

            // Start the pan only when M1 is pressed inside the tree viewport.
            // Once started, keep dragging even if the cursor leaves the viewport.
            if (!isDragging && pointerInsideTree && Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = mousePosition;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                Vector2 delta = (mousePosition - lastMousePosition) * dragSensitivity;
                if (invertDrag) delta = -delta;
                MoveContent(delta);
                lastMousePosition = mousePosition;
            }
            else if (!Input.GetMouseButton(0)) isDragging = false;

            if (enableZoom && pointerInsideTree)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f) ApplyZoom(scroll);
            }
        }

        private bool IsPointerInsideTree(Vector2 screenPosition)
        {
            var viewport = transform as RectTransform;
            if (viewport == null) return false;

            var canvas = ConfigureCanvasCamera();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);

            return RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPosition, eventCamera);
        }

        private Canvas ConfigureCanvasCamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
            return canvas;
        }

        private bool TryBindContent()
        {
            if (contentToMove == null)
            {
                var content = transform.Find("TreeContent") as RectTransform;
                if (content != null) SetContent(content);
            }
            return contentToMove != null;
        }

        private void Awake()
        {
            ConfigureCanvasCamera();
            if (contentToMove != null)
            {
                defaultPosition = contentToMove.anchoredPosition;
                defaultScale = contentToMove.localScale;
            }
        }

        public void SetContent(RectTransform target)
        {
            contentToMove = target;
            if (contentToMove != null)
            {
                defaultPosition = contentToMove.anchoredPosition;
                defaultScale = contentToMove.localScale;
            }
        }

        private void MoveContent(Vector2 delta)
        {
            Vector2 newPos = contentToMove.anchoredPosition + delta;

            if (clampMovement)
            {
                newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
                newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
            }

            contentToMove.anchoredPosition = newPos;
        }

        private void ApplyZoom(float scrollDelta)
        {
            // Normalize o delta para manter cada passo do zoom previsível.
            float scroll = Mathf.Clamp(scrollDelta, -1f, 1f);
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float currentScale = contentToMove.localScale.x;
                float targetScale = Mathf.Clamp(currentScale + scroll * zoomSensitivity, minZoom, maxZoom);
                contentToMove.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        public void ResetView()
        {
            if (contentToMove != null)
            {
                contentToMove.anchoredPosition = defaultPosition;
                contentToMove.localScale = defaultScale;
            }
        }
    }
}
