using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoomBlocks {

    /// <summary>
    /// Desktop camera for recording a trailer. It draws only to the Game view / monitor (never to the headset),
    /// so the VR rig keeps working as normal while you film.
    ///
    /// In Play mode, with the Game view focused :
    ///   C            show / hide the trailer camera in the Game view
    ///   Right mouse  hold to look around
    ///   W A S D      move,  Q / E  down / up,  Shift  faster
    ///   Mouse wheel  movement speed,  Z / X  zoom (field of view)
    ///   R            start / stop recording the camera's movement (saved as an animation clip in the editor)
    ///   P            play back the Playback Clip (or the latest recording)
    ///   O            orbit on / off (when an Orbit Target is set)
    ///
    /// Orbit : drag any object into Orbit Target and the camera locks onto it and circles around it, following it if it moves.
    ///   Right mouse  drag to move around the target by hand,  W / S  closer / further,  Q / E  lower / higher
    ///   A / D        slower / faster orbit (through zero to reverse),  Mouse wheel  zoom in / out
    ///
    /// Recordings are normal animation clips : open them in the Animation window to smooth or tweak keyframes,
    /// or animate this object by hand there and play it with P.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoomTrailerCamera : MonoBehaviour {

        [Header("View : ")]
        [Tooltip("Show the trailer camera in the Game view when Play starts")]
        public bool ActiveOnStart = false;

        [Tooltip("Drawn on top of the VR mirror view while active")]
        public float CameraDepth = 100f;

        [Header("Fly : ")]
        public float MoveSpeed = 2f;
        public float FastMultiplier = 3f;
        public float LookSensitivity = 0.15f;

        [Tooltip("Smooths movement and turning for a cinematic feel. 0 = instant, higher = smoother")]
        [Range(0f, 0.95f)]
        public float Smoothing = 0.85f;

        public float ZoomSpeed = 30f;
        public Vector2 FieldOfViewRange = new Vector2(20f, 90f);

        [Header("Orbit : ")]
        [Tooltip("Drag an object here to lock onto it and circle around it. Empty = free flying")]
        public Transform OrbitTarget;

        [Tooltip("Turn orbiting off without clearing the target (also the O key)")]
        public bool OrbitEnabled = true;

        [Tooltip("Degrees per second around the target. Negative goes the other way, 0 stays still")]
        public float OrbitSpeed = 15f;

        [Tooltip("Point to look at, relative to the target (e.g. raise it to aim at the middle of a tall structure)")]
        public Vector3 OrbitTargetOffset = new Vector3(0f, 0.5f, 0f);

        [Tooltip("How fast W / S and Q / E change the distance and height, in meters per second")]
        public float OrbitAdjustSpeed = 1.5f;

        public float MinOrbitDistance = 0.5f;

        [Header("Recording : ")]
        [Tooltip("Samples per second while recording")]
        public int RecordRate = 30;

        [Tooltip("Project folder for recorded clips (editor only)")]
        public string RecordFolder = "Assets/BoomBlocks/Trailer";

        [Tooltip("Clip played with P. Recording fills this with the newest take")]
        public AnimationClip PlaybackClip;

        [Tooltip("Loop the playback")]
        public bool LoopPlayback = false;

        Camera cam;
        bool viewActive;

        // Smoothed look and move
        float yaw;
        float pitch;
        Vector3 velocity;

        // Recording
        bool recording;
        float recordTime;
        float nextSampleTime;
        readonly List<float> times = new List<float>();
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Quaternion> rotations = new List<Quaternion>();
        readonly List<float> fovs = new List<float>();

        // Playback
        bool playing;
        float playTime;

        // Orbit, around the target's focus point
        Transform orbitingTarget;
        float orbitYaw;
        float orbitPitch;
        float orbitDistance;

        public bool IsRecording { get { return recording; } }
        public bool IsPlaying { get { return playing; } }

        void Awake() {
            cam = GetComponent<Camera>();
            // Never render to the headset, only to the monitor / Game view
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.depth = CameraDepth;

            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;

            setView(ActiveOnStart);
        }

        void Update() {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null) {
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame) {
                setView(!viewActive);
            }
            if (!viewActive) {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame) {
                if (recording) {
                    stopRecording();
                }
                else {
                    startRecording();
                }
            }
            if (keyboard.pKey.wasPressedThisFrame && !recording) {
                if (playing) {
                    playing = false;
                }
                else if (PlaybackClip != null) {
                    playing = true;
                    playTime = 0f;
                }
            }

            if (keyboard.oKey.wasPressedThisFrame) {
                OrbitEnabled = !OrbitEnabled;
            }

            if (playing) {
                updatePlayback();
            }
            else if (OrbitEnabled && OrbitTarget != null) {
                updateOrbit(keyboard, mouse);
            }
            else {
                if (orbitingTarget != null) {
                    leaveOrbit();
                }
                updateFly(keyboard, mouse);
            }

            if (recording) {
                updateRecording();
            }
        }

        void setView(bool active) {
            viewActive = active;
            cam.enabled = active;
            if (!active) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void updateFly(Keyboard keyboard, Mouse mouse) {
            float dt = Time.unscaledDeltaTime;
            float smooth = 1f - Mathf.Pow(Smoothing, dt * 60f);

            // Look while the right mouse button is held
            bool looking = mouse.rightButton.isPressed;
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            if (looking) {
                Vector2 delta = mouse.delta.ReadValue() * LookSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -89f, 89f);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, yaw, 0f), smooth);

            // Speed with the mouse wheel
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) {
                MoveSpeed = Mathf.Clamp(MoveSpeed * (scroll > 0 ? 1.15f : 1f / 1.15f), 0.1f, 50f);
            }

            Vector3 input = Vector3.zero;
            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.eKey.isPressed) input.y += 1f;
            if (keyboard.qKey.isPressed) input.y -= 1f;

            float speed = MoveSpeed * (keyboard.leftShiftKey.isPressed ? FastMultiplier : 1f);
            Vector3 target = (transform.right * input.x + Vector3.up * input.y + transform.forward * input.z) * speed;
            velocity = Vector3.Lerp(velocity, target, smooth);
            transform.position += velocity * dt;

            // Zoom
            float zoom = 0f;
            if (keyboard.zKey.isPressed) zoom -= 1f;
            if (keyboard.xKey.isPressed) zoom += 1f;
            if (zoom != 0f) {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + zoom * ZoomSpeed * dt, FieldOfViewRange.x, FieldOfViewRange.y);
            }
        }

        // ---------------- Orbit ----------------

        Vector3 orbitFocus() {
            return OrbitTarget.position + OrbitTargetOffset;
        }

        /// <summary>
        /// Starts orbiting from wherever the camera is now, so it doesn't jump
        /// </summary>
        void enterOrbit() {
            orbitingTarget = OrbitTarget;
            Vector3 offset = transform.position - orbitFocus();
            orbitDistance = Mathf.Max(MinOrbitDistance, offset.magnitude);
            orbitPitch = Mathf.Asin(Mathf.Clamp(offset.y / orbitDistance, -1f, 1f)) * Mathf.Rad2Deg;
            orbitYaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Back to free flying, looking the way the camera faces now
        /// </summary>
        void leaveOrbit() {
            orbitingTarget = null;
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            velocity = Vector3.zero;
        }

        void updateOrbit(Keyboard keyboard, Mouse mouse) {
            if (orbitingTarget != OrbitTarget) {
                enterOrbit();
            }

            float dt = Time.unscaledDeltaTime;
            float smooth = 1f - Mathf.Pow(Smoothing, dt * 60f);

            orbitYaw += OrbitSpeed * dt;

            // Move around the target by hand
            bool looking = mouse.rightButton.isPressed;
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            if (looking) {
                Vector2 delta = mouse.delta.ReadValue() * LookSensitivity;
                orbitYaw += delta.x;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y, -80f, 85f);
            }

            float speed = OrbitAdjustSpeed * (keyboard.leftShiftKey.isPressed ? FastMultiplier : 1f);
            if (keyboard.wKey.isPressed) orbitDistance -= speed * dt;
            if (keyboard.sKey.isPressed) orbitDistance += speed * dt;
            orbitDistance = Mathf.Max(MinOrbitDistance, orbitDistance);

            // Q / E move the camera height : convert to a pitch change at this distance
            float height = 0f;
            if (keyboard.eKey.isPressed) height += 1f;
            if (keyboard.qKey.isPressed) height -= 1f;
            if (height != 0f) {
                orbitPitch = Mathf.Clamp(orbitPitch + height * speed * dt / orbitDistance * Mathf.Rad2Deg, -80f, 85f);
            }

            if (keyboard.aKey.isPressed) OrbitSpeed -= 20f * dt;
            if (keyboard.dKey.isPressed) OrbitSpeed += 20f * dt;

            // Wheel zooms here, since movement speed isn't used while orbiting
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - Mathf.Sign(scroll) * 2f, FieldOfViewRange.x, FieldOfViewRange.y);
            }
            float zoom = 0f;
            if (keyboard.zKey.isPressed) zoom -= 1f;
            if (keyboard.xKey.isPressed) zoom += 1f;
            if (zoom != 0f) {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + zoom * ZoomSpeed * dt, FieldOfViewRange.x, FieldOfViewRange.y);
            }

            // Follow the target, smoothly
            Vector3 focus = orbitFocus();
            Vector3 desired = focus + Quaternion.Euler(orbitPitch, orbitYaw, 0f) * (Vector3.back * orbitDistance);
            transform.position = Vector3.Lerp(transform.position, desired, smooth);
            Vector3 look = focus - transform.position;
            if (look.sqrMagnitude > 0.0001f) {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), smooth);
            }
        }

        // ---------------- Recording ----------------

        void startRecording() {
            playing = false;
            recording = true;
            recordTime = 0f;
            nextSampleTime = 0f;
            times.Clear();
            positions.Clear();
            rotations.Clear();
            fovs.Clear();
            Debug.Log("Boom Blocks trailer : recording...");
        }

        void updateRecording() {
            recordTime += Time.unscaledDeltaTime;
            if (recordTime < nextSampleTime) {
                return;
            }
            nextSampleTime = recordTime + 1f / Mathf.Max(1, RecordRate);

            times.Add(recordTime);
            positions.Add(transform.localPosition);
            rotations.Add(transform.localRotation);
            fovs.Add(cam.fieldOfView);
        }

        void stopRecording() {
            recording = false;
            if (times.Count < 2) {
                Debug.Log("Boom Blocks trailer : recording too short, nothing saved");
                return;
            }

            AnimationClip clip = buildClip();
            PlaybackClip = clip;
            saveClip(clip);
        }

        AnimationClip buildClip() {
            AnimationClip clip = new AnimationClip { name = "TrailerTake", frameRate = RecordRate };

            string[] posNames = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };
            string[] rotNames = { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
            AnimationCurve[] pos = { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            AnimationCurve[] rot = { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            AnimationCurve fov = new AnimationCurve();

            Quaternion previous = rotations[0];
            for (int i = 0; i < times.Count; i++) {
                float t = times[i] - times[0];
                Vector3 p = positions[i];
                Quaternion q = rotations[i];
                // Keep quaternions on the same side so interpolation doesn't spin the long way round
                if (Quaternion.Dot(previous, q) < 0f) {
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                }
                previous = q;

                pos[0].AddKey(t, p.x);
                pos[1].AddKey(t, p.y);
                pos[2].AddKey(t, p.z);
                rot[0].AddKey(t, q.x);
                rot[1].AddKey(t, q.y);
                rot[2].AddKey(t, q.z);
                rot[3].AddKey(t, q.w);
                fov.AddKey(t, fovs[i]);
            }

            for (int i = 0; i < 3; i++) {
                clip.SetCurve("", typeof(Transform), posNames[i], pos[i]);
            }
            for (int i = 0; i < 4; i++) {
                clip.SetCurve("", typeof(Transform), rotNames[i], rot[i]);
            }
            clip.SetCurve("", typeof(Camera), "field of view", fov);
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        void saveClip(AnimationClip clip) {
#if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.IsValidFolder(RecordFolder)) {
                int slash = RecordFolder.LastIndexOf('/');
                UnityEditor.AssetDatabase.CreateFolder(RecordFolder.Substring(0, slash), RecordFolder.Substring(slash + 1));
            }
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(RecordFolder + "/TrailerTake.anim");
            UnityEditor.AssetDatabase.CreateAsset(clip, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Boom Blocks trailer : saved " + path + " (" + clip.length.ToString("0.0") + " s). Press P to play it");
#else
            Debug.Log("Boom Blocks trailer : recorded " + clip.length.ToString("0.0") + " s. Press P to play it");
#endif
        }

        // ---------------- Playback ----------------

        void updatePlayback() {
            if (PlaybackClip == null) {
                playing = false;
                return;
            }

            playTime += Time.unscaledDeltaTime;
            if (playTime > PlaybackClip.length) {
                if (LoopPlayback) {
                    playTime %= Mathf.Max(0.01f, PlaybackClip.length);
                }
                else {
                    playTime = PlaybackClip.length;
                    playing = false;
                }
            }

            PlaybackClip.SampleAnimation(gameObject, playTime);

            // Continue flying (or orbiting) from where the playback ended
            orbitingTarget = null;
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            velocity = Vector3.zero;
        }
    }
}
