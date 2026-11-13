using UnityEngine;

namespace BoomBlocks {

    public enum BoomTargetType {
        Bullseye,
        Block
    }

    /// <summary>
    /// Awards points once when hit by a BoomBall (Bullseye) or when knocked over (Block)
    /// </summary>
    public class BoomTarget : MonoBehaviour {

        public BoomTargetType TargetType = BoomTargetType.Bullseye;

        [Tooltip("Base points awarded for this target")]
        public int Points = 100;

        [Header("Bullseye : ")]
        [Tooltip("Extra points for hitting the exact center. Falls off linearly to 0 at Radius")]
        public int MaxPrecisionBonus = 100;

        [Tooltip("Radius of the board in world units")]
        public float Radius = 0.5f;

        [Tooltip("Center of the board. Its local up axis is the board normal. Defaults to this transform")]
        public Transform BoardCenter;

        [Tooltip("Also award the base points if the board is knocked over without a direct hit")]
        public bool ScoreWhenToppled = false;

        [Header("Topple : ")]
        [Tooltip("Block scores when tilted more than this many degrees from its starting rotation")]
        public float ToppleAngle = 45f;

        [Tooltip("Block scores when it has dropped this many meters below its starting position")]
        public float FallDistance = 0.3f;

        [Header("Feedback : ")]
        [Tooltip("These renderers are tinted with HitColor once the target has scored")]
        public Renderer[] HitRenderers;

        public Color HitColor = new Color(0.35f, 0.35f, 0.35f);

        public bool Scored { get; private set; }

        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody rigid;
        Color[] originalColors;

        void Awake() {
            startPosition = transform.position;
            startRotation = transform.rotation;
            rigid = GetComponent<Rigidbody>();

            if (HitRenderers == null || HitRenderers.Length == 0) {
                HitRenderers = GetComponentsInChildren<Renderer>();
            }

            originalColors = new Color[HitRenderers.Length];
            for (int x = 0; x < HitRenderers.Length; x++) {
                originalColors[x] = HitRenderers[x].material.color;
            }
        }

        void Update() {
            bool canTopple = TargetType == BoomTargetType.Block || ScoreWhenToppled;
            if (Scored || !canTopple) {
                return;
            }

            bool toppled = Vector3.Angle(transform.up, startRotation * Vector3.up) > ToppleAngle;
            bool fell = startPosition.y - transform.position.y > FallDistance;

            if (toppled || fell) {
                award(Points, transform.position);
            }
        }

        /// <summary>
        /// Called by BoomBall on its first impact
        /// </summary>
        /// <returns>True if this hit awarded points</returns>
        public bool OnBallHit(Vector3 hitPoint) {
            if (Scored || TargetType != BoomTargetType.Bullseye) {
                return false;
            }

            // Distance from the center measured along the board's surface
            Transform center = BoardCenter != null ? BoardCenter : transform;
            Vector3 offset = Vector3.ProjectOnPlane(hitPoint - center.position, center.up);
            float precision = Mathf.Clamp01(1f - offset.magnitude / Radius);

            award(Points + Mathf.RoundToInt(MaxPrecisionBonus * precision), hitPoint);
            return true;
        }

        public void ResetTarget() {
            Scored = false;

            if (rigid != null) {
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.position = startPosition;
                rigid.rotation = startRotation;
            }
            transform.SetPositionAndRotation(startPosition, startRotation);

            for (int x = 0; x < HitRenderers.Length; x++) {
                HitRenderers[x].material.color = originalColors[x];
            }
        }

        void award(int amount, Vector3 position) {
            Scored = true;

            for (int x = 0; x < HitRenderers.Length; x++) {
                HitRenderers[x].material.color = HitColor;
            }

            BoomVfx.Play(BoomVfxType.Score, position, Quaternion.identity);

            if (BoomScoreManager.Instance != null) {
                BoomScoreManager.Instance.AddPoints(amount, position);
            }
        }
    }
}
