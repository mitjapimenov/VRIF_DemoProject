using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Creates the TeleportBounds object at the scene root (outside the prototype, so rebuilding never removes it),
    /// centered where the player starts. If it already exists, it is kept where it is and only gets the walking blockers added.
    /// Also sets up the "PlayerBounds" layer, which only collides with the "Player" layer.
    /// </summary>
    public static class BoomBlocksTeleportBoundsTool {

        const string ObjectName = "TeleportBounds";
        public const string PlayerBoundsLayerName = "PlayerBounds";
        static readonly string[] wallNames = { "WallFront", "WallBack", "WallLeft", "WallRight" };

        [MenuItem("Boom Blocks/Add Teleport Bounds")]
        public static void AddInOpenScene() {
            int restricted = LayerMask.NameToLayer("Restricted");
            int player = LayerMask.NameToLayer("Player");
            if (restricted < 0 || player < 0) {
                Debug.LogError("Boom Blocks : the project needs VRIF's 'Restricted' and 'Player' layers");
                return;
            }

            int blockLayer = ensurePlayerBoundsLayer(player);
            if (blockLayer < 0) {
                return;
            }

            GameObject root = GameObject.Find(ObjectName);
            if (root == null) {
                // Center on the player's start, at their floor level and facing the same way
                Vector3 position = Vector3.zero;
                float yaw = 0f;
                BNG.PlayerTeleport teleport = Object.FindFirstObjectByType<BNG.PlayerTeleport>(FindObjectsInactive.Include);
                if (teleport != null) {
                    Transform rig = teleport.transform.root;
                    position = rig.position;
                    yaw = rig.eulerAngles.y;
                }

                root = new GameObject(ObjectName);
                Undo.RegisterCreatedObjectUndo(root, "Add Teleport Bounds");
                root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            }

            foreach (string wallName in wallNames) {
                Transform wall = root.transform.Find(wallName);
                if (wall == null) {
                    wall = new GameObject(wallName).transform;
                    wall.SetParent(root.transform, false);
                    wall.gameObject.layer = restricted;
                    wall.gameObject.AddComponent<BoxCollider>().isTrigger = true;
                }

                if (wall.Find("Blocker") == null) {
                    GameObject blocker = new GameObject("Blocker");
                    blocker.transform.SetParent(wall, false);
                    blocker.layer = blockLayer;
                    blocker.AddComponent<BoxCollider>();
                }
            }

            BoomTeleportBounds bounds = root.GetComponent<BoomTeleportBounds>();
            if (bounds == null) {
                bounds = root.AddComponent<BoomTeleportBounds>();
            }
            bounds.Layout();
            EditorUtility.SetDirty(bounds);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Boom Blocks : teleport bounds ready at " + root.transform.position + " (teleport and walking blocked)");
        }

        /// <summary>
        /// Adds the PlayerBounds layer to the first free slot and makes it collide with the Player layer only
        /// </summary>
        static int ensurePlayerBoundsLayer(int player) {
            int layer = LayerMask.NameToLayer(PlayerBoundsLayerName);
            if (layer < 0) {
                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty layers = tagManager.FindProperty("layers");
                for (int i = 8; i < layers.arraySize; i++) {
                    SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(slot.stringValue)) {
                        slot.stringValue = PlayerBoundsLayerName;
                        tagManager.ApplyModifiedPropertiesWithoutUndo();
                        layer = i;
                        break;
                    }
                }
                if (layer < 0) {
                    Debug.LogError("Boom Blocks : no free layer slot for '" + PlayerBoundsLayerName + "'");
                    return -1;
                }
            }

            for (int other = 0; other < 32; other++) {
                Physics.IgnoreLayerCollision(layer, other, other != player);
            }

            // Make sure the collision matrix is written to ProjectSettings/DynamicsManager.asset
            Object physicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0];
            EditorUtility.SetDirty(physicsSettings);
            AssetDatabase.SaveAssets();
            return layer;
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksTeleportBoundsTool.AddInMitjaScene
        /// </summary>
        public static void AddInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            AddInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
