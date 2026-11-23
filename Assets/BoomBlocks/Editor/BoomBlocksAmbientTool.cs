using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Adds a looping background ambience at the scene root (outside the prototype, so rebuilding never removes it).
    /// Only adds a new object : nothing else in the scene is changed. Adjust volume / clip on its AudioSource.
    /// </summary>
    public static class BoomBlocksAmbientTool {

        const string ObjectName = "AmbientAudio";
        const string ClipPath = "Assets/Nature - Essentials/Ambiance_Cicadas_Loop_Stereo.wav";
        const float Volume = 0.4f;

        [MenuItem("Boom Blocks/Add Ambient Audio")]
        public static void AddInOpenScene() {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null) {
                Debug.LogError("Boom Blocks : ambient clip not found at " + ClipPath);
                return;
            }

            GameObject go = GameObject.Find(ObjectName);
            if (go == null) {
                go = new GameObject(ObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Add Ambient Audio");
            }

            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null) {
                source = go.AddComponent<AudioSource>();
            }

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = Volume;
            // 2D : heard the same everywhere, like a background track
            source.spatialBlend = 0f;
            // Background sound gives way to effects if too many sounds play at once
            source.priority = 200;

            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("Boom Blocks : ambient audio added (" + clip.name + ", " + clip.length.ToString("0.0") + " s loop)");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksAmbientTool.AddInMitjaScene
        /// </summary>
        public static void AddInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            AddInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
