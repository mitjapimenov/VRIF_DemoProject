using System.Collections.Generic;
using BNG;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// RaycastWeapon that is loaded by hand with BoomAmmoPickups and fires a projectile matching the loaded ammo type
    /// </summary>
    public class BoomLauncher : RaycastWeapon {

        [Header("Boom Launcher : ")]
        [Tooltip("How many balls can be loaded at once")]
        public int Capacity = 1;

        public GameObject NormalProjectile;
        public GameObject ExplosiveProjectile;

        [Tooltip("Ball shown in the muzzle while something is loaded. Tinted with the loaded ammo's color")]
        public Renderer LoadedIndicator;

        public Color NormalColor = new Color(0.12f, 0.12f, 0.14f);
        public Color ExplosiveColor = new Color(0.9f, 0.1f, 0.08f);

        public AudioClip LoadSound;

        readonly Queue<BoomAmmoType> loaded = new Queue<BoomAmmoType>();

        public int LoadedCount {
            get { return loaded.Count; }
        }

        /// <summary>
        /// Type of the next ball to be fired, or null if empty
        /// </summary>
        public BoomAmmoType? NextAmmo {
            get { return loaded.Count > 0 ? loaded.Peek() : (BoomAmmoType?)null; }
        }

        public int GetLoadedCount(BoomAmmoType ammoType) {
            int count = 0;
            foreach (BoomAmmoType loadedType in loaded) {
                if (loadedType == ammoType) {
                    count++;
                }
            }
            return count;
        }

        public bool TryLoad(BoomAmmoType ammoType) {
            if (loaded.Count >= Capacity) {
                return false;
            }

            loaded.Enqueue(ammoType);
            syncAmmo();

            if (LoadSound) {
                VRUtils.Instance.PlaySpatialClipAt(LoadSound, transform.position, 1f, 0.5f);
            }

            return true;
        }

        public override void Shoot() {
            if (loaded.Count > 0) {
                ProjectilePrefab = loaded.Peek() == BoomAmmoType.Explosive ? ExplosiveProjectile : NormalProjectile;
            }

            float previousShotTime = GetLastShotTime();
            base.Shoot();

            // Shot actually fired (not blocked by fire rate or empty)
            if (GetLastShotTime() != previousShotTime && loaded.Count > 0) {
                loaded.Dequeue();
                syncAmmo();

                Transform muzzle = GetMuzzlePointTransform();
                BoomVfx.Play(BoomVfxType.Muzzle, muzzle.position, muzzle.rotation);
            }
        }

        /// <summary>
        /// Ammo only comes from pickups, so a reload empties the launcher instead of filling it
        /// </summary>
        public override void Reload() {
            loaded.Clear();
            syncAmmo();
        }

        void syncAmmo() {
            // RaycastWeapon's own ammo counter decides whether the trigger fires
            InternalAmmo = loaded.Count;
            MaxInternalAmmo = Capacity;
            BulletInChamber = false;

            if (LoadedIndicator != null) {
                LoadedIndicator.gameObject.SetActive(loaded.Count > 0);
                if (loaded.Count > 0) {
                    LoadedIndicator.material.color = loaded.Peek() == BoomAmmoType.Explosive ? ExplosiveColor : NormalColor;
                }
            }
        }
    }
}
