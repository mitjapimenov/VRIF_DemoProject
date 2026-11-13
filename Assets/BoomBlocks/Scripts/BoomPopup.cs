using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Floating "+100" text that rises, faces the camera and disappears
    /// </summary>
    public class BoomPopup : MonoBehaviour {

        public float RiseSpeed = 1f;
        public float Duration = 1.2f;

        [Tooltip("Text size per meter of distance from the camera, so far away popups stay readable")]
        public float SizePerMeter = 0.012f;

        TextMesh textMesh;
        Color startColor;
        float spawnTime;

        public static BoomPopup Spawn(string text, Vector3 position, Color color, Font font) {
            if (font == null) {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            GameObject go = new GameObject("BoomPopup");
            go.transform.position = position;

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            return go.AddComponent<BoomPopup>();
        }

        void Start() {
            textMesh = GetComponent<TextMesh>();
            startColor = textMesh.color;
            spawnTime = Time.time;
        }

        void LateUpdate() {
            float t = (Time.time - spawnTime) / Duration;
            if (t >= 1f) {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * RiseSpeed * Time.deltaTime;

            Camera cam = Camera.main;
            if (cam != null) {
                // TextMesh is readable when looking along its forward axis
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
                textMesh.characterSize = Vector3.Distance(transform.position, cam.transform.position) * SizePerMeter;
            }

            Color c = startColor;
            c.a = 1f - t;
            textMesh.color = c;
        }
    }
}
