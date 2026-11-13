using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BoomBlocks {

    /// <summary>
    /// Builds the world space HUD and the tutorial panel + arrow
    /// </summary>
    public static class BoomBlocksUiBuilder {

        static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.9f);
        static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color AccentColor = new Color(1f, 0.78f, 0.2f);
        static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.1f);
        static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color KeyColor = new Color(1f, 1f, 1f, 0.12f);

        // White rounded rectangle, 9-sliced so the corners keep their shape at any size.
        // An Image's corner radius in canvas pixels = RoundedSpriteRadius / Pixels Per Unit Multiplier (editable in the inspector)
        const string RoundedSpritePath = "Assets/BoomBlocks/UI/BB_RoundedRect.png";
        const int RoundedSpriteRadius = 64;

        // Numbered tutorial steps : pick up, load, aim and fire, explosives
        internal const int TutorialStepCount = 4;

        public static BoomHud BuildHud(Transform parent, Vector3 position, Vector3 viewer, BoomScoreManager manager) {
            // 720 x 240 px at 1.2 mm per px = 0.86 m x 0.29 m
            RectTransform canvas = createCanvas("HUD", parent, position, viewer, new Vector2(720, 240), 0.0012f);
            BuildHudContents(canvas, manager);
            return canvas.GetComponent<BoomHud>();
        }

        /// <summary>
        /// Score on the left, combo with its time bar on the right, and the game over screen. Fills an existing HUD canvas
        /// </summary>
        public static void BuildHudContents(RectTransform canvas, BoomScoreManager manager) {
            Vector2 size = new Vector2(720, 240);
            canvas.sizeDelta = size;

            rounded("Background", canvas, Vector2.zero, size, PanelColor, 36);

            BoomHud hud = canvas.gameObject.GetComponent<BoomHud>();
            if (hud == null) {
                hud = canvas.gameObject.AddComponent<BoomHud>();
            }
            hud.ScoreManager = manager;

            // ---- Playing ----
            RectTransform play = rect("PlayPanel", canvas, Vector2.zero, size);
            hud.PlayPanel = play.gameObject;

            text("ScoreLabel", play, new Vector2(-165, 70), new Vector2(330, 36), "SCORE", 28, LabelColor, TextAlignmentOptions.Left, FontStyles.Bold);
            hud.ScoreValue = text("ScoreValue", play, new Vector2(-165, -10), new Vector2(330, 110), "0", 100, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

            hud.ComboText = text("ComboText", play, new Vector2(175, 20), new Vector2(300, 80), "COMBO x2", 58, AccentColor, TextAlignmentOptions.Center, FontStyles.Bold);
            Image comboBar = rounded("ComboTimer", play, new Vector2(35, -40), new Vector2(280, 12), AccentColor, 6);
            comboBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            hud.ComboTimer = comboBar.rectTransform;

            // ---- Game over ----
            RectTransform over = rect("GameOverPanel", canvas, Vector2.zero, size);
            hud.GameOverPanel = over.gameObject;
            text("Title", over, new Vector2(0, 72), new Vector2(660, 70), "GAME OVER", 60, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            hud.GameOverScore = text("Score", over, new Vector2(0, -8), new Vector2(660, 100), "0", 90, AccentColor, TextAlignmentOptions.Center, FontStyles.Bold);
            text("Hint", over, new Vector2(0, -84), new Vector2(660, 40), "Press <b>A</b> or <b>X</b> to play again", 28, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Center, FontStyles.Normal);
            over.gameObject.SetActive(false);
        }

        public static BoomTutorial BuildTutorial(Transform parent, Vector3 position, Vector3 viewer, BoomLauncher launcher, BoomScoreManager manager,
            Transform ammoAnchor, Transform explosiveAnchor, Transform targetAnchor, GameObject hud) {

            GameObject root = new GameObject("Tutorial");
            root.transform.SetParent(parent, false);
            BoomTutorial tutorial = root.AddComponent<BoomTutorial>();
            tutorial.Launcher = launcher;
            tutorial.ScoreManager = manager;
            tutorial.Hud = hud;
            tutorial.LauncherAnchor = launcher.transform;
            tutorial.AmmoAnchor = ammoAnchor;
            tutorial.ExplosiveAnchor = explosiveAnchor;
            tutorial.TargetAnchor = targetAnchor;
            tutorial.StepDoneSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BNG Framework/Sounds/switch_on.wav");

            // 760 x 500 px at 1 mm per px. Texts come from the BoomTutorial component, the values here are only placeholders
            const float width = 672f;
            const float left = -width / 2f;

            RectTransform canvas = createCanvas("TutorialPanel", root.transform, position, viewer, new Vector2(760, 500), 0.001f);
            rounded("Background", canvas, Vector2.zero, new Vector2(760, 500), PanelColor, 36);
            tutorial.Panel = canvas.gameObject;

            // Header : step counter on the left, one progress dot per step on the right
            tutorial.StepCounter = text("StepCounter", canvas, new Vector2(0, 196), new Vector2(width, 36), "STEP 1 / 4", 24, AccentColor, TextAlignmentOptions.Left, FontStyles.Bold);
            tutorial.StepCounter.characterSpacing = 6;

            RectTransform dots = rect("StepDots", canvas, new Vector2(0, 196), new Vector2(width, 36));
            BuildStepDots(tutorial, dots, TutorialStepCount);

            tutorial.Title = text("Title", canvas, new Vector2(0, 142), new Vector2(width, 64), tutorial.PickUpText.Title, 48, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            tutorial.Body = text("Body", canvas, new Vector2(0, -2), new Vector2(width, 210), tutorial.PickUpText.Body, 29, new Color(1f, 1f, 1f, 0.82f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
            tutorial.Body.lineSpacing = 8;

            tutorial.Feedback = text("Feedback", canvas, new Vector2(0, -165), new Vector2(width, 50), tutorial.FeedbackMessage, 40, new Color(0.45f, 0.95f, 0.55f), TextAlignmentOptions.Left, FontStyles.Bold);
            tutorial.Feedback.gameObject.SetActive(false);

            // Footer : controller buttons shown as small keycaps
            const float footerY = -212f;
            RectTransform skip = rect("SkipHint", canvas, Vector2.zero, new Vector2(760, 500));
            keyCap("KeyB", skip, new Vector2(left + 20, footerY), "B", KeyColor, Color.white);
            tutorial.SkipHintText = text("Label", skip, new Vector2(left + 52 + 150, footerY), new Vector2(300, 40), tutorial.SkipMessage, 24, LabelColor, TextAlignmentOptions.Left, FontStyles.Normal);
            BuildSkipProgress(tutorial, skip, new Vector2(left + 20, footerY));

            RectTransform next = rect("ContinueHint", canvas, Vector2.zero, new Vector2(760, 500));
            keyCap("KeyA", next, new Vector2(-left - 20, footerY), "A", AccentColor, PanelColor);
            tutorial.ContinueHintText = text("Label", next, new Vector2(-left - 52 - 150, footerY), new Vector2(300, 40), tutorial.ContinueMessage, 24, AccentColor, TextAlignmentOptions.Right, FontStyles.Bold);
            tutorial.ContinueHint = next.gameObject;
            next.gameObject.SetActive(false);

            tutorial.Arrow = createArrow(root.transform);
            return tutorial;
        }

        /// <summary>
        /// One progress dot per numbered step, right aligned in the header row
        /// </summary>
        internal static void BuildStepDots(BoomTutorial tutorial, RectTransform dots, int count) {
            for (int i = dots.childCount - 1; i >= 0; i--) {
                Object.DestroyImmediate(dots.GetChild(i).gameObject);
            }

            float right = dots.sizeDelta.x / 2f;
            tutorial.StepDots = new Image[count];
            for (int i = 0; i < count; i++) {
                float x = right - 7 - (count - 1 - i) * 26;
                tutorial.StepDots[i] = rounded("Dot", dots, new Vector2(x, 0), new Vector2(14, 14), tutorial.DotUpcomingColor, 7);
            }
        }

        /// <summary>
        /// Pill behind the B keycap and its label that fills up while B is held. Built behind the key and label
        /// </summary>
        public static void BuildSkipProgress(BoomTutorial tutorial, Transform skipHint, Vector2 keyPosition) {
            const float width = 300f;
            const float height = 50f;

            Transform old = skipHint.Find("SkipProgress");
            if (old != null) {
                Object.DestroyImmediate(old.gameObject);
            }

            Vector2 center = new Vector2(keyPosition.x - 25f + width / 2f, keyPosition.y);
            RectTransform root = rect("SkipProgress", skipHint, center, new Vector2(width, height));
            root.SetAsFirstSibling();

            rounded("Track", root, Vector2.zero, new Vector2(width, height), new Color(1f, 1f, 1f, 0.06f), height / 2f);
            Image fill = rounded("Fill", root, new Vector2(-width / 2f, 0), new Vector2(width, height), new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.4f), height / 2f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);

            tutorial.SkipProgress = root.gameObject;
            tutorial.SkipProgressFill = fill.rectTransform;
            root.gameObject.SetActive(false);
        }

        [MenuItem("Boom Blocks/Add Hold To Skip In Open Scene")]
        public static void AddSkipProgressInOpenScene() {
            BoomTutorial tutorial = Object.FindFirstObjectByType<BoomTutorial>(FindObjectsInactive.Include);
            if (tutorial == null || tutorial.Panel == null) {
                Debug.LogError("Boom Blocks : no tutorial panel in the open scene");
                return;
            }

            Transform skip = tutorial.Panel.transform.Find("SkipHint");
            Transform key = skip != null ? skip.Find("KeyB") : null;
            if (key == null) {
                Debug.LogError("Boom Blocks : SkipHint/KeyB not found in the tutorial panel");
                return;
            }

            BuildSkipProgress(tutorial, skip, ((RectTransform)key).anchoredPosition);

            // Only replace the text if it is still the old default
            if (tutorial.SkipMessage == "Skip tutorial") {
                tutorial.SkipMessage = "Hold to skip tutorial";
            }
            if (tutorial.SkipHintText != null) {
                tutorial.SkipHintText.text = tutorial.SkipMessage;
                EditorUtility.SetDirty(tutorial.SkipHintText);
            }

            EditorUtility.SetDirty(tutorial);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tutorial.gameObject.scene);
            Debug.Log("Boom Blocks : hold to skip added to the tutorial");
        }

        /// <summary>
        /// Command line entry : Unity -batchmode -executeMethod BoomBlocks.BoomBlocksUiBuilder.AddSkipProgressInMitjaScene
        /// </summary>
        public static void AddSkipProgressInMitjaScene() {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/BNG Framework/Scenes/MitjaXRScene.unity");
            AddSkipProgressInOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        static Transform createArrow(Transform parent) {
            GameObject arrow = new GameObject("TutorialArrow");
            arrow.transform.SetParent(parent, false);
            arrow.AddComponent<MeshFilter>().sharedMesh = coneMesh();

            Material mat = BoomBlocksVfxBuilder.getSpriteMaterial("BB_TutorialArrow");
            mat.color = AccentColor;
            EditorUtility.SetDirty(mat);

            MeshRenderer r = arrow.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 12 cm wide, 18 cm tall, pointing down
            arrow.transform.localScale = Vector3.one;
            return arrow.transform;
        }

        /// <summary>
        /// Downward pointing cone, tip at the origin. Saved as an asset so the scene can reference it
        /// </summary>
        static Mesh coneMesh() {
            const string path = "Assets/BoomBlocks/Materials/BB_ArrowCone.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) {
                return existing;
            }

            const int segments = 16;
            const float radius = 0.06f;
            const float height = 0.18f;

            Vector3[] vertices = new Vector3[segments + 2];
            vertices[0] = Vector3.zero;
            vertices[1] = new Vector3(0, height, 0);
            for (int i = 0; i < segments; i++) {
                float a = i * Mathf.PI * 2f / segments;
                vertices[i + 2] = new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
            }

            int[] triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++) {
                int current = i + 2;
                int next = (i + 1) % segments + 2;
                // Side
                triangles[i * 6] = 0;
                triangles[i * 6 + 1] = next;
                triangles[i * 6 + 2] = current;
                // Top cap
                triangles[i * 6 + 3] = 1;
                triangles[i * 6 + 4] = current;
                triangles[i * 6 + 5] = next;
            }

            Mesh mesh = new Mesh { name = "BB_ArrowCone", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Image[] iconRow(string name, RectTransform parent, float startX, float y, int count, Color color) {
            RectTransform row = rect(name, parent, Vector2.zero, new Vector2(1000, 560));
            Image[] icons = new Image[count];
            for (int i = 0; i < count; i++) {
                icons[i] = image("Icon", row, new Vector2(startX + 24 + i * 62, y), new Vector2(48, 48), color, knobSprite(), false);
            }
            return icons;
        }

        static RectTransform createCanvas(string name, Transform parent, Vector3 position, Vector3 viewer, Vector2 sizePx, float metersPerPixel) {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = sizePx;
            rt.position = position;
            // A canvas is read from its -Z side, so point +Z away from the player
            rt.rotation = Quaternion.LookRotation(position - viewer);
            rt.localScale = Vector3.one * metersPerPixel;
            return rt;
        }

        static RectTransform rect(string name, Transform parent, Vector2 position, Vector2 size) {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static Image image(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite, bool sliced) {
            RectTransform rt = rect(name, parent, position, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style) {
            RectTransform rt = rect(name, parent, position, size);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = value;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void keyCap(string name, Transform parent, Vector2 position, string letter, Color color, Color letterColor) {
            Image cap = rounded(name, parent, position, new Vector2(40, 40), color, 12);
            TextMeshProUGUI label = text("Letter", cap.transform, Vector2.zero, new Vector2(40, 40), letter, 24, letterColor, TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>
        /// Sliced rounded rectangle with the given corner radius in canvas pixels
        /// </summary>
        static Image rounded(string name, Transform parent, Vector2 position, Vector2 size, Color color, float radius) {
            Image img = image(name, parent, position, size, color, roundedSprite(), true);
            img.pixelsPerUnitMultiplier = RoundedSpriteRadius / Mathf.Max(0.01f, radius);
            return img;
        }

        static Sprite roundedSprite() {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (existing != null) {
                return existing;
            }

            BoomBlocksPrototypeBuilder.ensureFolder("Assets/BoomBlocks/UI");

            // Four quarter circles around a small stretchable center, with an antialiased edge
            const int r = RoundedSpriteRadius;
            const int size = r * 2 + 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, r, size - r);
                    float cy = Mathf.Clamp(py, r, size - r);
                    float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(r - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            string fullPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), RoundedSpritePath);
            System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = new Vector4(r, r, r, r);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        static Sprite knobSprite() {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }
    }
}
