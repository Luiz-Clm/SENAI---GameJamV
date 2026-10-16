using UnityEditor;
using UnityEngine;

namespace SpellGlide.Editor
{
    /// <summary>
    /// Limpa automaticamente os dados de save toda vez que o Play Mode é iniciado no Editor.
    /// Isso garante que testes no Editor sempre começam com dados zerados.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeSaveCleaner
    {
        static PlayModeSaveCleaner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Limpa o save ANTES de entrar no Play Mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                WitchShmup.Core.SaveSystem.DeleteSave();
                // Também limpa o HighScore separado que o GameManager usa
                PlayerPrefs.DeleteKey("WitchShmup_HighScore");
                PlayerPrefs.Save();
                Debug.Log("[PlayModeSaveCleaner] Dados de save zerados automaticamente ao iniciar Play Mode.");
            }
        }
    }
}
