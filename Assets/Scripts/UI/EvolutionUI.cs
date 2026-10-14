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

            [HideInInspector] public Button nodeButton;
            [HideInInspector] public Image nodeImage;
            [HideInInspector] public Image nodeBorder;
            [HideInInspector] public TextMeshProUGUI nodeLevelText;
            [HideInInspector] public GameObject selectionRing;
        }

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI skillPointsText;
        [SerializeField] private Button closeButton;

        [Header("Detail Card Elements")]
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private TextMeshProUGUI detailEffectText;
        [SerializeField] private TextMeshProUGUI detailTotalText;
        [SerializeField] private TextMeshProUGUI detailStatusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;
        [SerializeField] private Transform pipsContainer;

        [Header("Staff Evolution (Opcional)")]
        [SerializeField] private GameObject staffEvolutionBox;
        [SerializeField] private TextMeshProUGUI staffStatusText;
        [SerializeField] private TextMeshProUGUI staffRequirementsText;
        [SerializeField] private Button evolveStaffButton;

        [Header("Nodes")]
        [SerializeField] private List<NodeConfig> nodes = new List<NodeConfig>();

        // Paleta oficial inspirada no esboço HTML
        private readonly Color ColorLocked = new Color(0.14f, 0.11f, 0.20f, 1f);     // #241c34
        private readonly Color ColorOpen = new Color(0.37f, 0.90f, 1.0f, 1f);        // #5ee6ff Ciano
        private readonly Color ColorBought = new Color(1.0f, 0.36f, 0.56f, 1f);      // #ff5d8f Rosa
        private readonly Color ColorMaxed = new Color(1.0f, 0.78f, 0.23f, 1f);       // #ffc83a Ouro

        private string selectedNodeId = "vit";

        private void Awake()
        {
            AutoFindReferences();
            EnsureDefaultNodes();
            EnsureStaffEvolutionBox();
        }

        private void Start()
        {
            AutoFindReferences();
            EnsureDefaultNodes();
            EnsureStaffEvolutionBox();
            HookNodeButtons();
            RefreshUI();
        }

        private void OnEnable()
        {
            AutoFindReferences();
            EnsureDefaultNodes();
            EnsureStaffEvolutionBox();
            HookNodeButtons();
            RefreshUI();
        }

        private void AutoFindReferences()
        {
            if (closeButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    if (b.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0) { closeButton = b; break; }
                }
            }
            if (buyButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    if (b.name.IndexOf("Buy", StringComparison.OrdinalIgnoreCase) >= 0) { buyButton = b; break; }
                }
            }

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = tmp.name.ToLowerInvariant();
                string parentName = tmp.transform.parent != null ? tmp.transform.parent.name.ToLowerInvariant() : "";

                if (detailNameText == null && (n.Contains("name") || n.Contains("detailname"))) detailNameText = tmp;
                else if (detailCostText == null && (n.Contains("cost") || n.Contains("detailcost"))) detailCostText = tmp;
                else if (detailDescText == null && (n.Contains("desc") || n.Contains("detaildesc"))) detailDescText = tmp;
                else if (detailEffectText == null && (n.Contains("effect") || n.Contains("detaileffect"))) detailEffectText = tmp;
                else if (detailTotalText == null && (n.Contains("total") || n.Contains("detailtotal"))) detailTotalText = tmp;
                else if (detailStatusText == null && (n.Contains("status") || n.Contains("detailstatus"))) detailStatusText = tmp;
                else if (skillPointsText == null && (n.Contains("sp") || parentName.Contains("sp") || n.Contains("skillpoint"))) skillPointsText = tmp;
            }

            if (pipsContainer == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Pips", StringComparison.OrdinalIgnoreCase) || t.name.Equals("PipsContainer", StringComparison.OrdinalIgnoreCase))
                    {
                        pipsContainer = t;
                        break;
                    }
                }
            }

            if (buyButtonText == null && buyButton != null)
            {
                buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
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
        }

        private void EnsureStaffEvolutionBox()
        {
            if (staffEvolutionBox != null) return;

            Transform existing = transform.Find("Frame/Body/StaffEvolutionBox");
            if (existing == null) existing = transform.Find("Frame/Body/DetailCard/StaffEvolutionBox");
            if (existing == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "StaffEvolutionBox" || t.name == "StaffBox") { existing = t; break; }
                }
            }

            if (existing != null)
            {
                staffEvolutionBox = existing.gameObject;
                staffStatusText = existing.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                staffRequirementsText = existing.Find("ReqText")?.GetComponent<TextMeshProUGUI>();
                evolveStaffButton = existing.GetComponentInChildren<Button>(true);
                if (evolveStaffButton != null)
                {
                    evolveStaffButton.onClick.RemoveAllListeners();
                    evolveStaffButton.onClick.AddListener(OnEvolveStaffClicked);
                }
                return;
            }

            // Cria dinamicamente o painel de evolução do cajado dentro do DetailCard
            Transform parentCard = transform.Find("Frame/Body/DetailCard");
            if (parentCard == null) parentCard = transform.Find("Frame");
            if (parentCard == null) return;

            GameObject box = new GameObject("StaffEvolutionBox");
            box.transform.SetParent(parentCard, false);
            var rect = box.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 12f);
            rect.sizeDelta = new Vector2(340f, 130f);

            var img = box.AddComponent<Image>();
            img.color = new Color(0.16f, 0.10f, 0.22f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(box.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -6f);
            tRect.sizeDelta = new Vector2(-20f, 22f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "CAJADO ARCANO (EVOLUÇÃO)";
            tTmp.fontSize = 14;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(1f, 0.75f, 0.2f, 1f);

            // Status
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(box.transform, false);
            var stRect = statusObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 1f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.anchoredPosition = new Vector2(0f, -28f);
            stRect.sizeDelta = new Vector2(-20f, 20f);
            staffStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            staffStatusText.text = "Cajado Básico (Apenas Gelo)";
            staffStatusText.fontSize = 12;
            staffStatusText.alignment = TextAlignmentOptions.Center;
            staffStatusText.color = new Color(0.4f, 0.85f, 1f, 1f);

            // Requirements
            GameObject reqObj = new GameObject("ReqText");
            reqObj.transform.SetParent(box.transform, false);
            var rRect = reqObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0.5f, 1f);
            rRect.anchoredPosition = new Vector2(0f, -48f);
            rRect.sizeDelta = new Vector2(-20f, 20f);
            staffRequirementsText = reqObj.AddComponent<TextMeshProUGUI>();
            staffRequirementsText.text = "Requer: 0/3 Corações de Fogo | 0/1 Núcleo de Boss";
            staffRequirementsText.fontSize = 12;
            staffRequirementsText.alignment = TextAlignmentOptions.Center;
            staffRequirementsText.color = new Color(1f, 0.85f, 0.7f, 1f);

            // Button
            GameObject btnObj = new GameObject("Btn_EvolveStaff");
            btnObj.transform.SetParent(box.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0f);
            bRect.anchoredPosition = new Vector2(0f, 8f);
            bRect.sizeDelta = new Vector2(300f, 36f);
            var bImg = btnObj.AddComponent<Image>();
            bImg.color = new Color(0.95f, 0.45f, 0.15f, 1f);
            evolveStaffButton = btnObj.AddComponent<Button>();

            GameObject bTxt = new GameObject("Text");
            bTxt.transform.SetParent(btnObj.transform, false);
            var btRect = bTxt.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var bTmp = bTxt.AddComponent<TextMeshProUGUI>();
            bTmp.text = "EVOLUIR CAJADO (LIBERAR FOGO)";
            bTmp.fontSize = 13;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.color = Color.white;

            evolveStaffButton.onClick.RemoveAllListeners();
            evolveStaffButton.onClick.AddListener(OnEvolveStaffClicked);

            staffEvolutionBox = box;
        }

        private void EnsureDefaultNodes()
        {
            if (nodes != null && nodes.Count > 0) return;

            nodes = new List<NodeConfig>
            {
                new NodeConfig { id = "vit", name = "Vitalidade", description = "Aumenta a energia vital do mago, concedendo corações extras.", requirementId = null, maxLevel = 5, baseCost = 1, effectText = "+1 Vida Máxima (Coração Extra)", unitText = " de vida", valuePerLevel = 1f },
                new NodeConfig { id = "vel", name = "Velocidade Arcana", description = "A vassoura corta o ar mais rápido e os projéteis voam velozes.", requirementId = "vit", maxLevel = 3, baseCost = 1, effectText = "+8% Movimento e +25% Velocidade dos Tiros", unitText = "% de velocidade", valuePerLevel = 25f },
                new NodeConfig { id = "foc", name = "Poder do Gelo", description = "O cristal canaliza o gelo com perfeição: mais dano e lentidão prolongada.", requirementId = "vel", maxLevel = 3, baseCost = 2, effectText = "+35% Dano e Lentidão Glacial", unitText = "% poder gelo", valuePerLevel = 35f },
                new NodeConfig { id = "des", name = "Despertar Arcano", description = "Equilibra corpo e magia: concede +1 Coração e +0.5 de Dano base.", requirementId = "foc", maxLevel = 1, baseCost = 2, effectText = "+1 Coração e +0.5 Dano Base", unitText = "", valuePerLevel = 0f },
                new NodeConfig { id = "pel", name = "Pele de Pedra", description = "Encantamento de resistência: reduz todo dano recebido em 8%.", requirementId = "des", maxLevel = 3, baseCost = 2, effectText = "8% Menos Dano Recebido", unitText = "% menos dano", valuePerLevel = 8f },
                new NodeConfig { id = "ima", name = "Ímã de Almas", description = "Os drops de skill points e corações voam até você de longe.", requirementId = "pel", maxLevel = 2, baseCost = 2, effectText = "+35% Alcance Magnético de Drops", unitText = "% alcance", valuePerLevel = 35f },
                new NodeConfig { id = "seg", name = "Segundo Fôlego", description = "Ao zerar os corações, a magia do mago revive-o com 35% de vida.", requirementId = "ima", maxLevel = 1, baseCost = 5, effectText = "Revive 1x por partida", unitText = "", valuePerLevel = 0f },
                new NodeConfig { id = "rap", name = "Cadência Rápida", description = "O cajado dispara magias em rápida sucessão.", requirementId = "des", maxLevel = 4, baseCost = 2, effectText = "+10% de Cadência de Tiro", unitText = "% de cadência", valuePerLevel = 10f },
                new NodeConfig { id = "dup", name = "Disparo Múltiplo", description = "Aumenta o número de magias disparadas juntas (Lv1: Duplo, Lv2: Triplo!).", requirementId = "rap", maxLevel = 2, baseCost = 3, effectText = "+1 Projétil por disparo (Lv1: Duplo, Lv2: Triplo)", unitText = " projéteis", valuePerLevel = 1f },
                new NodeConfig { id = "per", name = "Fúria do Fogo", description = "Magias flamejantes perfuram os inimigos e causam destruição em massa.", requirementId = "dup", maxLevel = 2, baseCost = 4, effectText = "Tiros Perfuram Inimigos e +45% Dano de Fogo", unitText = "% poder fogo", valuePerLevel = 45f }
            };
        }

        public void RegisterNode(NodeConfig cfg)
        {
            nodes.Add(cfg);
        }

        public void HookNodeButtons()
        {
            foreach (var node in nodes)
            {
                if (node.nodeButton == null)
                {
                    Transform t = transform.Find("Frame/Body/TreeContainer/Node_" + node.id);
                    if (t == null)
                    {
                        foreach (var btn in GetComponentsInChildren<Button>(true))
                        {
                            if (btn.name == "Node_" + node.id)
                            {
                                t = btn.transform;
                                break;
                            }
                        }
                    }

                    if (t != null)
                    {
                        node.nodeButton = t.GetComponent<Button>();
                        node.nodeBorder = t.GetComponent<Image>();
                        var ring = t.Find("Ring");
                        if (ring != null) node.selectionRing = ring.gameObject;
                        var badge = t.Find("LevelBadge");
                        if (badge != null) node.nodeLevelText = badge.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }

                if (node.nodeButton != null)
                {
                    string id = node.id;
                    node.nodeButton.onClick.RemoveAllListeners();
                    node.nodeButton.onClick.AddListener(() =>
                    {
                        SelectNode(id);
                    });
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

            // 1. Atualiza Skill Points no topo
            if (skillPointsText != null)
            {
                skillPointsText.text = $"SKILL POINTS: {currentSP}";
            }

            // 2. Atualiza estado visual de cada nó
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

            // 3. Atualiza Detalhes do Nó Selecionado
            UpdateDetailCard();

            // 4. Atualiza Caixa do Cajado
            UpdateStaffBox();
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

            // Pips (indicadores de nível)
            UpdatePips(node, lvl);

            // Mensagem de Status e Botão
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

        private void UpdateStaffBox()
        {
            if (staffEvolutionBox == null) return;

            if (GameManager.Instance.IsStaffEvolved)
            {
                if (staffStatusText != null) staffStatusText.text = "CAJADO FLAMEJANTE (EVOLUÍDO!)";
                if (staffRequirementsText != null) staffRequirementsText.text = "Poder Mágico Máximo (+60% Dano & Velocidade)!";
                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = false;
                    var txt = evolveStaffButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "JÁ EVOLUÍDO";
                }
            }
            else
            {
                int hearts = GameManager.Instance.FireHeartsCount;
                int reqH = GameManager.Instance.FireHeartsRequired;
                int cores = GameManager.Instance.BossCoresCount;
                int reqC = GameManager.Instance.BossCoresRequired;

                if (staffStatusText != null) staffStatusText.text = "CAJADO DE MADEIRA (NÍVEL 1)";
                if (staffRequirementsText != null) staffRequirementsText.text = $"Requer: {hearts}/{reqH} Corações de Fogo | {cores}/{reqC} Núcleo";

                bool canEvolve = GameManager.Instance.CanEvolveStaff();
                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = canEvolve;
                    var txt = evolveStaffButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = canEvolve ? "EVOLUIR CAJADO" : "MATERIAIS INSUFICIENTES";
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
