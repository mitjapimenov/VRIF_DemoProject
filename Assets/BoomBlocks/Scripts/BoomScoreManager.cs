using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Keeps score and combo, and ends the round when all ammo is used or every target has scored. BoomHud displays the state
    /// </summary>
    public class BoomScoreManager : MonoBehaviour {

        public static BoomScoreManager Instance { get; private set; }

        public BoomLauncher Weapon;

        [Tooltip("Refilled with the same balls when a new round starts")]
        public BoomAmmoRack AmmoRack;

        [Tooltip("Seconds to wait after the last ball is gone before ending the round, so blocks can finish falling")]
        public float GameOverDelay = 3f;

        public Color PopupColor = new Color(1f, 0.85f, 0.2f);

        [Header("Combo : ")]
        [Tooltip("Points scored within this many seconds of the previous score raise the combo multiplier. Long enough to load and fire the next ball")]
        public float ComboWindow = 7f;

        public int MaxComboMultiplier = 5;

        /// <summary>
        /// Set by the tutorial so practice shots can't end the round
        /// </summary>
        [HideInInspector]
        public bool Paused = false;

        public int Score { get; private set; }

        public bool IsGameOver { get; private set; }

        public int Combo { get; private set; }

        /// <summary>
        /// True while the combo multiplier is above 1 and the next score would still raise it
        /// </summary>
        public bool ComboActive {
            get { return Combo > 1 && Time.time - lastScoreTime <= ComboWindow; }
        }

        /// <summary>
        /// 1 right after a score, falling to 0 when the combo window closes
        /// </summary>
        public float ComboTimeLeft {
            get { return Mathf.Clamp01(1f - (Time.time - lastScoreTime) / ComboWindow); }
        }

        BoomTarget[] targets;
        BoomResetPose[] props;
        BoomTntBarrel[] barrels;
        float roundEndingTime = -1f;
        float lastScoreTime = -100f;

        void Awake() {
            Instance = this;
        }

        void Start() {
            targets = FindObjectsByType<BoomTarget>(FindObjectsSortMode.None);
            props = FindObjectsByType<BoomResetPose>(FindObjectsSortMode.None);
            barrels = FindObjectsByType<BoomTntBarrel>(FindObjectsSortMode.None);
        }

        void Update() {
            if (IsGameOver) {
                if (InputBridge.Instance.AButtonDown || InputBridge.Instance.XButtonDown) {
                    RestartRound();
                }
            }
            else if (!Paused) {
                checkRoundEnd();
            }
        }

        public void AddPoints(int amount, Vector3 position) {
            if (IsGameOver) {
                return;
            }

            // Chain reactions : every score soon after the previous one multiplies more
            if (Time.time - lastScoreTime <= ComboWindow) {
                Combo = Mathf.Min(Combo + 1, MaxComboMultiplier);
            }
            else {
                Combo = 1;
            }
            lastScoreTime = Time.time;

            int total = amount * Combo;
            Score += total;

            string popup = Combo > 1 ? "+" + total + " x" + Combo : "+" + total;
            BoomPopup.Spawn(popup, position, PopupColor, null);
        }

        public void RestartRound() {
            Score = 0;
            Combo = 0;
            lastScoreTime = -100f;
            IsGameOver = false;
            roundEndingTime = -1f;

            for (int x = 0; x < barrels.Length; x++) {
                barrels[x].ResetBarrel();
            }

            for (int x = 0; x < props.Length; x++) {
                props[x].ResetPose();
            }

            for (int x = 0; x < targets.Length; x++) {
                targets[x].ResetTarget();
            }

            if (Weapon != null) {
                Weapon.Reload();
            }

            if (AmmoRack != null) {
                AmmoRack.ResetRack();
            }
        }

        void checkRoundEnd() {
            // Out of ammo once nothing is loaded and no balls are left to pick up
            bool outOfAmmo = (Weapon == null || Weapon.LoadedCount == 0) && BoomAmmoPickup.GetTotalActiveCount() == 0;
            bool shouldEnd = (outOfAmmo || allTargetsScored()) && BoomBall.ActiveCount == 0;

            if (!shouldEnd) {
                roundEndingTime = -1f;
            }
            else if (roundEndingTime < 0) {
                roundEndingTime = Time.time;
            }
            else if (Time.time - roundEndingTime >= GameOverDelay) {
                IsGameOver = true;
            }
        }

        bool allTargetsScored() {
            for (int x = 0; x < targets.Length; x++) {
                if (!targets[x].Scored) {
                    return false;
                }
            }
            return targets.Length > 0;
        }
    }
}
