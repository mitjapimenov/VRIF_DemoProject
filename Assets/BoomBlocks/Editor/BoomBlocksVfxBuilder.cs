using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoomBlocks {

    /// <summary>
    /// Creates the placeholder particle effect prefabs (explosion, impact dust, muzzle smoke, score confetti) and the BoomVfx object that plays them
    /// </summary>
    public static class BoomBlocksVfxBuilder {

        const string VfxFolder = "Assets/BoomBlocks/Prefabs/VFX";

        public static BoomVfx Build(Transform parent) {
            BoomBlocksPrototypeBuilder.ensureFolder(VfxFolder);

            GameObject go = new GameObject("VFX");
            go.transform.SetParent(parent, false);

            BoomVfx vfx = go.AddComponent<BoomVfx>();
            vfx.Explosion = savePrefab(createExplosion(), "Vfx_Explosion");
            vfx.Impact = savePrefab(createImpact(), "Vfx_Impact");
            vfx.Muzzle = savePrefab(createMuzzle(), "Vfx_Muzzle");
            vfx.Score = savePrefab(createScore(), "Vfx_Score");
            return vfx;
        }

        /// <summary>
        /// Fading streak behind a flying ball
        /// </summary>
        public static void AddTrail(GameObject ball, Color color) {
            TrailRenderer trail = ball.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = ball.transform.localScale.x * 0.8f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = getSpriteMaterial("BB_Trail");

            Color end = color;
            end.a = 0f;
            trail.colorGradient = gradient(color, end);
        }

        static GameObject createExplosion() {
            GameObject root = new GameObject("Vfx_Explosion");

            // Fireball : bright core turning orange, then dark
            ParticleSystem fire = addSystem(root, 1f, 28, particleMaterial());
            var main = fire.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            setShape(fire, ParticleSystemShapeType.Sphere, 0.25f);
            setColorOverLifetime(fire, new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.35f), new GradientColorKey(new Color(0.25f, 0.2f, 0.18f), 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            setSizeOverLifetime(fire, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.4f));

            // Smoke rising slowly after the fire
            GameObject smokeGo = child(root, "Smoke");
            ParticleSystem smoke = addSystem(smokeGo, 2f, 14, particleMaterial());
            main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.gravityModifier = -0.05f;
            setShape(smoke, ParticleSystemShapeType.Sphere, 0.4f);
            setColorOverLifetime(smoke, new GradientColorKey[] {
                new GradientColorKey(new Color(0.35f, 0.33f, 0.32f), 0f), new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.15f), new GradientAlphaKey(0f, 1f) });
            setSizeOverLifetime(smoke, AnimationCurve.Linear(0, 0.7f, 1, 1.6f));

            // Sparks flying out and falling
            GameObject sparksGo = child(root, "Sparks");
            ParticleSystem sparks = addSystem(sparksGo, 1f, 35, particleMaterial());
            main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new Color(1f, 0.8f, 0.35f);
            main.gravityModifier = 1f;
            setShape(sparks, ParticleSystemShapeType.Sphere, 0.15f);
            setColorOverLifetime(sparks, new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystemRenderer sparkRenderer = sparksGo.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.velocityScale = 0.04f;
            sparkRenderer.lengthScale = 1f;

            return root;
        }

        static GameObject createImpact() {
            // Emits along +Z, which is aimed along the surface normal when spawned
            GameObject root = new GameObject("Vfx_Impact");
            ParticleSystem dust = addSystem(root, 1f, 10, particleMaterial());
            var main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.gravityModifier = 0.15f;
            setShape(dust, ParticleSystemShapeType.Cone, 0.05f, 60f);
            setColorOverLifetime(dust, new GradientColorKey[] {
                new GradientColorKey(new Color(0.75f, 0.68f, 0.58f), 0f), new GradientColorKey(new Color(0.6f, 0.58f, 0.55f), 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            setSizeOverLifetime(dust, AnimationCurve.Linear(0, 0.6f, 1, 1.5f));
            return root;
        }

        static GameObject createMuzzle() {
            // Smoke puff blown forward out of the barrel (+Z = muzzle forward)
            GameObject root = new GameObject("Vfx_Muzzle");
            ParticleSystem smoke = addSystem(root, 1f, 10, particleMaterial());
            var main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            setShape(smoke, ParticleSystemShapeType.Cone, 0.02f, 20f);
            setColorOverLifetime(smoke, new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 0.95f, 0.95f), 0f), new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) });
            setSizeOverLifetime(smoke, AnimationCurve.Linear(0, 0.5f, 1, 2f));

            // Short bright pop
            GameObject flashGo = child(root, "Flash");
            ParticleSystem flash = addSystem(flashGo, 0.2f, 1, particleMaterial());
            main = flash.main;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.9f, 0.6f);
            setShape(flash, ParticleSystemShapeType.Sphere, 0.001f);
            return root;
        }

        static GameObject createScore() {
            // Colourful toy confetti
            GameObject root = new GameObject("Vfx_Score");
            ParticleSystem confetti = addSystem(root, 1f, 40, getSpriteMaterial("BB_Confetti"));
            var main = confetti.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.6f;

            Gradient colors = new Gradient();
            colors.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.3f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.33f),
                new GradientColorKey(new Color(0.3f, 0.8f, 0.4f), 0.66f), new GradientColorKey(new Color(0.25f, 0.55f, 1f), 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

            setShape(confetti, ParticleSystemShapeType.Cone, 0.15f, 35f);
            // Cone emits along +Z : tip it so confetti bursts upwards
            var shape = confetti.shape;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var rotation = confetti.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            setColorOverLifetime(confetti, new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f)
            }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            return root;
        }

        static ParticleSystem addSystem(GameObject go, float duration, int burstCount, Material material) {
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Lets BoomVfx.Play scale the whole effect with the transform
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Max(burstCount, 1);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void setShape(ParticleSystem ps, ParticleSystemShapeType type, float radius, float angle = 25f) {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = type;
            shape.radius = radius;
            shape.angle = angle;
        }

        static void setColorOverLifetime(ParticleSystem ps, GradientColorKey[] colors, GradientAlphaKey[] alphas) {
            Gradient g = new Gradient();
            g.SetKeys(colors, alphas);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        static void setSizeOverLifetime(ParticleSystem ps, AnimationCurve curve) {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        static Gradient gradient(Color start, Color end) {
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) });
            return g;
        }

        static GameObject child(GameObject parent, string name) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject savePrefab(GameObject go, string name) {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, VfxFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Unity's soft round default particle material, or an unlit particle material if it can't be found
        /// </summary>
        static Material particleMaterial() {
            Material builtIn = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            if (builtIn != null) {
                return builtIn;
            }

            string path = "Assets/BoomBlocks/Materials/BB_Particle.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(Shader.Find("Particles/Standard Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        /// <summary>
        /// Flat unlit material that uses vertex colour : trails, confetti, overlays
        /// </summary>
        public static Material getSpriteMaterial(string name) {
            string path = "Assets/BoomBlocks/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
