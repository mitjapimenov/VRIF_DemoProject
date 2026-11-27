using BNG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Updates the existing scene without rebuilding it : gives the tutorial its three sounds
    /// and makes the launcher a hold grab (squeeze grip to keep it, let go to drop).
    /// </summary>
    public static class BoomBlocksTutorialSoundsTool {

        const string OldExplosivesBody = "<color=#FF5A45><b>Red balls</b></color> explode on impact. <color=#FF5A45><b>TNT barrels</b></color> blow up when hit by a ball or a falling pillar.\n\nScoring in quick succession raises your <b>combo multiplier</b>. Chain reactions pay off!";

        const string OldPickUpBody = "Reach for the launcher's handle on the table and press <b>Grip</b> once.\n\nIt stays in your hand until you press Grip again.";

        [MenuItem("Boom Blocks/Apply Sounds And Hold Grab")]
        public static void ApplyInOpenScene() {
            BoomTutorial tutorial = Object.FindFirstObjectByType<BoomTutorial>(FindObjectsInactive.Include);
            if (tutorial != null) {
                BoomBlocksUiBuilder.AssignTutorialSounds(tutorial);

                // Only replace texts that are still the old defaults
                if (tutorial.PickUpText != null && tutorial.PickUpText.Body == OldPickUpBody) {
                    tutorial.PickUpText = new BoomTutorialText("Pick up the launcher",
                        "Reach for the launcher's handle on the table and squeeze <b>Grip</b>.\n\nKeep squeezing to hold it. Let go of Grip to drop it.");
                }
                if (tutorial.ExplosivesText != null && tutorial.ExplosivesText.Body == OldExplosivesBody) {
                    tutorial.ExplosivesText = new BoomTutorialText("Explosives & combos",
                        "Load a <color=#FF5A45><b>red ball</b></color> from the right tray and fire it at a structure. Red balls explode on impact, and <color=#FF5A45><b>TNT barrels</b></color> blow up when hit.\n\nScoring in quick succession raises your <b>combo multiplier</b>.");
                }
                EditorUtility.SetDirty(tutorial);

                Debug.Log("Boom Blocks : tutorial sounds " +
                    (tutorial.ScreenChangeSound != null ? "screen change OK, " : "screen change MISSING, ") +
                    (tutorial.ArrowSound != null ? "arrow OK, " : "arrow MISSING, ") +
                    (tutorial.CompleteSound != null ? "complete OK" : "complete MISSING"));
            }

            foreach (BoomLauncher weapon in Object.FindObjectsByType<BoomLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                Grabbable grab = weapon.GetComponent<Grabbable>();
                BoomBlocksPrototypeBuilder.AssignLauncherSounds(weapon);
                EditorUtility.SetDirty(weapon);
                Debug.Log("Boom Blocks : launcher sounds fire = " + (weapon.GunShotSound != null ? weapon.GunShotSound.name : "none") +
                    ", load = " + (weapon.LoadSound != null ? weapon.LoadSound.name : "none"));

                if (grab != null) {
                    grab.Grabtype = HoldType.HoldDown;
                    EditorUtility.SetDirty(grab);
                    EditorSceneManager.MarkSceneDirty(grab.gameObject.scene);
                    Debug.Log("Boom Blocks : launcher grab set to hold");
                }
            }
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksTutorialSoundsTool.ApplyInMitjaScene
        /// </summary>
        public static void ApplyInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            ApplyInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
