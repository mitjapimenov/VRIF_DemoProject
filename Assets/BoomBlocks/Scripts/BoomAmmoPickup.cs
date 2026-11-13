using UnityEngine;

namespace BoomBlocks {

    public enum BoomAmmoType {
        Normal,
        Explosive
    }

    /// <summary>
    /// A grabbable ball the player loads into the launcher by pushing it into the muzzle.
    /// If it is dropped and falls off the table it returns to where it started, so the player never has to pick it up from the floor.
    /// </summary>
    public class BoomAmmoPickup : MonoBehaviour {

        public BoomAmmoType AmmoType = BoomAmmoType.Normal;

        [Tooltip("Return home once the ball is this many meters below its starting position")]
        public float FallDistance = 0.3f;

        [Tooltip("Seconds the ball stays fallen before returning, so the player sees it drop")]
        public float ReturnDelay = 0.75f;

        Vector3 homePosition;
        Quaternion homeRotation;
        Rigidbody rigid;
        BNG.Grabbable grab;
        float fallenTime = -1f;

        static int[] activeCounts = new int[System.Enum.GetValues(typeof(BoomAmmoType)).Length];

        /// <summary>
        /// Number of loose pickups of this type left in the scene
        /// </summary>
        public static int GetActiveCount(BoomAmmoType type) {
            return activeCounts[(int)type];
        }

        public static int GetTotalActiveCount() {
            int total = 0;
            for (int x = 0; x < activeCounts.Length; x++) {
                total += activeCounts[x];
            }
            return total;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void resetStatics() {
            activeCounts = new int[System.Enum.GetValues(typeof(BoomAmmoType)).Length];
        }

        void Awake() {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            rigid = GetComponent<Rigidbody>();
            grab = GetComponent<BNG.Grabbable>();
        }

        void Update() {
            bool held = grab != null && grab.BeingHeld;
            bool fallen = !held && homePosition.y - transform.position.y > FallDistance;

            if (!fallen) {
                fallenTime = -1f;
            }
            else if (fallenTime < 0f) {
                fallenTime = Time.time;
            }
            else if (Time.time - fallenTime >= ReturnDelay) {
                ReturnHome();
            }
        }

        public void ReturnHome() {
            fallenTime = -1f;

            if (rigid != null) {
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.position = homePosition;
                rigid.rotation = homeRotation;
            }
            transform.SetPositionAndRotation(homePosition, homeRotation);

            BoomVfx.Play(BoomVfxType.Impact, homePosition, Quaternion.LookRotation(Vector3.up));
        }

        void OnEnable() {
            activeCounts[(int)AmmoType]++;
        }

        void OnDisable() {
            activeCounts[(int)AmmoType]--;
        }
    }
}
