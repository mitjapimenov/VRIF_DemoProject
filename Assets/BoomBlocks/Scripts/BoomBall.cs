using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Ball fired from the launcher. The first thing it hits decides whether a Bullseye target scores.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoomBall : MonoBehaviour {

        /// <summary>
        /// Number of balls currently in the scene. Used to wait for the last shot before ending the round.
        /// </summary>
        public static int ActiveCount { get; private set; }

        [Tooltip("Destroy the ball after this many seconds if it never hits anything")]
        public float LifeTime = 6f;

        [Tooltip("Destroy the ball this many seconds after its first impact")]
        public float LifeTimeAfterImpact = 2.5f;

        bool hasImpacted = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void resetStatics() {
            ActiveCount = 0;
        }

        void OnEnable() {
            ActiveCount++;
        }

        void OnDisable() {
            ActiveCount--;
        }

        void Start() {
            Destroy(gameObject, LifeTime);
        }

        void OnCollisionEnter(Collision collision) {
            // Only the first impact counts. No bank shots
            if (hasImpacted) {
                return;
            }

            hasImpacted = true;

            ContactPoint contact = collision.GetContact(0);
            BoomVfx.Play(BoomVfxType.Impact, contact.point, Quaternion.LookRotation(contact.normal));

            BoomTarget target = collision.collider.GetComponentInParent<BoomTarget>();
            if (target != null) {
                target.OnBallHit(contact.point);
            }

            Destroy(gameObject, LifeTimeAfterImpact);
        }
    }
}
