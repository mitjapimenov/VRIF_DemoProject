using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Invisible walls around the play area that keep the player inside it, both when teleporting and when walking.
    /// Each wall is a trigger collider on the "Restricted" layer : VRIF's teleport arc stops on it and turns red.
    /// Its "Blocker" child is a solid collider on the "PlayerBounds" layer, which only collides with the player,
    /// so smooth locomotion can't walk through while balls, blocks and the launcher still fly through.
    /// Move / rotate this object to place the area and set its size here. The walls are only visible in the Scene view (orange outline).
    /// </summary>
    [ExecuteAlways]
    public class BoomTeleportBounds : MonoBehaviour {

        [Tooltip("Width (X) and depth (Z) of the play area in meters, centered on this object")]
        public Vector2 Size = new Vector2(6f, 5f);

        [Tooltip("Wall height in meters. Tall enough that the teleport arc can't go over")]
        public float Height = 6f;

        [Tooltip("How far below this object's position the walls start, to cover uneven ground")]
        public float Below = 1f;

        public float Thickness = 0.2f;

        [Tooltip("Turn off to place the walls by hand")]
        public bool AutoLayout = true;

        public bool ShowInSceneView = true;
        public Color GizmoColor = new Color(1f, 0.55f, 0.1f, 0.9f);

        static readonly string[] wallNames = { "WallFront", "WallBack", "WallLeft", "WallRight" };

        void OnValidate() {
            Size.x = Mathf.Max(0.5f, Size.x);
            Size.y = Mathf.Max(0.5f, Size.y);
            Height = Mathf.Max(0.5f, Height);
            Thickness = Mathf.Max(0.01f, Thickness);
            Layout();
        }

        /// <summary>
        /// Places the four walls around the area. Walls are the children named WallFront, WallBack, WallLeft and WallRight
        /// </summary>
        public void Layout() {
            if (!AutoLayout) {
                return;
            }

            float centerY = (Height - Below) / 2f;
            float totalHeight = Height + Below;
            float halfX = Size.x / 2f;
            float halfZ = Size.y / 2f;

            // Front / back run along X, left / right along Z. Corners overlap so there are no gaps
            place(wallNames[0], new Vector3(0, centerY, halfZ), new Vector3(Size.x + Thickness, totalHeight, Thickness));
            place(wallNames[1], new Vector3(0, centerY, -halfZ), new Vector3(Size.x + Thickness, totalHeight, Thickness));
            place(wallNames[2], new Vector3(-halfX, centerY, 0), new Vector3(Thickness, totalHeight, Size.y + Thickness));
            place(wallNames[3], new Vector3(halfX, centerY, 0), new Vector3(Thickness, totalHeight, Size.y + Thickness));
        }

        void place(string wallName, Vector3 localPosition, Vector3 size) {
            Transform wall = transform.Find(wallName);
            if (wall == null) {
                return;
            }

            wall.localPosition = localPosition;
            wall.localRotation = Quaternion.identity;
            wall.localScale = Vector3.one;

            BoxCollider box = wall.GetComponent<BoxCollider>();
            if (box != null) {
                box.center = Vector3.zero;
                box.size = size;
                box.isTrigger = true;
            }

            // Solid copy that stops the walking player
            Transform blocker = wall.Find("Blocker");
            if (blocker != null) {
                blocker.localPosition = Vector3.zero;
                blocker.localRotation = Quaternion.identity;
                blocker.localScale = Vector3.one;

                BoxCollider solid = blocker.GetComponent<BoxCollider>();
                if (solid != null) {
                    solid.center = Vector3.zero;
                    solid.size = size;
                    solid.isTrigger = false;
                }
            }
        }

        void OnDrawGizmos() {
            if (!ShowInSceneView) {
                return;
            }

            Gizmos.color = GizmoColor;
            foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>()) {
                if (!box.isTrigger) {
                    continue;
                }
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
