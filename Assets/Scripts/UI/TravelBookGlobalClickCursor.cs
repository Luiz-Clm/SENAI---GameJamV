using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    /// <summary>Shows the TravelBook click cursor animation in every loaded scene.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class TravelBookGlobalClickCursor : MonoBehaviour
    {
        private static TravelBookGlobalClickCursor instance;
        private static Sprite[] frames;

        private Canvas overlayCanvas;
        private RectTransform canvasRect;
        private Image cursorImage;
        private Coroutine clickAnimation;
        private const float FrameDuration = 0.055f;

        public static bool IsAnimating => instance != null && instance.clickAnimation != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("TravelBookGlobalClickCursor");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TravelBookGlobalClickCursor>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            frames = LoadFrames("UI_TravelBook_MouseCursorClick01a", 4);
            CreateOverlay();
        }

        private void Update()
        {
            if (cursorImage == null) return;

            if (TravelBookSkillTreeEffects.HasAnimatedCursor)
            {
                cursorImage.enabled = false;
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out Vector2 local))
                cursorImage.rectTransform.anchoredPosition = local;

            if (Input.GetMouseButtonDown(0)) PlayClick();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                GameManager.RefreshSystemCursor();
            }
        }

        private void OnApplicationQuit()
        {
            RestoreSystemCursor();
        }

        private void OnDisable()
        {
            if (clickAnimation != null) StopCoroutine(clickAnimation);
            clickAnimation = null;
            if (cursorImage != null) cursorImage.enabled = false;
            RestoreSystemCursor();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            RestoreSystemCursor();
        }

        private void CreateOverlay()
        {
            if (frames == null || frames.Length == 0) return;
            var canvasObject = new GameObject("AnimatedClickOverlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.SetAsLastSibling();
            overlayCanvas = canvasObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = short.MaxValue;
            canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;

            var cursorObject = new GameObject("TravelBookClickCursor", typeof(RectTransform), typeof(Image));
            cursorObject.transform.SetParent(canvasObject.transform, false);
            cursorImage = cursorObject.GetComponent<Image>();
            cursorImage.sprite = frames[frames.Length - 1];
            cursorImage.preserveAspect = true;
            cursorImage.raycastTarget = false;
            var rect = cursorImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(42f, 42f);
            cursorImage.enabled = false;
        }

        private void PlayClick()
        {
            if (frames == null || frames.Length == 0 || cursorImage == null) return;
            Cursor.visible = false;
            if (clickAnimation != null) StopCoroutine(clickAnimation);
            clickAnimation = StartCoroutine(AnimateClick());
        }

        private IEnumerator AnimateClick()
        {
            cursorImage.enabled = true;
            foreach (var frame in frames)
            {
                cursorImage.sprite = frame;
                yield return new WaitForSecondsRealtime(FrameDuration);
            }
            cursorImage.enabled = false;
            clickAnimation = null;
            GameManager.RefreshSystemCursor();
        }

        private static void RestoreSystemCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static Sprite[] LoadFrames(string baseName, int count)
        {
            var result = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                string path = $"TravelBookEffects/{baseName}_{i + 1}";
                Sprite[] subAssets = Resources.LoadAll<Sprite>(path);
                string expectedName = $"{baseName}_{i + 1}_0";

                foreach (Sprite sprite in subAssets)
                {
                    if (sprite != null && sprite.name == expectedName)
                    {
                        result[i] = sprite;
                        break;
                    }
                }

                if (result[i] == null && subAssets.Length > 0)
                    result[i] = subAssets[0];
            }
            return result;
        }
    }
}
