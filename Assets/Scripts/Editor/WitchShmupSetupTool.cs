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
            EnsureSpriteImportSettings("Parallax.png", 16f, true);
            EnsureSpriteImportSettings("Parallax 1.png", 16f, true);
            EnsureSpriteImportSettings("Parallax 2.png", 16f, true);
            EnsureSpriteImportSettings("MAGOidle.png", 16f);
            EnsureSpriteImportSettings("MAGOattak.png", 16f);
            EnsureSpriteImportSettings("bola de Fogo.png", 20f);
            EnsureSpriteImportSettings("ataqueMAgo.png", 20f);

            AssetDatabase.Refresh();

            // 1. Projéteis do Player
            Sprite iceBulletSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/ataqueMAgo.png"))
            {
                if (obj is Sprite s) { iceBulletSprite = s; break; }
            }
            if (iceBulletSprite == null) iceBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/ataqueMAgo.png");
            if (iceBulletSprite == null) iceBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/IceBullet.png");

            Sprite fireBulletSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/bola de Fogo.png"))
            {
                if (obj is Sprite s) { fireBulletSprite = s; break; }
            }
            if (fireBulletSprite == null) fireBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/bola de Fogo.png");
            if (fireBulletSprite == null) fireBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/FireBullet.png");

            CreateProjectilePrefab("Projectile_Ice", iceBulletSprite, ElementType.Ice, 18f, 1.2f, 2, true);
            CreateProjectilePrefab("Projectile_Fire", fireBulletSprite, ElementType.Fire, 14f, 2.5f, 1, true);

            // 2. Projétil de Fogo do Morcego
            CreateProjectilePrefab("Enemy_Fireball", fireBulletSprite, ElementType.Enemy, 8f, 1f, 1, false);

            // 3. Drops Coletáveis
            Sprite fireHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/FireHeart.png");
            if (fireHeartSprite == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/FireHeart.png"))
                {
                    if (obj is Sprite s) { fireHeartSprite = s; break; }
                }
            }
            if (fireHeartSprite == null) fireHeartSprite = Resources.Load<Sprite>("Sprites/FireHeart");

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
            Sprite bigRockSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/ataqueBoss2.png"))
            {
                if (obj is Sprite s) { bigRockSprite = s; break; }
            }
            if (bigRockSprite == null) bigRockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/ataqueBoss2.png");
            if (bigRockSprite == null) bigRockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/BigRock.png");
            CreateBigRockPrefab(bigRockSprite);

            Sprite miniStoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/MiniStone.png");
            CreateOrbitingStonePrefab(miniStoneSprite);

            Sprite stoneBlockSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/ATAQUEboss.png"))
            {
                if (obj is Sprite s) { stoneBlockSprite = s; break; }
            }
            if (stoneBlockSprite == null) stoneBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/ATAQUEboss.png");
            if (stoneBlockSprite == null) stoneBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/StoneWall_Block.png");
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

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            }

            EditorUtility.DisplayDialog("Witch Shmup",
                "Cena configurada com sucesso!\n\n" +
                "Novidades implementadas:\n" +
                "- Parallax dinâmico do cenário e tochas com iluminação 2D!\n" +
                "- Dano e morte funcionando em todos os inimigos e no Boss!\n" +
                "- Efeito de desaceleração nos inimigos ao atingir com tiro de gelo (boss imune)!\n" +
                "- Correção definitiva do flash de dano do Boss!\n" +
                "- Barreira de pedra empurra o jogador e esmaga na parede de trás!\n" +
                "- Sistema de Drops (Coração de Fogo, Poção de Cura, Skill Points, Cristal do Boss)!\n" +
                "- Árvore de Habilidades completa (+Corações, +Velocidade dos tiros, +Disparo Múltiplo, Maestria do Gelo e Fúria do Fogo)!\n" +
                "- Desbloqueio do Cajado Flamejante (Poder de Fogo com 3 Corações de Fogo + 1 Núcleo de Boss)!\n\n" +
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

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (repeat && settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    needReimport = true;
                }

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
            GameObject fireHeartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_FireHeart.prefab");
            GameObject potionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_HealthPotion.prefab");
            GameObject skillPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Pickup_SkillPoint.prefab");

            SerializedObject so = new SerializedObject(rusher);
            so.FindProperty("fireHeartDropPrefab").objectReferenceValue = fireHeartPrefab;
            so.FindProperty("healthPotionDropPrefab").objectReferenceValue = potionPrefab;
            so.FindProperty("skillPointDropPrefab").objectReferenceValue = skillPointPrefab;
            so.FindProperty("fireHeartDropChance").floatValue = 0.35f;
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
            // Remove GameObjects antigos (parallax ou estático)
            var existingBg = GameObject.Find("Environment_Parallax");
            if (existingBg != null) Object.DestroyImmediate(existingBg);
            var existingStatic = GameObject.Find("Environment_Background");
            if (existingStatic != null) Object.DestroyImmediate(existingStatic);

            // ---- Cenário PARALLAX Dinâmico: Fundo Distante + Parede Principal com Tochas ----
            GameObject bgRoot = new GameObject("Environment_Parallax");
            bgRoot.transform.position = Vector3.zero;
            var parallaxCtrl = bgRoot.AddComponent<ParallaxBackgroundController>();
            parallaxCtrl.SetScrollSpeed(3.0f);

            Sprite brickSprite = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/Parallax 2.png"))
            {
                if (obj is Sprite s) { brickSprite = s; break; }
            }
            if (brickSprite == null) brickSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Parallax 2.png");
            if (brickSprite == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/Parallax 1.png"))
                {
                    if (obj is Sprite s) { brickSprite = s; break; }
                }
            }
            if (brickSprite == null) brickSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Parallax 1.png");
            if (brickSprite == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/Parallax.png"))
                {
                    if (obj is Sprite s) { brickSprite = s; break; }
                }
            }
            if (brickSprite == null) brickSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Parallax.png");
            if (brickSprite == null) brickSprite = Resources.Load<Sprite>("Sprites/Parallax");

            if (brickSprite != null)
            {
                float segmentWidth = 24f;
                float segmentHeight = 13f;

                // --- 1. Camada Distante (Far Background) - Rola a 0.45x para dar profundidade ---
                GameObject farLayerObj = new GameObject("Parallax_Far");
                farLayerObj.transform.SetParent(bgRoot.transform, false);
                var farLayer = farLayerObj.AddComponent<ParallaxLayer>();
                farLayer.ParallaxFactor = 0.45f;
                farLayer.SegmentWidth = segmentWidth;

                Transform[] farSegments = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject segObj = new GameObject($"Far_Segment_{i}");
                    segObj.transform.SetParent(farLayerObj.transform, false);
                    segObj.transform.position = new Vector3((i - 1) * segmentWidth, 0f, 3f);

                    var sr = segObj.AddComponent<SpriteRenderer>();
                    sr.sprite = brickSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(segmentWidth, segmentHeight);
                    sr.sortingOrder = -15;
                    sr.color = new Color(0.24f, 0.20f, 0.35f, 1f); // Fundo escuro e misterioso

                    farSegments[i] = segObj.transform;
                }
                farLayer.SetSegments(farSegments, segmentWidth);

                // --- 2. Camada Principal (Main Scenario) com TOCHAS - Rola na velocidade base (1.0x) ---
                GameObject mainLayerObj = new GameObject("Parallax_Main");
                mainLayerObj.transform.SetParent(bgRoot.transform, false);
                var mainLayer = mainLayerObj.AddComponent<ParallaxLayer>();
                mainLayer.ParallaxFactor = 1.0f;
                mainLayer.SegmentWidth = segmentWidth;

                Transform[] mainSegments = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject segObj = new GameObject($"Main_Segment_{i}");
                    segObj.transform.SetParent(mainLayerObj.transform, false);
                    segObj.transform.position = new Vector3((i - 1) * segmentWidth, 0f, 1f);

                    var sr = segObj.AddComponent<SpriteRenderer>();
                    sr.sprite = brickSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(segmentWidth, segmentHeight);
                    sr.sortingOrder = -10;
                    sr.color = new Color(0.55f, 0.55f, 0.72f, 1f);

                    // Tochas distribuídas no segmento! Sendo filhas do segObj, rolam e dão wrap-around perfeitamente!
                    float[] torchOffsets = { -8f, 0f, 8f };
                    float torchY = 1.3f;
                    foreach (float tx in torchOffsets)
                    {
                        DungeonTorch.CreateTorch(segObj.transform, new Vector3(tx, torchY, 0f));
                    }

                    mainSegments[i] = segObj.transform;
                }
                mainLayer.SetSegments(mainSegments, segmentWidth);
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

            // AudioSource para o som de tiro
            var audioSrc = playerObj.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.volume = 0.65f;
            audioSrc.spatialBlend = 0f;

            // Setup fire points: Inicia com apenas 1 projétil central!
            GameObject muzzleCenter = new GameObject("Muzzle_Center");
            muzzleCenter.transform.SetParent(playerObj.transform);
            muzzleCenter.transform.localPosition = new Vector3(0.8f, 0f, 0f);

            GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Ice.prefab");
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Projectile_Fire.prefab");

            // Sons de tiro do mago (Gelo e Fogo)
            AudioClip iceClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SpritesPath}/PlayerIce_LancaCongelada.wav");
            AudioClip fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SpritesPath}/PlayerFire_DardoChamas.wav");
            AudioClip fallbackShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SpritesPath}/Tiro.Teste.wav");

            // Animação do Mago: Idle e Ataque
            var visualCtrl = playerObj.AddComponent<PlayerVisualController>();
            List<Sprite> idleList = new List<Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/MAGOidle.png"))
            {
                if (a is Sprite s) idleList.Add(s);
            }

            List<Sprite> attackList = new List<Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/MAGOattak.png"))
            {
                if (a is Sprite s) attackList.Add(s);
            }

            if (idleList.Count > 0 && attackList.Count > 0)
            {
                visualCtrl.SetFrames(idleList.ToArray(), attackList.ToArray());
                sr.sprite = idleList[0];
            }
            else
            {
                Sprite fallbackSprite = null;
                var magoFallbackAssets = AssetDatabase.LoadAllAssetsAtPath($"{SpritesPath}/MAGO.png");
                foreach (var asset in magoFallbackAssets)
                {
                    if (asset is Sprite s) { fallbackSprite = s; break; }
                }
                if (fallbackSprite == null) fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Wizard.png");
                sr.sprite = fallbackSprite;
            }

            SerializedObject so = new SerializedObject(shooting);
            so.FindProperty("iceConfig.projectilePrefab").objectReferenceValue = icePrefab;
            so.FindProperty("fireConfig.projectilePrefab").objectReferenceValue = firePrefab;
            so.FindProperty("isFireUnlocked").boolValue = true;

            // Vincula sons específicos para cada elemento
            so.FindProperty("iceConfig.shootSound").objectReferenceValue = (iceClip != null) ? iceClip : fallbackShootClip;
            so.FindProperty("fireConfig.shootSound").objectReferenceValue = (fireClip != null) ? fireClip : fallbackShootClip;

            // Vincula AudioSource
            so.FindProperty("audioSource").objectReferenceValue = audioSrc;

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var hud = canvasObj.AddComponent<HUDController>();
            Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/Heart.png");

            // --- TOP BAR (Score | Título da fase | Barra do Boss) ---
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(canvasObj.transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, 0f);
            topBarRect.sizeDelta = new Vector2(0f, 72f);
            var topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0.05f, 0.03f, 0.1f, 0.82f);

            // Score Text (canto esquerdo do top bar)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(topBar.transform, false);
            var scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.pivot = new Vector2(0f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(24f, 0f);
            scoreRect.sizeDelta = new Vector2(380f, 0f);
            var scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.text = "SCORE 0000000";
            scoreTmp.fontSize = 30;
            scoreTmp.fontStyle = FontStyles.Bold;
            scoreTmp.color = Color.white;
            scoreTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Stage Title Text (centro do top bar)
            GameObject stageObj = new GameObject("StageText");
            stageObj.transform.SetParent(topBar.transform, false);
            var stageRect = stageObj.AddComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0.35f, 0f);
            stageRect.anchorMax = new Vector2(0.65f, 1f);
            stageRect.pivot = new Vector2(0.5f, 0.5f);
            stageRect.sizeDelta = new Vector2(0f, 0f);
            stageRect.anchoredPosition = Vector2.zero;
            var stageTmp = stageObj.AddComponent<TextMeshProUGUI>();
            stageTmp.text = "FASE 1 - RUÍNAS DE PEDRA";
            stageTmp.fontSize = 26;
            stageTmp.fontStyle = FontStyles.Bold;
            stageTmp.color = new Color(0.9f, 0.85f, 1f, 1f);
            stageTmp.alignment = TextAlignmentOptions.Center;

            // === BOSS HEALTH BAR (canto direito do top bar) ===
            GameObject bossBarWrapper = new GameObject("BossBar_Wrapper");
            bossBarWrapper.transform.SetParent(topBar.transform, false);
            var bossWrapRect = bossBarWrapper.AddComponent<RectTransform>();
            bossWrapRect.anchorMin = new Vector2(1f, 0f);
            bossWrapRect.anchorMax = new Vector2(1f, 1f);
            bossWrapRect.pivot = new Vector2(1f, 0.5f);
            bossWrapRect.anchoredPosition = new Vector2(-24f, 0f);
            bossWrapRect.sizeDelta = new Vector2(400f, 0f);

            // Nome do Boss (label acima da barra)
            GameObject bossNameObj = new GameObject("BossNameText");
            bossNameObj.transform.SetParent(bossBarWrapper.transform, false);
            var bossNameRect = bossNameObj.AddComponent<RectTransform>();
            bossNameRect.anchorMin = new Vector2(0f, 0.55f);
            bossNameRect.anchorMax = new Vector2(1f, 1f);
            bossNameRect.sizeDelta = Vector2.zero;
            bossNameRect.anchoredPosition = Vector2.zero;
            var bossNameTmp = bossNameObj.AddComponent<TextMeshProUGUI>();
            bossNameTmp.text = "GOLEM DE PEDRA";
            bossNameTmp.fontSize = 20;
            bossNameTmp.fontStyle = FontStyles.Bold;
            bossNameTmp.color = new Color(1f, 0.85f, 0.85f, 1f);
            bossNameTmp.alignment = TextAlignmentOptions.MidlineRight;

            // Container da barra de HP
            GameObject bossBarObj = new GameObject("BossHealthBar");
            bossBarObj.transform.SetParent(bossBarWrapper.transform, false);
            var bossBarRect = bossBarObj.AddComponent<RectTransform>();
            bossBarRect.anchorMin = new Vector2(0f, 0f);
            bossBarRect.anchorMax = new Vector2(1f, 0.5f);
            bossBarRect.sizeDelta = Vector2.zero;
            bossBarRect.anchoredPosition = Vector2.zero;
            var bossBgImg = bossBarObj.AddComponent<Image>();
            bossBgImg.color = new Color(0.18f, 0.08f, 0.14f, 0.95f);

            // Fill vermelho da barra
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bossBarObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.01f, 0.08f);
            fillRect.anchorMax = new Vector2(0.99f, 0.92f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.97f, 0.3f, 0.35f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            // --- BOTTOM BAR (Corações | Indicador elemento | Poções) ---
            GameObject bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(canvasObj.transform, false);
            var bottomBarRect = bottomBar.AddComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0f, 0f);
            bottomBarRect.anchorMax = new Vector2(1f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.anchoredPosition = new Vector2(0f, 0f);
            bottomBarRect.sizeDelta = new Vector2(0f, 90f);
            var bottomBg = bottomBar.AddComponent<Image>();
            bottomBg.color = new Color(0.05f, 0.03f, 0.1f, 0.82f);

            // Hearts Container (canto esquerdo do bottom)
            GameObject heartsContainer = new GameObject("HeartsContainer");
            heartsContainer.transform.SetParent(bottomBar.transform, false);
            var heartsRect = heartsContainer.AddComponent<RectTransform>();
            heartsRect.anchorMin = new Vector2(0f, 0f);
            heartsRect.anchorMax = new Vector2(0f, 1f);
            heartsRect.pivot = new Vector2(0f, 0.5f);
            heartsRect.anchoredPosition = new Vector2(24f, 0f);
            heartsRect.sizeDelta = new Vector2(240f, 0f);
            var hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            Image[] heartImgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject hObj = new GameObject($"Heart_{i}");
                hObj.transform.SetParent(heartsContainer.transform, false);
                var hRect = hObj.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(52f, 52f);
                var img = hObj.AddComponent<Image>();
                img.sprite = heartSprite;
                img.color = new Color(1f, 0.35f, 0.45f, 1f);
                img.preserveAspect = true;
                heartImgs[i] = img;
            }

            // === POTION INVENTORY HUD (centro-esquerdo do bottom) ===
            GameObject potionContainer = new GameObject("PotionInventory");
            potionContainer.transform.SetParent(bottomBar.transform, false);
            var potionRect = potionContainer.AddComponent<RectTransform>();
            potionRect.anchorMin = new Vector2(0f, 0f);
            potionRect.anchorMax = new Vector2(0f, 1f);
            potionRect.pivot = new Vector2(0f, 0.5f);
            potionRect.anchoredPosition = new Vector2(300f, 0f);
            potionRect.sizeDelta = new Vector2(200f, 0f);
            potionContainer.SetActive(false); // Começa oculto; aparece quando tem poção

            // Ícone de poção
            Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/HealthPotion.png");
            GameObject potionIconObj = new GameObject("PotionIcon");
            potionIconObj.transform.SetParent(potionContainer.transform, false);
            var piRect = potionIconObj.AddComponent<RectTransform>();
            piRect.anchorMin = new Vector2(0f, 0.15f);
            piRect.anchorMax = new Vector2(0f, 0.85f);
            piRect.pivot = new Vector2(0f, 0.5f);
            piRect.anchoredPosition = new Vector2(0f, 0f);
            piRect.sizeDelta = new Vector2(48f, 0f);
            var potionIconImg = potionIconObj.AddComponent<Image>();
            potionIconImg.sprite = potionSprite;
            potionIconImg.preserveAspect = true;

            // Texto do contador de poções
            GameObject potionTextObj = new GameObject("PotionCountText");
            potionTextObj.transform.SetParent(potionContainer.transform, false);
            var ptRect = potionTextObj.AddComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0f, 0f);
            ptRect.anchorMax = new Vector2(1f, 1f);
            ptRect.pivot = new Vector2(0f, 0.5f);
            ptRect.anchoredPosition = new Vector2(56f, 0f);
            ptRect.sizeDelta = new Vector2(-56f, 0f);
            var potionTmp = potionTextObj.AddComponent<TextMeshProUGUI>();
            potionTmp.text = "[F] x0";
            potionTmp.fontSize = 26;
            potionTmp.fontStyle = FontStyles.Bold;
            potionTmp.color = new Color(0.7f, 1f, 0.7f, 1f);
            potionTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Element Buttons: [GELO] [FOGO] (canto direito do bottom)
            GameObject elemContainer = new GameObject("ElementSelector");
            elemContainer.transform.SetParent(bottomBar.transform, false);
            var elemRect = elemContainer.AddComponent<RectTransform>();
            elemRect.anchorMin = new Vector2(1f, 0f);
            elemRect.anchorMax = new Vector2(1f, 1f);
            elemRect.pivot = new Vector2(1f, 0.5f);
            elemRect.anchoredPosition = new Vector2(-24f, 0f);
            elemRect.sizeDelta = new Vector2(340f, 0f);
            var elemHlg = elemContainer.AddComponent<HorizontalLayoutGroup>();
            elemHlg.spacing = 16f;
            elemHlg.childForceExpandWidth = false;
            elemHlg.childForceExpandHeight = false;
            elemHlg.childAlignment = TextAnchor.MiddleRight;

            // Ice Badge
            GameObject iceObj = new GameObject("Badge_Gelo");
            iceObj.transform.SetParent(elemContainer.transform, false);
            var iceRect = iceObj.AddComponent<RectTransform>();
            iceRect.sizeDelta = new Vector2(155f, 58f);
            var iceImg = iceObj.AddComponent<Image>();
            iceImg.color = new Color(0.2f, 0.85f, 1f, 1f);

            GameObject iceTxtObj = new GameObject("Text");
            iceTxtObj.transform.SetParent(iceObj.transform, false);
            var iceTxtRect = iceTxtObj.AddComponent<RectTransform>();
            iceTxtRect.anchorMin = Vector2.zero;
            iceTxtRect.anchorMax = Vector2.one;
            iceTxtRect.sizeDelta = Vector2.zero;
            var iceTmp = iceTxtObj.AddComponent<TextMeshProUGUI>();
            iceTmp.text = "1: GELO";
            iceTmp.alignment = TextAlignmentOptions.Center;
            iceTmp.fontSize = 22;
            iceTmp.fontStyle = FontStyles.Bold;
            iceTmp.color = Color.white;

            // Fire Badge
            GameObject fireObj = new GameObject("Badge_Fogo");
            fireObj.transform.SetParent(elemContainer.transform, false);
            var fireRect = fireObj.AddComponent<RectTransform>();
            fireRect.sizeDelta = new Vector2(155f, 58f);
            var fireImg = fireObj.AddComponent<Image>();
            fireImg.color = new Color(0.45f, 0.25f, 0.05f, 0.5f);

            GameObject fireTxtObj = new GameObject("Text");
            fireTxtObj.transform.SetParent(fireObj.transform, false);
            var fireTxtRect = fireTxtObj.AddComponent<RectTransform>();
            fireTxtRect.anchorMin = Vector2.zero;
            fireTxtRect.anchorMax = Vector2.one;
            fireTxtRect.sizeDelta = Vector2.zero;
            var fireTmp = fireTxtObj.AddComponent<TextMeshProUGUI>();
            fireTmp.text = "FOGO [BLOQ]";
            fireTmp.alignment = TextAlignmentOptions.Center;
            fireTmp.fontSize = 22;
            fireTmp.fontStyle = FontStyles.Bold;
            fireTmp.color = new Color(0.55f, 0.45f, 0.45f, 0.5f);

            // Wire HUD refs
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
            hudSo.FindProperty("bossBarContainer").objectReferenceValue = bossBarWrapper;
            hudSo.FindProperty("bossHealthFill").objectReferenceValue = fillImg;
            hudSo.FindProperty("bossNameText").objectReferenceValue = bossNameTmp;
            hudSo.FindProperty("potionCountText").objectReferenceValue = potionTmp;
            hudSo.FindProperty("potionIconContainer").objectReferenceValue = potionContainer;
            hudSo.ApplyModifiedProperties();

            // --- EventSystem ---
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

            // --- GameOver Screen ---
            GameObject gameOverPanel = SetupGameOverPanel(canvasObj.transform);

            // --- Victory Screen ---
            GameObject victoryPanel = SetupVictoryPanel(canvasObj.transform);

            // --- Evolution Screen (Árvore de Upgrades) ---
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

            var evoUI = panel.AddComponent<EvolutionUI>();

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
            so.FindProperty("stageDuration").floatValue = 150f;
            so.ApplyModifiedProperties();
        }

        [InitializeOnLoadMethod]
        private static void RegisterAutoSetupTrigger()
        {
            EditorApplication.delayCall += () =>
            {
                string triggerFile = "Temp/TriggerSetup.flag";
                if (File.Exists(triggerFile))
                {
                    try { File.Delete(triggerFile); } catch {}
                    SetupCurrentScene();
                    Debug.Log("[WitchShmup] Cena atual montada e salva com sucesso automaticamente via SetupTool!");
                }
            };
        }
    }
}
#endif
