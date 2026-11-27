using BNG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoomBlocks {

    /// <summary>
    /// Text shown on the tutorial panel for one step. Rich text tags such as <b>bold</b> and <color=#FF5A45>color</color> work
    /// </summary>
    [System.Serializable]
    public class BoomTutorialText {
        public string Title;

        [TextArea(3, 8)]
        public string Body;

        public BoomTutorialText(string title, string body) {
            Title = title;
            Body = body;
        }
    }

    /// <summary>
    /// First time user experience : walks the player through picking up the launcher, loading, aiming with two hands and shooting,
    /// with a floating arrow pointing at what to use. When finished (or skipped by holding B) the round restarts with full ammo.
    /// All texts, timings and colors can be changed from the inspector.
    /// </summary>
    public class BoomTutorial : MonoBehaviour {

        enum Step {
            PickUp,
            Load,
            Shoot,
            Explosives,
            Done,
            Finished
        }

        // Steps before Done are numbered on the panel
        const int NumberedStepCount = (int)Step.Done;

        public BoomLauncher Launcher;
        public BoomScoreManager ScoreManager;

        [Tooltip("Show the tutorial on every launch. Turn off to only show it until it has been completed once")]
        public bool ShowEveryTime = true;

        [Tooltip("PlayerPrefs key that remembers the tutorial was completed")]
        public string CompletedKey = "BoomBlocks.TutorialCompleted";

        [Tooltip("Scoreboard hidden while the tutorial runs, so the player focuses on the instructions")]
        public GameObject Hud;

        [Header("Texts : ")]
        public BoomTutorialText PickUpText = new BoomTutorialText("Pick up the launcher",
            "Reach for the launcher's handle on the table and squeeze <b>Grip</b>.\n\nKeep squeezing to hold it. Let go of Grip to drop it.");

        public BoomTutorialText LoadText = new BoomTutorialText("Load a ball",
            "With your other hand, grab a <b>dark ball</b> from the left tray and push it into the <b>muzzle</b>.\n\nA ball appears at the end of the barrel when it is loaded.");

        public BoomTutorialText ShootText = new BoomTutorialText("Aim and fire",
            "Hold the blue <b>foregrip</b> under the barrel with your other hand. The launcher steadies and shows the <b>aim line</b>.\n\nAim a little above the target and pull the <b>Trigger</b>.");

        public BoomTutorialText ExplosivesText = new BoomTutorialText("Explosives & combos",
            "Load a <color=#FF5A45><b>red ball</b></color> from the right tray and fire it at a structure. Red balls explode on impact, and <color=#FF5A45><b>TNT barrels</b></color> blow up when hit.\n\nScoring in quick succession raises your <b>combo multiplier</b>.");

        public BoomTutorialText DoneText = new BoomTutorialText("You're ready!",
            "Ammo and targets have been reset.\n\nScore as many points as you can before you run out of ammo. Good luck!");

        [Tooltip("Step counter text. {0} = current step, {1} = number of steps")]
        public string StepCounterFormat = "STEP {0} / {1}";

        [Tooltip("Step counter text on the final message")]
        public string StepCounterDoneText = "TUTORIAL";

        public string FeedbackMessage = "Nice!";
        public string SkipMessage = "Hold to skip tutorial";
        public string ContinueMessage = "Continue";

        [Header("Panel : ")]
        public GameObject Panel;
        public TMP_Text StepCounter;
        public TMP_Text Title;
        public TMP_Text Body;
        public TMP_Text Feedback;
        public TMP_Text SkipHintText;

        [Header("Intro Popup : ")]
        [Tooltip("Small 'Press A for tutorial' popup shown after the start delay. Leave empty to start the tutorial right away")]
        public GameObject IntroPopup;

        [Tooltip("Hold-to-skip bar on the intro popup")]
        public GameObject IntroSkipProgress;
        public RectTransform IntroSkipProgressFill;

        [Header("Hold To Skip : ")]
        [Tooltip("Real-time seconds B must be held down to skip the tutorial")]
        public float SkipHoldDuration = 1f;

        [Tooltip("How fast the skip bar empties after letting go of B, in full bars per real-time second")]
        public float SkipDrainSpeed = 3f;

        [Tooltip("Shown while B is held. Hidden when the bar is empty")]
        public GameObject SkipProgress;

        [Tooltip("Fill of the skip bar. Its width follows the hold progress (left pivot)")]
        public RectTransform SkipProgressFill;

        [Tooltip("Light vibration on the right controller while B is held. 0 = off")]
        public float SkipHoldVibration = 0.1f;

        [Tooltip("'A Continue' hint. Not used by the current steps, kept hidden")]
        public GameObject ContinueHint;
        public TMP_Text ContinueHintText;

        [Header("Progress Dots : ")]
        [Tooltip("One dot per numbered step")]
        public Image[] StepDots;
        public Color DotCurrentColor = new Color(1f, 0.78f, 0.2f);
        public Color DotDoneColor = new Color(1f, 1f, 1f, 0.6f);
        public Color DotUpcomingColor = new Color(1f, 1f, 1f, 0.15f);

        [Header("Arrow : ")]
        public Transform Arrow;

        [Tooltip("What the arrow points at for each step. Leave empty for no arrow on that step")]
        public Transform LauncherAnchor;
        public Transform AmmoAnchor;
        public Transform TargetAnchor;
        public Transform ExplosiveAnchor;

        [Tooltip("What to fire the red ball at. Leave empty to pick the nearest bullseye that is still standing and isn't the one from the aim and fire step")]
        public Transform ExplosiveTargetAnchor;

        [Tooltip("Meters above the anchor")]
        public float ArrowHeight = 0.25f;
        public float ArrowBobAmount = 0.05f;
        public float ArrowBobSpeed = 4f;

        [Tooltip("Degrees per second")]
        public float ArrowSpinSpeed = 90f;

        [Tooltip("The arrow grows with distance so far targets stay easy to spot. Scale = distance * this, at least 1")]
        public float ArrowDistanceScale = 0.35f;

        [Tooltip("Seconds before the arrow appears when it gets something new to point at (and plays its sound), so it doesn't overlap the screen change")]
        public float ArrowDelay = 2f;

        [Header("Timing : ")]
        [Tooltip("Seconds after the game starts before the tutorial panel appears")]
        public float StartDelay = 5f;

        [Tooltip("Seconds the panel takes to fade in")]
        public float FadeInDuration = 0.8f;

        [Tooltip("Real-time seconds the feedback message shows before the next step")]
        public float StepDoneDelay = 1.2f;

        [Tooltip("Real-time seconds the final message shows before the round starts")]
        public float DoneDuration = 2.5f;

        [Header("Feedback : ")]
        [Tooltip("Plays when a step is completed (the 'Nice!' message). Leave empty for none")]
        public AudioClip StepDoneSound;
        public float StepDoneVolume = 0.8f;
        public float StepDoneVibration = 0.3f;

        [Header("Sounds : ")]
        [Tooltip("Plays when the panel changes to the next step")]
        public AudioClip ScreenChangeSound;
        public float ScreenChangeVolume = 0.8f;

        [Tooltip("Plays at the arrow when it appears (after Arrow Delay)")]
        public AudioClip ArrowSound;
        public float ArrowVolume = 0.8f;

        [Tooltip("0 = heard the same everywhere, 1 = comes from the arrow's direction")]
        [Range(0f, 1f)]
        public float ArrowSpatialBlend = 0.6f;

        [Tooltip("Plays when the tutorial is completed (the final 'You're ready!' screen). Not played when skipping")]
        public AudioClip CompleteSound;
        public float CompleteVolume = 1f;

        Step step = Step.PickUp;
        Grabbable launcherGrab;
        bool shotFired = false;
        bool explosiveFired = false;
        enum Phase {
            Waiting,    // start delay
            Intro,      // "Press A for tutorial" popup
            Running     // tutorial panel and steps
        }

        Phase phase = Phase.Waiting;
        float startTime;
        float phaseStartTime;
        CanvasGroup panelFade;
        CanvasGroup introFade;
        float introSkipFullWidth;
        bool stepComplete = false;
        float stepCompleteTime;
        float stepStartTime;
        float skipFullWidth;
        float skipHold;
        bool firstScreenShown;
        Transform lastArrowAnchor;
        float arrowShowTime = -1f;
        bool arrowShown;
        BoomTarget explosiveTarget;

        void Start() {
            if (!ShowEveryTime && PlayerPrefs.GetInt(CompletedKey, 0) == 1) {
                // Nothing has been used yet, so no need to restart the round
                finish(false, false);
                return;
            }

            if (Launcher != null) {
                launcherGrab = Launcher.GetComponent<Grabbable>();
                Launcher.onShootEvent.AddListener(onShoot);
            }

            if (ScoreManager != null) {
                ScoreManager.Paused = true;
            }

            if (Hud != null) {
                Hud.SetActive(false);
            }

            // Read both bars' full widths before updateSkipBar shrinks them to the (empty) hold progress
            if (SkipProgressFill != null) {
                skipFullWidth = SkipProgressFill.sizeDelta.x;
            }
            if (IntroSkipProgressFill != null) {
                introSkipFullWidth = IntroSkipProgressFill.sizeDelta.x;
            }
            updateSkipBar();

            setText(Feedback, FeedbackMessage);
            setText(SkipHintText, SkipMessage);
            setText(ContinueHintText, ContinueMessage);

            // Nothing shows during the start delay. Then the intro popup fades in, and the panel after A is pressed
            panelFade = hideWithFade(Panel);
            introFade = hideWithFade(IntroPopup);
            if (Arrow != null) {
                Arrow.gameObject.SetActive(false);
            }
            startTime = Time.unscaledTime;

            enterStep(Step.PickUp);
        }

        static CanvasGroup hideWithFade(GameObject target) {
            if (target == null) {
                return null;
            }
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null) {
                group = target.AddComponent<CanvasGroup>();
            }
            group.alpha = 0f;
            return group;
        }

        static void fadeIn(CanvasGroup group, float since, float duration) {
            if (group != null) {
                group.alpha = duration > 0f ? Mathf.Clamp01((Time.unscaledTime - since) / duration) : 1f;
            }
        }

        void showIntro() {
            phase = Phase.Intro;
            phaseStartTime = Time.unscaledTime;
            IntroPopup.SetActive(true);
            playPanelSound(ScreenChangeSound, ScreenChangeVolume);
        }

        /// <summary>
        /// Hides the intro popup and fades in the tutorial panel at its first step
        /// </summary>
        void beginTutorial() {
            bool fromIntro = phase == Phase.Intro;
            phase = Phase.Running;
            phaseStartTime = Time.unscaledTime;
            // Step timers count from when the player can see the panel
            stepStartTime = Time.unscaledTime;

            if (IntroPopup != null) {
                IntroPopup.SetActive(false);
            }
            if (fromIntro) {
                playPanelSound(ScreenChangeSound, ScreenChangeVolume);
            }
        }

        void OnDestroy() {
            if (Launcher != null) {
                Launcher.onShootEvent.RemoveListener(onShoot);
            }
        }

        void onShoot() {
            shotFired = true;
            if (Launcher != null && Launcher.ProjectilePrefab != null && Launcher.ProjectilePrefab == Launcher.ExplosiveProjectile) {
                explosiveFired = true;
            }
        }

        void Update() {
            if (step == Step.Finished) {
                return;
            }

            if (phase == Phase.Waiting) {
                if (Time.unscaledTime - startTime < StartDelay) {
                    return;
                }
                if (IntroPopup != null) {
                    showIntro();
                }
                else {
                    beginTutorial();
                }
            }

            // Skipping needs B held down until the bar is full, so a stray press can't skip by accident
            if (InputBridge.Instance.BButton) {
                skipHold += Time.unscaledDeltaTime / Mathf.Max(0.01f, SkipHoldDuration);
                if (SkipHoldVibration > 0f) {
                    InputBridge.Instance.VibrateController(0.1f, SkipHoldVibration * skipHold, 0.05f, ControllerHand.Right);
                }
            }
            else {
                skipHold -= Time.unscaledDeltaTime * SkipDrainSpeed;
            }
            skipHold = Mathf.Clamp01(skipHold);
            updateSkipBar();

            if (skipHold >= 1f) {
                finish(true, true);
                return;
            }

            if (phase == Phase.Intro) {
                fadeIn(introFade, phaseStartTime, FadeInDuration);
                // Ignore A until the popup is visible enough to read
                if (Time.unscaledTime - phaseStartTime >= FadeInDuration * 0.5f && InputBridge.Instance.AButtonDown) {
                    beginTutorial();
                }
                else {
                    return;
                }
            }

            fadeIn(panelFade, phaseStartTime, FadeInDuration);

            if (stepComplete) {
                if (Time.unscaledTime - stepCompleteTime >= StepDoneDelay) {
                    enterStep(step + 1);
                }
            }
            else if (isStepDone()) {
                completeStep();
            }

            updateArrow();
        }

        bool isStepDone() {
            switch (step) {
                case Step.PickUp:
                    return launcherGrab != null && launcherGrab.BeingHeld;
                case Step.Load:
                    return Launcher != null && Launcher.LoadedCount > 0;
                case Step.Shoot:
                    return shotFired;
                case Step.Explosives:
                    return explosiveFired;
                case Step.Done:
                    return Time.unscaledTime - stepStartTime >= DoneDuration;
            }
            return false;
        }

        void completeStep() {
            if (step == Step.Done) {
                finish(true, true);
                return;
            }

            stepComplete = true;
            stepCompleteTime = Time.unscaledTime;

            if (Feedback != null) {
                Feedback.gameObject.SetActive(true);
            }
            if (StepDoneSound) {
                VRUtils.Instance.PlaySpatialClipAt(StepDoneSound, Panel != null ? Panel.transform.position : transform.position, StepDoneVolume, 0f);
            }
            InputBridge.Instance.VibrateController(0.2f, StepDoneVibration, 0.1f, ControllerHand.Right);
        }

        void enterStep(Step newStep) {
            step = newStep;
            stepComplete = false;
            stepStartTime = Time.unscaledTime;

            // The final screen gets the completion sound, other screen changes the change sound. Nothing on the very first screen
            if (step == Step.Done) {
                playPanelSound(CompleteSound, CompleteVolume);
            }
            else if (firstScreenShown) {
                playPanelSound(ScreenChangeSound, ScreenChangeVolume);
            }
            firstScreenShown = true;

            if (Feedback != null) {
                Feedback.gameObject.SetActive(false);
            }
            if (ContinueHint != null) {
                ContinueHint.SetActive(false);
            }

            if (step == Step.Explosives) {
                explosiveFired = false;
                refillExplosivesIfUsed();
            }

            BoomTutorialText content = getText(step);
            if (content != null) {
                setText(Title, content.Title);
                setText(Body, content.Body);
            }

            if (step == Step.Done) {
                setText(StepCounter, StepCounterDoneText);
            }
            else {
                setText(StepCounter, string.Format(StepCounterFormat, (int)step + 1, NumberedStepCount));
            }

            updateDots();
        }

        BoomTutorialText getText(Step s) {
            switch (s) {
                case Step.PickUp:
                    return PickUpText;
                case Step.Load:
                    return LoadText;
                case Step.Shoot:
                    return ShootText;
                case Step.Explosives:
                    return ExplosivesText;
                case Step.Done:
                    return DoneText;
            }
            return null;
        }

        void updateDots() {
            if (StepDots == null) {
                return;
            }

            int current = (int)step;
            for (int x = 0; x < StepDots.Length; x++) {
                if (StepDots[x] == null) {
                    continue;
                }
                if (x < current) {
                    StepDots[x].color = DotDoneColor;
                }
                else if (x == current) {
                    StepDots[x].color = DotCurrentColor;
                }
                else {
                    StepDots[x].color = DotUpcomingColor;
                }
            }
        }

        void updateSkipBar() {
            if (SkipProgress != null) {
                SkipProgress.SetActive(skipHold > 0f);
            }
            if (SkipProgressFill != null) {
                Vector2 size = SkipProgressFill.sizeDelta;
                size.x = skipFullWidth * skipHold;
                SkipProgressFill.sizeDelta = size;
            }

            // Same hold progress on the intro popup's own skip bar
            if (IntroSkipProgress != null) {
                IntroSkipProgress.SetActive(skipHold > 0f);
            }
            if (IntroSkipProgressFill != null) {
                Vector2 size = IntroSkipProgressFill.sizeDelta;
                size.x = introSkipFullWidth * skipHold;
                IntroSkipProgressFill.sizeDelta = size;
            }
        }

        /// <summary>
        /// If the player already used both red balls in the earlier steps, refill the table so this step can be completed
        /// </summary>
        void refillExplosivesIfUsed() {
            bool explosiveLeft = BoomAmmoPickup.GetActiveCount(BoomAmmoType.Explosive) > 0
                || (Launcher != null && Launcher.GetLoadedCount(BoomAmmoType.Explosive) > 0);
            if (!explosiveLeft && ScoreManager != null && ScoreManager.AmmoRack != null) {
                ScoreManager.AmmoRack.ResetRack();
            }
        }

        void playPanelSound(AudioClip clip, float volume) {
            if (clip == null) {
                return;
            }
            VRUtils.Instance.PlaySpatialClipAt(clip, Panel != null ? Panel.transform.position : transform.position, volume, 0f);
        }

        static void setText(TMP_Text label, string value) {
            if (label != null) {
                label.text = value;
            }
        }

        Transform getAnchor() {
            switch (step) {
                case Step.PickUp:
                    return LauncherAnchor;
                case Step.Load:
                    return AmmoAnchor;
                case Step.Shoot:
                    return TargetAnchor;
                case Step.Explosives:
                    // Red ball tray first, then a target once a red ball is loaded
                    bool explosiveLoaded = Launcher != null && Launcher.NextAmmo.HasValue && Launcher.NextAmmo.Value == BoomAmmoType.Explosive;
                    return explosiveLoaded ? getExplosiveTarget() : ExplosiveAnchor;
            }
            return null;
        }

        /// <summary>
        /// A different, still standing target for the red ball, so the arrow never points at one that already fell
        /// </summary>
        Transform getExplosiveTarget() {
            if (ExplosiveTargetAnchor != null) {
                return ExplosiveTargetAnchor;
            }

            // Pick again if the chosen one was knocked over meanwhile
            if (explosiveTarget == null || explosiveTarget.Scored || !explosiveTarget.gameObject.activeInHierarchy) {
                explosiveTarget = pickStandingTarget();
            }
            if (explosiveTarget == null) {
                return TargetAnchor;
            }
            return explosiveTarget.BoardCenter != null ? explosiveTarget.BoardCenter : explosiveTarget.transform;
        }

        BoomTarget pickStandingTarget() {
            BoomTarget aimStepTarget = TargetAnchor != null ? TargetAnchor.GetComponentInParent<BoomTarget>() : null;
            Vector3 from = Camera.main != null ? Camera.main.transform.position : transform.position;

            BoomTarget best = null;
            float bestDistance = float.MaxValue;
            foreach (BoomTarget target in FindObjectsByType<BoomTarget>(FindObjectsSortMode.None)) {
                if (target.Scored || target == aimStepTarget || target.TargetType != BoomTargetType.Bullseye) {
                    continue;
                }
                float distance = Vector3.Distance(from, target.transform.position);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = target;
                }
            }
            return best;
        }

        void updateArrow() {
            if (Arrow == null) {
                return;
            }

            Transform anchor = stepComplete ? null : getAnchor();

            // Something new to point at : wait ArrowDelay before showing the arrow
            if (anchor != lastArrowAnchor) {
                lastArrowAnchor = anchor;
                arrowShown = false;
                arrowShowTime = anchor != null ? Time.unscaledTime + ArrowDelay : -1f;
            }

            bool visible = anchor != null && Time.unscaledTime >= arrowShowTime;
            Arrow.gameObject.SetActive(visible);
            if (!visible) {
                return;
            }

            Camera cam = Camera.main;
            float distance = cam != null ? Vector3.Distance(cam.transform.position, anchor.position) : 1f;
            float scale = Mathf.Max(1f, distance * ArrowDistanceScale);

            float bob = Mathf.Sin(Time.unscaledTime * ArrowBobSpeed) * ArrowBobAmount * scale;
            Arrow.position = anchor.position + Vector3.up * (ArrowHeight * scale + bob);
            Arrow.rotation = Quaternion.Euler(0, Time.unscaledTime * ArrowSpinSpeed, 0);
            Arrow.localScale = Vector3.one * scale;

            // Sound as the arrow appears, from where it is
            if (!arrowShown) {
                arrowShown = true;
                if (ArrowSound != null) {
                    VRUtils.Instance.PlaySpatialClipAt(ArrowSound, Arrow.position, ArrowVolume, ArrowSpatialBlend);
                }
            }
        }

        void finish(bool markCompleted, bool restartRound) {
            step = Step.Finished;

            if (markCompleted) {
                PlayerPrefs.SetInt(CompletedKey, 1);
                PlayerPrefs.Save();
            }

            if (Panel != null) {
                Panel.SetActive(false);
            }
            if (IntroPopup != null) {
                IntroPopup.SetActive(false);
            }
            if (Arrow != null) {
                Arrow.gameObject.SetActive(false);
            }
            if (Hud != null) {
                Hud.SetActive(true);
            }
            // Start the real round with everything refilled
            if (ScoreManager != null) {
                ScoreManager.Paused = false;
                if (restartRound) {
                    ScoreManager.RestartRound();
                }
            }
        }
    }
}
