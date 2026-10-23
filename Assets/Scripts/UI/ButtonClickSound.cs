using SpellGlide;
using UnityEngine;
using UnityEngine.UI;

namespace WitchShmup.UI
{
    /// <summary>Reaproveita o efeito de clique do menu nos botões da interface do jogo.</summary>
    [DisallowMultipleComponent]
    public sealed class ButtonClickSound : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            BindListener();
        }

        private void OnDisable()
        {
            UnbindListener();
        }

        public static void Bind(Button target)
        {
            if (target == null) return;

            ButtonClickSound sound = target.GetComponent<ButtonClickSound>();
            if (sound == null) sound = target.gameObject.AddComponent<ButtonClickSound>();
            sound.BindListener();
        }

        public static void PlayClickSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }

        private void BindListener()
        {
            if (button == null) button = GetComponent<Button>();
            if (button == null) return;

            // Remove antes de adicionar para evitar som duplicado ao reconstruir a UI.
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }

        private void UnbindListener()
        {
            if (button != null) button.onClick.RemoveListener(PlayButtonClick);
        }

        private void PlayButtonClick()
        {
            PlayClickSound();
        }
    }
}
