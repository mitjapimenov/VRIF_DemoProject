using UnityEngine;

namespace BoomBlocks {

    public enum BoomVfxType {
        Explosion,
        Impact,
        Muzzle,
        Score
    }

    /// <summary>
    /// Holds the particle effect prefabs for the scene. Play effects anywhere with BoomVfx.Play
    /// </summary>
    public class BoomVfx : MonoBehaviour {

        static BoomVfx instance;

        [Tooltip("Fireball, smoke and sparks")]
        public GameObject Explosion;

        [Tooltip("Dust puff where a ball first hits something")]
        public GameObject Impact;

        [Tooltip("Smoke puff and flash at the muzzle when firing")]
        public GameObject Muzzle;

        [Tooltip("Confetti burst when a target scores")]
        public GameObject Score;

        [Tooltip("Spawned effects are destroyed after this many (scaled) seconds")]
        public float Lifetime = 4f;

        void Awake() {
            instance = this;
        }

        /// <summary>
        /// Spawns an effect. Scale enlarges the whole effect, e.g. for bigger explosions
        /// </summary>
        public static void Play(BoomVfxType type, Vector3 position, Quaternion rotation, float scale = 1f) {
            if (instance == null) {
                return;
            }

            GameObject prefab = instance.getPrefab(type);
            if (prefab == null) {
                return;
            }

            GameObject fx = Instantiate(prefab, position, rotation);
            fx.transform.localScale = Vector3.one * scale;
            Destroy(fx, instance.Lifetime);
        }

        GameObject getPrefab(BoomVfxType type) {
            switch (type) {
                case BoomVfxType.Explosion:
                    return Explosion;
                case BoomVfxType.Impact:
                    return Impact;
                case BoomVfxType.Muzzle:
                    return Muzzle;
                default:
                    return Score;
            }
        }
    }
}
