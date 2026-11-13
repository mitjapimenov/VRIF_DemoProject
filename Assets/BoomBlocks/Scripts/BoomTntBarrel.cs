using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// TNT barrel : explodes when hit by a ball, struck hard enough by anything (a falling pillar..) or caught in another explosion
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoomTntBarrel : MonoBehaviour {

        [Tooltip("Points for blowing up this barrel. Counts towards the combo")]
        public int Points = 50;

        [Tooltip("Explode when something hits the barrel at least this fast (m/s). Balls always set it off")]
        public float ImpactSpeed = 1.5f;

        [Tooltip("Ignore impacts this many seconds after the round starts, while everything settles")]
        public float ArmDelay = 0.5f;

        [Tooltip("Delay before exploding when set off by another explosion, so chains ripple instead of going off all at once")]
        public float ChainDelay = 0.15f;

        [Header("Explosion : ")]
        public float Radius = 2f;
        public float Force = 30f;
        public float UpwardsModifier = 0.5f;
        public AudioClip ExplosionSound;
        public Material FlashMaterial;

        public bool IsExploded { get; private set; }

        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody rigid;
        float armTime;
        bool chainPending = false;

        void Awake() {
            startPosition = transform.position;
            startRotation = transform.rotation;
            rigid = GetComponent<Rigidbody>();
            armTime = Time.time + ArmDelay;
        }

        void OnCollisionEnter(Collision collision) {
            if (IsExploded || Time.time < armTime) {
                return;
            }

            bool hitByBall = collision.collider.GetComponentInParent<BoomBall>() != null;
            if (hitByBall || collision.relativeVelocity.magnitude >= ImpactSpeed) {
                Explode();
            }
        }

        /// <summary>
        /// Called when caught in another explosion
        /// </summary>
        public void TriggerChain() {
            if (IsExploded || chainPending) {
                return;
            }

            chainPending = true;
            Invoke(nameof(Explode), ChainDelay);
        }

        public void Explode() {
            if (IsExploded) {
                return;
            }

            IsExploded = true;
            chainPending = false;
            CancelInvoke();

            Vector3 position = rigid.worldCenterOfMass;
            BoomExplosion.Detonate(position, Radius, Force, UpwardsModifier, ExplosionSound, FlashMaterial, rigid);

            if (BoomScoreManager.Instance != null) {
                BoomScoreManager.Instance.AddPoints(Points, position);
            }

            // Hidden instead of destroyed so a new round can bring it back
            gameObject.SetActive(false);
        }

        public void ResetBarrel() {
            CancelInvoke();
            IsExploded = false;
            chainPending = false;

            gameObject.SetActive(true);
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            rigid.position = startPosition;
            rigid.rotation = startRotation;
            transform.SetPositionAndRotation(startPosition, startRotation);

            armTime = Time.time + ArmDelay;
        }
    }
}
