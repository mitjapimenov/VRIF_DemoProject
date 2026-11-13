using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Updates an already built scene without rebuilding it : removes slow motion, slims the scoreboard to score + combo,
    /// sets the longer combo window, drops the slow motion step from the tutorial and links the aim line to two-handed holding.
    /// Everything else edited in the scene is kept.
    /// </summary>
    public static class BoomBlocksSceneUpdate {

        const float ComboWindow = 7f;

        [MenuItem("Boom Blocks/Update Scene (Remove Slow Motion, Slim Scoreboard)")]
        public static void UpdateOpenScene() {
            // Slow motion object, its script is gone so it only has a missing component left
            GameObject root = GameObject.Find("BoomBlocks Prototype");
            if (root != null) {
                Transform slowMo = root.transform.Find("SlowMo");
                if (slowMo != null) {
                    Object.DestroyImmediate(slowMo.gameObject);
                }
            }
            BoomBlocksPrototypeBuilder.disableVrifSlowMotion();

            BoomScoreManager manager = Object.FindFirstObjectByType<BoomScoreManager>(FindObjectsInactive.Include);
            if (manager != null) {
                manager.ComboWindow = ComboWindow;
                EditorUtility.SetDirty(manager);
            }

            // Scoreboard : rebuild its contents in place, keeping where it stands
            BoomHud hud = Object.FindFirstObjectByType<BoomHud>(FindObjectsInactive.Include);
            if (hud != null) {
                RectTransform canvas = (RectTransform)hud.transform;
                for (int i = canvas.childCount - 1; i >= 0; i--) {
                    Object.DestroyImmediate(canvas.GetChild(i).gameObject);
                }
                BoomBlocksUiBuilder.BuildHudContents(canvas, manager);
                EditorUtility.SetDirty(hud);
            }

            // Tutorial : no slow motion meter, one dot less, and the shooting step now teaches the foregrip
            BoomTutorial tutorial = Object.FindFirstObjectByType<BoomTutorial>(FindObjectsInactive.Include);
            if (tutorial != null && tutorial.Panel != null) {
                Transform meter = tutorial.Panel.transform.Find("SlowMoMeter");
                if (meter != null) {
                    Object.DestroyImmediate(meter.gameObject);
                }

                Transform dots = tutorial.Panel.transform.Find("StepDots");
                if (dots != null) {
                    BoomBlocksUiBuilder.BuildStepDots(tutorial, (RectTransform)dots, BoomBlocksUiBuilder.TutorialStepCount);
                }

                // Only replace the text if it is still the old default
                if (tutorial.ShootText == null || tutorial.ShootText.Title == "Fire") {
                    tutorial.ShootText = new BoomTutorialText("Aim and fire",
                        "Hold the blue <b>foregrip</b> under the barrel with your other hand. The launcher steadies and shows the <b>aim line</b>.\n\nAim a little above the target and pull the <b>Trigger</b>.");
                }
                EditorUtility.SetDirty(tutorial);
            }

            foreach (BoomTrajectoryPreview preview in Object.FindObjectsByType<BoomTrajectoryPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                preview.OnlyWhenTwoHanded = true;
                if (preview.Handling == null) {
                    preview.Handling = preview.GetComponent<BoomLauncherHandling>();
                }
                EditorUtility.SetDirty(preview);
            }

            if (root != null) {
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            Debug.Log("Boom Blocks : scene updated (slow motion removed, scoreboard slimmed, combo window " + ComboWindow + " s)");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksSceneUpdate.UpdateMitjaScene
        /// </summary>
        public static void UpdateMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            UpdateOpenScene();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
