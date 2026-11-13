using System.Collections.Generic;
using BNG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoomBlocks {

    /// <summary>
    /// Builds the first playable Boom Blocks prototype into the open scene : table, launcher, ball prefab, score board and targets.
    /// Running it again replaces the previous prototype root.
    /// </summary>
    public static class BoomBlocksPrototypeBuilder {

        const string RootName = "BoomBlocks Prototype";
        const string BaseFolder = "Assets/BoomBlocks";
        const string MaterialFolder = BaseFolder + "/Materials";
        const string PrefabFolder = BaseFolder + "/Prefabs";
        const string BallPrefabPath = PrefabFolder + "/BoomBall.prefab";
        const string PistolPrefabPath = "Assets/BNG Framework/Prefabs/Weapons/Pistol.prefab";

        // Launcher tuning. 17 m/s gives a maximum range of ~29 m, so far targets need a clearly raised aim
        const float ShotSpeed = 17f;

        // Balls placed on the table per round
        const int NormalAmmoCount = 5;
        const int ExplosiveAmmoCount = 2;

        // Ball mass only affects how hard it hits, not its flight path. Compare against the structure masses below
        const float BallMass = 0.5f;

        // Ammo ball size in meters, used for the fired balls, the balls on the table and the loaded ball in the barrel.
        // Fits the enlarged (about 6.7 cm) barrel of the modelled launcher. Change existing ones with Boom Blocks > Apply Ball Size
        public const float BallDiameter = 0.05f;

        // Explosive ball : impulse at the center of the blast, falling off to zero at the radius
        const float ExplosionRadius = 1.5f;
        const float ExplosionForce = 20f;

        // TNT barrel : bigger blast than the explosive ball. ImpactSpeed is how hard something must hit it (m/s) to set it off
        const float TntRadius = 2f;
        const float TntForce = 30f;
        const float TntImpactSpeed = 1.5f;
        const int TntPoints = 50;

        const string SoundFolder = "Assets/BNG Framework/Sounds/";
        static readonly Color NormalBallColor = new Color(0.12f, 0.12f, 0.14f);
        static readonly Color ExplosiveBallColor = new Color(0.9f, 0.1f, 0.08f);

        // Structure tuning : heavier and wider pillars are harder to knock over
        class StructureMaterial {
            public string MaterialName;
            public Color Color;
            public float PillarWidth;
            public float PillarMass;
            public float SlabMass;
        }

        static readonly StructureMaterial Wood = new StructureMaterial {
            MaterialName = "BB_Pillar", Color = new Color(0.8f, 0.56f, 0.33f), PillarWidth = 0.15f, PillarMass = 5f, SlabMass = 8f
        };

        static readonly StructureMaterial Stone = new StructureMaterial {
            MaterialName = "BB_Stone", Color = new Color(0.55f, 0.55f, 0.57f), PillarWidth = 0.2f, PillarMass = 15f, SlabMass = 25f
        };

        [MenuItem("Boom Blocks/Build Prototype In Open Scene")]
        public static void Build() {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null) {
                Undo.DestroyObjectImmediate(existing);
            }

            ensureFolder(BaseFolder);
            ensureFolder(MaterialFolder);
            ensureFolder(PrefabFolder);

            float groundY = findGroundHeight();

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Boom Blocks Prototype");

            BoomBlocksVfxBuilder.Build(root.transform);

            GameObject normalProjectile = createProjectilePrefab(BoomAmmoType.Normal);
            GameObject explosiveProjectile = createProjectilePrefab(BoomAmmoType.Explosive);
            GameObject normalPickup = createPickupPrefab(BoomAmmoType.Normal);
            GameObject explosivePickup = createPickupPrefab(BoomAmmoType.Explosive);

            Vector3 tableTop = buildTable(root.transform, new Vector3(0, groundY, 0.6f));
            BoomLauncher weapon = buildLauncher(root.transform, tableTop + new Vector3(0, 0.1f, 0), normalProjectile, explosiveProjectile);
            BoomAmmoRack rack = buildAmmoRack(root.transform, tableTop, normalPickup, explosivePickup);
            Transform firstTarget = buildTargets(root.transform, groundY);

            disableVrifSlowMotion();
            BoomScoreManager manager = buildScoreManager(root.transform, weapon, rack);

            // Panels turn to face a standing player at the rig's origin
            Vector3 viewer = new Vector3(0, groundY + 1.6f, 0);
            BoomHud hud = BoomBlocksUiBuilder.BuildHud(root.transform, new Vector3(0, groundY + 2.4f, 2.8f), viewer, manager);
            // Centered above the back of the table, high enough to leave the view to the nearest targets clear
            BoomBlocksUiBuilder.BuildTutorial(root.transform, new Vector3(0, groundY + 1.95f, 1.2f), viewer, weapon, manager,
                rack.transform.Find("Tray_BoomAmmo_Normal"), rack.transform.Find("Tray_BoomAmmo_Explosive"), firstTarget, hud.gameObject);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log("Boom Blocks prototype built. Save the scene to keep it.");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksPrototypeBuilder.BuildInMitjaScene
        /// </summary>
        public static void BuildInMitjaScene() {
            var scene = EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            Build();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static Vector3 buildTable(Transform parent, Vector3 floorPosition) {
            const float height = 0.9f;
            Material wood = getMaterial("BB_Wood", new Color(0.85f, 0.62f, 0.38f));

            GameObject table = new GameObject("Table");
            table.transform.SetParent(parent, false);
            table.transform.position = floorPosition;

            createPrimitive(PrimitiveType.Cube, "Top", table.transform, new Vector3(0, height - 0.025f, 0), new Vector3(1.2f, 0.05f, 0.6f), wood, true);

            float legHeight = height - 0.05f;
            Vector3 legScale = new Vector3(0.06f, legHeight, 0.06f);
            foreach (Vector2 corner in new[] { new Vector2(-0.55f, -0.25f), new Vector2(0.55f, -0.25f), new Vector2(-0.55f, 0.25f), new Vector2(0.55f, 0.25f) }) {
                createPrimitive(PrimitiveType.Cube, "Leg", table.transform, new Vector3(corner.x, legHeight * 0.5f, corner.y), legScale, wood, true);
            }

            return floorPosition + new Vector3(0, height, 0);
        }

        static BoomLauncher buildLauncher(Transform parent, Vector3 position, GameObject normalProjectile, GameObject explosiveProjectile) {
            Material green = getMaterial("BB_LauncherGreen", new Color(0.45f, 0.7f, 0.2f));
            Material yellow = getMaterial("BB_LauncherYellow", new Color(1f, 0.8f, 0.15f));
            Material red = getMaterial("BB_LauncherRed", new Color(0.85f, 0.15f, 0.1f));

            // Root is at the grip so a Snap grab puts the hand on the handle with the barrel pointing forward (+Z)
            GameObject launcher = new GameObject("BoomLauncher");
            launcher.transform.SetParent(parent, false);
            launcher.transform.position = position;

            GameObject gfx = new GameObject("GFX");
            gfx.transform.SetParent(launcher.transform, false);
            createPrimitive(PrimitiveType.Cube, "Grip", gfx.transform, new Vector3(0, -0.02f, 0), new Vector3(0.04f, 0.12f, 0.05f), yellow, false);
            createPrimitive(PrimitiveType.Cube, "Body", gfx.transform, new Vector3(0, 0.07f, 0.06f), new Vector3(0.1f, 0.1f, 0.25f), green, false);
            createPrimitive(PrimitiveType.Cylinder, "Drum", gfx.transform, new Vector3(0, 0.07f, 0.08f), new Vector3(0.15f, 0.06f, 0.15f), red, false, Quaternion.Euler(90, 0, 0));
            createPrimitive(PrimitiveType.Cylinder, "Barrel", gfx.transform, new Vector3(0, 0.07f, 0.28f), new Vector3(0.08f, 0.15f, 0.08f), green, false, Quaternion.Euler(90, 0, 0));

            // Shows the loaded ball sticking out of the barrel. Hidden while empty
            GameObject loadedBall = createPrimitive(PrimitiveType.Sphere, "LoadedBall", gfx.transform, new Vector3(0, 0.07f, 0.43f), Vector3.one * BallDiameter, getMaterial("BB_LoadedBall", NormalBallColor), false);
            loadedBall.SetActive(false);

            // Grabbers only detect colliders on the same object as the Grabbable
            BoxCollider col = launcher.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.035f, 0.17f);
            col.size = new Vector3(0.14f, 0.23f, 0.54f);

            // Ball spawns just past the barrel tip and the collider so it doesn't hit the launcher
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(launcher.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 0.07f, 0.5f);

            Rigidbody rb = launcher.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Grabbable grab = launcher.AddComponent<Grabbable>();
            grab.GrabMechanic = GrabType.Snap;
            // Click grip once to pick up, again to drop
            grab.Grabtype = HoldType.Toggle;

            BoomLauncher weapon = launcher.AddComponent<BoomLauncher>();
            weapon.Capacity = 1;
            weapon.NormalProjectile = normalProjectile;
            weapon.ExplosiveProjectile = explosiveProjectile;
            weapon.LoadedIndicator = loadedBall.GetComponent<Renderer>();
            weapon.NormalColor = NormalBallColor;
            weapon.ExplosiveColor = ExplosiveBallColor;
            weapon.LoadSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "shotgun_shell.wav");
            weapon.FiringMethod = FiringType.Semi;
            weapon.FiringRate = 0.6f;
            weapon.ReloadMethod = ReloadType.InternalAmmo;
            // Starts empty : balls are loaded by hand from the table
            weapon.InternalAmmo = 0;
            weapon.MaxInternalAmmo = 1;
            weapon.MustChamberRounds = false;
            weapon.AutoChamberRounds = true;
            weapon.ForceSlideBackOnLastShot = false;
            weapon.AlwaysFireProjectile = true;
            weapon.FireProjectileInSlowMo = false;
            weapon.LaserGuided = false;
            weapon.ShotForce = ShotSpeed;
            weapon.ProjectilePrefab = normalProjectile;
            weapon.MuzzlePointTransform = muzzle.transform;
            // Ammo is limited : no button may refill or eject it
            weapon.ReloadInput = new List<GrabbedControllerBinding>();
            weapon.EjectInput = new List<GrabbedControllerBinding>();
            weapon.ReleaseSlideInput = new List<GrabbedControllerBinding>();

            copyWeaponSounds(weapon);

            LineRenderer line = launcher.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.015f;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = getLineMaterial();
            line.startColor = new Color(1f, 1f, 1f, 0.8f);
            line.endColor = new Color(1f, 1f, 1f, 0.1f);
            line.positionCount = 0;

            BoomTrajectoryPreview preview = launcher.AddComponent<BoomTrajectoryPreview>();
            preview.Weapon = weapon;

            // Push a held ball into this trigger at the muzzle to load it
            GameObject loaderGo = new GameObject("AmmoLoader");
            loaderGo.transform.SetParent(launcher.transform, false);
            loaderGo.transform.localPosition = new Vector3(0, 0.07f, 0.47f);
            SphereCollider loaderTrigger = loaderGo.AddComponent<SphereCollider>();
            loaderTrigger.isTrigger = true;
            loaderTrigger.radius = 0.07f;
            BoomAmmoLoader loader = loaderGo.AddComponent<BoomAmmoLoader>();
            loader.Launcher = weapon;

            // Swap the placeholder primitives for the modelled launcher when it is in the project
            if (BoomBlocksLauncherModel.HasModel()) {
                BoomBlocksLauncherModel.Apply(weapon);
            }

            return weapon;
        }

        /// <summary>
        /// Two trays on the table : normal balls on the left, explosive balls on the right
        /// </summary>
        static BoomAmmoRack buildAmmoRack(Transform parent, Vector3 tableTop, GameObject normalPickup, GameObject explosivePickup) {
            GameObject rackGo = new GameObject("AmmoRack");
            rackGo.transform.SetParent(parent, false);
            rackGo.transform.position = tableTop;

            buildTray(rackGo.transform, new Vector3(-0.4f, 0, 0), normalPickup, NormalAmmoCount);
            buildTray(rackGo.transform, new Vector3(0.4f, 0, 0), explosivePickup, ExplosiveAmmoCount);

            BoomAmmoRack rack = rackGo.AddComponent<BoomAmmoRack>();
            rack.NormalPickupPrefab = normalPickup;
            rack.ExplosivePickupPrefab = explosivePickup;
            return rack;
        }

        static void buildTray(Transform parent, Vector3 localCenter, GameObject pickupPrefab, int count) {
            const float width = 0.3f;
            const float depth = 0.22f;
            const float wallHeight = 0.04f;
            const float wall = 0.015f;
            const float spacing = 0.1f;
            Material trayMat = getMaterial("BB_Tray", new Color(0.55f, 0.38f, 0.22f));

            GameObject tray = new GameObject("Tray_" + pickupPrefab.name);
            tray.transform.SetParent(parent, false);
            tray.transform.localPosition = localCenter;

            createPrimitive(PrimitiveType.Cube, "Wall", tray.transform, new Vector3(0, wallHeight * 0.5f, -depth * 0.5f), new Vector3(width, wallHeight, wall), trayMat, true);
            createPrimitive(PrimitiveType.Cube, "Wall", tray.transform, new Vector3(0, wallHeight * 0.5f, depth * 0.5f), new Vector3(width, wallHeight, wall), trayMat, true);
            createPrimitive(PrimitiveType.Cube, "Wall", tray.transform, new Vector3(-width * 0.5f, wallHeight * 0.5f, 0), new Vector3(wall, wallHeight, depth), trayMat, true);
            createPrimitive(PrimitiveType.Cube, "Wall", tray.transform, new Vector3(width * 0.5f, wallHeight * 0.5f, 0), new Vector3(wall, wallHeight, depth), trayMat, true);

            // Fill a grid of up to 3 per row, centered in the tray
            int columns = Mathf.Min(count, 3);
            int rows = Mathf.CeilToInt(count / (float)columns);
            for (int i = 0; i < count; i++) {
                int column = i % columns;
                int row = i / columns;
                int inRow = Mathf.Min(columns, count - row * columns);
                float x = (column - (inRow - 1) * 0.5f) * spacing;
                float z = (row - (rows - 1) * 0.5f) * spacing;

                GameObject ball = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, parent);
                ball.transform.position = tray.transform.TransformPoint(new Vector3(x, 0.05f, z));
            }
        }

        static BoomScoreManager buildScoreManager(Transform parent, BoomLauncher weapon, BoomAmmoRack rack) {
            GameObject go = new GameObject("ScoreManager");
            go.transform.SetParent(parent, false);

            BoomScoreManager manager = go.AddComponent<BoomScoreManager>();
            manager.Weapon = weapon;
            manager.AmmoRack = rack;
            return manager;
        }

        /// <summary>
        /// The game has no slow motion, so VRIF's own Y button slow motion is switched off too
        /// </summary>
        internal static void disableVrifSlowMotion() {
            foreach (TimeController time in Object.FindObjectsByType<TimeController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (time.enabled) {
                    Undo.RecordObject(time, "Disable VRIF slow motion");
                    time.enabled = false;
                    EditorUtility.SetDirty(time);
                }
            }
        }

        /// <returns>Arrow anchor of the nearest bullseye, used by the tutorial's shooting step</returns>
        static Transform buildTargets(Transform parent, float groundY) {
            GameObject targets = new GameObject("Targets");
            targets.transform.SetParent(parent, false);

            // Pillar structures with a bullseye on top : position, levels, pillar height, slab size, board radius, points
            Transform first = buildStructure(targets.transform, Wood, new Vector3(-3f, groundY, 10f), 1, 1.2f, new Vector2(1f, 0.8f), 0.45f, 100);
            buildStructure(targets.transform, Wood, new Vector3(3.5f, groundY, 15f), 2, 0.9f, new Vector2(1f, 0.8f), 0.4f, 150);
            buildStructure(targets.transform, Wood, new Vector3(-4.5f, groundY, 20f), 1, 1.8f, new Vector2(0.9f, 0.7f), 0.4f, 200);
            buildStructure(targets.transform, Wood, new Vector3(3f, groundY, 25f), 2, 1.2f, new Vector2(1f, 0.8f), 0.35f, 300);
            buildStructure(targets.transform, Stone, new Vector3(6.5f, groundY, 18f), 1, 1.2f, new Vector2(1.1f, 0.9f), 0.4f, 400);

            buildPyramid(targets.transform, new Vector3(0.5f, groundY, 13f), 0.4f, 50);
            buildTower(targets.transform, new Vector3(-1.5f, groundY, 22f), 0.5f, 4, 75);

            // TNT barrels next to structures, so a toppling pillar can set them off
            buildTntBarrel(targets.transform, new Vector3(-1.95f, groundY, 10f));
            buildTntBarrel(targets.transform, new Vector3(-0.6f, groundY, 13f));
            buildTntBarrel(targets.transform, new Vector3(5.6f, groundY, 18f));
            buildTntBarrel(targets.transform, new Vector3(-3.2f, groundY, 21f));

            return first;
        }

        static void buildTntBarrel(Transform parent, Vector3 groundPosition) {
            const float diameter = 0.5f;
            const float height = 0.7f;
            Material red = getMaterial("BB_TntRed", new Color(0.85f, 0.1f, 0.08f));
            Material band = getMaterial("BB_TntBand", new Color(1f, 0.8f, 0.15f));

            // Unity's cylinder primitive (height 2) comes with a capsule collider : swap it for a convex mesh so the barrel has flat ends
            GameObject barrel = createPrimitive(PrimitiveType.Cylinder, "TntBarrel", parent, Vector3.zero, new Vector3(diameter, height * 0.5f, diameter), red, false);
            barrel.transform.position = groundPosition + new Vector3(0, height * 0.5f, 0);
            MeshCollider col = barrel.AddComponent<MeshCollider>();
            col.sharedMesh = barrel.GetComponent<MeshFilter>().sharedMesh;
            col.convex = true;

            createPrimitive(PrimitiveType.Cylinder, "Band", barrel.transform, new Vector3(0, 0.6f, 0), new Vector3(1.03f, 0.08f, 1.03f), band, false);
            createPrimitive(PrimitiveType.Cylinder, "Band", barrel.transform, new Vector3(0, -0.6f, 0), new Vector3(1.03f, 0.08f, 1.03f), band, false);

            Rigidbody rb = barrel.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            BoomTntBarrel tnt = barrel.AddComponent<BoomTntBarrel>();
            tnt.Points = TntPoints;
            tnt.ImpactSpeed = TntImpactSpeed;
            tnt.Radius = TntRadius;
            tnt.Force = TntForce;
            tnt.ExplosionSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "explosion.wav");
            tnt.FlashMaterial = getFlashMaterial();
        }

        /// <summary>
        /// Angry Birds style structure : four wooden pillars, a slab on top, repeated for each level, with a standing bullseye on the top slab
        /// </summary>
        static Transform buildStructure(Transform parent, StructureMaterial material, Vector3 basePosition, int levels, float pillarHeight, Vector2 slabSize, float radius, int points) {
            float pillarWidth = material.PillarWidth;
            const float slabThickness = 0.08f;
            Material mat = getMaterial(material.MaterialName, material.Color);

            GameObject structure = new GameObject("Structure_" + points);
            structure.transform.SetParent(parent, false);
            structure.transform.position = basePosition;

            float insetX = slabSize.x * 0.5f - pillarWidth * 0.5f - 0.03f;
            float insetZ = slabSize.y * 0.5f - pillarWidth * 0.5f - 0.03f;
            float y = 0f;

            for (int level = 0; level < levels; level++) {
                foreach (Vector2 corner in new[] { new Vector2(-insetX, -insetZ), new Vector2(insetX, -insetZ), new Vector2(-insetX, insetZ), new Vector2(insetX, insetZ) }) {
                    GameObject pillar = createPrimitive(PrimitiveType.Cube, "Pillar", structure.transform, new Vector3(corner.x, y + pillarHeight * 0.5f, corner.y), new Vector3(pillarWidth, pillarHeight, pillarWidth), mat, true);
                    addPhysicsProp(pillar, material.PillarMass);
                }
                y += pillarHeight;

                GameObject slab = createPrimitive(PrimitiveType.Cube, "Slab", structure.transform, new Vector3(0, y + slabThickness * 0.5f, 0), new Vector3(slabSize.x, slabThickness, slabSize.y), mat, true);
                addPhysicsProp(slab, material.SlabMass);
                y += slabThickness;
            }

            return buildStandingBullseye(structure.transform, new Vector3(0, y, 0), radius, points);
        }

        static void addPhysicsProp(GameObject go, float mass) {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<BoomResetPose>();
        }

        /// <summary>
        /// Free standing bullseye on a foot. Scores with a precision bonus on a direct hit, or base points when knocked over
        /// </summary>
        /// <returns>Point just above the board, for the tutorial arrow</returns>
        static Transform buildStandingBullseye(Transform parent, Vector3 localPosition, float radius, int points) {
            const float footHeight = 0.05f;
            Material white = getMaterial("BB_BoardWhite", new Color(0.95f, 0.95f, 0.92f));
            Material red = getMaterial("BB_BoardRed", new Color(0.9f, 0.15f, 0.12f));
            Material foot = getMaterial("BB_Post", new Color(0.45f, 0.3f, 0.18f));

            GameObject target = new GameObject("Bullseye_" + points);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;

            Vector3 center = new Vector3(0, footHeight + radius, 0);
            Quaternion facePlayer = Quaternion.Euler(90, 0, 0);

            GameObject footGo = createPrimitive(PrimitiveType.Cube, "Foot", target.transform, new Vector3(0, footHeight * 0.5f, 0), new Vector3(radius * 1.2f, footHeight, 0.25f), foot, true);
            // Cylinder's local up becomes the board normal, facing the player along -Z
            GameObject board = createPrimitive(PrimitiveType.Cylinder, "Board", target.transform, center, new Vector3(radius * 2f, 0.015f, radius * 2f), white, true, facePlayer);
            GameObject ring = createPrimitive(PrimitiveType.Cylinder, "Ring", target.transform, center + new Vector3(0, 0, -0.02f), new Vector3(radius * 1.3f, 0.005f, radius * 1.3f), red, false, facePlayer);
            GameObject inner = createPrimitive(PrimitiveType.Cylinder, "Inner", target.transform, center + new Vector3(0, 0, -0.025f), new Vector3(radius * 0.9f, 0.005f, radius * 0.9f), white, false, facePlayer);
            GameObject eye = createPrimitive(PrimitiveType.Cylinder, "Eye", target.transform, center + new Vector3(0, 0, -0.03f), new Vector3(radius * 0.4f, 0.005f, radius * 0.4f), red, false, facePlayer);

            Rigidbody rb = target.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            BoomTarget bt = target.AddComponent<BoomTarget>();
            bt.TargetType = BoomTargetType.Bullseye;
            bt.Points = points;
            bt.MaxPrecisionBonus = points;
            bt.Radius = radius;
            bt.BoardCenter = board.transform;
            bt.ScoreWhenToppled = true;
            bt.HitRenderers = new Renderer[] {
                board.GetComponent<Renderer>(), ring.GetComponent<Renderer>(), inner.GetComponent<Renderer>(), eye.GetComponent<Renderer>(), footGo.GetComponent<Renderer>()
            };

            GameObject anchor = new GameObject("ArrowAnchor");
            anchor.transform.SetParent(target.transform, false);
            anchor.transform.localPosition = center + new Vector3(0, radius, 0);
            return anchor.transform;
        }

        static void buildPyramid(Transform parent, Vector3 basePosition, float size, int points) {
            GameObject pyramid = new GameObject("BlockPyramid");
            pyramid.transform.SetParent(parent, false);
            pyramid.transform.position = basePosition;

            int rows = 3;
            for (int row = 0; row < rows; row++) {
                int count = rows - row;
                for (int x = 0; x < count; x++) {
                    float offsetX = (x - (count - 1) * 0.5f) * size * 1.02f;
                    createBlock(pyramid.transform, new Vector3(offsetX, size * (row + 0.5f), 0), size, points, row + x);
                }
            }
        }

        static void buildTower(Transform parent, Vector3 basePosition, float size, int height, int points) {
            GameObject tower = new GameObject("BlockTower");
            tower.transform.SetParent(parent, false);
            tower.transform.position = basePosition;

            for (int y = 0; y < height; y++) {
                createBlock(tower.transform, new Vector3(0, size * (y + 0.5f), 0), size, points, y + 1);
            }
        }

        static void createBlock(Transform parent, Vector3 localPosition, float size, int points, int colorIndex) {
            Color[] toyColors = {
                new Color(0.95f, 0.3f, 0.25f), new Color(0.25f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.35f, 0.8f, 0.4f)
            };
            Color color = toyColors[colorIndex % toyColors.Length];
            Material mat = getMaterial("BB_Block" + (colorIndex % toyColors.Length), color);

            GameObject block = createPrimitive(PrimitiveType.Cube, "Block", parent, localPosition, Vector3.one * size, mat, true);

            Rigidbody rb = block.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            BoomTarget bt = block.AddComponent<BoomTarget>();
            bt.TargetType = BoomTargetType.Block;
            bt.Points = points;
        }

        static Material getBallMaterial(BoomAmmoType type) {
            return type == BoomAmmoType.Explosive ? getMaterial("BB_BallExplosive", ExplosiveBallColor) : getMaterial("BB_Ball", NormalBallColor);
        }

        /// <summary>
        /// The ball the launcher fires
        /// </summary>
        static GameObject createProjectilePrefab(BoomAmmoType type) {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = type == BoomAmmoType.Explosive ? "BoomBallExplosive" : "BoomBall";
            ball.transform.localScale = Vector3.one * BallDiameter;
            ball.GetComponent<Renderer>().sharedMaterial = getBallMaterial(type);

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = BallMass;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            ball.AddComponent<BoomBall>();
            BoomBlocksVfxBuilder.AddTrail(ball, type == BoomAmmoType.Explosive ? new Color(1f, 0.45f, 0.15f, 0.8f) : new Color(1f, 1f, 1f, 0.5f));

            if (type == BoomAmmoType.Explosive) {
                BoomExplosive explosive = ball.AddComponent<BoomExplosive>();
                explosive.Radius = ExplosionRadius;
                explosive.Force = ExplosionForce;
                explosive.ExplosionSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "explosion.wav");
                explosive.FlashMaterial = getFlashMaterial();
            }

            string path = type == BoomAmmoType.Explosive ? PrefabFolder + "/BoomBallExplosive.prefab" : BallPrefabPath;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, path);
            Object.DestroyImmediate(ball);
            return prefab;
        }

        /// <summary>
        /// The grabbable ball on the table that the player loads into the launcher
        /// </summary>
        static GameObject createPickupPrefab(BoomAmmoType type) {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = type == BoomAmmoType.Explosive ? "BoomAmmo_Explosive" : "BoomAmmo_Normal";
            ball.transform.localScale = Vector3.one * BallDiameter;
            ball.GetComponent<Renderer>().sharedMaterial = getBallMaterial(type);

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            // Keeps balls from rolling around the tray
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            ball.AddComponent<Grabbable>();

            BoomAmmoPickup pickup = ball.AddComponent<BoomAmmoPickup>();
            pickup.AmmoType = type;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, PrefabFolder + "/" + ball.name + ".prefab");
            Object.DestroyImmediate(ball);
            return prefab;
        }

        static Material getFlashMaterial() {
            string path = MaterialFolder + "/BB_ExplosionFlash.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = new Color(1f, 0.55f, 0.1f, 0.7f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void copyWeaponSounds(RaycastWeapon weapon) {
            GameObject pistol = AssetDatabase.LoadAssetAtPath<GameObject>(PistolPrefabPath);
            RaycastWeapon source = pistol != null ? pistol.GetComponentInChildren<RaycastWeapon>(true) : null;
            if (source == null) {
                return;
            }

            weapon.GunShotSound = source.GunShotSound;
            weapon.GunShotVolume = 0.5f;
            weapon.EmptySound = source.EmptySound;
        }

        static GameObject createPrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material mat, bool keepCollider, Quaternion? localRotation = null) {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            if (!keepCollider) {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        static Material getMaterial(string name, Color color) {
            string path = MaterialFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                Shader shader = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material getLineMaterial() {
            string path = MaterialFolder + "/BB_TrajectoryLine.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static float findGroundHeight() {
            Physics.SyncTransforms();
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(0, 50f, 0.6f), Vector3.down, out hit, 200f, ~0, QueryTriggerInteraction.Ignore)) {
                return hit.point.y;
            }
            return 0f;
        }

        internal static void ensureFolder(string path) {
            if (AssetDatabase.IsValidFolder(path)) {
                return;
            }

            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
