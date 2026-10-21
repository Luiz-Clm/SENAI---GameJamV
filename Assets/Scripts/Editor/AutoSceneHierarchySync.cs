#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitchShmup.Player;
using WitchShmup.UI;

namespace WitchShmup.Editor
{
    public static class AutoSceneHierarchySync
    {
        [MenuItem("Tools/Witch Shmup/★ Sincronizar Mana e Painel de Evolução na Hierarchy ★")]
        public static void SyncCurrentScene()
        {
            if (Application.isPlaying) return;

            bool sceneModified = false;

            // 1. Garante PlayerMana no Player_Wizard
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var pShooting = Object.FindFirstObjectByType<PlayerShooting>();
                if (pShooting != null) player = pShooting.gameObject;
            }

            if (player != null)
            {
                var mana = player.GetComponent<PlayerMana>();
                if (mana == null)
                {
                    Undo.AddComponent<PlayerMana>(player);
                    sceneModified = true;
                    Debug.Log("<color=cyan>[AutoSync] Componente PlayerMana adicionado ao Player_Wizard na Hierarchy!</color>");
                }
            }

            // 2. Garante ManaBar_Container no Canvas
            var hud = Object.FindFirstObjectByType<HUDController>();
            if (hud != null)
            {
                hud.EnsureManaBarSetup();
                EditorUtility.SetDirty(hud.gameObject);
                sceneModified = true;
                Debug.Log("<color=cyan>[AutoSync] ManaBar_Container gerado na Hierarchy do HUD_Canvas!</color>");
            }

            // 3. Garante estrutura visual de preview no Panel_Evolution
            var evoUI = Object.FindFirstObjectByType<EvolutionUI>();
            if (evoUI != null)
            {
                evoUI.EditorBuildUI();
                EditorUtility.SetDirty(evoUI.gameObject);
                sceneModified = true;
                Debug.Log("<color=green>[AutoSync] Estrutura do Panel_Evolution gerada na Hierarchy!</color>");
            }

            if (sceneModified)
            {
                var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }
        }
    }
}
#endif
