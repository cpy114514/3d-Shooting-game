using PlayerBlock;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class CombatHudSceneSetup
    {
        static CombatHudSceneSetup()
        {
            EditorApplication.delayCall += EnsureHudInActiveScene;
        }

        [MenuItem("Tools/Block Player/Add Combat HUD To Scene")]
        public static void EnsureHudInActiveScene()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var existingHud = Object.FindFirstObjectByType<CombatHud>();
            if (existingHud != null)
            {
                existingHud.EnsureEditableUi();
                EditorUtility.SetDirty(existingHud.gameObject);
                return;
            }

            var hudObject = new GameObject("PlayerBlockCombatHud");
            Undo.RegisterCreatedObjectUndo(hudObject, "Add Player Block Combat HUD");
            SceneManager.MoveGameObjectToScene(hudObject, scene);

            var hud = hudObject.AddComponent<CombatHud>();
            hud.EnsureEditableUi();

            Selection.activeGameObject = hudObject;
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
