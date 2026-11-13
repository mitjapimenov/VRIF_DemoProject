using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Placeholder explosion visual : a sphere that quickly grows to the blast radius and fades out
    /// </summary>
    public class BoomExplosionFlash : MonoBehaviour {

        public float Duration = 0.3f;

        float maxDiameter;
        float spawnTime;
        Material material;
        Color startColor;

        public static BoomExplosionFlash Spawn(Vector3 position, float radius, Material flashMaterial) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BoomExplosionFlash";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;

            // Own copy so fading this flash doesn't affect others
            Material material = new Material(flashMaterial);
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            BoomExplosionFlash flash = go.AddComponent<BoomExplosionFlash>();
            flash.maxDiameter = radius * 2f;
            flash.material = material;
            return flash;
        }

        void Start() {
            spawnTime = Time.time;
            startColor = material.color;
            transform.localScale = Vector3.zero;
        }

        void Update() {
            float t = (Time.time - spawnTime) / Duration;
            if (t >= 1f) {
                Destroy(gameObject);
                return;
            }

            // Fast expansion that eases out
            float size = Mathf.Lerp(0.2f, maxDiameter, 1f - (1f - t) * (1f - t));
            transform.localScale = Vector3.one * size;

            Color c = startColor;
            c.a = startColor.a * (1f - t);
            material.color = c;
        }

        void OnDestroy() {
            Destroy(material);
        }
    }
}
