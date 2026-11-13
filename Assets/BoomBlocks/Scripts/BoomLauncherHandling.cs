using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Makes the launcher feel heavy in one hand : it follows the hand slowly and the muzzle droops.
    /// Holding the foregrip with the other hand steadies it, and the barrel aims along the line from the grip hand to the foregrip hand.
    /// Runs after the Grabbable so it can add to / replace the Grabbable's rotation each physics step.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class BoomLauncherHandling : MonoBehaviour {

        public Grabbable Grab;

        [Tooltip("Second hand grab point under the barrel")]
        public Grabbable Foregrip;

        [Header("One Hand : ")]
        [Tooltip("How strongly the launcher follows the hand's position. Lower = heavier and laggier")]
        public float OneHandPositionForce = 1000f;

        [Tooltip("How strongly the launcher follows the hand's rotation. Lower = heavier and slower to aim")]
        public float OneHandRotationForce = 10f;

        [Tooltip("How much the muzzle droops when held in one hand. 0 = no droop. With rotation force 10, 0.8 droops about 5 degrees")]
        public float OneHandDroop = 0.8f;

        [Header("Two Hands : ")]
        public float TwoHandPositionForce = 3000f;

        [Tooltip("How strongly the barrel turns towards the line between the hands")]
        public float TwoHandRotationForce = 90f;

        Rigidbody rigid;
        bool wasTwoHanded;

        // Direction from the grip hand to the foregrip hand in the launcher's own space, recorded when the foregrip is grabbed
        Vector3 localHandAxis = Vector3.forward;

        /// <summary>
        /// True while both the grip and the foregrip are held
        /// </summary>
        public bool TwoHanded {
            get { return Grab != null && Grab.BeingHeld && Foregrip != null && Foregrip.BeingHeld; }
        }

        void Awake() {
            if (Grab == null) {
                Grab = GetComponent<Grabbable>();
            }
            rigid = GetComponent<Rigidbody>();
        }

        void Update() {
            if (Grab == null) {
                return;
            }

            bool twoHanded = TwoHanded;
            Grab.MoveVelocityForce = twoHanded ? TwoHandPositionForce : OneHandPositionForce;
            Grab.MoveAngularVelocityForce = twoHanded ? TwoHandRotationForce : OneHandRotationForce;
        }

        void FixedUpdate() {
            if (rigid == null || Grab == null || !Grab.BeingHeld) {
                wasTwoHanded = false;
                return;
            }

            if (TwoHanded) {
                aimWithTwoHands();
                return;
            }

            wasTwoHanded = false;

            // Pitch the muzzle down around the launcher's right axis. The Grabbable pulls it back up,
            // so it settles a few degrees low, more when the rotation force is weak
            if (OneHandDroop > 0f) {
                rigid.angularVelocity += transform.right * OneHandDroop;
            }
        }

        void aimWithTwoHands() {
            Grabber primary = Grab.GetPrimaryGrabber();
            Grabber secondary = Foregrip.GetPrimaryGrabber();
            if (primary == null || secondary == null) {
                return;
            }

            Vector3 primaryPos = primary.transform.position;
            Vector3 secondaryPos = secondary.transform.position;

            if (!wasTwoHanded) {
                wasTwoHanded = true;
                Vector3 local = transform.InverseTransformPoint(secondaryPos) - transform.InverseTransformPoint(primaryPos);
                localHandAxis = local.sqrMagnitude > 0.0001f ? local.normalized : Vector3.forward;
            }

            Vector3 handAxis = secondaryPos - primaryPos;
            if (handAxis.sqrMagnitude < 0.0001f) {
                return;
            }

            // Start from the grip hand's rotation (keeps its roll), then turn it so the hands line up again
            Quaternion oneHand = Grab.GetGrabbersAveragedRotation();
            Quaternion target = Quaternion.FromToRotation(oneHand * localHandAxis, handAxis.normalized) * oneHand;

            Quaternion delta = target * Quaternion.Inverse(rigid.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) {
                angle -= 360f;
            }
            if (float.IsNaN(axis.x) || Mathf.Abs(angle) < 0.5f) {
                rigid.angularVelocity = Vector3.zero;
                return;
            }

            // Same response as the Grabbable's own velocity rotation, so one and two hands feel related
            rigid.angularVelocity = axis * angle * TwoHandRotationForce * Time.fixedDeltaTime;
        }
    }
}
