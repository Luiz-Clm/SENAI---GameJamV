using UnityEngine;

namespace WitchShmup.Environment
{
    public class DungeonTorch : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private SpriteRenderer torchRenderer;
        [SerializeField] private SpriteRenderer flameRenderer;
        [SerializeField] private MeshFilter lightConeMeshFilter;
        [SerializeField] private MeshRenderer lightConeMeshRenderer;

        [Header("Spotlight Cone Settings")]
        [SerializeField] private float coneHeight = 6.5f;
        [SerializeField] private float coneWidth = 4.0f;
        [SerializeField] [Range(0f, 1f)] private float baseLightAlpha = 0.38f;
        [SerializeField] private Color lightColorTop = new Color(1f, 0.82f, 0.35f, 0.45f);
        [SerializeField] private Color lightColorBottom = new Color(1f, 0.5f, 0.08f, 0.0f);

        [Header("Flicker Animation")]
        [SerializeField] private float flickerSpeed = 7.5f;
        [SerializeField] private float flickerIntensity = 0.12f;

        private float randomSeed;
        private Vector3 flameBaseScale;
        private Material coneMaterial;

        private void Awake()
        {
            randomSeed = Random.Range(0f, 100f);

            if (flameRenderer != null)
            {
                flameBaseScale = flameRenderer.transform.localScale;
            }

            SetupLightCone();
        }

        public void SetupLightCone()
        {
            if (lightConeMeshFilter == null || lightConeMeshRenderer == null)
            {
                // Cria o filho para o cone triangular de luz caso não exista
                GameObject coneObj = new GameObject("Torch_Spotlight_Cone");
                coneObj.transform.SetParent(transform, false);
                coneObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

                lightConeMeshFilter = coneObj.AddComponent<MeshFilter>();
                lightConeMeshRenderer = coneObj.AddComponent<MeshRenderer>();
            }

            // Material transparente para o cone de luz com sorting order no fundo do cenário (-5)
            Shader unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("UI/Default");

            coneMaterial = new Material(unlitShader);
            lightConeMeshRenderer.material = coneMaterial;
            lightConeMeshRenderer.sortingOrder = -5; // Atrás de todo gameplay, sem atrapalhar a UI

            BuildTriangleMesh();
        }

        private void BuildTriangleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "TorchLightConeMesh";

            // Vértices do triângulo (topo na chama, base larga descendo)
            Vector3[] vertices = new Vector3[3];
            vertices[0] = new Vector3(0f, 0f, 0f);                            // Topo (origem da chama)
            vertices[1] = new Vector3(-coneWidth * 0.5f, -coneHeight, 0f);   // Canto inferior esquerdo
            vertices[2] = new Vector3(coneWidth * 0.5f, -coneHeight, 0f);    // Canto inferior direito

            int[] triangles = new int[] { 0, 2, 1 };

            // Cores por vértice para criar o gradiente suave de iluminação
            Color topC = lightColorTop;
            topC.a = baseLightAlpha;

            Color botC = lightColorBottom;
            botC.a = 0f; // Fim transparente na base

            Color[] colors = new Color[3];
            colors[0] = topC;
            colors[1] = botC;
            colors[2] = botC;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();

            lightConeMeshFilter.mesh = mesh;
        }

        private void Update()
        {
            float time = Time.time * flickerSpeed + randomSeed;
            float flicker = Mathf.Sin(time) * 0.5f + Mathf.PerlinNoise(time * 0.8f, randomSeed) - 0.5f;
            float currentAlpha = Mathf.Clamp01(baseLightAlpha + (flicker * flickerIntensity));

            // Chama da tocha tremeluzindo
            if (flameRenderer != null)
            {
                float scaleMod = 1f + (flicker * 0.2f);
                flameRenderer.transform.localScale = new Vector3(flameBaseScale.x * scaleMod, flameBaseScale.y * (1f + flicker * 0.3f), 1f);
            }

            // Atualiza cor do material do cone com tremulação suave
            if (coneMaterial != null)
            {
                Color c = Color.Lerp(lightColorTop, new Color(1f, 0.9f, 0.4f, 1f), (flicker + 0.5f) * 0.5f);
                c.a = currentAlpha;
                coneMaterial.color = c;
            }
        }

        public static GameObject CreateTorch(Transform parent, Vector3 localPos)
        {
            GameObject torchObj = new GameObject("Dungeon_Torch");
            torchObj.transform.SetParent(parent, false);
            torchObj.transform.localPosition = localPos;

            var torch = torchObj.AddComponent<DungeonTorch>();

            // 1. Suporte da tocha (Braçadeira de ferro e madeira)
            GameObject mountObj = new GameObject("Torch_Mount");
            mountObj.transform.SetParent(torchObj.transform, false);
            var mountSr = mountObj.AddComponent<SpriteRenderer>();
            mountSr.color = new Color(0.45f, 0.32f, 0.22f, 1f);
            mountSr.sortingOrder = -4; // No fundo, logo à frente dos tijolos

            Texture2D mountTex = new Texture2D(6, 14, TextureFormat.RGBA32, false);
            mountTex.filterMode = FilterMode.Point;
            Color[] mCols = new Color[6 * 14];
            for (int i = 0; i < mCols.Length; i++) mCols[i] = Color.clear;

            // Suporte de ferro e tocha em pixel art
            Color iron = new Color(0.25f, 0.22f, 0.3f, 1f);
            Color wood = new Color(0.55f, 0.35f, 0.18f, 1f);
            for (int y = 0; y < 10; y++)
            {
                for (int x = 2; x <= 3; x++) mCols[y * 6 + x] = wood;
            }
            // Anel de metal
            for (int x = 1; x <= 4; x++) mCols[7 * 6 + x] = iron;
            for (int x = 1; x <= 4; x++) mCols[8 * 6 + x] = iron;

            mountTex.SetPixels(mCols);
            mountTex.Apply();
            mountSr.sprite = Sprite.Create(mountTex, new Rect(0, 0, 6, 14), new Vector2(0.5f, 0.2f), 16f);
            torch.torchRenderer = mountSr;

            // 2. Fogo da tocha
            GameObject flameObj = new GameObject("Torch_Flame");
            flameObj.transform.SetParent(torchObj.transform, false);
            flameObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var flameSr = flameObj.AddComponent<SpriteRenderer>();
            flameSr.sortingOrder = -3;

            Texture2D flameTex = new Texture2D(10, 14, TextureFormat.RGBA32, false);
            flameTex.filterMode = FilterMode.Point;
            Color[] fCols = new Color[10 * 14];
            for (int i = 0; i < fCols.Length; i++) fCols[i] = Color.clear;

            Color cRed = new Color(0.95f, 0.25f, 0.1f, 1f);
            Color cOrange = new Color(1f, 0.6f, 0.1f, 1f);
            Color cYellow = new Color(1f, 0.95f, 0.4f, 1f);

            // Desenha labareda
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

            torch.SetupLightCone();
            return torchObj;
        }
    }
}
