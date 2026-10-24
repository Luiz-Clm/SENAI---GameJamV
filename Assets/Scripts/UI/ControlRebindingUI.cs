using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    /// <summary>Connects the scene's fixed binding labels and edit buttons to control rebinding.</summary>
    public sealed class ControlRebindingUI : MonoBehaviour
    {
        private enum RebindAction
        {
            None,
            Movement,
            Fire,
            SkillBook,
            SwitchWeapon
        }

        private static readonly KeyCode[] KeyboardKeys = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Where(IsBindableKeyboardKey)
            .ToArray();

        private TextMeshProUGUI movementText;
        private TextMeshProUGUI fireText;
        private TextMeshProUGUI skillBookText;
        private TextMeshProUGUI switchWeaponText;
        private TextMeshProUGUI movementButtonText;
        private TextMeshProUGUI fireButtonText;
        private TextMeshProUGUI skillBookButtonText;
        private TextMeshProUGUI switchWeaponButtonText;
        private RebindAction activeAction;
        private int captureStartedFrame;
        private int movementStep;
        private readonly KeyCode[] pendingMovementKeys = new KeyCode[4];

        public void Initialize(GameObject optionsPanel)
        {
            if (optionsPanel == null) return;

            var labels = optionsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            movementText = FindLabel(labels, "MoverKey");
            fireText = FindLabel(labels, "AtirarKey");
            skillBookText = FindLabel(labels, "ArvoredeUpgradesKey");
            switchWeaponText = FindLabel(labels, "Trocar ArmaKey");

            Transform controlsBox = optionsPanel.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "ControlsBox");
            if (controlsBox == null)
                controlsBox = movementText != null ? movementText.transform.parent : optionsPanel.transform;

            movementButtonText = BindRebindButton(controlsBox, "MoverRebindButton", BeginMovementCapture);
            fireButtonText = BindRebindButton(controlsBox, "AtirarRebindButton", () => BeginCapture(RebindAction.Fire));
            skillBookButtonText = BindRebindButton(controlsBox, "SkillBookRebindButton", () => BeginCapture(RebindAction.SkillBook));
            switchWeaponButtonText = BindRebindButton(controlsBox, "SwitchWeaponRebindButton", () => BeginCapture(RebindAction.SwitchWeapon));

            RefreshLabels();
        }

        private void Update()
        {
            if (activeAction == RebindAction.None || Time.frameCount <= captureStartedFrame) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelCapture();
                return;
            }

            if (activeAction == RebindAction.Fire)
            {
                for (int button = 0; button <= 4; button++)
                {
                    if (!Input.GetMouseButtonDown(button)) continue;
                    ControlBindings.SetFireMouseButton(button);
                    FinishCapture();
                    return;
                }
            }

            if (!TryReadPressedKey(out KeyCode key)) return;
            if (HasConflict(key))
            {
                SetActiveButtonLabel("EM USO");
                return;
            }

            switch (activeAction)
            {
                case RebindAction.Movement:
                    CaptureMovementKey(key);
                    break;
                case RebindAction.Fire:
                    ControlBindings.SetFireKey(key);
                    FinishCapture();
                    break;
                case RebindAction.SkillBook:
                    ControlBindings.SetSkillBookKey(key);
                    FinishCapture();
                    break;
                case RebindAction.SwitchWeapon:
                    ControlBindings.SetSwitchWeaponKey(key);
                    FinishCapture();
                    break;
            }
        }

        private static TextMeshProUGUI FindLabel(TextMeshProUGUI[] labels, string objectName)
        {
            foreach (var label in labels)
            {
                if (label != null && label.gameObject.name == objectName) return label;
            }
            return null;
        }

        private static TextMeshProUGUI BindRebindButton(Transform parent, string objectName,
            UnityEngine.Events.UnityAction onClick)
        {
            Transform buttonTransform = parent != null ? parent.Find(objectName) : null;
            if (buttonTransform == null)
            {
                Debug.LogWarning($"Botão de remapeamento '{objectName}' não está na cena do Menu.");
                return null;
            }

            Button button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"O objeto '{objectName}' precisa ter um componente Button.");
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            ButtonClickSound.Bind(button);
            return button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void BeginMovementCapture()
        {
            activeAction = RebindAction.Movement;
            movementStep = 0;
            captureStartedFrame = Time.frameCount;
            ClearPendingMovementKeys();
            ShowMovementPrompt();
        }

        private void BeginCapture(RebindAction action)
        {
            activeAction = action;
            captureStartedFrame = Time.frameCount;
            SetActiveButtonLabel(action == RebindAction.Fire ? "TECLA/MOUSE" : "PRESSIONE");
        }

        private void CaptureMovementKey(KeyCode key)
        {
            for (int i = 0; i < movementStep; i++)
            {
                if (pendingMovementKeys[i] != key) continue;
                SetActiveButtonLabel("OUTRA TECLA");
                return;
            }

            pendingMovementKeys[movementStep] = key;
            movementStep++;
            if (movementStep < pendingMovementKeys.Length)
            {
                ShowMovementPrompt();
                return;
            }

            ControlBindings.SetMovementKeys(
                pendingMovementKeys[0], pendingMovementKeys[1],
                pendingMovementKeys[2], pendingMovementKeys[3]);
            FinishCapture();
        }

        private void ShowMovementPrompt()
        {
            string[] directions = { "CIMA?", "ESQUERDA?", "BAIXO?", "DIREITA?" };
            SetActiveButtonLabel(directions[movementStep]);
        }

        private void SetActiveButtonLabel(string value)
        {
            TextMeshProUGUI label = GetButtonLabel(activeAction);
            if (label != null) label.text = value;
        }

        private void FinishCapture()
        {
            activeAction = RebindAction.None;
            ClearPendingMovementKeys();
            RefreshLabels();
        }

        private void CancelCapture()
        {
            activeAction = RebindAction.None;
            ClearPendingMovementKeys();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (movementText != null) movementText.text = ControlBindings.GetMovementDisplay();
            if (fireText != null) fireText.text = ControlBindings.GetFireDisplay();
            if (skillBookText != null) skillBookText.text = ControlBindings.GetSkillBookDisplay();
            if (switchWeaponText != null) switchWeaponText.text = ControlBindings.GetSwitchWeaponDisplay();

            ResetButtonLabel(movementButtonText);
            ResetButtonLabel(fireButtonText);
            ResetButtonLabel(skillBookButtonText);
            ResetButtonLabel(switchWeaponButtonText);
        }

        private static void ResetButtonLabel(TextMeshProUGUI label)
        {
            if (label != null) label.text = "MUDAR";
        }

        private bool HasConflict(KeyCode key)
        {
            // Keep the fixed inventory, pause, and boss shortcuts available.
            if (key == KeyCode.F || key == KeyCode.P || key == KeyCode.B ||
                key == KeyCode.Alpha1 || key == KeyCode.Alpha2)
                return true;

            if (activeAction != RebindAction.SkillBook && key == ControlBindings.SkillBookKey)
                return true;

            if (activeAction != RebindAction.SwitchWeapon)
            {
                if (ControlBindings.IsSwitchCustomized)
                {
                    if (key == ControlBindings.SwitchWeaponKey) return true;
                }
                else if (key == KeyCode.Q || key == KeyCode.E || key == KeyCode.Tab || key == KeyCode.LeftShift)
                    return true;
            }

            if (activeAction != RebindAction.Fire)
            {
                if (ControlBindings.IsFireCustomized)
                {
                    if (key == ControlBindings.FireKey) return true;
                }
                else if (key == KeyCode.Space || key == KeyCode.J || key == KeyCode.Return || key == KeyCode.KeypadEnter)
                    return true;
            }

            if (activeAction != RebindAction.Movement)
            {
                if (key == ControlBindings.MoveUp || key == ControlBindings.MoveLeft ||
                    key == ControlBindings.MoveDown || key == ControlBindings.MoveRight)
                    return true;

                if (!ControlBindings.IsMovementCustomized &&
                    (key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
                     key == KeyCode.LeftArrow || key == KeyCode.RightArrow))
                    return true;
            }

            return false;
        }

        private TextMeshProUGUI GetButtonLabel(RebindAction action)
        {
            switch (action)
            {
                case RebindAction.Movement: return movementButtonText;
                case RebindAction.Fire: return fireButtonText;
                case RebindAction.SkillBook: return skillBookButtonText;
                case RebindAction.SwitchWeapon: return switchWeaponButtonText;
                default: return null;
            }
        }

        private void ClearPendingMovementKeys()
        {
            for (int i = 0; i < pendingMovementKeys.Length; i++) pendingMovementKeys[i] = KeyCode.None;
        }

        private static bool TryReadPressedKey(out KeyCode pressed)
        {
            foreach (KeyCode key in KeyboardKeys)
            {
                if (key == KeyCode.Escape || key == KeyCode.None) continue;
                if (!Input.GetKeyDown(key)) continue;
                pressed = key;
                return true;
            }

            pressed = KeyCode.None;
            return false;
        }

        private static bool IsBindableKeyboardKey(KeyCode key)
        {
            return key != KeyCode.None && key != KeyCode.Escape &&
                   (int)key < (int)KeyCode.Mouse0;
        }
    }
}
