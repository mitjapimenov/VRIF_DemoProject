using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Resizes the existing ammo balls to BoomBlocksPrototypeBuilder.BallDiameter without rebuilding the scene :
    /// the four ball prefabs, their trails, the balls on the table and the loaded ball in the barrel.
    /// </summary>
    public static class BoomBlocksBallSize {

        static readonly string[] prefabPaths = {
            "Assets/BoomBlocks/Prefabs/BoomBall.prefab",
            "Assets/BoomBlocks/Prefabs/BoomBallExplosive.prefab",
            "Assets/BoomBlocks/Prefabs/BoomAmmo_Normal.prefab",
            "Assets/BoomBlocks/Prefabs/BoomAmmo_Explosive.prefab"
        };

        [MenuItem("Boom Blocks/Apply Ball Size")]
        public static void ApplyInOpenScene() {
            float diameter = BoomBlocksPrototypeBuilder.BallDiameter;

            foreach (string path in prefabPaths) {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) {
                    continue;
                }
                root.transform.localScale = Vector3.one * diameter;
                TrailRenderer trail = root.GetComponent<TrailRenderer>();
                if (trail != null) {
                    trail.widthMultiplier = diameter * 0.8f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Balls on the table follow the prefab, unless their scale was overridden
            foreach (BoomAmmoPickup pickup in Object.FindObjectsByType<BoomAmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                pickup.transform.localScale = Vector3.one * diameter;
                EditorUtility.SetDirty(pickup.transform);
            }

            foreach (BoomLauncher weapon in Object.FindObjectsByType<BoomLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (weapon.LoadedIndicator != null) {
                    weapon.LoadedIndicator.transform.localScale = Vector3.one * diameter;
                    EditorUtility.SetDirty(weapon.LoadedIndicator.transform);
                }
                EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Boom Blocks : ball size set to " + diameter + " m");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksBallSize.ApplyInMitjaScene
        /// </summary>
        public static void ApplyInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            ApplyInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
