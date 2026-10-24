#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WitchShmup.Core;
using WitchShmup.UI;

namespace WitchShmup.EditorTools
{
    public static class PauseUIEditorTool
    {
        [MenuItem("Tools/SpellGlide/Build Editable Pause Panel in Testes")]
        private static void BuildEditablePausePanel()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Testes")
            {
                Debug.LogWarning("Abra a cena Testes antes de gerar o painel de pausa editável.");
                return;
            }

            var gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("A cena Testes não contém um GameManager.");
                return;
            }

            var pauseUI = gameManager.GetComponent<PauseUI>();
            if (pauseUI == null) pauseUI = Undo.AddComponent<PauseUI>(gameManager.gameObject);
            pauseUI.BuildEditablePanelInEditor();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Painel de pausa gerado na cena Testes. Agora ele pode ser ajustado na Hierarchy e no Inspector.");
        }
    }
}
#endif
