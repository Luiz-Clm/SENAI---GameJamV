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
            // Auto-associa botões caso referências estejam nulas
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

            if (detailNameText == null)
            {
                foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp.name == "Detail_Name") detailNameText = tmp;
                    else if (tmp.name == "Detail_Cost") detailCostText = tmp;
                    else if (tmp.name == "Detail_Desc") detailDescText = tmp;
                    else if (tmp.name == "Detail_Effect") detailEffectText = tmp;
                    else if (tmp.name == "Detail_Total") detailTotalText = tmp;
                    else if (tmp.name == "Detail_Status") detailStatusText = tmp;
                    else if (tmp.name == "SP_Counter") skillPointsText = tmp;
                }
            }

            if (pipsContainer == null)
            {
                var t = transform.Find("Frame/Body/DetailCard/PipsContainer");
                if (t != null) pipsContainer = t;
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

            if (evolveStaffButton != null)
            {
                evolveStaffButton.onClick.RemoveAllListeners();
                evolveStaffButton.onClick.AddListener(OnEvolveStaffClicked);
            }
        }

        private void Start()
        {
            EnsureDefaultNodes();
            HookNodeButtons();
            RefreshUI();
        }

        private void OnEnable()
        {
            EnsureDefaultNodes();
            HookNodeButtons();
            RefreshUI();
        }

        private void EnsureDefaultNodes()
        {
            if (nodes != null && nodes.Count > 0) return;

            nodes = new List<NodeConfig>
            {
                new NodeConfig { id = "vit", name = "Vitalidade", description = "Seu corpo de mago aguenta mais pancada na masmorra.", requirementId = null, maxLevel = 5, baseCost = 1, effectText = "+1 Vida Máxima", unitText = " de vida", valuePerLevel = 1f },
                new NodeConfig { id = "vel", name = "Vassoura Veloz", description = "A vassoura corta o ar mais rápido para desviar dos tiros.", requirementId = "vit", maxLevel = 3, baseCost = 1, effectText = "+8% de velocidade", unitText = "% de velocidade", valuePerLevel = 8f },
                new NodeConfig { id = "foc", name = "Foco Arcano", description = "O cristal do cajado brilha mais forte e causa mais dano.", requirementId = "vel", maxLevel = 3, baseCost = 2, effectText = "+0.5 de dano mágico", unitText = " de dano", valuePerLevel = 0.5f },
                new NodeConfig { id = "des", name = "Despertar Arcano", description = "Libera os dois caminhos: proteção e ofensiva.", requirementId = "foc", maxLevel = 1, baseCost = 2, effectText = "+1 Vida e +0.5 de dano", unitText = "", valuePerLevel = 0f },
                new NodeConfig { id = "pel", name = "Pele de Pedra", description = "Você aprendeu um truque com o golem: pancadas doem menos.", requirementId = "des", maxLevel = 3, baseCost = 2, effectText = "8% menos dano recebido", unitText = "% menos dano", valuePerLevel = 8f },
                new NodeConfig { id = "ima", name = "Ímã de Almas", description = "Os drops de skill points voam até você de mais longe.", requirementId = "pel", maxLevel = 2, baseCost = 2, effectText = "+35% alcance dos drops", unitText = "% alcance", valuePerLevel = 35f },
                new NodeConfig { id = "seg", name = "Segundo Fôlego", description = "Quando a vida zera, você revive uma vez com vida.", requirementId = "ima", maxLevel = 1, baseCost = 5, effectText = "Revive 1x com 35% de vida", unitText = "", valuePerLevel = 0f },
                new NodeConfig { id = "rap", name = "Tiro Rápido", description = "O cajado dispara magias com mais frequência.", requirementId = "des", maxLevel = 4, baseCost = 2, effectText = "+10% de cadência de tiro", unitText = "% de cadência", valuePerLevel = 10f },
                new NodeConfig { id = "dup", name = "Tiro Duplo", description = "Cada disparo solta 2 magias paralelas ao mesmo tempo.", requirementId = "rap", maxLevel = 1, baseCost = 4, effectText = "+1 Projétil por disparo (Tiro Duplo!)", unitText = "", valuePerLevel = 0f },
                new NodeConfig { id = "per", name = "Tiro Perfurante", description = "As magias atravessam o primeiro inimigo e acertam o próximo.", requirementId = "dup", maxLevel = 1, baseCost = 5, effectText = "Tiros atravessam 1 inimigo", unitText = "", valuePerLevel = 0f }
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
