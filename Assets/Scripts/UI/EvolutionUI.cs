using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    public class EvolutionUI : MonoBehaviour
    {
        [System.Serializable]
        public class SkillNodeData
        {
            public string skillId;
            public string skillName;
            public string description;
            public Button nodeButton;
            public Image nodeImage;
        }

        [Header("Staff Evolution UI")]
        [SerializeField] private TextMeshProUGUI staffRequirementsText;
        [SerializeField] private TextMeshProUGUI staffStatusText;
        [SerializeField] private Button evolveStaffButton;
        [SerializeField] private Image basicStaffImage;
        [SerializeField] private Image evolvedStaffImage;

        [Header("Skill Tree UI")]
        [SerializeField] private TextMeshProUGUI skillPointsHeader;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI skillCostText;
        [SerializeField] private TextMeshProUGUI skillDescText;
        [SerializeField] private Button learnSkillButton;
        [SerializeField] private SkillNodeData[] skillNodes;

        [Header("Window Controls")]
        [SerializeField] private Button closeButton;

        private SkillNodeData selectedNode;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (evolveStaffButton != null)
            {
                evolveStaffButton.onClick.AddListener(OnEvolveStaffClicked);
            }

            if (learnSkillButton != null)
            {
                learnSkillButton.onClick.AddListener(OnLearnSkillClicked);
            }

            // Configura os nós da árvore
            if (skillNodes != null)
            {
                for (int i = 0; i < skillNodes.Length; i++)
                {
                    int index = i;
                    if (skillNodes[index].nodeButton != null)
                    {
                        skillNodes[index].nodeButton.onClick.AddListener(() => SelectSkillNode(skillNodes[index]));
                    }
                }
            }

            // Seleciona o primeiro nó por padrão se disponível
            if (skillNodes != null && skillNodes.Length > 0)
            {
                SelectSkillNode(skillNodes[0]);
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (GameManager.Instance == null) return;

            // 1. Atualiza dados do Cajado
            if (GameManager.Instance.IsStaffEvolved)
            {
                if (staffStatusText != null) staffStatusText.text = "CAJADO MÁGICO FLAMEJANTE (EVOLUÍDO!)";
                if (staffRequirementsText != null) staffRequirementsText.text = "Poder Mágico Máximo Atingido (+60% Dano & Velocidade)!";
                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = false;
                    var btnText = evolveStaffButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "JÁ EVOLUÍDO";
                }
            }
            else
            {
                int hearts = GameManager.Instance.FireHeartsCount;
                int reqHearts = GameManager.Instance.FireHeartsRequired;
                int cores = GameManager.Instance.BossCoresCount;
                int reqCores = GameManager.Instance.BossCoresRequired;

                if (staffStatusText != null) staffStatusText.text = "CAJADO DE MADEIRA (NÍVEL 1)";
                if (staffRequirementsText != null)
                {
                    staffRequirementsText.text = $"Requer: {hearts}/{reqHearts} Corações de Fogo | {cores}/{reqCores} Núcleo do Boss";
                }

                bool canEvolve = GameManager.Instance.CanEvolveStaff();
                if (evolveStaffButton != null)
                {
                    evolveStaffButton.interactable = canEvolve;
                    var btnText = evolveStaffButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = canEvolve ? "EVOLUIR CAJADO!" : "MATERIAIS INSUFICIENTES";
                }
            }

            // 2. Atualiza Skill Points
            if (skillPointsHeader != null)
            {
                skillPointsHeader.text = $"Skill Points: {GameManager.Instance.SkillPoints}";
            }

            // 3. Atualiza cores dos nós
            if (skillNodes != null)
            {
                foreach (var node in skillNodes)
                {
                    if (node.nodeImage != null)
                    {
                        bool isUnlocked = IsSkillUnlocked(node.skillId);
                        node.nodeImage.color = isUnlocked ? new Color(0.3f, 1f, 0.4f, 1f) : new Color(1f, 0.55f, 0.65f, 1f);
                    }
                }
            }

            // 4. Atualiza detalhes do nó selecionado
            UpdateSelectedNodeDetails();
        }

        private void SelectSkillNode(SkillNodeData node)
        {
            selectedNode = node;
            UpdateSelectedNodeDetails();
        }

        private void UpdateSelectedNodeDetails()
        {
            if (selectedNode == null) return;

            if (skillNameText != null) skillNameText.text = selectedNode.skillName;
            if (skillCostText != null) skillCostText.text = "Custo: 1 Skill Point";
            if (skillDescText != null) skillDescText.text = selectedNode.description;

            bool isUnlocked = IsSkillUnlocked(selectedNode.skillId);
            bool hasPoints = (GameManager.Instance != null && GameManager.Instance.SkillPoints >= 1);

            if (learnSkillButton != null)
            {
                var btnTxt = learnSkillButton.GetComponentInChildren<TextMeshProUGUI>();
                if (isUnlocked)
                {
                    learnSkillButton.interactable = false;
                    if (btnTxt != null) btnTxt.text = "DESBLOQUEADO";
                }
                else
                {
                    learnSkillButton.interactable = hasPoints;
                    if (btnTxt != null) btnTxt.text = hasPoints ? "APRENDER" : "PONTOS INSUFICIENTES";
                }
            }
        }

        private bool IsSkillUnlocked(string skillId)
        {
            if (GameManager.Instance == null) return false;

            switch (skillId)
            {
                case "Vitality": return GameManager.Instance.SkillVitalityUnlocked;
                case "AttackSpeed": return GameManager.Instance.SkillAttackSpeedUnlocked;
                case "IcePower": return GameManager.Instance.SkillIcePowerUnlocked;
                case "FirePower": return GameManager.Instance.SkillFirePowerUnlocked;
                case "FlightSpeed": return GameManager.Instance.SkillFlightSpeedUnlocked;
                default: return false;
            }
        }

        private void OnEvolveStaffClicked()
        {
            if (GameManager.Instance != null)
            {
                bool success = GameManager.Instance.TryEvolveStaff();
                if (success)
                {
                    RefreshUI();
                }
            }
        }

        private void OnLearnSkillClicked()
        {
            if (selectedNode != null && GameManager.Instance != null)
            {
                bool success = GameManager.Instance.UnlockSkill(selectedNode.skillId);
                if (success)
                {
                    RefreshUI();
                }
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
    }
}
