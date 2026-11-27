using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Adds the TrailerCamera at the scene root, just behind the player's start looking the same way.
    /// It is a separate desktop-only camera : the VR rig is not touched.
    /// </summary>
    public static class BoomBlocksTrailerCameraTool {

        const string ObjectName = "TrailerCamera";

        [MenuItem("Boom Blocks/Add Trailer Camera")]
        public static void AddInOpenScene() {
            if (Object.FindFirstObjectByType<BoomTrailerCamera>(FindObjectsInactive.Include) != null) {
                Debug.LogWarning("Boom Blocks : the scene already has a trailer camera");
                return;
            }

            Vector3 position = new Vector3(0, 1.7f, -1.5f);
            Quaternion rotation = Quaternion.identity;
            BNG.PlayerTeleport teleport = Object.FindFirstObjectByType<BNG.PlayerTeleport>(FindObjectsInactive.Include);
            if (teleport != null) {
                Transform rig = teleport.transform.root;
                rotation = Quaternion.Euler(0, rig.eulerAngles.y, 0);
                position = rig.position + Vector3.up * 1.7f - (rotation * Vector3.forward) * 1.5f;
            }

            GameObject go = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Add Trailer Camera");
            go.transform.SetPositionAndRotation(position, rotation);

            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            // Off until switched on with C in Play mode, so it never replaces the VR view by accident
            cam.enabled = false;

            go.AddComponent<BoomTrailerCamera>();

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("Boom Blocks : trailer camera added at " + position);
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksTrailerCameraTool.AddInMitjaScene
        /// </summary>
        public static void AddInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            AddInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
