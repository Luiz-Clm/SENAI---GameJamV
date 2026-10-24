using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WitchShmup.Core;

namespace WitchShmup.Boss
{
    /// <summary>Máscara de tinta: escurece a tela e reabre a visão em um círculo suave.</summary>
    public class InkBlindnessOverlay : MonoBehaviour
    {
        private static InkBlindnessOverlay instance;
        [SerializeField] private float revealDuration = 1.1f;
        [SerializeField, Range(0f, 1f)] private float darkness = 0.97f;
        [SerializeField] private float edgeSoftness = 0.05f;

        private Image overlayImage;
        private Material overlayMaterial;
        private float blindUntil;
        private float revealStartsAt;
        private Transform player;

        public static void Apply(float duration)
        {
            if (instance == null) instance = CreateOverlay();
            instance.StartBlindness(duration);
        }

        private static InkBlindnessOverlay CreateOverlay()
        {
            var root = new GameObject("Ink_Blindness_Overlay");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<InkBlindnessOverlay>();
            instance.BuildOverlay();
            return instance;
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Ink_Blindness_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imageObject = new GameObject("Ink_Blindness_Mask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlayImage = imageObject.GetComponent<Image>();
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;

            Shader shader = Resources.Load<Shader>("Shaders/InkBlindness");
            if (shader == null) shader = Shader.Find("UI/InkBlindness");
            if (shader != null)
            {
                overlayMaterial = new Material(shader);
                overlayImage.material = overlayMaterial;
                overlayMaterial.SetFloat("_Feather", edgeSoftness);
            }
            overlayImage.enabled = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearBlindness();
        }

        private void StartBlindness(float duration)
        {
            if (player == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }

            float now = Time.time;
            blindUntil = Mathf.Max(blindUntil, now + Mathf.Max(0.1f, duration));
            revealStartsAt = blindUntil - Mathf.Max(0.1f, revealDuration);
            if (overlayImage != null) overlayImage.enabled = true;
        }

        private void Update()
        {
            if (overlayImage == null || !overlayImage.enabled) return;

            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory))
            {
                ClearBlindness();
                return;
            }

            if (player == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }

            if (overlayMaterial != null)
            {
                Vector3 viewport = player != null && Camera.main != null
                    ? Camera.main.WorldToViewportPoint(player.position)
                    : new Vector3(0.5f, 0.5f, 0f);
                overlayMaterial.SetVector("_Center", new Vector4(viewport.x, viewport.y, 0f, 0f));
                overlayMaterial.SetColor("_Color", new Color(0.008f, 0.012f, 0.025f, darkness));
                float revealT = Mathf.InverseLerp(revealStartsAt, blindUntil, Time.time);
                overlayMaterial.SetFloat("_RevealRadius", Mathf.Lerp(0f, 1.2f, revealT));
            }

            if (Time.time >= blindUntil) overlayImage.enabled = false;
        }

        private void ClearBlindness()
        {
            blindUntil = 0f;
            revealStartsAt = 0f;
            if (overlayImage != null) overlayImage.enabled = false;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (overlayMaterial != null) Destroy(overlayMaterial);
            if (instance == this) instance = null;
        }
    }
}
