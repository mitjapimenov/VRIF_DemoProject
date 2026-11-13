using System.Collections.Generic;
using BNG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Puts the modelled launcher (FBX) inside the launcher's GFX in place of the placeholder primitives,
    /// and fits the grip point, muzzle, loading trigger, loaded ball and collider to the model.
    /// Only the launcher is touched, so everything else edited in the scene is kept.
    /// </summary>
    public static class BoomBlocksLauncherModel {

        public const string ModelPath = "Assets/3D Art/GranadelauncherToy.fbx";
        const string MaterialFolder = "Assets/3D Art/Materials";
        const string ModelName = "Model";

        // Placeholder primitives the builder makes inside GFX
        static readonly string[] placeholderNames = { "Grip", "Body", "Drum", "Barrel" };

        // Where the hand holds the grip, measured up from the bottom of the grip
        const float HandHeightFromGripBottom = 0.07f;

        // Toy plastic look for the extracted materials
        const float PlasticSmoothness = 0.3f;

        [MenuItem("Boom Blocks/Apply Launcher Model In Open Scene")]
        public static void ApplyInOpenScene() {
            BoomLauncher weapon = Object.FindFirstObjectByType<BoomLauncher>();
            if (weapon == null) {
                Debug.LogError("Boom Blocks : no BoomLauncher in the open scene");
                return;
            }

            if (Apply(weapon)) {
                EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
                Selection.activeGameObject = weapon.gameObject;
            }
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksLauncherModel.ApplyInMitjaScene
        /// </summary>
        public static void ApplyInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            ApplyInOpenScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static bool HasModel() {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null;
        }

        public static bool Apply(BoomLauncher weapon) {
            GameObject modelAsset = prepareModelAsset();
            if (modelAsset == null) {
                Debug.LogError("Boom Blocks : launcher model not found at " + ModelPath);
                return false;
            }

            Transform root = weapon.transform;
            Transform gfx = root.Find("GFX");
            if (gfx == null) {
                gfx = new GameObject("GFX").transform;
                gfx.SetParent(root, false);
            }

            // Remove the placeholder and any previously applied model. The loaded ball indicator is kept
            foreach (string name in placeholderNames) {
                Transform old = gfx.Find(name);
                if (old != null) {
                    Object.DestroyImmediate(old.gameObject);
                }
            }
            Transform previous = gfx.Find(ModelName);
            if (previous != null) {
                Object.DestroyImmediate(previous.gameObject);
            }

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, gfx);
            model.name = ModelName;
            model.transform.localPosition = Vector3.zero;

            // The grabber only detects the collider on the launcher root, so the model must not add its own
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) {
                Object.DestroyImmediate(c);
            }

            List<Vector3> points = getVertices(model, root);
            if (points.Count == 0) {
                Debug.LogError("Boom Blocks : the launcher model has no mesh");
                return false;
            }

            // ---- Point the barrel along +Z ----
            Bounds b = getBounds(points);
            bool lengthAlongX = b.size.x > b.size.z;
            float lengthCenter = lengthAlongX ? b.center.x : b.center.z;

            // The grip is the lowest part and sits behind the middle, so the muzzle is on the other side
            Vector3 lowest = points[0];
            foreach (Vector3 p in points) {
                if (p.y < lowest.y) {
                    lowest = p;
                }
            }
            float gripSide = Mathf.Sign((lengthAlongX ? lowest.x : lowest.z) - lengthCenter);

            Quaternion turn;
            if (lengthAlongX) {
                // +X to +Z is -90 around Y
                turn = Quaternion.Euler(0, gripSide > 0 ? 90f : -90f, 0);
            }
            else {
                turn = gripSide > 0 ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity;
            }
            model.transform.localRotation = turn * model.transform.localRotation;

            points = getVertices(model, root);
            b = getBounds(points);

            // ---- Put the hand on the grip ----
            float gripBottom = b.min.y;
            float sumZ = 0f;
            int count = 0;
            foreach (Vector3 p in points) {
                if (p.y < gripBottom + 0.02f) {
                    sumZ += p.z;
                    count++;
                }
            }
            Vector3 handPoint = new Vector3(b.center.x, gripBottom + HandHeightFromGripBottom, sumZ / Mathf.Max(1, count));
            model.transform.localPosition -= handPoint;

            points = getVertices(model, root);
            b = getBounds(points);

            // ---- Barrel : measured from the front tenth of the model ----
            float front = b.max.z;
            float sliceStart = front - b.size.z * 0.1f;
            Bounds barrel = new Bounds();
            bool first = true;
            foreach (Vector3 p in points) {
                if (p.z < sliceStart) {
                    continue;
                }
                if (first) {
                    barrel = new Bounds(p, Vector3.zero);
                    first = false;
                }
                else {
                    barrel.Encapsulate(p);
                }
            }
            // Width gives the diameter. Height may include a front sight, so measure up from the bottom
            float diameter = barrel.size.x;
            Vector3 barrelCenter = new Vector3(barrel.center.x, barrel.min.y + diameter / 2f, front);

            Transform muzzle = weapon.MuzzlePointTransform;
            if (muzzle != null) {
                // Past the collider so the ball does not hit the launcher
                muzzle.localPosition = barrelCenter + new Vector3(0, 0, 0.06f);
                muzzle.localRotation = Quaternion.identity;
            }

            BoomAmmoLoader loader = root.GetComponentInChildren<BoomAmmoLoader>(true);
            if (loader != null) {
                loader.transform.localPosition = barrelCenter + new Vector3(0, 0, 0.04f);
            }

            if (weapon.LoadedIndicator != null) {
                // Ball sits in the barrel opening, half sticking out
                Transform ball = weapon.LoadedIndicator.transform;
                ball.SetParent(gfx, false);
                ball.localPosition = barrelCenter;
                ball.SetAsLastSibling();
            }

            BoxCollider col = root.GetComponent<BoxCollider>();
            if (col != null) {
                col.center = b.center;
                col.size = b.size;
            }

            Bounds foregrip = setupTwoHanded(root, points, b, barrel);

            EditorUtility.SetDirty(root.gameObject);
            Debug.Log(string.Format("Boom Blocks : foregrip at {0}, size {1}", foregrip.center, foregrip.size));

            Debug.Log(string.Format("Boom Blocks : launcher model applied. Size {0:0.000} x {1:0.000} x {2:0.000} m, barrel diameter {3:0.000} m (ammo ball {5:0.000} m), model rotation {4}",
                b.size.x, b.size.y, b.size.z, diameter, model.transform.localEulerAngles, BoomBlocksPrototypeBuilder.BallDiameter));
            return true;
        }

        /// <summary>
        /// Second hand grab under the barrel : one hand holds the launcher heavily, the foregrip steadies and aims it.
        /// Returns the foregrip area in the launcher's local space
        /// </summary>
        static Bounds setupTwoHanded(Transform root, List<Vector3> points, Bounds model, Bounds barrel) {
            Grabbable grab = root.GetComponent<Grabbable>();
            if (grab == null) {
                return new Bounds();
            }

            // The foregrip is what hangs under the barrel in front of the body
            float underBarrel = barrel.min.y - 0.005f;
            float frontStart = model.center.z + 0.03f;
            Bounds area = new Bounds();
            bool found = false;
            foreach (Vector3 p in points) {
                if (p.z < frontStart || p.y > underBarrel) {
                    continue;
                }
                if (!found) {
                    area = new Bounds(p, Vector3.zero);
                    found = true;
                }
                else {
                    area.Encapsulate(p);
                }
            }
            if (found) {
                // Reach up to the barrel so the whole foregrip is grabbable, not just its bottom
                area.Encapsulate(new Vector3(area.center.x, underBarrel, area.center.z));
            }
            if (!found || area.size.z < 0.03f) {
                // No part under the barrel : use a spot a third of the way back from the muzzle
                area = new Bounds(new Vector3(barrel.center.x, barrel.min.y - 0.03f, model.max.z - model.size.z * 0.33f), new Vector3(0.05f, 0.06f, 0.14f));
            }

            Transform old = root.Find("Foregrip");
            if (old != null) {
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject go = new GameObject("Foregrip");
            go.transform.SetParent(root, false);
            go.transform.localPosition = area.center;

            // A little bigger than the part so it is easy to reach for
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = area.size + Vector3.one * 0.03f;

            Grabbable foregrip = go.AddComponent<Grabbable>();
            // Doesn't move anything itself : the launcher handles the aiming
            foregrip.GrabPhysics = GrabPhysics.None;
            foregrip.GrabMechanic = GrabType.Precise;
            // Hold the grip button to keep the hand on it
            foregrip.Grabtype = HoldType.HoldDown;
            foregrip.OtherGrabbableMustBeGrabbed = grab;
            foregrip.RemoteGrabbable = false;
            foregrip.CanBeSnappedToSnapZone = false;

            // While held, the second hand can only take the foregrip, not the whole launcher
            grab.SecondaryGrabBehavior = OtherGrabBehavior.DualGrab;
            grab.SecondaryGrabbable = foregrip;
            grab.TwoHandedPosition = TwoHandedPositionType.None;
            grab.TwoHandedRotation = TwoHandedRotationType.None;
            grab.TwoHandedDropBehavior = TwoHandedDropMechanic.Drop;

            BoomLauncherHandling handling = root.GetComponent<BoomLauncherHandling>();
            if (handling == null) {
                handling = root.gameObject.AddComponent<BoomLauncherHandling>();
            }
            handling.Grab = grab;
            handling.Foregrip = foregrip;

            EditorUtility.SetDirty(grab);
            EditorUtility.SetDirty(handling);
            return area;
        }

        /// <summary>
        /// Imports at real size and extracts the materials so they can be edited in the inspector
        /// </summary>
        static GameObject prepareModelAsset() {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) {
                return null;
            }

            bool changed = false;
            if (importer.addCollider) {
                importer.addCollider = false;
                changed = true;
            }
            if (importer.importCameras || importer.importLights || importer.importAnimation) {
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                changed = true;
            }
            if (importer.materialImportMode == ModelImporterMaterialImportMode.None) {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                changed = true;
            }
            if (changed) {
                importer.SaveAndReimport();
            }

            extractMaterials();
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }

        static void extractMaterials() {
            BoomBlocksPrototypeBuilder.ensureFolder(MaterialFolder);

            bool extracted = false;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath)) {
                Material embedded = asset as Material;
                if (embedded == null) {
                    continue;
                }

                string path = MaterialFolder + "/" + embedded.name + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) == null) {
                    string error = AssetDatabase.ExtractAsset(embedded, path);
                    if (!string.IsNullOrEmpty(error)) {
                        Debug.LogWarning("Boom Blocks : could not extract material " + embedded.name + " : " + error);
                        continue;
                    }
                    AssetDatabase.WriteImportSettingsIfDirty(ModelPath);
                    extracted = true;

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat != null) {
                        if (mat.HasProperty("_Glossiness")) {
                            mat.SetFloat("_Glossiness", PlasticSmoothness);
                        }
                        if (mat.HasProperty("_Metallic")) {
                            mat.SetFloat("_Metallic", 0f);
                        }
                        EditorUtility.SetDirty(mat);
                    }
                }
            }

            if (extracted) {
                AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
            }
        }

        static List<Vector3> getVertices(GameObject model, Transform space) {
            List<Vector3> result = new List<Vector3>();
            Matrix4x4 toSpace = space.worldToLocalMatrix;
            foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true)) {
                if (mf.sharedMesh == null) {
                    continue;
                }
                Matrix4x4 m = toSpace * mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices) {
                    result.Add(m.MultiplyPoint3x4(v));
                }
            }
            return result;
        }

        static Bounds getBounds(List<Vector3> points) {
            Bounds b = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points) {
                b.Encapsulate(p);
            }
            return b;
        }
    }
}
