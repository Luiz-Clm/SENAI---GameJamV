using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class EvolutionUI : MonoBehaviour
    {
        [System.Serializable]
        public class NodeConfig
        {
            public string id;
            public string name;
            public string description;
            public string requirementId;
            public int maxLevel;
            public int baseCost;
            public string effectText;
            public string unitText;
            public float valuePerLevel;
            public Vector2 htmlPos; // Coordenadas originais do HTML (560x500)
            public string iconText;

            [HideInInspector] public Button nodeButton;
            [HideInInspector] public Image nodeBorder;
            [HideInInspector] public Image nodeFill;
            [HideInInspector] public TextMeshProUGUI nodeIconText;
            [HideInInspector] public TextMeshProUGUI nodeLevelText;
            [HideInInspector] public GameObject selectionRing;
        }

        [Header("Tabs")]
        [SerializeField] private GameObject tabTreeContent;
        [SerializeField] private GameObject tabStaffContent;
        [SerializeField] private Button tabTreeButton;
        [SerializeField] private Button tabStaffButton;
        [SerializeField] private Image tabTreeButtonBg;
        [SerializeField] private Image tabStaffButtonBg;

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI skillPointsText;
        [SerializeField] private Button closeButton;

        [Header("Detail Card Elements (Tab 1)")]
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private TextMeshProUGUI detailEffectText;
        [SerializeField] private TextMeshProUGUI detailTotalText;
        [SerializeField] private TextMeshProUGUI detailStatusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;
        [SerializeField] private Transform pipsContainer;

        [Header("Staff Evolution Elements (Tab 2)")]
        [SerializeField] private TextMeshProUGUI currentStaffNameText;
        [SerializeField] private TextMeshProUGUI currentStaffStatText;
        [SerializeField] private Image currentStaffImage;
        [SerializeField] private TextMeshProUGUI nextStaffNameText;
        [SerializeField] private TextMeshProUGUI nextStaffStatText;
        [SerializeField] private Image nextStaffImage;
        [SerializeField] private GameObject nextStaffBox;
        [SerializeField] private TextMeshProUGUI staffNeedSummaryText;
        [SerializeField] private TextMeshProUGUI fireHeartCountText;
        [SerializeField] private Transform fireHeartSegmentsContainer;
        [SerializeField] private TextMeshProUGUI bossCoreCountText;
        [SerializeField] private Transform bossCoreSegmentsContainer;
        [SerializeField] private Button evolveStaffButton;
        [SerializeField] private TextMeshProUGUI evolveStaffButtonText;
        [SerializeField] private TextMeshProUGUI staffMessageText;

        [Header("Nodes")]
        [SerializeField] private List<NodeConfig> nodes = new List<NodeConfig>();

        // Cores oficiais da paleta pixel art do HTML
        private readonly Color ColorBgDark   = new Color(0.05f, 0.04f, 0.09f, 0.96f); // #0d0916
        private readonly Color ColorPanel    = new Color(0.11f, 0.08f, 0.19f, 1f);    // #1d1530
        private readonly Color ColorLine     = new Color(0.29f, 0.21f, 0.47f, 1f);    // #4b3677
        private readonly Color ColorLocked   = new Color(0.14f, 0.11f, 0.20f, 1f);    // #241c34
        private readonly Color ColorOpen     = new Color(0.37f, 0.90f, 1.0f, 1f);     // #5ee6ff Ciano
        private readonly Color ColorBought   = new Color(1.0f, 0.36f, 0.56f, 1f);     // #ff5d8f Rosa
        private readonly Color ColorMaxed    = new Color(1.0f, 0.78f, 0.23f, 1f);     // #ffc83a Dourado

        private string selectedNodeId = "vit";
        private bool isStaffTabActive = false;

        private void Awake()
        {
            EnsureDefaultNodes();
            BuildFullHDHierarchyIfNeeded();
            HookListeners();
        }

        private void Start()
        {
            EnsureDefaultNodes();
            BuildFullHDHierarchyIfNeeded();
            HookListeners();
            RefreshUI();
        }

        private void OnEnable()
        {
            EnsureDefaultNodes();
            HookListeners();
            RefreshUI();
        }

        public void SwitchTab(bool toStaff)
        {
            isStaffTabActive = toStaff;
            if (tabTreeContent != null) tabTreeContent.SetActive(!toStaff);
            if (tabStaffContent != null) tabStaffContent.SetActive(toStaff);

            if (tabTreeButtonBg != null)
                tabTreeButtonBg.color = !toStaff ? ColorBought : new Color(0.16f, 0.12f, 0.27f, 1f);

            if (tabStaffButtonBg != null)
                tabStaffButtonBg.color = toStaff ? ColorMaxed : new Color(0.16f, 0.12f, 0.27f, 1f);

            RefreshUI();
        }

        private void EnsureDefaultNodes()
        {
            if (nodes != null && nodes.Count == 10) return;

            // 10 nós com posições originais exatas do HTML (arvore_de_upgrades.html viewBox: 560 x 500)
            nodes = new List<NodeConfig>
            {
                new NodeConfig { id = "vit", name = "Vitalidade", description = "Seu corpo de mago aguenta mais pancada na masmorra.", requirementId = null, maxLevel = 5, baseCost = 1, effectText = "+1 Vida Máxima (Coração Extra)", unitText = " de vida", valuePerLevel = 1f, htmlPos = new Vector2(280f, 440f), iconText = "♥" },
                new NodeConfig { id = "vel", name = "Vassoura Veloz", description = "A vassoura corta o ar mais rápido e fica fácil desviar dos tiros.", requirementId = "vit", maxLevel = 3, baseCost = 1, effectText = "+8% Movimento e +25% Vel. dos Tiros", unitText = "% velocidade", valuePerLevel = 25f, htmlPos = new Vector2(280f, 370f), iconText = "⚡" },
                new NodeConfig { id = "foc", name = "Foco Arcano", description = "O cristal do cajado brilha mais forte e seus tiros de gelo machucam mais.", requirementId = "vel", maxLevel = 3, baseCost = 2, effectText = "+35% Dano Glacial e Lentidão", unitText = "% poder gelo", valuePerLevel = 35f, htmlPos = new Vector2(280f, 300f), iconText = "⚔" },
                new NodeConfig { id = "des", name = "Despertar Arcano", description = "Libera os dois caminhos: proteção e ofensiva.", requirementId = "foc", maxLevel = 1, baseCost = 2, effectText = "+1 Coração e +0.5 Dano Base", unitText = "", valuePerLevel = 0f, htmlPos = new Vector2(280f, 230f), iconText = "★" },

                // Ramo Esquerdo (Defesa)
                new NodeConfig { id = "pel", name = "Pele de Pedra", description = "Você aprendeu um truque com o golem: pancadas doem menos.", requirementId = "des", maxLevel = 3, baseCost = 2, effectText = "8% Menos Dano Recebido", unitText = "% menos dano", valuePerLevel = 8f, htmlPos = new Vector2(218f, 172f), iconText = "🛡" },
                new NodeConfig { id = "ima", name = "Ímã de Almas", description = "Os skill points e corações dropados voam até você de mais longe.", requirementId = "pel", maxLevel = 2, baseCost = 2, effectText = "+35% Alcance Magnético de Drops", unitText = "% alcance", valuePerLevel = 35f, htmlPos = new Vector2(158f, 118f), iconText = "🧲" },
                new NodeConfig { id = "seg", name = "Segundo Fôlego", description = "Quando a vida zera, a magia do mago revive-o com 35% de vida.", requirementId = "ima", maxLevel = 1, baseCost = 5, effectText = "Revive 1x por partida", unitText = "", valuePerLevel = 0f, htmlPos = new Vector2(84f, 96f), iconText = "☠" },

                // Ramo Direito (Ofensiva)
                new NodeConfig { id = "rap", name = "Tiro Rápido", description = "O cajado dispara magias em rápida sucessão.", requirementId = "des", maxLevel = 4, baseCost = 2, effectText = "+10% de Cadência de Disparo", unitText = "% cadência", valuePerLevel = 10f, htmlPos = new Vector2(342f, 172f), iconText = "🏹" },
                new NodeConfig { id = "dup", name = "Tiro Duplo", description = "Cada disparo solta uma magia a mais (Lv1: Duplo, Lv2: Triplo).", requirementId = "rap", maxLevel = 2, baseCost = 3, effectText = "+1 Projétil por disparo (Lv1: Duplo, Lv2: Triplo)", unitText = " projéteis", valuePerLevel = 1f, htmlPos = new Vector2(402f, 118f), iconText = "✦" },
                new NodeConfig { id = "per", name = "Tiro Perfurante", description = "Magias flamejantes atravessam os inimigos causando devastação em massa.", requirementId = "dup", maxLevel = 2, baseCost = 4, effectText = "Tiros Perfuram Inimigos e +45% Dano de Fogo", unitText = "% poder fogo", valuePerLevel = 45f, htmlPos = new Vector2(476f, 96f), iconText = "🔥" }
            };
        }

        private void BuildFullHDHierarchyIfNeeded()
        {
            // Se a hierarquia já foi construída e possui as referências, não precisa recriar
            if (tabTreeContent != null && tabStaffContent != null && buyButton != null && evolveStaffButton != null)
            {
                return;
            }

            // Limpa filhos antigos para reconstruir limpo
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            var bgImg = GetComponent<Image>();
            if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
            bgImg.color = ColorBgDark;

            // Frame Central (1640 x 920 Full HD)
            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(transform, false);
            var fRect = frame.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0.5f, 0.5f);
            fRect.anchorMax = new Vector2(0.5f, 0.5f);
            fRect.sizeDelta = new Vector2(1640f, 920f);
            var fImg = frame.AddComponent<Image>();
            fImg.color = ColorPanel;

            // Top Header: Tabs + Skill Points + Close
            GameObject header = new GameObject("Header");
            header.transform.SetParent(frame.transform, false);
            var hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -16f);
            hRect.sizeDelta = new Vector2(-48f, 68f);

            // Tab 1 Button: ÁRVORE DE UPGRADES
            GameObject btnTabTree = new GameObject("Btn_Tab_Tree");
            btnTabTree.transform.SetParent(header.transform, false);
            var bttRect = btnTabTree.AddComponent<RectTransform>();
            bttRect.anchorMin = new Vector2(0f, 0.5f);
            bttRect.anchorMax = new Vector2(0f, 0.5f);
            bttRect.pivot = new Vector2(0f, 0.5f);
            bttRect.anchoredPosition = new Vector2(0f, 0f);
            bttRect.sizeDelta = new Vector2(300f, 54f);
            tabTreeButtonBg = btnTabTree.AddComponent<Image>();
            tabTreeButtonBg.color = ColorBought;
            tabTreeButton = btnTabTree.AddComponent<Button>();

            GameObject bttTxt = new GameObject("Text");
            bttTxt.transform.SetParent(btnTabTree.transform, false);
            var btttRect = bttTxt.AddComponent<RectTransform>();
            btttRect.anchorMin = Vector2.zero;
            btttRect.anchorMax = Vector2.one;
            btttRect.sizeDelta = Vector2.zero;
            var btttTmp = bttTxt.AddComponent<TextMeshProUGUI>();
            btttTmp.text = "ÁRVORE DE UPGRADES";
            btttTmp.fontSize = 20;
            btttTmp.fontStyle = FontStyles.Bold;
            btttTmp.alignment = TextAlignmentOptions.Center;
            btttTmp.color = Color.white;

            // Tab 2 Button: EVOLUIR CAJADO
            GameObject btnTabStaff = new GameObject("Btn_Tab_Staff");
            btnTabStaff.transform.SetParent(header.transform, false);
            var btsRect = btnTabStaff.AddComponent<RectTransform>();
            btsRect.anchorMin = new Vector2(0f, 0.5f);
            btsRect.anchorMax = new Vector2(0f, 0.5f);
            btsRect.pivot = new Vector2(0f, 0.5f);
            btsRect.anchoredPosition = new Vector2(320f, 0f);
            btsRect.sizeDelta = new Vector2(280f, 54f);
            tabStaffButtonBg = btnTabStaff.AddComponent<Image>();
            tabStaffButtonBg.color = new Color(0.16f, 0.12f, 0.27f, 1f);
            tabStaffButton = btnTabStaff.AddComponent<Button>();

            GameObject btsTxt = new GameObject("Text");
            btsTxt.transform.SetParent(btnTabStaff.transform, false);
            var btstRect = btsTxt.AddComponent<RectTransform>();
            btstRect.anchorMin = Vector2.zero;
            btstRect.anchorMax = Vector2.one;
            btstRect.sizeDelta = Vector2.zero;
            var btstTmp = btsTxt.AddComponent<TextMeshProUGUI>();
            btstTmp.text = "EVOLUIR CAJADO";
            btstTmp.fontSize = 20;
            btstTmp.fontStyle = FontStyles.Bold;
            btstTmp.alignment = TextAlignmentOptions.Center;
            btstTmp.color = Color.white;

            // Skill Points Box (Top Right)
            GameObject spBox = new GameObject("SP_Box");
            spBox.transform.SetParent(header.transform, false);
            var spbRect = spBox.AddComponent<RectTransform>();
            spbRect.anchorMin = new Vector2(1f, 0.5f);
            spbRect.anchorMax = new Vector2(1f, 0.5f);
            spbRect.pivot = new Vector2(1f, 0.5f);
            spbRect.anchoredPosition = new Vector2(-70f, 0f);
            spbRect.sizeDelta = new Vector2(250f, 54f);
            var spbImg = spBox.AddComponent<Image>();
            spbImg.color = new Color(0.04f, 0.03f, 0.06f, 1f);

            GameObject spTxt = new GameObject("Text");
            spTxt.transform.SetParent(spBox.transform, false);
            var sptRect = spTxt.AddComponent<RectTransform>();
            sptRect.anchorMin = Vector2.zero;
            sptRect.anchorMax = Vector2.one;
            sptRect.sizeDelta = Vector2.zero;
            skillPointsText = spTxt.AddComponent<TextMeshProUGUI>();
            skillPointsText.text = "SKILL POINTS: 0";
            skillPointsText.fontSize = 22;
            skillPointsText.fontStyle = FontStyles.Bold;
            skillPointsText.alignment = TextAlignmentOptions.Center;
            skillPointsText.color = ColorMaxed;

            // Close Button [X]
            GameObject closeObj = new GameObject("Btn_Close");
            closeObj.transform.SetParent(header.transform, false);
            var cRect = closeObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0.5f);
            cRect.anchorMax = new Vector2(1f, 0.5f);
            cRect.pivot = new Vector2(1f, 0.5f);
            cRect.anchoredPosition = new Vector2(0f, 0f);
            cRect.sizeDelta = new Vector2(54f, 54f);
            var cImg = closeObj.AddComponent<Image>();
            cImg.color = ColorBought;
            closeButton = closeObj.AddComponent<Button>();

            GameObject closeTxt = new GameObject("Text");
            closeTxt.transform.SetParent(closeObj.transform, false);
            var ctRect = closeTxt.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTmp = closeTxt.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "X";
            ctTmp.fontSize = 28;
            ctTmp.fontStyle = FontStyles.Bold;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = Color.white;

            // --- TAB 1 CONTENT: ÁRVORE DE UPGRADES ---
            tabTreeContent = new GameObject("Tab_TreeContent");
            tabTreeContent.transform.SetParent(frame.transform, false);
            var ttcRect = tabTreeContent.AddComponent<RectTransform>();
            ttcRect.anchorMin = new Vector2(0f, 0f);
            ttcRect.anchorMax = new Vector2(1f, 1f);
            ttcRect.anchoredPosition = new Vector2(0f, -44f);
            ttcRect.sizeDelta = new Vector2(-48f, -110f);

            BuildTreeTab(tabTreeContent.transform);

            // --- TAB 2 CONTENT: EVOLUIR CAJADO ---
            tabStaffContent = new GameObject("Tab_StaffContent");
            tabStaffContent.transform.SetParent(frame.transform, false);
            var tscRect = tabStaffContent.AddComponent<RectTransform>();
            tscRect.anchorMin = new Vector2(0f, 0f);
            tscRect.anchorMax = new Vector2(1f, 1f);
            tscRect.anchoredPosition = new Vector2(0f, -44f);
            tscRect.sizeDelta = new Vector2(-48f, -110f);

            BuildStaffTab(tabStaffContent.transform);

            tabStaffContent.SetActive(false);
        }

        private void BuildTreeTab(Transform parent)
        {
            // Container da Árvore (Esquerda, 960 x 740)
            GameObject treeBox = new GameObject("TreeContainer");
            treeBox.transform.SetParent(parent, false);
            var tbRect = treeBox.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 0.5f);
            tbRect.anchorMax = new Vector2(0f, 0.5f);
            tbRect.pivot = new Vector2(0f, 0.5f);
            tbRect.anchoredPosition = new Vector2(0f, 0f);
            tbRect.sizeDelta = new Vector2(980f, 750f);
            var tbImg = treeBox.AddComponent<Image>();
            tbImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            // Container de Linhas
            GameObject linesBox = new GameObject("Lines");
            linesBox.transform.SetParent(treeBox.transform, false);
            var lbRect = linesBox.AddComponent<RectTransform>();
            lbRect.anchorMin = Vector2.zero;
            lbRect.anchorMax = Vector2.one;
            lbRect.sizeDelta = Vector2.zero;

            // Converte coordenadas do HTML (560x500) para o container (980x750)
            Dictionary<string, Vector2> nodeCanvasPositions = new Dictionary<string, Vector2>();
            foreach (var n in nodes)
            {
                float posX = (n.htmlPos.x / 560f - 0.5f) * 860f;
                float posY = (0.5f - n.htmlPos.y / 500f) * 650f;
                nodeCanvasPositions[n.id] = new Vector2(posX, posY);
            }

            // Desenha linhas de requisitos
            foreach (var n in nodes)
            {
                if (!string.IsNullOrEmpty(n.requirementId) && nodeCanvasPositions.ContainsKey(n.requirementId))
                {
                    Vector2 start = nodeCanvasPositions[n.requirementId];
                    Vector2 end = nodeCanvasPositions[n.id];
                    CreateLine(linesBox.transform, start, end);
                }
            }

            // Cria os nós
            foreach (var n in nodes)
            {
                Vector2 pos = nodeCanvasPositions[n.id];

                GameObject nodeObj = new GameObject($"Node_{n.id}");
                nodeObj.transform.SetParent(treeBox.transform, false);
                var nRect = nodeObj.AddComponent<RectTransform>();
                nRect.anchoredPosition = pos;
                nRect.sizeDelta = new Vector2(72f, 72f);

                // Anel de seleção
                GameObject ringObj = new GameObject("Ring");
                ringObj.transform.SetParent(nodeObj.transform, false);
                var rRect = ringObj.AddComponent<RectTransform>();
                rRect.anchorMin = Vector2.zero;
                rRect.anchorMax = Vector2.one;
                rRect.sizeDelta = new Vector2(16f, 16f);
                var rImg = ringObj.AddComponent<Image>();
                rImg.color = ColorOpen;
                n.selectionRing = ringObj;
                ringObj.SetActive(false);

                // Borda do Nó
                n.nodeBorder = nodeObj.AddComponent<Image>();
                n.nodeBorder.color = ColorLocked;

                // Botão
                n.nodeButton = nodeObj.AddComponent<Button>();

                // Ícone do Nó
                GameObject icObj = new GameObject("Icon");
                icObj.transform.SetParent(nodeObj.transform, false);
                var icRect = icObj.AddComponent<RectTransform>();
                icRect.anchorMin = Vector2.zero;
                icRect.anchorMax = Vector2.one;
                icRect.sizeDelta = Vector2.zero;
                n.nodeIconText = icObj.AddComponent<TextMeshProUGUI>();
                n.nodeIconText.text = n.iconText;
                n.nodeIconText.fontSize = 28;
                n.nodeIconText.fontStyle = FontStyles.Bold;
                n.nodeIconText.alignment = TextAlignmentOptions.Center;
                n.nodeIconText.color = Color.white;

                // Nível do Nó (badge abaixo)
                GameObject lvlObj = new GameObject("LevelText");
                lvlObj.transform.SetParent(nodeObj.transform, false);
                var lRect = lvlObj.AddComponent<RectTransform>();
                lRect.anchoredPosition = new Vector2(0f, -48f);
                lRect.sizeDelta = new Vector2(90f, 26f);
                n.nodeLevelText = lvlObj.AddComponent<TextMeshProUGUI>();
                n.nodeLevelText.text = $"0/{n.maxLevel}";
                n.nodeLevelText.fontSize = 18;
                n.nodeLevelText.fontStyle = FontStyles.Bold;
                n.nodeLevelText.alignment = TextAlignmentOptions.Center;
                n.nodeLevelText.color = new Color(0.7f, 0.65f, 0.85f, 1f);
            }

            // Card de Detalhes (Direita, 580 x 750) — Estilo do HTML .card (#e9d6f7)
            GameObject card = new GameObject("DetailCard");
            card.transform.SetParent(parent, false);
            var cRect = card.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0.5f);
            cRect.anchorMax = new Vector2(1f, 0.5f);
            cRect.pivot = new Vector2(1f, 0.5f);
            cRect.anchoredPosition = new Vector2(0f, 0f);
            cRect.sizeDelta = new Vector2(580f, 750f);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.91f, 0.84f, 0.97f, 1f); // #e9d6f7 Lilás claro

            // Top Row: Nome + Custo SP
            GameObject topRow = new GameObject("TopRow");
            topRow.transform.SetParent(card.transform, false);
            var trRect = topRow.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(0.5f, 1f);
            trRect.anchoredPosition = new Vector2(0f, -24f);
            trRect.sizeDelta = new Vector2(-40f, 54f);

            GameObject nameObj = new GameObject("DetailName");
            nameObj.transform.SetParent(topRow.transform, false);
            var noRect = nameObj.AddComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0f, 0.5f);
            noRect.anchorMax = new Vector2(0.68f, 0.5f);
            noRect.pivot = new Vector2(0f, 0.5f);
            detailNameText = nameObj.AddComponent<TextMeshProUGUI>();
            detailNameText.text = "Vitalidade";
            detailNameText.fontSize = 28;
            detailNameText.fontStyle = FontStyles.Bold;
            detailNameText.color = new Color(0.16f, 0.09f, 0.25f, 1f);

            GameObject costBox = new GameObject("DetailCost");
            costBox.transform.SetParent(topRow.transform, false);
            var cbRect = costBox.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.7f, 0.5f);
            cbRect.anchorMax = new Vector2(1f, 0.5f);
            cbRect.pivot = new Vector2(1f, 0.5f);
            cbRect.sizeDelta = new Vector2(160f, 44f);
            var cbImg = costBox.AddComponent<Image>();
            cbImg.color = new Color(0.04f, 0.03f, 0.06f, 1f);

            detailCostText = costBox.AddComponent<TextMeshProUGUI>();
            detailCostText.text = "CUSTO: 1 SP";
            detailCostText.fontSize = 18;
            detailCostText.fontStyle = FontStyles.Bold;
            detailCostText.alignment = TextAlignmentOptions.Center;
            detailCostText.color = ColorMaxed;

            // Label Descrição
            GameObject lblDesc = new GameObject("LblDesc");
            lblDesc.transform.SetParent(card.transform, false);
            var ldRect = lblDesc.AddComponent<RectTransform>();
            ldRect.anchoredPosition = new Vector2(0f, 260f);
            ldRect.sizeDelta = new Vector2(500f, 24f);
            var ldTmp = lblDesc.AddComponent<TextMeshProUGUI>();
            ldTmp.text = "Descrição:";
            ldTmp.fontSize = 20;
            ldTmp.color = new Color(0.42f, 0.29f, 0.59f, 1f);

            // Texto Descrição
            GameObject descObj = new GameObject("DetailDesc");
            descObj.transform.SetParent(card.transform, false);
            var dRect = descObj.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(0f, 205f);
            dRect.sizeDelta = new Vector2(500f, 80f);
            detailDescText = descObj.AddComponent<TextMeshProUGUI>();
            detailDescText.text = "Seu corpo de mago aguenta mais pancada na masmorra.";
            detailDescText.fontSize = 22;
            detailDescText.color = new Color(0.16f, 0.09f, 0.25f, 1f);

            // Art Frame (#2a1f45)
            GameObject artFrame = new GameObject("ArtFrame");
            artFrame.transform.SetParent(card.transform, false);
            var afRect = artFrame.AddComponent<RectTransform>();
            afRect.anchoredPosition = new Vector2(0f, 45f);
            afRect.sizeDelta = new Vector2(500f, 180f);
            var afImg = artFrame.AddComponent<Image>();
            afImg.color = new Color(0.16f, 0.12f, 0.27f, 1f);

            // Ícone grande dentro da moldura
            GameObject artIcon = new GameObject("ArtIcon");
            artIcon.transform.SetParent(artFrame.transform, false);
            var aicRect = artIcon.AddComponent<RectTransform>();
            aicRect.anchoredPosition = new Vector2(0f, 25f);
            aicRect.sizeDelta = new Vector2(100f, 70f);
            var aicTmp = artIcon.AddComponent<TextMeshProUGUI>();
            aicTmp.text = "✦";
            aicTmp.fontSize = 54;
            aicTmp.alignment = TextAlignmentOptions.Center;
            aicTmp.color = ColorBought;

            // Pips Container (indicadores de nível)
            GameObject pipsObj = new GameObject("Pips");
            pipsObj.transform.SetParent(artFrame.transform, false);
            var pRect = pipsObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0f, -48f);
            pRect.sizeDelta = new Vector2(300f, 30f);
            var pHlg = pipsObj.AddComponent<HorizontalLayoutGroup>();
            pHlg.spacing = 10f;
            pHlg.childAlignment = TextAnchor.MiddleCenter;
            pHlg.childForceExpandWidth = false;
            pHlg.childForceExpandHeight = false;
            pipsContainer = pipsObj.transform;

            for (int p = 0; p < 5; p++)
            {
                GameObject pip = new GameObject($"Pip_{p}");
                pip.transform.SetParent(pipsContainer, false);
                var pipRect = pip.AddComponent<RectTransform>();
                pipRect.sizeDelta = new Vector2(22f, 22f);
                var pipImg = pip.AddComponent<Image>();
                pipImg.color = ColorLocked;
            }

            // Efeito
            GameObject effObj = new GameObject("DetailEffect");
            effObj.transform.SetParent(card.transform, false);
            var efRect = effObj.AddComponent<RectTransform>();
            efRect.anchoredPosition = new Vector2(0f, -80f);
            efRect.sizeDelta = new Vector2(500f, 40f);
            detailEffectText = effObj.AddComponent<TextMeshProUGUI>();
            detailEffectText.text = "+1 Vida Máxima (Coração Extra)";
            detailEffectText.fontSize = 26;
            detailEffectText.fontStyle = FontStyles.Bold;
            detailEffectText.alignment = TextAlignmentOptions.Center;
            detailEffectText.color = new Color(0.54f, 0.12f, 0.33f, 1f);

            // Total Atual
            GameObject totObj = new GameObject("DetailTotal");
            totObj.transform.SetParent(card.transform, false);
            var totRect = totObj.AddComponent<RectTransform>();
            totRect.anchoredPosition = new Vector2(0f, -125f);
            totRect.sizeDelta = new Vector2(500f, 32f);
            detailTotalText = totObj.AddComponent<TextMeshProUGUI>();
            detailTotalText.text = "Total Atual: +0 de vida";
            detailTotalText.fontSize = 20;
            detailTotalText.alignment = TextAlignmentOptions.Center;
            detailTotalText.color = new Color(0.42f, 0.29f, 0.59f, 1f);

            // Botão Desbloquear
            GameObject buyBtnObj = new GameObject("Btn_Buy");
            buyBtnObj.transform.SetParent(card.transform, false);
            var bbRect = buyBtnObj.AddComponent<RectTransform>();
            bbRect.anchoredPosition = new Vector2(0f, -205f);
            bbRect.sizeDelta = new Vector2(500f, 68f);
            var bbImg = buyBtnObj.AddComponent<Image>();
            bbImg.color = ColorBought;
            buyButton = buyBtnObj.AddComponent<Button>();

            GameObject bbtTxt = new GameObject("Text");
            bbtTxt.transform.SetParent(buyBtnObj.transform, false);
            var bbtRect = bbtTxt.AddComponent<RectTransform>();
            bbtRect.anchorMin = Vector2.zero;
            bbtRect.anchorMax = Vector2.one;
            bbtRect.sizeDelta = Vector2.zero;
            buyButtonText = bbtTxt.AddComponent<TextMeshProUGUI>();
            buyButtonText.text = "DESBLOQUEAR";
            buyButtonText.fontSize = 24;
            buyButtonText.fontStyle = FontStyles.Bold;
            buyButtonText.alignment = TextAlignmentOptions.Center;
            buyButtonText.color = Color.white;

            // Mensagem de Status
            GameObject statusObj = new GameObject("DetailStatus");
            statusObj.transform.SetParent(card.transform, false);
            var stRect = statusObj.AddComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(0f, -275f);
            stRect.sizeDelta = new Vector2(500f, 34f);
            detailStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            detailStatusText.text = "Pronto para desbloquear";
            detailStatusText.fontSize = 20;
            detailStatusText.alignment = TextAlignmentOptions.Center;
            detailStatusText.color = new Color(0.29f, 0.17f, 0.35f, 1f);
        }

        private void BuildStaffTab(Transform parent)
        {
            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 320f);
            tRect.sizeDelta = new Vector2(1000f, 50f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "EVOLUÇÃO DO CAJADO MÁGICO";
            tTmp.fontSize = 36;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(1f, 0.61f, 0.73f, 1f);

            // Subtítulo
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(parent, false);
            var sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 275f);
            sRect.sizeDelta = new Vector2(1000f, 36f);
            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "Troque drops dos inimigos por uma versão flamejante do seu cajado.";
            sTmp.fontSize = 22;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = new Color(0.65f, 0.58f, 0.8f, 1f);

            // 3 Caixas: [Atual] ---> [Próximo]
            // Caixa Atual (Esquerda)
            GameObject boxCur = new GameObject("Box_Current");
            boxCur.transform.SetParent(parent, false);
            var bcRect = boxCur.AddComponent<RectTransform>();
            bcRect.anchoredPosition = new Vector2(-360f, 100f);
            bcRect.sizeDelta = new Vector2(380f, 260f);
            var bcImg = boxCur.AddComponent<Image>();
            bcImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            GameObject curTag = new GameObject("Tag");
            curTag.transform.SetParent(boxCur.transform, false);
            var ctRect = curTag.AddComponent<RectTransform>();
            ctRect.anchoredPosition = new Vector2(0f, 100f);
            ctRect.sizeDelta = new Vector2(300f, 24f);
            var ctTmp = curTag.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "ATUAL";
            ctTmp.fontSize = 16;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = new Color(0.65f, 0.58f, 0.8f, 1f);

            GameObject curImgObj = new GameObject("StaffImage");
            curImgObj.transform.SetParent(boxCur.transform, false);
            var ciRect = curImgObj.AddComponent<RectTransform>();
            ciRect.anchoredPosition = new Vector2(0f, 15f);
            ciRect.sizeDelta = new Vector2(90f, 120f);
            currentStaffImage = curImgObj.AddComponent<Image>();
            currentStaffImage.sprite = Resources.Load<Sprite>("Sprites/Staff_Basic");
            currentStaffImage.preserveAspect = true;

            GameObject curName = new GameObject("Name");
            curName.transform.SetParent(boxCur.transform, false);
            var cnRect = curName.AddComponent<RectTransform>();
            cnRect.anchoredPosition = new Vector2(0f, -65f);
            cnRect.sizeDelta = new Vector2(340f, 30f);
            currentStaffNameText = curName.AddComponent<TextMeshProUGUI>();
            currentStaffNameText.text = "Cajado Básico";
            currentStaffNameText.fontSize = 22;
            currentStaffNameText.fontStyle = FontStyles.Bold;
            currentStaffNameText.alignment = TextAlignmentOptions.Center;
            currentStaffNameText.color = Color.white;

            GameObject curStat = new GameObject("Stat");
            curStat.transform.SetParent(boxCur.transform, false);
            var csRect = curStat.AddComponent<RectTransform>();
            csRect.anchoredPosition = new Vector2(0f, -95f);
            csRect.sizeDelta = new Vector2(340f, 26f);
            currentStaffStatText = curStat.AddComponent<TextMeshProUGUI>();
            currentStaffStatText.text = "Magia de Gelo (Dano 1)";
            currentStaffStatText.fontSize = 18;
            currentStaffStatText.alignment = TextAlignmentOptions.Center;
            currentStaffStatText.color = ColorOpen;

            // Seta Central
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(parent, false);
            var aRect = arrowObj.AddComponent<RectTransform>();
            aRect.anchoredPosition = new Vector2(0f, 100f);
            aRect.sizeDelta = new Vector2(120f, 60f);
            var aTmp = arrowObj.AddComponent<TextMeshProUGUI>();
            aTmp.text = "➔";
            aTmp.fontSize = 64;
            aTmp.fontStyle = FontStyles.Bold;
            aTmp.alignment = TextAlignmentOptions.Center;
            aTmp.color = ColorBought;

            // Caixa Próximo (Direita)
            nextStaffBox = new GameObject("Box_Next");
            nextStaffBox.transform.SetParent(parent, false);
            var bnRect = nextStaffBox.AddComponent<RectTransform>();
            bnRect.anchoredPosition = new Vector2(360f, 100f);
            bnRect.sizeDelta = new Vector2(380f, 260f);
            var bnImg = nextStaffBox.AddComponent<Image>();
            bnImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            GameObject nextTag = new GameObject("Tag");
            nextTag.transform.SetParent(nextStaffBox.transform, false);
            var ntRect = nextTag.AddComponent<RectTransform>();
            ntRect.anchoredPosition = new Vector2(0f, 100f);
            ntRect.sizeDelta = new Vector2(300f, 24f);
            var ntTmp = nextTag.AddComponent<TextMeshProUGUI>();
            ntTmp.text = "PRÓXIMO NÍVEL";
            ntTmp.fontSize = 16;
            ntTmp.alignment = TextAlignmentOptions.Center;
            ntTmp.color = ColorMaxed;

            GameObject nextImgObj = new GameObject("StaffImage");
            nextImgObj.transform.SetParent(nextStaffBox.transform, false);
            var niRect = nextImgObj.AddComponent<RectTransform>();
            niRect.anchoredPosition = new Vector2(0f, 15f);
            niRect.sizeDelta = new Vector2(90f, 120f);
            nextStaffImage = nextImgObj.AddComponent<Image>();
            nextStaffImage.sprite = Resources.Load<Sprite>("Sprites/Staff_Evolved");
            nextStaffImage.preserveAspect = true;

            GameObject nextName = new GameObject("Name");
            nextName.transform.SetParent(nextStaffBox.transform, false);
            var nnRect = nextName.AddComponent<RectTransform>();
            nnRect.anchoredPosition = new Vector2(0f, -65f);
            nnRect.sizeDelta = new Vector2(340f, 30f);
            nextStaffNameText = nextName.AddComponent<TextMeshProUGUI>();
            nextStaffNameText.text = "Cajado Flamejante";
            nextStaffNameText.fontSize = 22;
            nextStaffNameText.fontStyle = FontStyles.Bold;
            nextStaffNameText.alignment = TextAlignmentOptions.Center;
            nextStaffNameText.color = Color.white;

            GameObject nextStat = new GameObject("Stat");
            nextStat.transform.SetParent(nextStaffBox.transform, false);
            var nsRect = nextStat.AddComponent<RectTransform>();
            nsRect.anchoredPosition = new Vector2(0f, -95f);
            nsRect.sizeDelta = new Vector2(340f, 26f);
            nextStaffStatText = nextStat.AddComponent<TextMeshProUGUI>();
            nextStaffStatText.text = "Libera FOGO (+60% Dano & Cadência!)";
            nextStaffStatText.fontSize = 18;
            nextStaffStatText.alignment = TextAlignmentOptions.Center;
            nextStaffStatText.color = ColorMaxed;

            // Linha de Requisitos de Materiais
            GameObject reqSummary = new GameObject("ReqSummary");
            reqSummary.transform.SetParent(parent, false);
            var rsRect = reqSummary.AddComponent<RectTransform>();
            rsRect.anchoredPosition = new Vector2(0f, -60f);
            rsRect.sizeDelta = new Vector2(800f, 36f);
            staffNeedSummaryText = reqSummary.AddComponent<TextMeshProUGUI>();
            staffNeedSummaryText.text = "Requer 3 Corações de Fogo e 1 Núcleo do Boss";
            staffNeedSummaryText.fontSize = 24;
            staffNeedSummaryText.fontStyle = FontStyles.Bold;
            staffNeedSummaryText.alignment = TextAlignmentOptions.Center;
            staffNeedSummaryText.color = ColorMaxed;

            // Box Coração de Fogo
            GameObject hBox = new GameObject("Mat_FireHeart");
            hBox.transform.SetParent(parent, false);
            var hbRect = hBox.AddComponent<RectTransform>();
            hbRect.anchoredPosition = new Vector2(-220f, -130f);
            hbRect.sizeDelta = new Vector2(400f, 64f);
            var hbImg = hBox.AddComponent<Image>();
            hbImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            GameObject hIcon = new GameObject("Icon");
            hIcon.transform.SetParent(hBox.transform, false);
            var hiRect = hIcon.AddComponent<RectTransform>();
            hiRect.anchorMin = new Vector2(0f, 0.5f);
            hiRect.anchorMax = new Vector2(0f, 0.5f);
            hiRect.pivot = new Vector2(0f, 0.5f);
            hiRect.anchoredPosition = new Vector2(12f, 0f);
            hiRect.sizeDelta = new Vector2(44f, 44f);
            var hiImg = hIcon.AddComponent<Image>();
            hiImg.sprite = Resources.Load<Sprite>("Sprites/FireHeart");
            hiImg.preserveAspect = true;

            GameObject hTitle = new GameObject("Title");
            hTitle.transform.SetParent(hBox.transform, false);
            var htRect = hTitle.AddComponent<RectTransform>();
            htRect.anchoredPosition = new Vector2(0f, 12f);
            htRect.sizeDelta = new Vector2(180f, 24f);
            var htTmp = hTitle.AddComponent<TextMeshProUGUI>();
            htTmp.text = "Coração de Fogo";
            htTmp.fontSize = 18;
            htTmp.alignment = TextAlignmentOptions.MidlineLeft;
            htTmp.color = Color.white;

            GameObject hSegs = new GameObject("Segments");
            hSegs.transform.SetParent(hBox.transform, false);
            var hsgRect = hSegs.AddComponent<RectTransform>();
            hsgRect.anchoredPosition = new Vector2(0f, -14f);
            hsgRect.sizeDelta = new Vector2(180f, 14f);
            var hhlg = hSegs.AddComponent<HorizontalLayoutGroup>();
            hhlg.spacing = 6f;
            hhlg.childForceExpandWidth = true;
            hhlg.childForceExpandHeight = true;
            fireHeartSegmentsContainer = hSegs.transform;

            for (int i = 0; i < 3; i++)
            {
                GameObject seg = new GameObject($"Seg_{i}");
                seg.transform.SetParent(fireHeartSegmentsContainer, false);
                var segImg = seg.AddComponent<Image>();
                segImg.color = ColorLocked;
            }

            GameObject hCount = new GameObject("Count");
            hCount.transform.SetParent(hBox.transform, false);
            var hcRect = hCount.AddComponent<RectTransform>();
            hcRect.anchorMin = new Vector2(1f, 0.5f);
            hcRect.anchorMax = new Vector2(1f, 0.5f);
            hcRect.pivot = new Vector2(1f, 0.5f);
            hcRect.anchoredPosition = new Vector2(-16f, 0f);
            hcRect.sizeDelta = new Vector2(90f, 32f);
            fireHeartCountText = hCount.AddComponent<TextMeshProUGUI>();
            fireHeartCountText.text = "0 / 3";
            fireHeartCountText.fontSize = 20;
            fireHeartCountText.fontStyle = FontStyles.Bold;
            fireHeartCountText.alignment = TextAlignmentOptions.MidlineRight;
            fireHeartCountText.color = Color.white;

            // Box Núcleo do Boss
            GameObject cBox = new GameObject("Mat_BossCore");
            cBox.transform.SetParent(parent, false);
            var cbBoxRect = cBox.AddComponent<RectTransform>();
            cbBoxRect.anchoredPosition = new Vector2(220f, -130f);
            cbBoxRect.sizeDelta = new Vector2(400f, 64f);
            var cbBoxImg = cBox.AddComponent<Image>();
            cbBoxImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            GameObject cIcon = new GameObject("Icon");
            cIcon.transform.SetParent(cBox.transform, false);
            var ciIconRect = cIcon.AddComponent<RectTransform>();
            ciIconRect.anchorMin = new Vector2(0f, 0.5f);
            ciIconRect.anchorMax = new Vector2(0f, 0.5f);
            ciIconRect.pivot = new Vector2(0f, 0.5f);
            ciIconRect.anchoredPosition = new Vector2(12f, 0f);
            ciIconRect.sizeDelta = new Vector2(44f, 44f);
            var ciImg = cIcon.AddComponent<Image>();
            ciImg.sprite = Resources.Load<Sprite>("Sprites/BossCore");
            ciImg.preserveAspect = true;

            GameObject cTitle = new GameObject("Title");
            cTitle.transform.SetParent(cBox.transform, false);
            var ctTitleRect = cTitle.AddComponent<RectTransform>();
            ctTitleRect.anchoredPosition = new Vector2(0f, 12f);
            ctTitleRect.sizeDelta = new Vector2(180f, 24f);
            var cttTmp = cTitle.AddComponent<TextMeshProUGUI>();
            cttTmp.text = "Núcleo do Boss";
            cttTmp.fontSize = 18;
            cttTmp.alignment = TextAlignmentOptions.MidlineLeft;
            cttTmp.color = Color.white;

            GameObject cSegs = new GameObject("Segments");
            cSegs.transform.SetParent(cBox.transform, false);
            var csgRect = cSegs.AddComponent<RectTransform>();
            csgRect.anchoredPosition = new Vector2(0f, -14f);
            csgRect.sizeDelta = new Vector2(180f, 14f);
            var chlg = cSegs.AddComponent<HorizontalLayoutGroup>();
            chlg.spacing = 6f;
            chlg.childForceExpandWidth = true;
            chlg.childForceExpandHeight = true;
            bossCoreSegmentsContainer = cSegs.transform;

            GameObject cseg0 = new GameObject("Seg_0");
            cseg0.transform.SetParent(bossCoreSegmentsContainer, false);
            var csegImg = cseg0.AddComponent<Image>();
            csegImg.color = ColorLocked;

            GameObject cCount = new GameObject("Count");
            cCount.transform.SetParent(cBox.transform, false);
            var ccRect = cCount.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(1f, 0.5f);
            ccRect.anchorMax = new Vector2(1f, 0.5f);
            ccRect.pivot = new Vector2(1f, 0.5f);
            ccRect.anchoredPosition = new Vector2(-16f, 0f);
            ccRect.sizeDelta = new Vector2(90f, 32f);
            bossCoreCountText = cCount.AddComponent<TextMeshProUGUI>();
            bossCoreCountText.text = "0 / 1";
            bossCoreCountText.fontSize = 20;
            bossCoreCountText.fontStyle = FontStyles.Bold;
            bossCoreCountText.alignment = TextAlignmentOptions.MidlineRight;
            bossCoreCountText.color = Color.white;

            // Botão Evoluir
            GameObject btnEvolve = new GameObject("Btn_Evolve");
            btnEvolve.transform.SetParent(parent, false);
            var beRect = btnEvolve.AddComponent<RectTransform>();
            beRect.anchoredPosition = new Vector2(0f, -220f);
            beRect.sizeDelta = new Vector2(440f, 68f);
            var beImg = btnEvolve.AddComponent<Image>();
            beImg.color = ColorBought;
            evolveStaffButton = btnEvolve.AddComponent<Button>();

            GameObject beTxt = new GameObject("Text");
            beTxt.transform.SetParent(btnEvolve.transform, false);
            var betRect = beTxt.AddComponent<RectTransform>();
            betRect.anchorMin = Vector2.zero;
            betRect.anchorMax = Vector2.one;
            betRect.sizeDelta = Vector2.zero;
            evolveStaffButtonText = beTxt.AddComponent<TextMeshProUGUI>();
            evolveStaffButtonText.text = "EVOLUIR CAJADO";
            evolveStaffButtonText.fontSize = 24;
            evolveStaffButtonText.fontStyle = FontStyles.Bold;
            evolveStaffButtonText.alignment = TextAlignmentOptions.Center;
            evolveStaffButtonText.color = Color.white;

            // Mensagem de Status
            GameObject sMsg = new GameObject("StaffMessage");
            sMsg.transform.SetParent(parent, false);
            var smRect = sMsg.AddComponent<RectTransform>();
            smRect.anchoredPosition = new Vector2(0f, -285f);
            smRect.sizeDelta = new Vector2(800f, 32f);
            staffMessageText = sMsg.AddComponent<TextMeshProUGUI>();
            staffMessageText.text = "Derrote morcegos e o golem para coletar os materiais!";
            staffMessageText.fontSize = 20;
            staffMessageText.alignment = TextAlignmentOptions.Center;
            staffMessageText.color = new Color(0.65f, 0.58f, 0.8f, 1f);
        }

        private void CreateLine(Transform parent, Vector2 start, Vector2 end)
        {
            GameObject line = new GameObject("Line");
            line.transform.SetParent(parent, false);
            var rect = line.AddComponent<RectTransform>();

            Vector2 dir = end - start;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(distance, 6f);
            rect.localRotation = Quaternion.Euler(0, 0, angle);

            var img = line.AddComponent<Image>();
            img.color = ColorLine;
        }

        private void HookListeners()
        {
            if (tabTreeButton != null)
            {
                tabTreeButton.onClick.RemoveAllListeners();
                tabTreeButton.onClick.AddListener(() => SwitchTab(false));
            }

            if (tabStaffButton != null)
            {
                tabStaffButton.onClick.RemoveAllListeners();
                tabStaffButton.onClick.AddListener(() => SwitchTab(true));
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }

            if (evolveStaffButton != null)
            {
                evolveStaffButton.onClick.RemoveAllListeners();
                evolveStaffButton.onClick.AddListener(OnEvolveStaffClicked);
            }

            foreach (var node in nodes)
            {
                if (node.nodeButton != null)
                {
                    string id = node.id;
                    node.nodeButton.onClick.RemoveAllListeners();
                    node.nodeButton.onClick.AddListener(() => SelectNode(id));
                }
            }
        }

        public void SelectNode(string id)
        {
            selectedNodeId = id;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (GameManager.Instance == null) return;

            int currentSP = GameManager.Instance.SkillPoints;

            // 1. Atualiza Skill Points no Topo
            if (skillPointsText != null)
            {
                skillPointsText.text = $"SKILL POINTS: {currentSP}";
            }

            // 2. Atualiza os nós da árvore
            foreach (var n in nodes)
            {
                int lvl = GameManager.Instance.GetSkillLevel(n.id);
                bool reach = IsNodeReachable(n);
                int cost = GetCost(n, lvl);
                bool canBuy = reach && (lvl < n.maxLevel) && (currentSP >= cost);

                if (n.nodeLevelText != null)
                {
                    n.nodeLevelText.text = reach ? $"{lvl}/{n.maxLevel}" : "";
                }

                if (n.nodeBorder != null)
                {
                    if (lvl >= n.maxLevel) n.nodeBorder.color = ColorMaxed;
                    else if (lvl > 0) n.nodeBorder.color = ColorBought;
                    else if (canBuy) n.nodeBorder.color = ColorOpen;
                    else n.nodeBorder.color = ColorLocked;
                }

                if (n.selectionRing != null)
                {
                    n.selectionRing.SetActive(n.id == selectedNodeId);
                }
            }

            // 3. Atualiza o Card de Detalhes
            UpdateDetailCard();

            // 4. Atualiza a tela de Evolução de Cajado
            UpdateStaffTab();
        }

        private void UpdateDetailCard()
        {
            NodeConfig node = GetNodeConfig(selectedNodeId);
            if (node == null) return;

            int sp = GameManager.Instance.SkillPoints;
            int lvl = GameManager.Instance.GetSkillLevel(node.id);
            bool reach = IsNodeReachable(node);
            int cost = GetCost(node, lvl);
            bool canBuy = reach && (lvl < node.maxLevel) && (sp >= cost);

            if (detailNameText != null) detailNameText.text = node.name;
            if (detailCostText != null) detailCostText.text = (lvl >= node.maxLevel) ? "CUSTO: -" : $"CUSTO: {cost} SP";
            if (detailDescText != null) detailDescText.text = node.description;
            if (detailEffectText != null) detailEffectText.text = node.effectText;

            if (detailTotalText != null)
            {
                if (lvl > 0 && node.valuePerLevel > 0f)
                {
                    float totalVal = node.valuePerLevel * lvl;
                    detailTotalText.text = $"Total Atual: +{totalVal}{node.unitText}";
                }
                else
                {
                    detailTotalText.text = (lvl > 0) ? "Desbloqueado!" : "";
                }
            }

            // Pips de nível
            UpdatePips(node, lvl);

            // Estado do Botão
            string statusMsg = "";
            bool btnInteractable = false;
            string btnLabel = "DESBLOQUEAR";

            if (lvl >= node.maxLevel)
            {
                statusMsg = "Nível máximo alcançado!";
                btnLabel = "MÁXIMO";
            }
            else if (!reach)
            {
                NodeConfig req = GetNodeConfig(node.requirementId);
                statusMsg = $"Requer: {(req != null ? req.name : "Anterior")}";
                btnLabel = "BLOQUEADO";
            }
            else if (sp < cost)
            {
                statusMsg = $"Faltam {cost - sp} skill point(s)";
                btnLabel = "SP INSUFICIENTE";
            }
            else
            {
                statusMsg = "Pronto para desbloquear!";
                btnInteractable = true;
                btnLabel = "DESBLOQUEAR";
            }

            if (detailStatusText != null) detailStatusText.text = statusMsg;

            if (buyButton != null)
            {
                buyButton.interactable = btnInteractable;
                var btnImg = buyButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = btnInteractable ? ColorBought : new Color(0.45f, 0.35f, 0.55f, 1f);
                }
                if (buyButtonText != null) buyButtonText.text = btnLabel;
            }
        }

        private void UpdatePips(NodeConfig node, int currentLevel)
        {
            if (pipsContainer == null) return;

            int childCount = pipsContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = pipsContainer.GetChild(i).gameObject;
                if (i < node.maxLevel)
                {
                    child.SetActive(true);
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = (i < currentLevel) ? ColorBought : ColorLocked;
                    }
                }
                else
                {
                    child.SetActive(false);
                }
            }
        }

        private void UpdateStaffTab()
        {
            if (GameManager.Instance == null) return;

            bool isEvolved = GameManager.Instance.IsStaffEvolved;
            int hearts = GameManager.Instance.FireHeartsCount;
            int reqHearts = GameManager.Instance.FireHeartsRequired;
            int cores = GameManager.Instance.BossCoresCount;
            int reqCores = GameManager.Instance.BossCoresRequired;

            // Atualiza contadores e segmentos
            if (fireHeartCountText != null)
            {
                fireHeartCountText.text = $"{hearts} / {reqHearts}";
                fireHeartCountText.color = (hearts >= reqHearts) ? ColorMaxed : Color.white;
            }

            if (fireHeartSegmentsContainer != null)
            {
                for (int i = 0; i < fireHeartSegmentsContainer.childCount; i++)
                {
                    var segImg = fireHeartSegmentsContainer.GetChild(i).GetComponent<Image>();
                    if (segImg != null)
                    {
                        segImg.color = (i < hearts) ? (hearts >= reqHearts ? ColorMaxed : ColorBought) : ColorLocked;
                    }
                }
            }

            if (bossCoreCountText != null)
            {
                bossCoreCountText.text = $"{cores} / {reqCores}";
                bossCoreCountText.color = (cores >= reqCores) ? ColorMaxed : Color.white;
            }

            if (bossCoreSegmentsContainer != null)
            {
                for (int i = 0; i < bossCoreSegmentsContainer.childCount; i++)
                {
                    var segImg = bossCoreSegmentsContainer.GetChild(i).GetComponent<Image>();
                    if (segImg != null)
                    {
                        segImg.color = (i < cores) ? ColorMaxed : ColorLocked;
                    }
                }
            }

            if (isEvolved)
            {
                if (currentStaffNameText != null) currentStaffNameText.text = "Cajado Flamejante";
                if (currentStaffStatText != null) currentStaffStatText.text = "Poder Mágico Máximo (+60% Dano & Fogo)";
                if (currentStaffImage != null) currentStaffImage.sprite = Resources.Load<Sprite>("Sprites/Staff_Evolved");

                if (nextStaffBox != null) nextStaffBox.SetActive(false);
                if (staffNeedSummaryText != null) staffNeedSummaryText.text = "Cajado Totalmente Evoluído!";

                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = false;
                    var bImg = evolveStaffButton.GetComponent<Image>();
                    if (bImg != null) bImg.color = new Color(0.3f, 0.25f, 0.4f, 1f);
                    if (evolveStaffButtonText != null) evolveStaffButtonText.text = "MÁXIMO";
                }

                if (staffMessageText != null)
                {
                    staffMessageText.text = "Seu cajado atingiu a maestria suprema do fogo e gelo!";
                }
            }
            else
            {
                bool canEvolve = GameManager.Instance.CanEvolveStaff();

                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = canEvolve;
                    var bImg = evolveStaffButton.GetComponent<Image>();
                    if (bImg != null)
                    {
                        bImg.color = canEvolve ? ColorMaxed : new Color(0.35f, 0.28f, 0.48f, 1f);
                    }
                    if (evolveStaffButtonText != null)
                    {
                        evolveStaffButtonText.text = canEvolve ? "EVOLUIR CAJADO" : "MATERIAIS INSUFICIENTES";
                    }
                }

                if (staffMessageText != null)
                {
                    staffMessageText.text = canEvolve
                        ? "Tudo pronto! Pressione o botão para desbloquear a Magia de Fogo!"
                        : "Derrote morcegos de fogo e o Golem para coletar os materiais restantes.";
                }
            }
        }

        private void OnBuyClicked()
        {
            NodeConfig node = GetNodeConfig(selectedNodeId);
            if (node == null || GameManager.Instance == null) return;

            int lvl = GameManager.Instance.GetSkillLevel(node.id);
            int cost = GetCost(node, lvl);

            if (IsNodeReachable(node) && lvl < node.maxLevel && GameManager.Instance.SkillPoints >= cost)
            {
                bool success = GameManager.Instance.UpgradeSkill(node.id, cost);
                if (success)
                {
                    RefreshUI();
                }
            }
        }

        private void OnEvolveStaffClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.TryEvolveStaff())
            {
                RefreshUI();
            }
        }

        private void OnCloseClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CloseEvolutionPanel();
            }
            else
            {
                gameObject.SetActive(false);
                Time.timeScale = 1f;
            }
        }

        private bool IsNodeReachable(NodeConfig node)
        {
            if (string.IsNullOrEmpty(node.requirementId)) return true;
            if (GameManager.Instance == null) return false;
            return GameManager.Instance.GetSkillLevel(node.requirementId) > 0;
        }

        private int GetCost(NodeConfig node, int currentLevel)
        {
            return node.baseCost + currentLevel;
        }

        private NodeConfig GetNodeConfig(string id)
        {
            return nodes.Find(n => n.id == id);
        }
    }
}
