using BNG;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomBlocks {

    /// <summary>
    /// Restarts the whole level, like pressing Play again : hold A, B, X, Y and both triggers together for HoldDuration seconds.
    /// The controllers vibrate more and more while holding. Letting go of any button cancels.
    /// </summary>
    public class BoomLevelReset : MonoBehaviour {

        [Tooltip("Seconds all six buttons must be held")]
        public float HoldDuration = 2f;

        [Tooltip("How far a trigger must be pulled to count as pressed (0 - 1)")]
        [Range(0.1f, 1f)]
        public float TriggerThreshold = 0.75f;

        [Tooltip("Vibration strength at the end of the hold. 0 = off")]
        public float MaxVibration = 0.6f;

        float heldTime;
        bool reloading;

        void Update() {
            if (reloading || InputBridge.Instance == null) {
                return;
            }

            InputBridge input = InputBridge.Instance;
            bool allHeld = input.AButton && input.BButton && input.XButton && input.YButton
                && input.LeftTrigger >= TriggerThreshold && input.RightTrigger >= TriggerThreshold;

            if (!allHeld) {
                heldTime = 0f;
                return;
            }

            heldTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(heldTime / Mathf.Max(0.01f, HoldDuration));

            if (MaxVibration > 0f) {
                input.VibrateController(0.2f, MaxVibration * progress, 0.05f, ControllerHand.Left);
                input.VibrateController(0.2f, MaxVibration * progress, 0.05f, ControllerHand.Right);
            }

            if (progress >= 1f) {
                ReloadLevel();
            }
        }

        /// <summary>
        /// Loads the current scene again from scratch
        /// </summary>
        public void ReloadLevel() {
            reloading = true;
            Time.timeScale = 1f;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }

#if UNITY_EDITOR
            // Not in the build's scene list : the editor can still reload it by path
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogWarning("Boom Blocks : add this scene to the build's scene list so it can be reloaded");
            reloading = false;
#endif
        }
    }
}
