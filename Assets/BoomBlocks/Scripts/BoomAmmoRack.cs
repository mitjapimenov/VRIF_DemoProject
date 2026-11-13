using System.Collections.Generic;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Remembers the ammo pickups placed under it so a new round can put the same balls back on the table
    /// </summary>
    public class BoomAmmoRack : MonoBehaviour {

        public GameObject NormalPickupPrefab;
        public GameObject ExplosivePickupPrefab;

        struct Slot {
            public BoomAmmoType Type;
            public Vector3 LocalPosition;
        }

        readonly List<Slot> slots = new List<Slot>();

        void Awake() {
            foreach (BoomAmmoPickup pickup in GetComponentsInChildren<BoomAmmoPickup>()) {
                slots.Add(new Slot { Type = pickup.AmmoType, LocalPosition = transform.InverseTransformPoint(pickup.transform.position) });
            }
        }

        /// <summary>
        /// How many balls of this type the rack puts on the table each round
        /// </summary>
        public int GetSlotCount(BoomAmmoType type) {
            int count = 0;
            for (int x = 0; x < slots.Count; x++) {
                if (slots[x].Type == type) {
                    count++;
                }
            }
            return count;
        }

        public void ResetRack() {
            // Remove every loose ball, including ones dropped on the floor
            foreach (BoomAmmoPickup pickup in FindObjectsByType<BoomAmmoPickup>(FindObjectsSortMode.None)) {
                // Let go of it first so the hand isn't left holding a destroyed object
                BNG.Grabbable grab = pickup.GetComponent<BNG.Grabbable>();
                if (grab != null && grab.BeingHeld) {
                    grab.DropItem(false, true);
                }

                pickup.gameObject.SetActive(false);
                Destroy(pickup.gameObject);
            }

            foreach (Slot slot in slots) {
                GameObject prefab = slot.Type == BoomAmmoType.Explosive ? ExplosivePickupPrefab : NormalPickupPrefab;
                if (prefab != null) {
                    Instantiate(prefab, transform.TransformPoint(slot.LocalPosition), Quaternion.identity, transform);
                }
            }
        }
    }
}
