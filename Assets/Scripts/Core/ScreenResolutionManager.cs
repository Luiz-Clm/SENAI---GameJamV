using UnityEngine;

namespace WitchShmup.Core
{
    /// <summary>
    /// Garante que o jogo rode em 1920x1080 (Full HD) e configura o ScreenManager.
    /// Coloque este componente no GameManager ou em outro objeto persistente.
    /// </summary>
    public class ScreenResolutionManager : MonoBehaviour
    {
        [Header("Configuração de Tela")]
        [SerializeField] private int targetWidth = 1920;
        [SerializeField] private int targetHeight = 1080;
        [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

        [Header("Frame Rate")]
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            ApplyResolution();
            Application.targetFrameRate = targetFrameRate;
        }

        public void ApplyResolution()
        {
            Screen.SetResolution(targetWidth, targetHeight, fullScreenMode);
        }

        /// <summary>
        /// Retorna o fator de escala da UI baseado na resolução atual vs referência Full HD.
        /// </summary>
        public static float GetUIScaleFactor()
        {
            return Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        }
    }
}
