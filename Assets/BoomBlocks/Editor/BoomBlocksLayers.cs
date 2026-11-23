using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Puts the launcher and the balls on VRIF's "Grabbable" layer. The project's physics settings make that layer ignore
    /// the "Player" layer, so a held or dropped launcher can't push the player around or lift them up.
    /// </summary>
    public static class BoomBlocksLayers {

        public const string GrabbableLayerName = "Grabbable";
        public const string PlayerLayerName = "Player";

        static readonly string[] ballPrefabPaths = {
            "Assets/BoomBlocks/Prefabs/BoomBall.prefab",
            "Assets/BoomBlocks/Prefabs/BoomBallExplosive.prefab",
            "Assets/BoomBlocks/Prefabs/BoomAmmo_Normal.prefab",
            "Assets/BoomBlocks/Prefabs/BoomAmmo_Explosive.prefab"
        };

        [MenuItem("Boom Blocks/Fix Launcher And Ball Layers")]
        public static void ApplyInOpenScene() {
            int layer = LayerMask.NameToLayer(GrabbableLayerName);
            if (layer < 0) {
                Debug.LogError("Boom Blocks : no '" + GrabbableLayerName + "' layer in the project");
                return;
            }

            foreach (string path in ballPrefabPaths) {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) {
                    continue;
                }
                setLayerRecursively(root, layer);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (BoomLauncher weapon in Object.FindObjectsByType<BoomLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                setLayerRecursively(weapon.gameObject, layer);
                EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
            }

            // Balls already on the table
            foreach (BoomAmmoPickup pickup in Object.FindObjectsByType<BoomAmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                setLayerRecursively(pickup.gameObject, layer);
            }

            // The aim line should not stop on the player's own body
            int player = LayerMask.NameToLayer(PlayerLayerName);
            foreach (BoomTrajectoryPreview preview in Object.FindObjectsByType<BoomTrajectoryPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                LayerMask mask = ~0;
                if (player >= 0) {
                    mask &= ~(1 << player);
                }
                mask &= ~(1 << layer);
                preview.HitLayers = mask;
                EditorUtility.SetDirty(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Boom Blocks : launcher and balls moved to the '" + GrabbableLayerName + "' layer");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksLayers.ApplyInMitjaScene
        /// </summary>
        public static void ApplyInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            ApplyInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }

        static void setLayerRecursively(GameObject go, int layer) {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) {
                t.gameObject.layer = layer;
                EditorUtility.SetDirty(t.gameObject);
            }
        }
    }
}
