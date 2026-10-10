using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WitchShmup.Environment
{
    /// <summary>
    /// Tocha de dungeon: estrutura + chama com oscilação leve + Point Light2D natural do URP.
    /// A tocha é completamente ESTÁTICA no mundo — apenas a chama escala suavemente.
    /// </summary>
    public class DungeonTorch : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private SpriteRenderer torchRenderer;
        [SerializeField] private SpriteRenderer flameRenderer;
        [SerializeField] private Light2D torchLight;

        [Header("Flame Flicker (Muito Sutil)")]
        [SerializeField] private float flickerSpeed = 2.5f;
        [SerializeField] [Range(0f, 0.1f)] private float flickerIntensity = 0.04f;
        [SerializeField] private float baseLightIntensity = 1.2f;
        [SerializeField] [Range(0f, 0.15f)] private float lightFlickerIntensity = 0.06f;

        private float randomSeed;
        private Vector3 flameBaseScale;

        private void Awake()
        {
            randomSeed = Random.Range(0f, 100f);

            if (flameRenderer != null)
            {
                flameBaseScale = flameRenderer.transform.localScale;
            }
        }

        private void Update()
        {
            float time = Time.time * flickerSpeed + randomSeed;
            // Perlin noise para oscilação suave e orgânica
            float flicker = (Mathf.PerlinNoise(time * 0.8f, randomSeed) - 0.5f) * 2f; // -1 a 1

            // Apenas a chama oscila (escala Y levemente)
            if (flameRenderer != null)
            {
                float sx = flameBaseScale.x * (1f + flicker * flickerIntensity * 0.4f);
                float sy = flameBaseScale.y * (1f + flicker * flickerIntensity);
                flameRenderer.transform.localScale = new Vector3(sx, sy, 1f);
            }

            // Intensidade da luz oscila sutilmente
            if (torchLight != null)
            {
                torchLight.intensity = baseLightIntensity + flicker * lightFlickerIntensity;
            }
        }

        /// <summary>
        /// Cria uma tocha completa como filho de <paramref name="parent"/> na posição local especificada.
        /// Inclui suporte de madeira/ferro, chama pixel art e Point Light2D nativo do URP.
        /// </summary>
        public static GameObject CreateTorch(Transform parent, Vector3 localPos)
        {
            GameObject torchObj = new GameObject("Dungeon_Torch");
            torchObj.transform.SetParent(parent, false);
            torchObj.transform.localPosition = localPos;

            var torch = torchObj.AddComponent<DungeonTorch>();

            // --- 1. Suporte da tocha (pixel art: madeira + anel de ferro) ---
            GameObject mountObj = new GameObject("Torch_Mount");
            mountObj.transform.SetParent(torchObj.transform, false);
            var mountSr = mountObj.AddComponent<SpriteRenderer>();
            mountSr.color = Color.white;
            mountSr.sortingOrder = -4;

            Texture2D mountTex = new Texture2D(6, 14, TextureFormat.RGBA32, false);
            mountTex.filterMode = FilterMode.Point;
            Color[] mCols = new Color[6 * 14];
            for (int i = 0; i < mCols.Length; i++) mCols[i] = Color.clear;

            Color iron = new Color(0.25f, 0.22f, 0.3f, 1f);
            Color wood = new Color(0.55f, 0.35f, 0.18f, 1f);
            for (int y = 0; y < 10; y++)
                for (int x = 2; x <= 3; x++) mCols[y * 6 + x] = wood;
            for (int x = 1; x <= 4; x++) mCols[7 * 6 + x] = iron;
            for (int x = 1; x <= 4; x++) mCols[8 * 6 + x] = iron;

            mountTex.SetPixels(mCols);
            mountTex.Apply();
            mountSr.sprite = Sprite.Create(mountTex, new Rect(0, 0, 6, 14), new Vector2(0.5f, 0.2f), 16f);
            torch.torchRenderer = mountSr;

            // --- 2. Chama da tocha (pixel art) ---
            GameObject flameObj = new GameObject("Torch_Flame");
            flameObj.transform.SetParent(torchObj.transform, false);
            flameObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var flameSr = flameObj.AddComponent<SpriteRenderer>();
            flameSr.sortingOrder = -3;

            Texture2D flameTex = new Texture2D(10, 14, TextureFormat.RGBA32, false);
            flameTex.filterMode = FilterMode.Point;
            Color[] fCols = new Color[10 * 14];
            for (int i = 0; i < fCols.Length; i++) fCols[i] = Color.clear;

            Color cRed    = new Color(0.95f, 0.25f, 0.1f, 1f);
            Color cOrange = new Color(1f,    0.60f, 0.1f, 1f);
            Color cYellow = new Color(1f,    0.95f, 0.4f, 1f);

            for (int y = 0; y < 14; y++)
            {
                int w = (y < 4) ? 3 : (y < 9) ? 4 : (y < 12) ? 2 : 1;
                for (int x = 5 - w; x <= 5 + w - 1; x++)
                {
                    Color col = (y < 3) ? cYellow : (y < 8) ? cOrange : cRed;
                    if (x >= 0 && x < 10) fCols[y * 10 + x] = col;
                }
            }
            flameTex.SetPixels(fCols);
            flameTex.Apply();
            flameSr.sprite = Sprite.Create(flameTex, new Rect(0, 0, 10, 14), new Vector2(0.5f, 0.1f), 16f);
            torch.flameRenderer = flameSr;

            // --- 3. Point Light2D nativo do URP 2D (iluminação natural) ---
            GameObject lightObj = new GameObject("Torch_Light2D");
            lightObj.transform.SetParent(torchObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            var light2d = lightObj.AddComponent<Light2D>();
            light2d.lightType = Light2D.LightType.Point;
            light2d.color     = new Color(1f, 0.62f, 0.22f, 1f); // laranja quente
            light2d.intensity = 1.2f;
            light2d.pointLightOuterRadius = 4.5f;
            light2d.pointLightInnerRadius = 0.4f;
            light2d.falloffIntensity      = 0.6f;
            light2d.shadowsEnabled        = false; // sem sombras para performance
            torch.torchLight        = light2d;
            torch.baseLightIntensity = 1.2f;

            return torchObj;
        }
    }
}
