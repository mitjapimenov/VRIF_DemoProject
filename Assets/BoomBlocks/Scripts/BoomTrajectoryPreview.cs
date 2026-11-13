using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Aim line : draws the ball's flight path from the weapon's muzzle using the same speed and gravity as the real shot.
    /// By default only shown while the launcher is held with both hands, as a reward for aiming properly.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BoomTrajectoryPreview : MonoBehaviour {

        public RaycastWeapon Weapon;

        [Tooltip("Only draw the line while the weapon is held")]
        public bool OnlyWhenHeld = true;

        [Tooltip("Only draw the line while the launcher is held with both hands (grip and foregrip)")]
        public bool OnlyWhenTwoHanded = true;

        [Tooltip("Tells when both hands are on the launcher. Found on this object if left empty")]
        public BoomLauncherHandling Handling;

        public int MaxSteps = 80;

        [Tooltip("Seconds of flight between line points")]
        public float TimeStep = 0.04f;

        public LayerMask HitLayers = ~0;

        LineRenderer line;
        Grabbable grab;
        Vector3[] points;

        void Awake() {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            grab = GetComponent<Grabbable>();
            points = new Vector3[MaxSteps + 1];

            if (Weapon == null) {
                Weapon = GetComponent<RaycastWeapon>();
            }
            if (Handling == null) {
                Handling = GetComponent<BoomLauncherHandling>();
            }
        }

        void LateUpdate() {
            Transform muzzle = Weapon != null ? Weapon.GetMuzzlePointTransform() : null;
            bool show = muzzle != null
                && (!OnlyWhenHeld || (grab != null && grab.BeingHeld))
                && (!OnlyWhenTwoHanded || (Handling != null && Handling.TwoHanded));

            line.enabled = show;
            if (!show) {
                return;
            }

            Vector3 position = muzzle.position;
            Vector3 velocity = muzzle.forward * Weapon.ShotForce;
            Vector3 gravity = Physics.gravity;

            int count = 0;
            points[count++] = position;

            for (int x = 0; x < MaxSteps; x++) {
                // Same order as the physics engine : velocity first, then position
                velocity += gravity * TimeStep;
                Vector3 next = position + velocity * TimeStep;

                RaycastHit hit;
                if (Physics.Linecast(position, next, out hit, HitLayers, QueryTriggerInteraction.Ignore)) {
                    points[count++] = hit.point;
                    break;
                }

                points[count++] = next;
                position = next;
            }

            line.positionCount = count;
            line.SetPositions(points);
        }
    }
}
