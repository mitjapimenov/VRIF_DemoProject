using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Adds the LevelReset object at the scene root (outside the prototype, so rebuilding never removes it).
    /// Only adds a new object : nothing else in the scene is changed.
    /// </summary>
    public static class BoomBlocksLevelResetTool {

        const string ObjectName = "LevelReset";

        [MenuItem("Boom Blocks/Add Level Reset (Hold All Buttons)")]
        public static void AddInOpenScene() {
            if (Object.FindFirstObjectByType<BoomLevelReset>(FindObjectsInactive.Include) != null) {
                Debug.LogWarning("Boom Blocks : the scene already has a level reset");
                return;
            }

            GameObject go = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Add Level Reset");
            go.AddComponent<BoomLevelReset>();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("Boom Blocks : level reset added");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksLevelResetTool.AddInMitjaScene
        /// </summary>
        public static void AddInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            AddInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
