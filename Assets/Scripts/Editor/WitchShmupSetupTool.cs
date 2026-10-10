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
using WitchShmup.Core;
using WitchShmup.Cutscenes;
using WitchShmup.Drops;
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
            EnsureSpriteImportSettings("FireHeart.png", 16f);
            EnsureSpriteImportSettings("HealthPotion.png", 16f);
            EnsureSpriteImportSettings("SkillPoint.png", 16f);
            EnsureSpriteImportSettings("BossCore.png", 16f);
            EnsureSpriteImportSettings("Staff_Basic.png", 16f);
            EnsureSpriteImportSettings("Staff_Evolved.png", 16f);

            AssetDatabase.Refresh();

            // 1. Projéteis do Player
            Sprite iceBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/IceBullet.png");
            Sprite fireBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/FireBullet.png");
            CreateProjectilePrefab("Projectile_Ice", iceBulletSprite, ElementType.Ice, 18f, 1.2f, 2, true);
            CreateProjectilePrefab("Projectile_Fire", fireBulletSprite, ElementType.Fire, 14f, 2.5f, 1, true);

            // 2. Projétil de Fogo do Morcego
            CreateProjectilePrefab("Enemy_Fireball", fireBulletSprite, ElementType.Enemy, 8f, 1f, 1, false);

            // 3. Drops Coletáveis
            Sprite fireHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/FireHeart.png");
            Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/HealthPotion.png");
            Sprite skillPointSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/SkillPoint.png");
            Sprite bossCoreSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/BossCore.png");

            CreatePickupPrefab("Pickup_FireHeart", fireHeartSprite, PickupType.FireHeart);
            CreatePickupPrefab("Pickup_HealthPotion", potionSprite, PickupType.HealthPotion);
            CreatePickupPrefab("Pickup_SkillPoint", skillPointSprite, PickupType.SkillPoint);
            CreatePickupPrefab("Pickup_BossCore", bossCoreSprite, PickupType.BossCore, 5f);

            // 4. Inimigo Rusher Prefab
            Sprite rusherSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Rusher.png");
            CreateRusherPrefab(rusherSprite);

            // 5. Inimigo Morcego Prefab
            Sprite batSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Bat.png");
            GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Enemy_Fireball.prefab");
            CreateBatPrefab(batSprite, fireballPrefab);

            // 6. Boss Prefabs: Pedra Gigante, Pedra Orbital, Parede de Pedra
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

        [MenuItem("Tools/Witch Shmup/2. Montar Cena Atual (Completa com Drops, Evolução e Derrota)")]
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

            // 4. Setup HUD, GameOverUI, EvolutionUI e GameManager
            SetupHUD();

            // 5. Setup Golem Boss e Cutscene
            SetupBossAndCutscene();

            // 6. Setup Wave Spawner
            SetupWaveSpawner();

            EditorUtility.DisplayDialog("Witch Shmup",
                "Cena configurada com sucesso!\n\n" +
                "Novidades implementadas:\n" +
                "- Dano e morte funcionando em todos os inimigos e no Boss!\n" +
                "- Sistema de Drops (Coração de Fogo, Poção de Cura, Skill Points, Cristal do Boss) com atração magnética!\n" +
                "- Sistema de Pontuação funcionando em tempo real!\n" +
                "- Tela de Derrota (Game Over) ao zerar os corações!\n" +
                "- Tela de Evolução e Árvore de Habilidades em Y (Pressione 'E' para abrir e pausar o jogo)!\n\n" +
                "Atalhos úteis no Play Mode:\n" +
                "- 'E': Abre/Fecha Tela de Evolução\n" +
                "- 'B': Chama a Cutscene do Boss imediatamente", "Jogar!");
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

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var proj = obj.AddComponent<Projectile>();
            proj.Initialize(Vector2.right, speed, damage, element, isPlayerShot);

            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
        }

        private static void CreatePickupPrefab(string name, Sprite sprite, PickupType type, float magnetRadius = 3.5f)
        {
            string path = $"{PrefabsPath}/{name}.prefab";
            GameObject obj = new GameObject(name);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 9;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var pickup = obj.AddComponent<PickupItem>();
            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("pickupType").enumValueIndex = (int)type;
            so.FindProperty("magnetRadius").floatValue = magnetRadius;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(obj, path);
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

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var rusher = obj.AddComponent<RusherEnemy>();

            // Drops do Rusher
            GameObject potionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_HealthPotion.prefab");
            GameObject skillPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_SkillPoint.prefab");

            SerializedObject so = new SerializedObject(rusher);
            so.FindProperty("healthPotionDropPrefab").objectReferenceValue = potionPrefab;
            so.FindProperty("skillPointDropPrefab").objectReferenceValue = skillPointPrefab;
            so.FindProperty("healthPotionDropChance").floatValue = 0.20f;
            so.FindProperty("skillPointDropChance").floatValue = 0.25f;
            so.ApplyModifiedProperties();

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

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bat = obj.AddComponent<BatEnemy>();

            // Drops do Morcego: Coração de Fogo, Poção de Cura, Skill Points
            GameObject fireHeartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_FireHeart.prefab");
            GameObject potionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_HealthPotion.prefab");
            GameObject skillPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_SkillPoint.prefab");

            SerializedObject so = new SerializedObject(bat);
            so.FindProperty("fireballPrefab").objectReferenceValue = fireballPrefab;
            so.FindProperty("fireHeartDropPrefab").objectReferenceValue = fireHeartPrefab;
            so.FindProperty("healthPotionDropPrefab").objectReferenceValue = potionPrefab;
            so.FindProperty("skillPointDropPrefab").objectReferenceValue = skillPointPrefab;
            so.FindProperty("fireHeartDropChance").floatValue = 0.40f;
            so.FindProperty("healthPotionDropChance").floatValue = 0.25f;
            so.FindProperty("skillPointDropChance").floatValue = 0.35f;
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

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

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

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

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

                var rb = seg.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;

                seg.AddComponent<StoneWallSegment>();
            }

            PrefabUtility.SaveAsPrefabAsset(wall, path);
            Object.DestroyImmediate(wall);
        }

        private static void SetupParallaxHierarchy(Camera cam)
        {
            // Remove o GameObject antigo (parallax ou estático)
            var existingBg = GameObject.Find("Environment_Parallax");
            if (existingBg != null) Object.DestroyImmediate(existingBg);
            var existingStatic = GameObject.Find("Environment_Background");
            if (existingStatic != null) Object.DestroyImmediate(existingStatic);

            // ---- Cenário ESTÁTICO: apenas fundo de tijolos + tochas fixas ----
            GameObject bgRoot = new GameObject("Environment_Background");
            bgRoot.transform.position = Vector3.zero;

            Sprite brickSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/BrickWall.png");

            if (brickSprite != null)
            {
                // Um único segmento de tijolos cobrindo a tela inteira (estático, sem movimento)
                // Câmera ortográfica size=5, aspecto 16:9 → janela ≈ 18x10 unidades
                float bgW = 22f;
                float bgH = 12f;

                GameObject bgWall = new GameObject("BrickWall_Background");
                bgWall.transform.SetParent(bgRoot.transform);
                bgWall.transform.position = new Vector3(0f, 0f, 2f);

                var sr = bgWall.AddComponent<SpriteRenderer>();
                sr.sprite    = brickSprite;
                sr.drawMode  = SpriteDrawMode.Tiled;
                sr.size      = new Vector2(bgW, bgH);
                sr.sortingOrder = -10;
                sr.color     = new Color(0.55f, 0.55f, 0.72f, 1f);

                // Tochas FIXAS distribuídas pelo cenário
                float[] torchXPositions = { -8f, -4f, 0f, 4f, 8f };
                float torchY = 1.3f;
                foreach (float tx in torchXPositions)
                {
                    DungeonTorch.CreateTorch(bgRoot.transform, new Vector3(tx, torchY, 0f));
                }
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

            // Setup fire points: Inicia com apenas 1 projétil central!
            GameObject muzzleCenter = new GameObject("Muzzle_Center");
            muzzleCenter.transform.SetParent(playerObj.transform);
            muzzleCenter.transform.localPosition = new Vector3(0.8f, 0f, 0f);

            GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Ice.prefab");
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Fire.prefab");

            SerializedObject so = new SerializedObject(shooting);
            so.FindProperty("iceConfig.projectilePrefab").objectReferenceValue = icePrefab;
            so.FindProperty("fireConfig.projectilePrefab").objectReferenceValue = firePrefab;

            SerializedProperty firePointsProp = so.FindProperty("firePoints");
            firePointsProp.arraySize = 1;
            firePointsProp.GetArrayElementAtIndex(0).objectReferenceValue = muzzleCenter.transform;
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
            fillImg.color = new Color(0.97f, 0.3f, 0.35f, 1f);
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

            // --- EventSystem (Essencial para cliques e interação de botões na UI) ---
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // --- GameManager ---
            var existingGm = Object.FindFirstObjectByType<GameManager>();
            if (existingGm == null)
            {
                var gmObj = new GameObject("GameManager");
                existingGm = gmObj.AddComponent<GameManager>();
            }

            // --- GameOver Screen (Derrota) ---
            GameObject gameOverPanel = SetupGameOverPanel(canvasObj.transform);

            // --- Victory Screen (Vitória) ---
            GameObject victoryPanel = SetupVictoryPanel(canvasObj.transform);

            // --- Evolution Screen (Árvore de Upgrades do Esboço HTML) ---
            GameObject evolutionPanel = SetupEvolutionPanel(canvasObj.transform);

            // Connect panels to GameManager
            SerializedObject gmSo = new SerializedObject(existingGm);
            gmSo.FindProperty("hudController").objectReferenceValue = hud;
            gmSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            gmSo.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            gmSo.FindProperty("evolutionPanel").objectReferenceValue = evolutionPanel;
            gmSo.ApplyModifiedProperties();
        }

        private static GameObject SetupGameOverPanel(Transform canvasTransform)
        {
            GameObject panel = new GameObject("Panel_GameOver");
            panel.transform.SetParent(canvasTransform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = panel.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.04f, 0.08f, 0.92f);

            var goUI = panel.AddComponent<GameOverUI>();

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 120f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "VOCÊ FOI DERROTADO";
            tTmp.fontSize = 52;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(0.98f, 0.28f, 0.35f, 1f);

            // Score text
            GameObject scoreObj = new GameObject("FinalScore");
            scoreObj.transform.SetParent(panel.transform, false);
            var sRect = scoreObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 20f);
            var sTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "PONTUAÇÃO FINAL: 0000000";
            sTmp.fontSize = 30;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = Color.white;

            // Retry Button
            GameObject btnObj = new GameObject("Btn_Retry");
            btnObj.transform.SetParent(panel.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -80f);
            bRect.sizeDelta = new Vector2(280f, 60f);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.75f, 0.5f, 1f);
            var btn = btnObj.AddComponent<Button>();

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var btTmp = btnTxtObj.AddComponent<TextMeshProUGUI>();
            btTmp.text = "TENTAR NOVAMENTE";
            btTmp.fontSize = 24;
            btTmp.fontStyle = FontStyles.Bold;
            btTmp.alignment = TextAlignmentOptions.Center;
            btTmp.color = Color.white;

            SerializedObject goSo = new SerializedObject(goUI);
            goSo.FindProperty("finalScoreText").objectReferenceValue = sTmp;
            goSo.FindProperty("retryButton").objectReferenceValue = btn;
            goSo.ApplyModifiedProperties();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject SetupVictoryPanel(Transform canvasTransform)
        {
            GameObject panel = new GameObject("Panel_Victory");
            panel.transform.SetParent(canvasTransform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = panel.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.16f, 0.94f);

            var vicUI = panel.AddComponent<VictoryUI>();

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 130f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "VITÓRIA!";
            tTmp.fontSize = 58;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(panel.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 75f);
            var subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "O GOLEM DE PEDRA FOI DESTRUÍDO!";
            subTmp.fontSize = 24;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.6f, 0.95f, 0.8f, 1f);

            // Final score text
            GameObject scoreObj = new GameObject("FinalScore");
            scoreObj.transform.SetParent(panel.transform, false);
            var sRect = scoreObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 0f);
            var sTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "PONTUAÇÃO FINAL: 0000000";
            sTmp.fontSize = 30;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = Color.white;

            // Stats text
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(panel.transform, false);
            var stRect = statsObj.AddComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(0f, -55f);
            var stTmp = statsObj.AddComponent<TextMeshProUGUI>();
            stTmp.text = "Parabéns, você completou a fase!";
            stTmp.fontSize = 20;
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.color = new Color(0.85f, 0.85f, 0.95f, 1f);

            // Play Again button
            GameObject btnObj = new GameObject("Btn_PlayAgain");
            btnObj.transform.SetParent(panel.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -125f);
            bRect.sizeDelta = new Vector2(300f, 60f);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.72f, 0.42f, 1f);
            var btn = btnObj.AddComponent<Button>();

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var btTmp = btnTxtObj.AddComponent<TextMeshProUGUI>();
            btTmp.text = "JOGAR NOVAMENTE";
            btTmp.fontSize = 24;
            btTmp.fontStyle = FontStyles.Bold;
            btTmp.alignment = TextAlignmentOptions.Center;
            btTmp.color = Color.white;

            SerializedObject voSo = new SerializedObject(vicUI);
            voSo.FindProperty("victoryTitleText").objectReferenceValue = tTmp;
            voSo.FindProperty("finalScoreText").objectReferenceValue = sTmp;
            voSo.FindProperty("statsText").objectReferenceValue = stTmp;
            voSo.FindProperty("playAgainButton").objectReferenceValue = btn;
            voSo.ApplyModifiedProperties();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject SetupEvolutionPanel(Transform canvasTransform)
        {
            GameObject panel = new GameObject("Panel_Evolution");
            panel.transform.SetParent(canvasTransform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = panel.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.04f, 0.09f, 0.94f);

            var evoUI = panel.AddComponent<EvolutionUI>();

            // Frame principal (#1d1530)
            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(panel.transform, false);
            var fRect = frame.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0.5f, 0.5f);
            fRect.anchorMax = new Vector2(0.5f, 0.5f);
            fRect.sizeDelta = new Vector2(1100f, 680f);
            var fImg = frame.AddComponent<Image>();
            fImg.color = new Color(0.11f, 0.08f, 0.19f, 1f);

            // Header Container
            GameObject header = new GameObject("Header");
            header.transform.SetParent(frame.transform, false);
            var hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -14f);
            hRect.sizeDelta = new Vector2(-40f, 60f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.5f);
            tRect.anchorMax = new Vector2(0f, 0.5f);
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.anchoredPosition = new Vector2(10f, 12f);
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "ÁRVORE DE UPGRADES";
            tTmp.fontSize = 24;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(1f, 0.61f, 0.73f, 1f);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(header.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.5f);
            subRect.anchorMax = new Vector2(0f, 0.5f);
            subRect.pivot = new Vector2(0f, 0.5f);
            subRect.anchoredPosition = new Vector2(10f, -12f);
            var subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Inimigos dropam skill points. Clique em um nó para ver e desbloquear.";
            subTmp.fontSize = 15;
            subTmp.color = new Color(0.65f, 0.58f, 0.8f, 1f);

            // Skill Points Box
            GameObject spBox = new GameObject("SP_Box");
            spBox.transform.SetParent(header.transform, false);
            var spbRect = spBox.AddComponent<RectTransform>();
            spbRect.anchorMin = new Vector2(1f, 0.5f);
            spbRect.anchorMax = new Vector2(1f, 0.5f);
            spbRect.pivot = new Vector2(1f, 0.5f);
            spbRect.anchoredPosition = new Vector2(-60f, 0f);
            spbRect.sizeDelta = new Vector2(190f, 48f);
            var spbImg = spBox.AddComponent<Image>();
            spbImg.color = new Color(0.04f, 0.03f, 0.06f, 1f);

            GameObject spTxtObj = new GameObject("Text");
            spTxtObj.transform.SetParent(spBox.transform, false);
            var sptRect = spTxtObj.AddComponent<RectTransform>();
            sptRect.anchorMin = Vector2.zero;
            sptRect.anchorMax = Vector2.one;
            sptRect.sizeDelta = Vector2.zero;
            var spTmp = spTxtObj.AddComponent<TextMeshProUGUI>();
            spTmp.text = "SKILL POINTS: 2";
            spTmp.fontSize = 18;
            spTmp.fontStyle = FontStyles.Bold;
            spTmp.alignment = TextAlignmentOptions.Center;
            spTmp.color = new Color(1f, 0.78f, 0.23f, 1f);

            // Close button (X)
            GameObject closeObj = new GameObject("Btn_Close");
            closeObj.transform.SetParent(header.transform, false);
            var cRect = closeObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0.5f);
            cRect.anchorMax = new Vector2(1f, 0.5f);
            cRect.pivot = new Vector2(1f, 0.5f);
            cRect.anchoredPosition = new Vector2(0f, 0f);
            cRect.sizeDelta = new Vector2(44f, 44f);
            var cImg = closeObj.AddComponent<Image>();
            cImg.color = new Color(0.8f, 0.2f, 0.25f, 1f);
            var closeBtn = closeObj.AddComponent<Button>();

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            var ctRect = closeTxtObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "X";
            ctTmp.fontSize = 24;
            ctTmp.fontStyle = FontStyles.Bold;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = Color.white;

            // --- Body: Split em Árvore (Esquerda) e Card de Detalhes (Direita) ---
            GameObject body = new GameObject("Body");
            body.transform.SetParent(frame.transform, false);
            var bRect = body.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0f);
            bRect.anchorMax = new Vector2(1f, 1f);
            bRect.anchoredPosition = new Vector2(0f, -40f);
            bRect.sizeDelta = new Vector2(-40f, -120f);

            // Container da Árvore
            GameObject treeBox = new GameObject("TreeContainer");
            treeBox.transform.SetParent(body.transform, false);
            var tbRect = treeBox.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 0.5f);
            tbRect.anchorMax = new Vector2(0f, 0.5f);
            tbRect.pivot = new Vector2(0f, 0.5f);
            tbRect.anchoredPosition = new Vector2(0f, 0f);
            tbRect.sizeDelta = new Vector2(650f, 520f);
            var tbImg = treeBox.AddComponent<Image>();
            tbImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);

            // Container de Linhas
            GameObject linesBox = new GameObject("Lines");
            linesBox.transform.SetParent(treeBox.transform, false);
            var lbRect = linesBox.AddComponent<RectTransform>();
            lbRect.anchorMin = Vector2.zero;
            lbRect.anchorMax = Vector2.one;
            lbRect.sizeDelta = Vector2.zero;

            // Dados dos 10 Nós conforme HTML sketch
            var nodeConfigs = new (string id, string name, string desc, string req, int max, int baseCost, string eff, string unit, float val, Vector2 pos, string icon)[]
            {
                ("vit", "Vitalidade", "Seu corpo de mago aguenta mais pancada na masmorra.", null, 5, 1, "+1 Vida Máxima", " de vida", 1f, new Vector2(0f, -185f), "HP"),
                ("vel", "Vassoura Veloz", "A vassoura corta o ar mais rápido para desviar dos tiros.", "vit", 3, 1, "+8% de velocidade", "% de velocidade", 8f, new Vector2(0f, -115f), "VEL"),
                ("foc", "Foco Arcano", "O cristal do cajado brilha mais forte e causa mais dano.", "vel", 3, 2, "+0.5 de dano mágico", " de dano", 0.5f, new Vector2(0f, -45f), "DMG"),
                ("des", "Despertar Arcano", "Libera os dois caminhos: proteção e ofensiva.", "foc", 1, 2, "+1 Vida e +0.5 de dano", "", 0f, new Vector2(0f, 25f), "★"),

                // Ramo Esquerdo (Defesa)
                ("pel", "Pele de Pedra", "Você aprendeu um truque com o golem: pancadas doem menos.", "des", 3, 2, "8% menos dano recebido", "% menos dano", 8f, new Vector2(-95f, 95f), "DEF"),
                ("ima", "Ímã de Almas", "Os drops de skill points voam até você de mais longe.", "pel", 2, 2, "+35% alcance dos drops", "% alcance", 35f, new Vector2(-175f, 155f), "IMA"),
                ("seg", "Segundo Fôlego", "Quando a vida zera, você revive uma vez com vida.", "ima", 1, 5, "Revive 1x com 35% de vida", "", 0f, new Vector2(-245f, 195f), "REV"),

                // Ramo Direito (Ataque)
                ("rap", "Tiro Rápido", "O cajado dispara magias com mais frequência.", "des", 4, 2, "+10% de cadência de tiro", "% de cadência", 10f, new Vector2(95f, 95f), "ATK"),
                ("dup", "Tiro Duplo", "Cada disparo solta 2 magias paralelas ao mesmo tempo.", "rap", 1, 4, "+1 Projétil por disparo (Tiro Duplo!)", "", 0f, new Vector2(175f, 155f), "x2"),
                ("per", "Tiro Perfurante", "As magias atravessam o primeiro inimigo e acertam o próximo.", "dup", 1, 5, "Tiros atravessam 1 inimigo", "", 0f, new Vector2(245f, 195f), "PER")
            };

            var posDict = new Dictionary<string, Vector2>();
            foreach (var item in nodeConfigs) posDict[item.id] = item.pos;

            // Linhas de conexão
            foreach (var item in nodeConfigs)
            {
                if (!string.IsNullOrEmpty(item.req) && posDict.ContainsKey(item.req))
                {
                    Vector2 start = posDict[item.req];
                    Vector2 end = item.pos;
                    CreateUILine(linesBox.transform, start, end, new Color(0.29f, 0.21f, 0.45f, 1f));
                }
            }

            // Criação dos Nós
            SerializedObject evoSo = new SerializedObject(evoUI);
            SerializedProperty nodesProp = evoSo.FindProperty("nodes");
            nodesProp.arraySize = nodeConfigs.Length;

            for (int i = 0; i < nodeConfigs.Length; i++)
            {
                var cfg = nodeConfigs[i];

                GameObject nObj = new GameObject($"Node_{cfg.id}");
                nObj.transform.SetParent(treeBox.transform, false);
                var nRect = nObj.AddComponent<RectTransform>();
                nRect.anchoredPosition = cfg.pos;
                nRect.sizeDelta = new Vector2(50f, 50f);

                // Anel de seleção
                GameObject selObj = new GameObject("Ring");
                selObj.transform.SetParent(nObj.transform, false);
                var selRect = selObj.AddComponent<RectTransform>();
                selRect.anchorMin = Vector2.zero;
                selRect.anchorMax = Vector2.one;
                selRect.sizeDelta = new Vector2(10f, 10f);
                var selImg = selObj.AddComponent<Image>();
                selImg.color = new Color(0.37f, 0.90f, 1f, 0.8f);
                selObj.SetActive(false);

                // Borda / Fundo
                var borderImg = nObj.AddComponent<Image>();
                borderImg.color = new Color(0.14f, 0.11f, 0.20f, 1f);

                // Botão
                var btn = nObj.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.37f, 0.90f, 1.0f, 1f);
                colors.pressedColor = new Color(1f, 0.78f, 0.23f, 1f);
                btn.colors = colors;

                // Ícone Texto
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(nObj.transform, false);
                var icRect = iconObj.AddComponent<RectTransform>();
                icRect.anchorMin = Vector2.zero;
                icRect.anchorMax = Vector2.one;
                icRect.sizeDelta = Vector2.zero;
                var icTmp = iconObj.AddComponent<TextMeshProUGUI>();
                icTmp.text = cfg.icon;
                icTmp.fontSize = (cfg.icon.Length > 2) ? 14 : 18;
                icTmp.fontStyle = FontStyles.Bold;
                icTmp.alignment = TextAlignmentOptions.Center;
                icTmp.color = Color.white;

                // Nível Texto (abaixo do nó)
                GameObject lvlObj = new GameObject("LevelText");
                lvlObj.transform.SetParent(nObj.transform, false);
                var lRect = lvlObj.AddComponent<RectTransform>();
                lRect.anchoredPosition = new Vector2(0f, -32f);
                lRect.sizeDelta = new Vector2(60f, 20f);
                var lTmp = lvlObj.AddComponent<TextMeshProUGUI>();
                lTmp.text = $"0/{cfg.max}";
                lTmp.fontSize = 13;
                lTmp.alignment = TextAlignmentOptions.Center;
                lTmp.color = new Color(0.65f, 0.58f, 0.8f, 1f);

                // Serializa dados
                var elem = nodesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("id").stringValue = cfg.id;
                elem.FindPropertyRelative("name").stringValue = cfg.name;
                elem.FindPropertyRelative("description").stringValue = cfg.desc;
                elem.FindPropertyRelative("requirementId").stringValue = cfg.req ?? "";
                elem.FindPropertyRelative("maxLevel").intValue = cfg.max;
                elem.FindPropertyRelative("baseCost").intValue = cfg.baseCost;
                elem.FindPropertyRelative("effectText").stringValue = cfg.eff;
                elem.FindPropertyRelative("unitText").stringValue = cfg.unit;
                elem.FindPropertyRelative("valuePerLevel").floatValue = cfg.val;
                elem.FindPropertyRelative("nodeButton").objectReferenceValue = btn;
                elem.FindPropertyRelative("nodeBorder").objectReferenceValue = borderImg;
                elem.FindPropertyRelative("nodeLevelText").objectReferenceValue = lTmp;
                elem.FindPropertyRelative("selectionRing").objectReferenceValue = selObj;
            }

            // --- Card de Detalhes (Direita) ---
            GameObject card = new GameObject("DetailCard");
            card.transform.SetParent(body.transform, false);
            var cCardRect = card.AddComponent<RectTransform>();
            cCardRect.anchorMin = new Vector2(1f, 0.5f);
            cCardRect.anchorMax = new Vector2(1f, 0.5f);
            cCardRect.pivot = new Vector2(1f, 0.5f);
            cCardRect.anchoredPosition = new Vector2(0f, 0f);
            cCardRect.sizeDelta = new Vector2(380f, 520f);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.91f, 0.84f, 0.97f, 1f);

            // Top Row
            GameObject dTop = new GameObject("Top");
            dTop.transform.SetParent(card.transform, false);
            var dtRect = dTop.AddComponent<RectTransform>();
            dtRect.anchorMin = new Vector2(0f, 1f);
            dtRect.anchorMax = new Vector2(1f, 1f);
            dtRect.pivot = new Vector2(0.5f, 1f);
            dtRect.anchoredPosition = new Vector2(0f, -12f);
            dtRect.sizeDelta = new Vector2(-24f, 40f);

            GameObject dnObj = new GameObject("DetailName");
            dnObj.transform.SetParent(dTop.transform, false);
            var dnRect = dnObj.AddComponent<RectTransform>();
            dnRect.anchorMin = new Vector2(0f, 0.5f);
            dnRect.anchorMax = new Vector2(0.65f, 0.5f);
            dnRect.pivot = new Vector2(0f, 0.5f);
            var dnTmp = dnObj.AddComponent<TextMeshProUGUI>();
            dnTmp.text = "Vitalidade";
            dnTmp.fontSize = 20;
            dnTmp.fontStyle = FontStyles.Bold;
            dnTmp.color = new Color(0.16f, 0.09f, 0.25f, 1f);

            GameObject dcObj = new GameObject("DetailCost");
            dcObj.transform.SetParent(dTop.transform, false);
            var dcRect = dcObj.AddComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0.68f, 0.5f);
            dcRect.anchorMax = new Vector2(1f, 0.5f);
            dcRect.pivot = new Vector2(1f, 0.5f);
            dcRect.sizeDelta = new Vector2(110f, 32f);
            var dcImg = dcObj.AddComponent<Image>();
            dcImg.color = new Color(0.04f, 0.03f, 0.06f, 1f);
            var dcTmp = dcObj.AddComponent<TextMeshProUGUI>();
            dcTmp.text = "CUSTO: 1 SP";
            dcTmp.fontSize = 14;
            dcTmp.fontStyle = FontStyles.Bold;
            dcTmp.alignment = TextAlignmentOptions.Center;
            dcTmp.color = new Color(1f, 0.78f, 0.23f, 1f);

            // Label Descrição
            GameObject lblDesc = new GameObject("LblDesc");
            lblDesc.transform.SetParent(card.transform, false);
            var ldRect = lblDesc.AddComponent<RectTransform>();
            ldRect.anchoredPosition = new Vector2(0f, 195f);
            ldRect.sizeDelta = new Vector2(330f, 20f);
            var ldTmp = lblDesc.AddComponent<TextMeshProUGUI>();
            ldTmp.text = "Descrição:";
            ldTmp.fontSize = 15;
            ldTmp.color = new Color(0.42f, 0.29f, 0.59f, 1f);

            // Descrição Texto
            GameObject ddObj = new GameObject("DetailDesc");
            ddObj.transform.SetParent(card.transform, false);
            var ddRect = ddObj.AddComponent<RectTransform>();
            ddRect.anchoredPosition = new Vector2(0f, 150f);
            ddRect.sizeDelta = new Vector2(330f, 65f);
            var ddTmp = ddObj.AddComponent<TextMeshProUGUI>();
            ddTmp.text = "Seu corpo de mago aguenta mais pancada na masmorra.";
            ddTmp.fontSize = 15;
            ddTmp.color = new Color(0.16f, 0.09f, 0.25f, 1f);

            // Art Frame
            GameObject artFrame = new GameObject("ArtFrame");
            artFrame.transform.SetParent(card.transform, false);
            var afRect = artFrame.AddComponent<RectTransform>();
            afRect.anchoredPosition = new Vector2(0f, 40f);
            afRect.sizeDelta = new Vector2(330f, 110f);
            var afImg = artFrame.AddComponent<Image>();
            afImg.color = new Color(0.16f, 0.12f, 0.27f, 1f);

            // Pips Container
            GameObject pipsObj = new GameObject("Pips");
            pipsObj.transform.SetParent(artFrame.transform, false);
            var pRect = pipsObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0f, -30f);
            pRect.sizeDelta = new Vector2(200f, 22f);
            var pHlg = pipsObj.AddComponent<HorizontalLayoutGroup>();
            pHlg.spacing = 8f;
            pHlg.childAlignment = TextAnchor.MiddleCenter;
            pHlg.childForceExpandWidth = false;
            pHlg.childForceExpandHeight = false;

            for (int p = 0; p < 5; p++)
            {
                GameObject pip = new GameObject($"Pip_{p}");
                pip.transform.SetParent(pipsObj.transform, false);
                var pipRect = pip.AddComponent<RectTransform>();
                pipRect.sizeDelta = new Vector2(16f, 16f);
                var pipImg = pip.AddComponent<Image>();
                pipImg.color = new Color(0.07f, 0.05f, 0.12f, 1f);
            }

            // Efeito Texto
            GameObject effObj = new GameObject("DetailEffect");
            effObj.transform.SetParent(card.transform, false);
            var efRect = effObj.AddComponent<RectTransform>();
            efRect.anchoredPosition = new Vector2(0f, -50f);
            efRect.sizeDelta = new Vector2(330f, 30f);
            var efTmp = effObj.AddComponent<TextMeshProUGUI>();
            efTmp.text = "+1 Vida Máxima";
            efTmp.fontSize = 20;
            efTmp.fontStyle = FontStyles.Bold;
            efTmp.alignment = TextAlignmentOptions.Center;
            efTmp.color = new Color(0.54f, 0.12f, 0.33f, 1f);

            // Total Texto
            GameObject totObj = new GameObject("DetailTotal");
            totObj.transform.SetParent(card.transform, false);
            var totRect = totObj.AddComponent<RectTransform>();
            totRect.anchoredPosition = new Vector2(0f, -80f);
            totRect.sizeDelta = new Vector2(330f, 25f);
            var totTmp = totObj.AddComponent<TextMeshProUGUI>();
            totTmp.text = "Total Atual: +0 de vida";
            totTmp.fontSize = 15;
            totTmp.alignment = TextAlignmentOptions.Center;
            totTmp.color = new Color(0.42f, 0.29f, 0.59f, 1f);

            // Botão Desbloquear
            GameObject buyBtnObj = new GameObject("Btn_Buy");
            buyBtnObj.transform.SetParent(card.transform, false);
            var bbRect = buyBtnObj.AddComponent<RectTransform>();
            bbRect.anchoredPosition = new Vector2(0f, -140f);
            bbRect.sizeDelta = new Vector2(310f, 48f);
            var bbImg = buyBtnObj.AddComponent<Image>();
            bbImg.color = new Color(1f, 0.36f, 0.56f, 1f);
            var buyBtn = buyBtnObj.AddComponent<Button>();

            GameObject bbTxtObj = new GameObject("Text");
            bbTxtObj.transform.SetParent(buyBtnObj.transform, false);
            var bbtRect = bbTxtObj.AddComponent<RectTransform>();
            bbtRect.anchorMin = Vector2.zero;
            bbtRect.anchorMax = Vector2.one;
            bbtRect.sizeDelta = Vector2.zero;
            var bbtTmp = bbTxtObj.AddComponent<TextMeshProUGUI>();
            bbtTmp.text = "DESBLOQUEAR";
            bbtTmp.fontSize = 18;
            bbtTmp.fontStyle = FontStyles.Bold;
            bbtTmp.alignment = TextAlignmentOptions.Center;
            bbtTmp.color = Color.white;

            // Status Message Texto
            GameObject statusObj = new GameObject("DetailStatus");
            statusObj.transform.SetParent(card.transform, false);
            var stRect = statusObj.AddComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(0f, -195f);
            stRect.sizeDelta = new Vector2(330f, 30f);
            var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "Pronto para desbloquear";
            statusTmp.fontSize = 15;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.color = new Color(0.29f, 0.17f, 0.35f, 1f);

            // Conecta referências no EvolutionUI
            evoSo.FindProperty("skillPointsText").objectReferenceValue = spTmp;
            evoSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
            evoSo.FindProperty("detailNameText").objectReferenceValue = dnTmp;
            evoSo.FindProperty("detailCostText").objectReferenceValue = dcTmp;
            evoSo.FindProperty("detailDescText").objectReferenceValue = ddTmp;
            evoSo.FindProperty("detailEffectText").objectReferenceValue = efTmp;
            evoSo.FindProperty("detailTotalText").objectReferenceValue = totTmp;
            evoSo.FindProperty("detailStatusText").objectReferenceValue = statusTmp;
            evoSo.FindProperty("buyButton").objectReferenceValue = buyBtn;
            evoSo.FindProperty("buyButtonText").objectReferenceValue = bbtTmp;
            evoSo.FindProperty("pipsContainer").objectReferenceValue = pipsObj.transform;
            evoSo.ApplyModifiedProperties();

            evoUI.HookNodeButtons();
            evoUI.SelectNode("vit");

            panel.SetActive(false);
            return panel;
        }

        private static void CreateUILine(Transform parent, Vector2 start, Vector2 end, Color color)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(parent, false);
            var rect = lineObj.AddComponent<RectTransform>();

            Vector2 dir = end - start;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(distance, 4.5f);
            rect.localRotation = Quaternion.Euler(0, 0, angle);

            var img = lineObj.AddComponent<Image>();
            img.color = color;
        }

        private static void SetupBossAndCutscene()
        {
            var existingBoss = GameObject.Find("Boss_Golem");
            if (existingBoss != null) Object.DestroyImmediate(existingBoss);

            var existingCutscene = GameObject.Find("Cutscene_Manager");
            if (existingCutscene != null) Object.DestroyImmediate(existingCutscene);

            // --- 1. Criar Boss GameObject com novo sprite sheet ---
            GameObject bossObj = new GameObject("Boss_Golem");
            bossObj.tag = "Boss";
            bossObj.transform.position = new Vector3(14f, 0f, 0f);

            // Carrega os frames de idle (golem_idle_sheet_64x64_0/1/2)
            Sprite[] idleFrames   = LoadSpriteSheet("golem_idle_sheet_64x64.png",   3);
            // Carrega os frames de ataque (golem_ataque_sheet_64x64_0/1/2)
            Sprite[] attackFrames = LoadSpriteSheet("golem_ataque_sheet_64x64.png", 3);

            // Corpo animado principal (usa o idle como frame inicial)
            GameObject body = new GameObject("Body");
            body.transform.SetParent(bossObj.transform);
            body.transform.localPosition = Vector3.zero;
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sortingOrder = 5;
            if (idleFrames != null && idleFrames.Length > 0)
                bodySr.sprite = idleFrames[0];

            // SimpleSpriteAnimator para animação do boss
            var animator = body.AddComponent<SimpleSpriteAnimator>();

            // Collider principal do Boss
            var col = bossObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.2f, 4f);

            var rb = bossObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var golem = bossObj.AddComponent<GolemBoss>();

            // Conectar prefabs de ataques e drops
            GameObject bigRockPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_BigRock.prefab");
            GameObject stoneWallPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_StoneWall.prefab");
            GameObject miniStonePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boss_OrbitingStone.prefab");
            GameObject bossCorePrefab   = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_BossCore.prefab");
            GameObject skillPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_SkillPoint.prefab");

            SerializedObject golemSo = new SerializedObject(golem);
            golemSo.FindProperty("bigRockPrefab").objectReferenceValue      = bigRockPrefab;
            golemSo.FindProperty("stoneWallPrefab").objectReferenceValue    = stoneWallPrefab;
            golemSo.FindProperty("orbitingStonePrefab").objectReferenceValue = miniStonePrefab;
            golemSo.FindProperty("bossCoreDropPrefab").objectReferenceValue  = bossCorePrefab;
            golemSo.FindProperty("skillPointDropPrefab").objectReferenceValue = skillPointPrefab;
            golemSo.FindProperty("mainBodyRenderer").objectReferenceValue    = bodySr;
            golemSo.FindProperty("bossAnimator").objectReferenceValue        = animator;

            // Injeta frames de idle e ataque diretamente no GolemBoss
            if (idleFrames != null && idleFrames.Length > 0)
            {
                SerializedProperty idleProp = golemSo.FindProperty("idleFrames");
                idleProp.arraySize = idleFrames.Length;
                for (int i = 0; i < idleFrames.Length; i++)
                    idleProp.GetArrayElementAtIndex(i).objectReferenceValue = idleFrames[i];
            }
            if (attackFrames != null && attackFrames.Length > 0)
            {
                SerializedProperty attackProp = golemSo.FindProperty("attackFrames");
                attackProp.arraySize = attackFrames.Length;
                for (int i = 0; i < attackFrames.Length; i++)
                    attackProp.GetArrayElementAtIndex(i).objectReferenceValue = attackFrames[i];
            }
            golemSo.ApplyModifiedProperties();

            // --- 2. Criar Cutscene Manager ---
            GameObject cutsceneObj = new GameObject("Cutscene_Manager");
            var cutscene = cutsceneObj.AddComponent<BossIntroCutscene>();

            var player = GameObject.FindWithTag("Player");
            SerializedObject csSo = new SerializedObject(cutscene);
            csSo.FindProperty("golemBoss").objectReferenceValue = golem;
            if (player != null)
            {
                csSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
                csSo.FindProperty("playerShooting").objectReferenceValue   = player.GetComponent<PlayerShooting>();
                csSo.FindProperty("playerTransform").objectReferenceValue  = player.transform;
            }
            csSo.ApplyModifiedProperties();
        }

        /// <summary>
        /// Carrega os sprites individuais de um sprite sheet.
        /// </summary>
        private static Sprite[] LoadSpriteSheet(string filename, int frameCount)
        {
            string path = $"{SpritesPath}/{filename}";
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var obj in all)
            {
                if (obj is Sprite s) sprites.Add(s);
            }
            // Ordena pelo nome para garantir a ordem correta (_0, _1, _2…)
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.Count > 0 ? sprites.ToArray() : null;
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
            so.FindProperty("stageDuration").floatValue = 75f;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
