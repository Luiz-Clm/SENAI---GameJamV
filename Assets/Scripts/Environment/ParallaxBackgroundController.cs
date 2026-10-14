using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchShmup.Environment
{
    public class ParallaxBackgroundController : MonoBehaviour
    {
        public static ParallaxBackgroundController Instance { get; private set; }

        [Header("Global Scroll Settings")]
        [SerializeField] private float baseScrollSpeed = 3f;
        [SerializeField] private bool isScrolling = true;

        [Header("Layers")]
        [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private float originalScrollSpeed;
        private Coroutine speedTransitionCoroutine;

        public float BaseScrollSpeed => baseScrollSpeed;
        public bool IsScrolling => isScrolling;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            originalScrollSpeed = baseScrollSpeed;

            if (layers.Count == 0)
            {
                layers.AddRange(GetComponentsInChildren<ParallaxLayer>());
            }
        }

        private void Update()
        {
            if (!isScrolling) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null)
                {
                    layers[i].Scroll(baseScrollSpeed, dt);
                }
            }
        }

        public void SetScrollSpeed(float speed)
        {
            baseScrollSpeed = speed;
        }

        public void StopScrolling()
        {
            isScrolling = false;
        }

        public void ResumeScrolling()
        {
            isScrolling = true;
            baseScrollSpeed = originalScrollSpeed;
        }

        public void TransitionScrollSpeed(float targetSpeed, float duration)
        {
            if (speedTransitionCoroutine != null)
            {
                StopCoroutine(speedTransitionCoroutine);
            }
            speedTransitionCoroutine = StartCoroutine(TransitionSpeedRoutine(targetSpeed, duration));
        }

        private IEnumerator TransitionSpeedRoutine(float targetSpeed, float duration)
        {
            float startSpeed = baseScrollSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                baseScrollSpeed = Mathf.Lerp(startSpeed, targetSpeed, elapsed / duration);
                yield return null;
            }

            baseScrollSpeed = targetSpeed;
            speedTransitionCoroutine = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureParallax()
        {
            if (FindFirstObjectByType<ParallaxBackgroundController>() != null) return;

            var existingBg = GameObject.Find("Environment_Background");
            Sprite brickSprite = null;
            if (existingBg != null)
            {
                var sr = existingBg.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) brickSprite = sr.sprite;
                Destroy(existingBg);
            }

            if (brickSprite == null)
            {
                var allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                foreach (var r in allSrs)
                {
                    if (r.sprite != null && (r.sprite.name.Contains("Brick") || r.sprite.name.Contains("Wall") || r.sprite.name.Contains("Parallax")))
                    {
                        brickSprite = r.sprite;
                        break;
                    }
                }
            }

            if (brickSprite == null)
            {
                var parallaxSprites = Resources.LoadAll<Sprite>("Sprites/Parallax");
                if (parallaxSprites != null && parallaxSprites.Length > 0)
                {
                    brickSprite = parallaxSprites[0];
                }
                if (brickSprite == null)
                {
                    brickSprite = Resources.Load<Sprite>("Sprites/Parallax");
                }
            }

            if (brickSprite == null)
            {
                brickSprite = CreateFallbackBrickSprite();
            }

            GameObject bgRoot = new GameObject("Environment_Parallax");
            bgRoot.transform.position = Vector3.zero;
            var controller = bgRoot.AddComponent<ParallaxBackgroundController>();
            controller.SetScrollSpeed(3.0f);

            float segmentWidth = 24f;
            float segmentHeight = 13f;

            // 1. Camada Distante (Far Background) - 0.45x
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
                sr.color = new Color(0.24f, 0.20f, 0.35f, 1f);

                farSegments[i] = segObj.transform;
            }
            farLayer.SetSegments(farSegments, segmentWidth);

            // 2. Camada Principal (Main Scenario) com TOCHAS - 1.0x
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

                // 3 tochas distribuídas no segmento (rolam e dão wrap-around perfeitamente)
                float[] torchOffsets = { -8f, 0f, 8f };
                float torchY = 1.3f;
                foreach (float tx in torchOffsets)
                {
                    DungeonTorch.CreateTorch(segObj.transform, new Vector3(tx, torchY, 0f));
                }

                mainSegments[i] = segObj.transform;
            }
            mainLayer.SetSegments(mainSegments, segmentWidth);

            controller.layers.Clear();
            controller.layers.Add(farLayer);
            controller.layers.Add(mainLayer);
        }

        private static Sprite CreateFallbackBrickSprite()
        {
            Texture2D tex = new Texture2D(32, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            Color dark = new Color(0.18f, 0.15f, 0.22f, 1f);
            Color brick = new Color(0.35f, 0.32f, 0.44f, 1f);
            Color mortar = new Color(0.12f, 0.10f, 0.16f, 1f);

            Color[] cols = new Color[32 * 16];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (y == 0 || y == 8 || y == 15) cols[y * 32 + x] = mortar;
                    else if (y < 8 && (x == 0 || x == 16)) cols[y * 32 + x] = mortar;
                    else if (y >= 8 && (x == 8 || x == 24)) cols[y * 32 + x] = mortar;
                    else cols[y * 32 + x] = ((x + y) % 5 == 0) ? dark : brick;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 16), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
