using UnityEngine;

namespace WitchShmup.Core
{
    /// <summary>Player-configurable keyboard and mouse bindings, persisted between runs.</summary>
    public static class ControlBindings
    {
        private const string Prefix = "WitchShmup_Controls_";
        private const string MoveCustomKey = Prefix + "MoveCustom";
        private const string FireCustomKey = Prefix + "FireCustom";
        private const string SwitchCustomKey = Prefix + "SwitchCustom";

        public static KeyCode MoveUp => GetKey("MoveUp", KeyCode.W);
        public static KeyCode MoveLeft => GetKey("MoveLeft", KeyCode.A);
        public static KeyCode MoveDown => GetKey("MoveDown", KeyCode.S);
        public static KeyCode MoveRight => GetKey("MoveRight", KeyCode.D);
        public static KeyCode FireKey => GetKey("FireKey", KeyCode.None);
        public static int FireMouseButton => PlayerPrefs.GetInt(Prefix + "FireMouse", 0);
        public static KeyCode SkillBookKey => GetKey("SkillBook", KeyCode.T);
        public static KeyCode SwitchWeaponKey => GetKey("SwitchWeapon", KeyCode.Tab);

        public static bool IsMovementCustomized => PlayerPrefs.GetInt(MoveCustomKey, 0) == 1;
        public static bool IsFireCustomized => PlayerPrefs.GetInt(FireCustomKey, 0) == 1;
        public static bool IsSwitchCustomized => PlayerPrefs.GetInt(SwitchCustomKey, 0) == 1;

        public static Vector2 ReadMovement()
        {
            float horizontal = (Input.GetKey(MoveRight) ? 1f : 0f) - (Input.GetKey(MoveLeft) ? 1f : 0f);
            float vertical = (Input.GetKey(MoveUp) ? 1f : 0f) - (Input.GetKey(MoveDown) ? 1f : 0f);

            // Keep the original arrow-key alternative until the player chooses a custom layout.
            if (!IsMovementCustomized)
            {
                horizontal += (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                vertical += (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        public static bool IsFireHeld()
        {
            if (IsFireCustomized)
            {
                bool keyHeld = FireKey != KeyCode.None && Input.GetKey(FireKey);
                bool mouseHeld = FireMouseButton >= 0 && Input.GetMouseButton(FireMouseButton);
                return keyHeld || mouseHeld;
            }

            // Preserve the previous default aliases until the player remaps Fire.
            return Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) ||
                   Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
        }

        public static bool WasSkillBookPressedThisFrame() => Input.GetKeyDown(SkillBookKey);

        public static bool WasSwitchWeaponPressedThisFrame()
        {
            if (IsSwitchCustomized)
                return Input.GetKeyDown(SwitchWeaponKey);

            // Preserve the existing keyboard and secondary-mouse aliases by default.
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) ||
                   Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.LeftShift) ||
                   Input.GetMouseButtonDown(1);
        }

        public static void SetMovementKeys(KeyCode up, KeyCode left, KeyCode down, KeyCode right)
        {
            PlayerPrefs.SetInt(Prefix + "MoveUp", (int)up);
            PlayerPrefs.SetInt(Prefix + "MoveLeft", (int)left);
            PlayerPrefs.SetInt(Prefix + "MoveDown", (int)down);
            PlayerPrefs.SetInt(Prefix + "MoveRight", (int)right);
            PlayerPrefs.SetInt(MoveCustomKey, 1);
            PlayerPrefs.Save();
        }

        public static void SetFireKey(KeyCode key)
        {
            PlayerPrefs.SetInt(Prefix + "FireKey", (int)key);
            PlayerPrefs.SetInt(Prefix + "FireMouse", -1);
            PlayerPrefs.SetInt(FireCustomKey, 1);
            PlayerPrefs.Save();
        }

        public static void SetFireMouseButton(int button)
        {
            PlayerPrefs.SetInt(Prefix + "FireKey", (int)KeyCode.None);
            PlayerPrefs.SetInt(Prefix + "FireMouse", button);
            PlayerPrefs.SetInt(FireCustomKey, 1);
            PlayerPrefs.Save();
        }

        public static void SetSkillBookKey(KeyCode key)
        {
            PlayerPrefs.SetInt(Prefix + "SkillBook", (int)key);
            PlayerPrefs.Save();
        }

        public static void SetSwitchWeaponKey(KeyCode key)
        {
            PlayerPrefs.SetInt(Prefix + "SwitchWeapon", (int)key);
            PlayerPrefs.SetInt(SwitchCustomKey, 1);
            PlayerPrefs.Save();
        }

        public static string GetMovementDisplay()
        {
            if (!IsMovementCustomized) return "SETAS/WASD";
            return $"{FormatKey(MoveUp)}/{FormatKey(MoveLeft)}/{FormatKey(MoveDown)}/{FormatKey(MoveRight)}";
        }

        public static string GetFireDisplay()
        {
            if (!IsFireCustomized) return "M1";
            if (FireMouseButton >= 0) return FormatMouseButton(FireMouseButton);
            return FormatKey(FireKey);
        }

        public static string GetSkillBookDisplay() => FormatKey(SkillBookKey);

        public static string GetSwitchWeaponDisplay()
        {
            return IsSwitchCustomized ? FormatKey(SwitchWeaponKey) : "TAB/Q";
        }

        public static string FormatKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                case KeyCode.LeftControl: return "LCTRL";
                case KeyCode.RightControl: return "RCTRL";
                case KeyCode.LeftShift: return "LSHIFT";
                case KeyCode.RightShift: return "RSHIFT";
                case KeyCode.LeftAlt: return "LALT";
                case KeyCode.RightAlt: return "RALT";
                case KeyCode.Return: return "ENTER";
                case KeyCode.KeypadEnter: return "NUM ENTER";
                case KeyCode.Space: return "SPACE";
                case KeyCode.Escape: return "ESC";
                case KeyCode.None: return "---";
                default:
                    string name = key.ToString();
                    if (name.StartsWith("Alpha")) name = name.Substring(5);
                    else if (name.StartsWith("Keypad")) name = "NUM " + name.Substring(6);
                    return name.ToUpperInvariant();
            }
        }

        public static string FormatMouseButton(int button)
        {
            switch (button)
            {
                case 0: return "M1";
                case 1: return "M2";
                case 2: return "M3";
                case 3: return "M4";
                case 4: return "M5";
                default: return $"M{button + 1}";
            }
        }

        private static KeyCode GetKey(string name, KeyCode defaultValue)
        {
            return (KeyCode)PlayerPrefs.GetInt(Prefix + name, (int)defaultValue);
        }
    }
}
