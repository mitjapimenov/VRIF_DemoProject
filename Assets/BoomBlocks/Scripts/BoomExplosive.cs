using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Explosive ball : explodes on first contact, pushing every nearby Rigidbody away from the impact point
    /// </summary>
    public class BoomExplosive : MonoBehaviour {

        public float Radius = 1.5f;

        [Tooltip("Impulse applied at the center of the explosion. Falls off towards Radius")]
        public float Force = 20f;

        [Tooltip("Lifts objects slightly so they tip over instead of only sliding")]
        public float UpwardsModifier = 0.5f;

        public AudioClip ExplosionSound;

        [Tooltip("Material for the expanding flash sphere. Should be transparent so it can fade out")]
        public Material FlashMaterial;

        bool hasExploded = false;

        void OnCollisionEnter(Collision collision) {
            if (hasExploded) {
                return;
            }

            hasExploded = true;
            Explode(collision.GetContact(0).point);
        }

        public void Explode(Vector3 position) {
            BoomExplosion.Detonate(position, Radius, Force, UpwardsModifier, ExplosionSound, FlashMaterial, GetComponent<Rigidbody>());
            Destroy(gameObject);
        }
    }
}
