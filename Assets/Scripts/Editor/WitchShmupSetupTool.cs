#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WitchShmup.Boss;
using WitchShmup.CameraSystem;
using WitchShmup.Combat;
using WitchShmup.Cutscenes;
using WitchShmup.Enemies;
using WitchShmup.Environment;
using WitchShmup.Player;
using WitchShmup.Spawners;
using WitchShmup.UI;
using WitchShmup.Utils;

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

            // Reimport existing sprites with crisp pixel settings
            EnsureSpriteImportSettings("MAGO.png", 16f);
            EnsureSpriteImportSettings("MorcegoDeFogoEnemy.png", 16f);
            EnsureSpriteImportSettings("Wizard.png", 16f);
            EnsureSpriteImportSettings("IceBullet.png", 16f);
            EnsureSpriteImportSettings("FireBullet.png", 16f);
            EnsureSpriteImportSettings("Heart.png", 16f);
            EnsureSpriteImportSettings("BrickWall.png", 16f, true);
            EnsureSpriteImportSettings("Spikes.png", 16f, true);
            EnsureSpriteImportSettings("Bat.png", 16f);
            EnsureSpriteImportSettings("Rusher.png", 16f);
            EnsureSpriteImportSettings("Golem_Head.png", 16f);
            EnsureSpriteImportSettings("Golem_Body.png", 16f);
            EnsureSpriteImportSettings("BigRock.png", 16f);
            EnsureSpriteImportSettings("StoneWall_Block.png", 16f);
            EnsureSpriteImportSettings("MiniStone.png", 16f);

            AssetDatabase.Refresh();

            // 1. Projéteis do Player
            Sprite iceBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/IceBullet.png");
            Sprite fireBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/FireBullet.png");
            CreateProjectilePrefab("Projectile_Ice", iceBulletSprite, ElementType.Ice, 18f, 1.2f, 2, true);
            CreateProjectilePrefab("Projectile_Fire", fireBulletSprite, ElementType.Fire, 14f, 2.5f, 1, true);

            // 2. Projétil de Fogo do Morcego
            CreateProjectilePrefab("Enemy_Fireball", fireBulletSprite, ElementType.Enemy, 8f, 1f, 1, false);

            // 3. Inimigo Rusher Prefab
            Sprite rusherSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Rusher.png");
            CreateRusherPrefab(rusherSprite);

            // 4. Inimigo Morcego Prefab
            Sprite batSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Bat.png");
            GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Enemy_Fireball.prefab");
            CreateBatPrefab(batSprite, fireballPrefab);

            // 5. Boss Prefabs: Pedra Gigante, Pedra Orbital, Parede de Pedra
            Sprite bigRockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/BigRock.png");
            CreateBigRockPrefab(bigRockSprite);

            Sprite miniStoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/MiniStone.png");
            CreateOrbitingStonePrefab(miniStoneSprite);

            Sprite stoneBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/StoneWall_Block.png");
            CreateStoneWallPrefab(stoneBlockSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Witch Shmup", "Sprites e Prefabs gerados e configurados com sucesso em Assets/Prefabs!", "OK");
        }

        [MenuItem("Tools/Witch Shmup/2. Montar Cena Atual (Player + Parallax + Inimigos + Boss + Cutscene + HUD)")]
        public static void SetupCurrentScene()
        {
            GenerateSpritesAndPrefabs();

            // 1. Setup Camera com ScreenShake
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
            if (camBounds == null) camBounds = cam.gameObject.AddComponent<CameraBounds>();

            var screenShake = cam.GetComponent<ScreenShake>();
            if (screenShake == null) screenShake = cam.gameObject.AddComponent<ScreenShake>();

            // 2. Setup Parallax Background
            SetupParallaxHierarchy(cam);

            // 3. Setup Player
            SetupPlayer();

            // 4. Setup HUD com barra de vida do Boss
            SetupHUD();

            // 5. Setup Golem Boss e Cutscene
            SetupBossAndCutscene();

            // 6. Setup Wave Spawner
            SetupWaveSpawner();

            EditorUtility.DisplayDialog("Witch Shmup", "Cena configurada com sucesso!\n\nNovidades implementadas:\n- Inimigos Rushers (investem contra o jogador)\n- Morcegos atiradores de fogo que desviam dos tiros\n- Golem de Pedra com mini-pedras orbitais\n- Ataque de Pedra Gigante e Parede Destrutível\n- Cutscene do Mago com medo e Golem rugindo\n\nAtalho útil: Pressione 'B' no Play Mode para acionar a Cutscene do Boss imediatamente!", "Jogar!");
        }

        private static void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder(SpritesPath)) AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(PrefabsPath)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static void EnsureSpriteImportSettings(string filename, float pixelsPerUnit, bool repeat = false)
        {
            string path = $"{SpritesPath}/{filename}";
            if (!File.Exists(path)) return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; needReimport = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; needReimport = true; }
                if (importer.spritePixelsPerUnit != pixelsPerUnit) { importer.spritePixelsPerUnit = pixelsPerUnit; needReimport = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; needReimport = true; }

                var wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (importer.wrapMode != wrap) { importer.wrapMode = wrap; needReimport = true; }

                if (needReimport) importer.SaveAndReimport();
            }
        }

        private static void CreateProjectilePrefab(string name, Sprite sprite, ElementType element, float speed, float damage, int pierce, bool isPlayerShot)
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
            proj.Initialize(Vector2.right, speed, damage, element, isPlayerShot);

            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
        }

        private static void CreateRusherPrefab(Sprite sprite)
        {
            string path = $"{PrefabsPath}/Enemy_Rusher.prefab";
            GameObject obj = new GameObject("Enemy_Rusher");
            obj.tag = "Enemy";

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 0.8f);

            obj.AddComponent<RusherEnemy>();

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        private static void CreateBatPrefab(Sprite sprite, GameObject fireballPrefab)
        {
            string path = $"{PrefabsPath}/Enemy_Bat.prefab";
            GameObject obj = new GameObject("Enemy_Bat");
            obj.tag = "Enemy";

            var sr = obj.AddComponent<SpriteRenderer>();

            // Verifica se há frames de animação do MorcegoDeFogoEnemy
            var batAssets = AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/MorcegoDeFogoEnemy.png");
            List<Sprite> batFrames = new List<Sprite>();
            foreach (var asset in batAssets)
            {
                if (asset is Sprite s) batFrames.Add(s);
            }

            if (batFrames.Count > 0)
            {
                sr.sprite = batFrames[0];
                var anim = obj.AddComponent<SimpleSpriteAnimator>();
                anim.SetFrames(batFrames.ToArray(), 6f);
            }
            else
            {
                sr.sprite = sprite;
            }

            sr.sortingOrder = 8;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            var bat = obj.AddComponent<BatEnemy>();
            SerializedObject so = new SerializedObject(bat);
            so.FindProperty("fireballPrefab").objectReferenceValue = fireballPrefab;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        private static void CreateBigRockPrefab(Sprite sprite)
        {
            string path = $"{PrefabsPath}/Boss_BigRock.prefab";
            GameObject obj = new GameObject("Boss_BigRock");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.0f;

            obj.AddComponent<BigRockProjectile>();

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        private static void CreateOrbitingStonePrefab(Sprite sprite)
        {
            string path = $"{PrefabsPath}/Boss_OrbitingStone.prefab";
            GameObject obj = new GameObject("Boss_OrbitingStone");

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 9;

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            obj.AddComponent<OrbitingStone>();

            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        private static void CreateStoneWallPrefab(Sprite blockSprite)
        {
            string path = $"{PrefabsPath}/Boss_StoneWall.prefab";
            GameObject wall = new GameObject("Boss_StoneWall");
            wall.AddComponent<StoneWallObstacle>();

            float[] yOffsets = { -2.2f, 0f, 2.2f };
            for (int i = 0; i < yOffsets.Length; i++)
            {
                GameObject seg = new GameObject($"StoneSegment_{i}");
                seg.transform.SetParent(wall.transform);
                seg.transform.localPosition = new Vector3(0f, yOffsets[i], 0f);

                var sr = seg.AddComponent<SpriteRenderer>();
                sr.sprite = blockSprite;
                sr.sortingOrder = 7;

                var col = seg.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.2f, 2.0f);

                seg.AddComponent<StoneWallSegment>();
            }

            PrefabUtility.SaveAsPrefabAsset(wall, path);
            Object.DestroyImmediate(wall);
        }

        private static void SetupParallaxHierarchy(Camera cam)
        {
            var existingBg = GameObject.Find("Environment_Parallax");
            if (existingBg != null) Object.DestroyImmediate(existingBg);

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
            if (existingPlayer != null) Object.DestroyImmediate(existingPlayer);

            GameObject playerObj = new GameObject("Player_Wizard");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-6f, 0f, 0f);

            var sr = playerObj.AddComponent<SpriteRenderer>();
            Sprite wizardSprite = null;
            var magoAssets = AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/MAGO.png");
            foreach (var asset in magoAssets)
            {
                if (asset is Sprite s) { wizardSprite = s; break; }
            }
            if (wizardSprite == null) wizardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Wizard.png");
            sr.sprite = wizardSprite;
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.8f);

            playerObj.AddComponent<PlayerController>();
            playerObj.AddComponent<PlayerHealth>();
            var shooting = playerObj.AddComponent<PlayerShooting>();

            // Setup fire points
            GameObject muzzleTop = new GameObject("Muzzle_Top");
            muzzleTop.transform.SetParent(playerObj.transform);
            muzzleTop.transform.localPosition = new Vector3(0.8f, 0.2f, 0f);

            GameObject muzzleBot = new GameObject("Muzzle_Bottom");
            muzzleBot.transform.SetParent(playerObj.transform);
            muzzleBot.transform.localPosition = new Vector3(0.8f, -0.2f, 0f);

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
            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

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
            scoreTmp.text = "SCORE 0000000";
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
            stageTmp.text = "FASE 1 - RUÍNAS DE PEDRA";
            stageTmp.fontSize = 28;
            stageTmp.fontStyle = FontStyles.Bold;
            stageTmp.color = new Color(0.9f, 0.85f, 1f, 1f);

            // Boss Health Bar Container (no topo direito, como no mockup!)
            GameObject bossBarObj = new GameObject("BossHealthBar");
            bossBarObj.transform.SetParent(topBar.transform, false);
            var bossBarRect = bossBarObj.AddComponent<RectTransform>();
            bossBarRect.anchorMin = new Vector2(0.96f, 0.5f);
            bossBarRect.anchorMax = new Vector2(0.96f, 0.5f);
            bossBarRect.pivot = new Vector2(1f, 0.5f);
            bossBarRect.sizeDelta = new Vector2(360f, 26f);

            // Fundo escuro da barra
            var bossBgImg = bossBarObj.AddComponent<Image>();
            bossBgImg.color = new Color(0.18f, 0.08f, 0.14f, 0.9f);

            // Fill vermelho da barra
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bossBarObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.97f, 0.3f, 0.35f, 1f); // Vermelho vibrante
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

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
            hudSo.FindProperty("bossBarContainer").objectReferenceValue = bossBarObj;
            hudSo.FindProperty("bossHealthFill").objectReferenceValue = fillImg;
            hudSo.ApplyModifiedProperties();
        }

        private static void SetupBossAndCutscene()
        {
            var existingBoss = GameObject.Find("Boss_Golem");
            if (existingBoss != null) Object.DestroyImmediate(existingBoss);

            var existingCutscene = GameObject.Find("Cutscene_Manager");
            if (existingCutscene != null) Object.DestroyImmediate(existingCutscene);

            // 1. Criar Boss GameObject
            GameObject bossObj = new GameObject("Boss_Golem");
            bossObj.tag = "Boss";
            bossObj.transform.position = new Vector3(14f, 0f, 0f); // Inicia fora da tela

            Sprite headSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Golem_Head.png");
            Sprite bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Golem_Body.png");
            Sprite stoneBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/StoneWall_Block.png");

            // Aura circular roxa no fundo (fiel à imagem!)
            GameObject aura = new GameObject("Aura");
            aura.transform.SetParent(bossObj.transform);
            aura.transform.localPosition = Vector3.zero;
            var auraSr = aura.AddComponent<SpriteRenderer>();
            auraSr.sortingOrder = 1;
            auraSr.color = new Color(0.24f, 0.12f, 0.35f, 0.85f); // Roxo escuro
            // Gera círculo de aura
            Texture2D auraTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            auraTex.filterMode = FilterMode.Point;
            Color[] auraCols = new Color[64 * 64];
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                    auraCols[y * 64 + x] = (d <= 30f) ? Color.white : Color.clear;
                }
            }
            auraTex.SetPixels(auraCols);
            auraTex.Apply();
            auraSr.sprite = Sprite.Create(auraTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 12f);

            // Corpo central
            GameObject body = new GameObject("Body");
            body.transform.SetParent(bossObj.transform);
            body.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = bodySprite;
            bodySr.sortingOrder = 3;

            // Cabeça
            GameObject head = new GameObject("Head");
            head.transform.SetParent(bossObj.transform);
            head.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            var headSr = head.AddComponent<SpriteRenderer>();
            headSr.sprite = headSprite;
            headSr.sortingOrder = 4;

            // Ombro / Braço esquerdo e direito
            if (stoneBlockSprite != null)
            {
                GameObject leftShoulder = new GameObject("Left_Plate");
                leftShoulder.transform.SetParent(bossObj.transform);
                leftShoulder.transform.localPosition = new Vector3(-2.2f, 0.4f, 0f);
                var lsSr = leftShoulder.AddComponent<SpriteRenderer>();
                lsSr.sprite = stoneBlockSprite;
                lsSr.sortingOrder = 2;

                GameObject rightShoulder = new GameObject("Right_Plate");
                rightShoulder.transform.SetParent(bossObj.transform);
                rightShoulder.transform.localPosition = new Vector3(2.2f, 0.4f, 0f);
                var rsSr = rightShoulder.AddComponent<SpriteRenderer>();
                rsSr.sprite = stoneBlockSprite;
                rsSr.sortingOrder = 2;
            }

            // Collider principal do Boss
            var col = bossObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.5f, 4.8f);

            var golem = bossObj.AddComponent<GolemBoss>();

            // Conectar prefabs de ataques no GolemBoss
            GameObject bigRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_BigRock.prefab");
            GameObject stoneWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_StoneWall.prefab");
            GameObject miniStonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_OrbitingStone.prefab");

            SerializedObject golemSo = new SerializedObject(golem);
            golemSo.FindProperty("bigRockPrefab").objectReferenceValue = bigRockPrefab;
            golemSo.FindProperty("stoneWallPrefab").objectReferenceValue = stoneWallPrefab;
            golemSo.FindProperty("orbitingStonePrefab").objectReferenceValue = miniStonePrefab;
            golemSo.FindProperty("mainBodyRenderer").objectReferenceValue = bodySr;
            golemSo.ApplyModifiedProperties();

            // 2. Criar Cutscene Manager
            GameObject cutsceneObj = new GameObject("Cutscene_Manager");
            var cutscene = cutsceneObj.AddComponent<BossIntroCutscene>();

            var player = GameObject.FindWithTag("Player");
            SerializedObject csSo = new SerializedObject(cutscene);
            csSo.FindProperty("golemBoss").objectReferenceValue = golem;
            if (player != null)
            {
                csSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
                csSo.FindProperty("playerShooting").objectReferenceValue = player.GetComponent<PlayerShooting>();
                csSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
            }
            csSo.ApplyModifiedProperties();
        }

        private static void SetupWaveSpawner()
        {
            var existingSpawner = GameObject.Find("WaveSpawner");
            if (existingSpawner != null) Object.DestroyImmediate(existingSpawner);

            GameObject spawnerObj = new GameObject("WaveSpawner");
            var spawner = spawnerObj.AddComponent<WaveSpawner>();

            GameObject rusherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Enemy_Rusher.prefab");
            GameObject batPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Enemy_Bat.prefab");
            var cutscene = Object.FindFirstObjectByType<BossIntroCutscene>();

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("rusherPrefab").objectReferenceValue = rusherPrefab;
            so.FindProperty("batPrefab").objectReferenceValue = batPrefab;
            so.FindProperty("bossCutscene").objectReferenceValue = cutscene;
            so.FindProperty("stageDuration").floatValue = 35f; // 35 segundos de fase antes do Boss
            so.ApplyModifiedProperties();
        }
    }
}
#endif
