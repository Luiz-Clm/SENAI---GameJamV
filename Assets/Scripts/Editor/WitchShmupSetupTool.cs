#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Environment;
using WitchShmup.Player;
using WitchShmup.UI;

namespace WitchShmup.Editor
{
    public static class WitchShmupSetupTool
    {
        private const string SpritesPath = "Assets/Sprites";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Tools/Witch Shmup/1. Gerar Sprites e Prefabs")]
        public static void GenerateSpritesAndPrefabs()
        {
            EnsureFoldersExist();

            Sprite wizardSprite = CreateAndSaveWizardSprite();
            Sprite iceBulletSprite = CreateAndSaveIceBulletSprite();
            Sprite fireBulletSprite = CreateAndSaveFireBulletSprite();
            Sprite heartSprite = CreateAndSaveHeartSprite();
            Sprite brickSprite = CreateAndSaveBrickSprite();
            Sprite spikesSprite = CreateAndSaveSpikesSprite();

            AssetDatabase.Refresh();

            // Create Projectile Prefabs
            CreateProjectilePrefab("Projectile_Ice", iceBulletSprite, ElementType.Ice, new Color(0.35f, 0.85f, 1f, 1f), 18f, 1.2f, 2);
            CreateProjectilePrefab("Projectile_Fire", fireBulletSprite, ElementType.Fire, new Color(1f, 0.55f, 0.15f, 1f), 14f, 2.5f, 1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Witch Shmup", "Sprites e Prefabs gerados com sucesso em Assets/Sprites e Assets/Prefabs!", "OK");
        }

        [MenuItem("Tools/Witch Shmup/2. Montar Cena Atual (Player + Parallax + HUD)")]
        public static void SetupCurrentScene()
        {
            GenerateSpritesAndPrefabs();

            // 1. Setup Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.14f, 1f); // #151024 Dark dungeon
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var camBounds = cam.GetComponent<CameraBounds>();
            if (camBounds == null)
            {
                camBounds = cam.gameObject.AddComponent<CameraBounds>();
            }

            // 2. Setup Parallax Background
            SetupParallaxHierarchy(cam);

            // 3. Setup Player
            SetupPlayer();

            // 4. Setup HUD
            SetupHUD();

            EditorUtility.DisplayDialog("Witch Shmup", "Cena configurada com sucesso!\n\nPressione PLAY para testar:\n- Movimento: WASD ou Setas\n- Atirar: Barra de Espaço ou Clique Esquerdo\n- Trocar Elemento: Q, E, Tab, Botão Direito do Mouse, ou 1 e 2", "Jogar!");
        }

        private static void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder(SpritesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static void SetupParallaxHierarchy(Camera cam)
        {
            var existingBg = GameObject.Find("Environment_Parallax");
            if (existingBg != null)
            {
                Object.DestroyImmediate(existingBg);
            }

            GameObject bgRoot = new GameObject("Environment_Parallax");
            var bgController = bgRoot.AddComponent<ParallaxBackgroundController>();

            Sprite brickSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/BrickWall.png");
            Sprite spikesSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Spikes.png");

            // Layer 1: Dark Bricks
            if (brickSprite != null)
            {
                GameObject layerBricks = new GameObject("Layer_Bricks");
                layerBricks.transform.SetParent(bgRoot.transform);
                var pl1 = layerBricks.AddComponent<ParallaxLayer>();

                // Create 2 segments
                float segWidth = 24f;
                for (int i = 0; i < 2; i++)
                {
                    GameObject seg = new GameObject($"BrickSegment_{i}");
                    seg.transform.SetParent(layerBricks.transform);
                    seg.transform.position = new Vector3(i * segWidth - 4f, 0f, 2f);
                    seg.transform.localScale = new Vector3(segWidth / 4f, 10f / 4f, 1f);

                    var sr = seg.AddComponent<SpriteRenderer>();
                    sr.sprite = brickSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(segWidth, 10f);
                    sr.sortingOrder = -10;
                    sr.color = new Color(0.7f, 0.7f, 0.85f, 1f);
                }
                pl1.AutoSetupSegments();
            }

            // Layer 2: Ceiling & Floor Spikes
            if (spikesSprite != null)
            {
                GameObject layerSpikes = new GameObject("Layer_Spikes_Obstacles");
                layerSpikes.transform.SetParent(bgRoot.transform);
                var pl2 = layerSpikes.AddComponent<ParallaxLayer>();

                float spikeWidth = 20f;
                for (int i = 0; i < 2; i++)
                {
                    GameObject seg = new GameObject($"SpikeSegment_{i}");
                    seg.transform.SetParent(layerSpikes.transform);
                    seg.transform.position = new Vector3(i * spikeWidth - 2f, 0f, 0f);

                    // Top ceiling spikes
                    GameObject topSpike = new GameObject("TopSpikes");
                    topSpike.transform.SetParent(seg.transform);
                    topSpike.transform.localPosition = new Vector3(0f, 4.6f, 0f);
                    topSpike.transform.localRotation = Quaternion.Euler(0, 0, 180f);
                    var srTop = topSpike.AddComponent<SpriteRenderer>();
                    srTop.sprite = spikesSprite;
                    srTop.drawMode = SpriteDrawMode.Tiled;
                    srTop.size = new Vector2(spikeWidth, 1.2f);
                    srTop.sortingOrder = -2;
                    srTop.color = new Color(0.4f, 0.35f, 0.5f, 1f);
                    var topCol = topSpike.AddComponent<BoxCollider2D>();
                    topCol.isTrigger = true;
                    topSpike.tag = "Hazard";

                    // Bottom floor spikes
                    GameObject botSpike = new GameObject("BottomSpikes");
                    botSpike.transform.SetParent(seg.transform);
                    botSpike.transform.localPosition = new Vector3(0f, -4.6f, 0f);
                    var srBot = botSpike.AddComponent<SpriteRenderer>();
                    srBot.sprite = spikesSprite;
                    srBot.drawMode = SpriteDrawMode.Tiled;
                    srBot.size = new Vector2(spikeWidth, 1.2f);
                    srBot.sortingOrder = -2;
                    srBot.color = new Color(0.4f, 0.35f, 0.5f, 1f);
                    var botCol = botSpike.AddComponent<BoxCollider2D>();
                    botCol.isTrigger = true;
                    botSpike.tag = "Hazard";
                }
                pl2.AutoSetupSegments();
            }
        }

        private static void SetupPlayer()
        {
            var existingPlayer = GameObject.FindWithTag("Player");
            if (existingPlayer != null)
            {
                Object.DestroyImmediate(existingPlayer);
            }

            GameObject playerObj = new GameObject("Player_Wizard");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-6f, 0f, 0f);

            var sr = playerObj.AddComponent<SpriteRenderer>();
            Sprite wizardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Wizard.png");
            sr.sprite = wizardSprite;
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.8f);
            col.offset = new Vector2(0f, 0f);

            playerObj.AddComponent<PlayerController>();
            var health = playerObj.AddComponent<PlayerHealth>();

            var shooting = playerObj.AddComponent<PlayerShooting>();

            // Setup fire points
            GameObject muzzleTop = new GameObject("Muzzle_Top");
            muzzleTop.transform.SetParent(playerObj.transform);
            muzzleTop.transform.localPosition = new Vector3(0.8f, 0.2f, 0f);

            GameObject muzzleBot = new GameObject("Muzzle_Bottom");
            muzzleBot.transform.SetParent(playerObj.transform);
            muzzleBot.transform.localPosition = new Vector3(0.8f, -0.2f, 0f);

            // Assign prefabs to shooting configs
            GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Ice.prefab");
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Fire.prefab");

            SerializedObject so = new SerializedObject(shooting);
            so.FindProperty("iceConfig.projectilePrefab").objectReferenceValue = icePrefab;
            so.FindProperty("fireConfig.projectilePrefab").objectReferenceValue = firePrefab;

            SerializedProperty firePointsProp = so.FindProperty("firePoints");
            firePointsProp.arraySize = 2;
            firePointsProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzleTop.transform;
            firePointsProp.GetArrayElementAtIndex(1).objectReferenceValue = muzzleBot.transform;
            so.ApplyModifiedProperties();
        }

        private static void SetupHUD()
        {
            var existingCanvas = GameObject.Find("HUD_Canvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            GameObject canvasObj = new GameObject("HUD_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var hud = canvasObj.AddComponent<HUDController>();
            Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Heart.png");

            // --- Top Bar ---
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(canvasObj.transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(0f, 70f);

            // Score Text
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(topBar.transform, false);
            var scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.04f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.04f, 0.5f);
            scoreRect.pivot = new Vector2(0f, 0.5f);
            var scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.text = "SCORE 0012480";
            scoreTmp.fontSize = 32;
            scoreTmp.fontStyle = FontStyles.Bold;
            scoreTmp.color = Color.white;

            // Stage Title Text
            GameObject stageObj = new GameObject("StageText");
            stageObj.transform.SetParent(topBar.transform, false);
            var stageRect = stageObj.AddComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0.5f, 0.5f);
            stageRect.anchorMax = new Vector2(0.5f, 0.5f);
            stageRect.pivot = new Vector2(0.5f, 0.5f);
            var stageTmp = stageObj.AddComponent<TextMeshProUGUI>();
            stageTmp.text = "FASE 1 - FLORESTA SOMBRIA";
            stageTmp.fontSize = 28;
            stageTmp.fontStyle = FontStyles.Bold;
            stageTmp.color = new Color(0.9f, 0.85f, 1f, 1f);

            // --- Bottom Bar ---
            GameObject bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(canvasObj.transform, false);
            var bottomBarRect = bottomBar.AddComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0f, 0f);
            bottomBarRect.anchorMax = new Vector2(1f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.sizeDelta = new Vector2(0f, 90f);

            // Hearts Container
            GameObject heartsContainer = new GameObject("HeartsContainer");
            heartsContainer.transform.SetParent(bottomBar.transform, false);
            var heartsRect = heartsContainer.AddComponent<RectTransform>();
            heartsRect.anchorMin = new Vector2(0.04f, 0.5f);
            heartsRect.anchorMax = new Vector2(0.04f, 0.5f);
            heartsRect.pivot = new Vector2(0f, 0.5f);
            var hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image[] heartImgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject hObj = new GameObject($"Heart_{i}");
                hObj.transform.SetParent(heartsContainer.transform, false);
                var hRect = hObj.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(36f, 36f);
                var img = hObj.AddComponent<Image>();
                img.sprite = heartSprite;
                img.color = new Color(1f, 0.35f, 0.45f, 1f);
                heartImgs[i] = img;
            }

            // Element Buttons: [GELO] [FOGO]
            GameObject elemContainer = new GameObject("ElementSelector");
            elemContainer.transform.SetParent(bottomBar.transform, false);
            var elemRect = elemContainer.AddComponent<RectTransform>();
            elemRect.anchorMin = new Vector2(0.96f, 0.5f);
            elemRect.anchorMax = new Vector2(0.96f, 0.5f);
            elemRect.pivot = new Vector2(1f, 0.5f);
            var elemHlg = elemContainer.AddComponent<HorizontalLayoutGroup>();
            elemHlg.spacing = 20f;
            elemHlg.childForceExpandWidth = false;
            elemHlg.childForceExpandHeight = false;

            // Ice Badge
            GameObject iceObj = new GameObject("Badge_Gelo");
            iceObj.transform.SetParent(elemContainer.transform, false);
            var iceRect = iceObj.AddComponent<RectTransform>();
            iceRect.sizeDelta = new Vector2(140f, 48f);
            var iceImg = iceObj.AddComponent<Image>();
            iceImg.color = new Color(0.2f, 0.85f, 1f, 1f);

            GameObject iceTxtObj = new GameObject("Text");
            iceTxtObj.transform.SetParent(iceObj.transform, false);
            var iceTxtRect = iceTxtObj.AddComponent<RectTransform>();
            iceTxtRect.anchorMin = Vector2.zero;
            iceTxtRect.anchorMax = Vector2.one;
            iceTxtRect.sizeDelta = Vector2.zero;
            var iceTmp = iceTxtObj.AddComponent<TextMeshProUGUI>();
            iceTmp.text = "GELO";
            iceTmp.alignment = TextAlignmentOptions.Center;
            iceTmp.fontSize = 24;
            iceTmp.fontStyle = FontStyles.Bold;
            iceTmp.color = Color.white;

            // Fire Badge
            GameObject fireObj = new GameObject("Badge_Fogo");
            fireObj.transform.SetParent(elemContainer.transform, false);
            var fireRect = fireObj.AddComponent<RectTransform>();
            fireRect.sizeDelta = new Vector2(140f, 48f);
            var fireImg = fireObj.AddComponent<Image>();
            fireImg.color = new Color(0.45f, 0.25f, 0.05f, 0.5f);

            GameObject fireTxtObj = new GameObject("Text");
            fireTxtObj.transform.SetParent(fireObj.transform, false);
            var fireTxtRect = fireTxtObj.AddComponent<RectTransform>();
            fireTxtRect.anchorMin = Vector2.zero;
            fireTxtRect.anchorMax = Vector2.one;
            fireTxtRect.sizeDelta = Vector2.zero;
            var fireTmp = fireTxtObj.AddComponent<TextMeshProUGUI>();
            fireTmp.text = "FOGO";
            fireTmp.alignment = TextAlignmentOptions.Center;
            fireTmp.fontSize = 24;
            fireTmp.fontStyle = FontStyles.Bold;
            fireTmp.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);

            // Wire HUD
            SerializedObject hudSo = new SerializedObject(hud);
            SerializedProperty heartsProp = hudSo.FindProperty("heartImages");
            heartsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartImgs[i];

            hudSo.FindProperty("iceBadge").objectReferenceValue = iceImg;
            hudSo.FindProperty("fireBadge").objectReferenceValue = fireImg;
            hudSo.FindProperty("iceText").objectReferenceValue = iceTmp;
            hudSo.FindProperty("fireText").objectReferenceValue = fireTmp;
            hudSo.FindProperty("scoreText").objectReferenceValue = scoreTmp;
            hudSo.FindProperty("stageText").objectReferenceValue = stageTmp;
            hudSo.ApplyModifiedProperties();
        }

        private static void CreateProjectilePrefab(string name, Sprite sprite, ElementType element, Color tint, float speed, float damage, int pierce)
        {
            string prefabPath = $"{PrefabsPath}/{name}.prefab";

            GameObject obj = new GameObject(name);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 6;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.35f);

            var proj = obj.AddComponent<Projectile>();
            proj.Initialize(Vector2.right, speed, damage, element, true);

            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
        }

        private static Sprite CreateAndSaveWizardSprite()
        {
            int w = 32, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color hatColor = new Color(0.48f, 0.32f, 0.68f, 1f);     // Witch Hat Purple
            Color robeColor = new Color(0.24f, 0.45f, 0.88f, 1f);    // Blue Robe
            Color skinColor = new Color(1.0f, 0.85f, 0.72f, 1f);     // Skin
            Color woodColor = new Color(0.68f, 0.46f, 0.22f, 1f);    // Broom wood
            Color bristleColor = new Color(0.92f, 0.78f, 0.32f, 1f); // Yellow bristles
            Color magicGlow = new Color(0.35f, 0.90f, 1.0f, 1f);     // Cyan lantern glow

            // Broom handle
            for (int x = 2; x < 28; x++)
            {
                SetPixel(pixels, w, x, 7, woodColor);
                SetPixel(pixels, w, x, 8, woodColor);
            }
            // Broom bristles
            for (int x = 0; x < 5; x++)
            {
                for (int y = 5; y < 11; y++)
                {
                    SetPixel(pixels, w, x, y, bristleColor);
                }
            }
            // Wizard body / robe
            for (int x = 10; x < 20; x++)
            {
                for (int y = 9; y < 17; y++)
                {
                    SetPixel(pixels, w, x, y, robeColor);
                }
            }
            // Wizard face
            for (int x = 12; x < 18; x++)
            {
                for (int y = 14; y < 18; y++)
                {
                    SetPixel(pixels, w, x, y, skinColor);
                }
            }
            // Wizard Hat
            for (int x = 8; x < 22; x++) SetPixel(pixels, w, x, 18, hatColor);
            for (int x = 10; x < 20; x++) SetPixel(pixels, w, x, 19, hatColor);
            for (int x = 11; x < 19; x++) SetPixel(pixels, w, x, 20, hatColor);
            for (int x = 12; x < 17; x++) SetPixel(pixels, w, x, 21, hatColor);
            for (int x = 13; x < 16; x++) SetPixel(pixels, w, x, 22, hatColor);
            for (int x = 14; x < 16; x++) SetPixel(pixels, w, x, 23, hatColor);

            // Magic orb / lantern at front of broom
            for (int x = 23; x < 27; x++)
            {
                for (int y = 10; y < 14; y++)
                {
                    SetPixel(pixels, w, x, y, magicGlow);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/Wizard.png", 16f);
        }

        private static Sprite CreateAndSaveIceBulletSprite()
        {
            int w = 16, h = 6;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color cyan = new Color(0.25f, 0.85f, 1f, 1f);
            Color white = Color.white;

            for (int x = 0; x < w; x++)
            {
                for (int y = 1; y < 5; y++)
                {
                    SetPixel(pixels, w, x, y, (x > 8 && y >= 2 && y <= 3) ? white : cyan);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/IceBullet.png", 16f);
        }

        private static Sprite CreateAndSaveFireBulletSprite()
        {
            int w = 16, h = 6;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color orange = new Color(1f, 0.5f, 0.1f, 1f);
            Color yellow = new Color(1f, 0.95f, 0.3f, 1f);

            for (int x = 0; x < w; x++)
            {
                for (int y = 1; y < 5; y++)
                {
                    SetPixel(pixels, w, x, y, (x > 8 && y >= 2 && y <= 3) ? yellow : orange);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/FireBullet.png", 16f);
        }

        private static Sprite CreateAndSaveHeartSprite()
        {
            int w = 16, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color red = Color.white; // We tint it in Image/SpriteRenderer
            // Draw pixel heart
            int[] rowStarts = { 7, 6, 5, 4, 3, 2, 2, 1, 1, 1, 1, 2, 2, 3 };
            for (int y = 2; y <= 13; y++)
            {
                int r = y - 2;
                if (r < rowStarts.Length)
                {
                    int span = 8 - rowStarts[r];
                    for (int dx = -span; dx <= span; dx++)
                    {
                        // top notch indentation
                        if (y >= 12 && dx >= -1 && dx <= 1) continue;
                        SetPixel(pixels, w, 8 + dx, y, red);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/Heart.png", 16f);
        }

        private static Sprite CreateAndSaveBrickSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[size * size];

            Color darkMortar = new Color(0.10f, 0.08f, 0.16f, 1f);
            Color brickColor1 = new Color(0.16f, 0.13f, 0.25f, 1f);
            Color brickColor2 = new Color(0.20f, 0.16f, 0.30f, 1f);

            int brickHeight = 16;
            int brickWidth = 32;

            for (int y = 0; y < size; y++)
            {
                int row = y / brickHeight;
                bool isMortarY = (y % brickHeight == 0);

                int xOffset = (row % 2 == 0) ? 0 : brickWidth / 2;

                for (int x = 0; x < size; x++)
                {
                    int adjustedX = (x + xOffset) % size;
                    bool isMortarX = (adjustedX % brickWidth == 0);

                    if (isMortarY || isMortarX)
                    {
                        pixels[y * size + x] = darkMortar;
                    }
                    else
                    {
                        pixels[y * size + x] = ((x + y) % 5 == 0) ? brickColor2 : brickColor1;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/BrickWall.png", 16f, true);
        }

        private static Sprite CreateAndSaveSpikesSprite()
        {
            int w = 32, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color spikeColor = new Color(0.22f, 0.18f, 0.32f, 1f);
            Color edgeColor = new Color(0.35f, 0.28f, 0.48f, 1f);

            // Two pointed stalagmites
            int[] peakXs = { 8, 24 };
            foreach (int peak in peakXs)
            {
                for (int y = 0; y < h; y++)
                {
                    int halfW = (int)((1f - (float)y / h) * 7f);
                    for (int dx = -halfW; dx <= halfW; dx++)
                    {
                        int px = peak + dx;
                        if (px >= 0 && px < w)
                        {
                            Color c = (dx == -halfW || dx == halfW || y == h - 1) ? edgeColor : spikeColor;
                            SetPixel(pixels, w, px, y, c);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return SaveTextureAsSprite(tex, $"{SpritesPath}/Spikes.png", 16f, true);
        }

        private static void SetPixel(Color[] pixels, int width, int x, int y, Color color)
        {
            if (x >= 0 && x < width && y >= 0 && y < (pixels.Length / width))
            {
                pixels[y * width + x] = color;
            }
        }

        private static Sprite SaveTextureAsSprite(Texture2D tex, string path, float pixelsPerUnit, bool repeat = false)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
