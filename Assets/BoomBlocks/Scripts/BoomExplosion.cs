using System.Collections.Generic;
using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Shared explosion : pushes nearby Rigidbodies, sets off TNT barrels in range, plays sound and flash
    /// </summary>
    public static class BoomExplosion {

        public static void Detonate(Vector3 position, float radius, float force, float upwardsModifier, AudioClip sound, Material flashMaterial, Rigidbody ignore = null) {
            HashSet<Rigidbody> pushed = new HashSet<Rigidbody>();

            Collider[] hits = Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int x = 0; x < hits.Length; x++) {
                // Chain reaction
                BoomTntBarrel barrel = hits[x].GetComponentInParent<BoomTntBarrel>();
                if (barrel != null) {
                    barrel.TriggerChain();
                }

                Rigidbody rb = hits[x].attachedRigidbody;
                if (rb == null || rb == ignore || rb.isKinematic || !pushed.Add(rb)) {
                    continue;
                }

                rb.AddExplosionForce(force, position, radius, upwardsModifier, ForceMode.Impulse);
            }

            if (sound) {
                VRUtils.Instance.PlaySpatialClipAt(sound, position, 1f);
            }

            if (flashMaterial != null) {
                BoomExplosionFlash.Spawn(position, radius, flashMaterial);
            }

            // Effect is authored for a 1.5 m blast
            BoomVfx.Play(BoomVfxType.Explosion, position, Quaternion.identity, radius / 1.5f);
        }
    }
}
