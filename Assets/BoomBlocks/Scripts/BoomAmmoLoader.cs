using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Trigger at the launcher's muzzle. A held BoomAmmoPickup pushed into it is loaded into the launcher
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoomAmmoLoader : MonoBehaviour {

        public BoomLauncher Launcher;

        void OnTriggerEnter(Collider other) {
            BoomAmmoPickup pickup = other.GetComponent<BoomAmmoPickup>();
            if (pickup == null || Launcher == null) {
                return;
            }

            // Only load balls the player is actively pushing in, not ones lying next to the launcher on the table
            Grabbable grab = pickup.GetComponent<Grabbable>();
            if (grab == null || !grab.BeingHeld) {
                return;
            }

            if (!Launcher.TryLoad(pickup.AmmoType)) {
                return;
            }

            grab.DropItem(false, true);
            pickup.transform.parent = null;
            Destroy(pickup.gameObject);
        }
    }
}
