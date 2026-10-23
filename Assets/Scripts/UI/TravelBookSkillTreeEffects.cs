using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchShmup.Core;

namespace WitchShmup.UI
{
    /// <summary>
    /// TravelBookLite feedback effects used only by the Testes scene.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class TravelBookSkillTreeEffects : MonoBehaviour
    {
        [Header("TravelBookLite sprites")]
        [SerializeField] Sprite[] clickCursorFrames;
        [SerializeField] Sprite[] nodeBackgroundFrames;
        [SerializeField] Sprite[] tabIndicatorFrames;
        [SerializeField] Sprite[] hoverTopLeftFrames;
        [SerializeField] Sprite[] hoverTopRightFrames;
        [SerializeField] Sprite[] hoverBottomLeftFrames;
        [SerializeField] Sprite[] hoverBottomRightFrames;

        [Header("Sizes and timing")]
        [SerializeField] Vector2 cursorSize = new Vector2(42f, 42f);
        [SerializeField] float hoverCornerSize = 14f;
        [SerializeField] float hoverFrameMargin = 5f;
        [SerializeField] float frameDuration = 0.055f;
        [SerializeField] bool hideSystemCursor = true;

        readonly Dictionary<GameObject, Image> nodeBackgrounds = new Dictionary<GameObject, Image>();
        readonly Dictionary<GameObject, Coroutine> nodeAnimations = new Dictionary<GameObject, Coroutine>();
        readonly Dictionary<GameObject, EventTrigger.Entry> nodeClickEntries = new Dictionary<GameObject, EventTrigger.Entry>();
        readonly Dictionary<GameObject, EventTrigger.Entry> hoverEnterEntries = new Dictionary<GameObject, EventTrigger.Entry>();
        readonly Dictionary<GameObject, EventTrigger.Entry> hoverExitEntries = new Dictionary<GameObject, EventTrigger.Entry>();
        readonly Dictionary<GameObject, RectTransform> hoverFrames = new Dictionary<GameObject, RectTransform>();
        readonly Dictionary<Button, UnityEngine.Events.UnityAction> tabClickListeners = new Dictionary<Button, UnityEngine.Events.UnityAction>();
        readonly Dictionary<Button, Vector3> tabButtonScales = new Dictionary<Button, Vector3>();
        readonly Dictionary<Button, Coroutine> tabAnimations = new Dictionary<Button, Coroutine>();

        EvolutionUI evolutionUI;
        Canvas ownerCanvas;
        RectTransform ownerRect;
        Image cursorImage;
        Coroutine cursorAnimation;
        GameObject hoveredObject;
        Image treeIndicator;
        Image staffIndicator;
        Transform activeIndicator;
        Vector3 treeIndicatorScale;
        Vector3 staffIndicatorScale;
        float indicatorPulseStart;

        public static bool HasAnimatedCursor { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState()
        {
            HasAnimatedCursor = false;
        }

        void OnEnable()
        {
            HasAnimatedCursor = hideSystemCursor && clickCursorFrames != null && clickCursorFrames.Length > 0;
            if (hideSystemCursor) Cursor.visible = false;
            StartCoroutine(InitializeAfterEvolutionUI());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (cursorImage != null)
            {
                cursorImage.enabled = false;
                if (clickCursorFrames != null && clickCursorFrames.Length > 0)
                    cursorImage.sprite = clickCursorFrames[clickCursorFrames.Length - 1];
            }
            foreach (var background in nodeBackgrounds.Values)
            {
                if (background != null && nodeBackgroundFrames != null && nodeBackgroundFrames.Length > 0)
                    background.sprite = nodeBackgroundFrames[0];
            }
            foreach (var frame in hoverFrames.Values)
            {
                if (frame != null) frame.gameObject.SetActive(false);
            }
            hoveredObject = null;
            HasAnimatedCursor = false;
            GameManager.RefreshSystemCursor();

            if (treeIndicator != null) treeIndicator.transform.localScale = treeIndicatorScale;
            if (staffIndicator != null) staffIndicator.transform.localScale = staffIndicatorScale;
            activeIndicator = null;

            foreach (var pair in tabClickListeners)
            {
                if (pair.Key == null) continue;
                pair.Key.onClick.RemoveListener(pair.Value);
                if (tabButtonScales.TryGetValue(pair.Key, out Vector3 scale)) pair.Key.transform.localScale = scale;
            }
            tabClickListeners.Clear();
            tabButtonScales.Clear();
            tabAnimations.Clear();
        }

        IEnumerator InitializeAfterEvolutionUI()
        {
            // EvolutionUI binds and refreshes its scene-authored elements during Start.
            yield return null;
            yield return new WaitForEndOfFrame();

            evolutionUI = GetComponent<EvolutionUI>();
            if (evolutionUI == null) evolutionUI = FindObjectOfType<EvolutionUI>();
            ownerCanvas = GetComponent<Canvas>();
            if (ownerCanvas == null) ownerCanvas = GetComponentInParent<Canvas>();
            ownerRect = ownerCanvas != null ? ownerCanvas.transform as RectTransform : transform as RectTransform;

            CreateCursorOverlay();
            BindTreeNodes();
            BindButtonsAndTabs();
        }

        void LateUpdate()
        {
            if (cursorImage != null && cursorImage.enabled)
            {
                Camera eventCamera = ownerCanvas != null && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? ownerCanvas.worldCamera
                    : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(ownerRect, Input.mousePosition, eventCamera, out Vector2 localPoint))
                    cursorImage.rectTransform.anchoredPosition = localPoint;

                if (Input.GetMouseButtonDown(0)) PlayCursorClick();
            }

            UpdateActiveTabIndicator();
        }

        void CreateCursorOverlay()
        {
            if (ownerCanvas == null || clickCursorFrames == null || clickCursorFrames.Length == 0)
                return;

            if (cursorImage != null)
            {
                cursorImage.sprite = clickCursorFrames[clickCursorFrames.Length - 1];
                cursorImage.enabled = true;
                return;
            }

            var go = new GameObject("TravelBookAnimatedCursor", typeof(RectTransform), typeof(Canvas), typeof(Image));
            go.transform.SetParent(ownerCanvas.transform, false);
            go.transform.SetAsLastSibling();

            var overlayCanvas = go.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = short.MaxValue;

            cursorImage = go.GetComponent<Image>();
            cursorImage.sprite = clickCursorFrames[clickCursorFrames.Length - 1];
            cursorImage.preserveAspect = true;
            cursorImage.raycastTarget = false;
            cursorImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cursorImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cursorImage.rectTransform.pivot = new Vector2(0f, 1f);
            cursorImage.rectTransform.sizeDelta = cursorSize;
            cursorImage.enabled = true;
        }

        void PlayCursorClick()
        {
            if (cursorImage == null || clickCursorFrames == null || clickCursorFrames.Length == 0) return;
            if (cursorAnimation != null) StopCoroutine(cursorAnimation);
            cursorAnimation = StartCoroutine(AnimateCursorClick());
        }

        IEnumerator AnimateCursorClick()
        {
            for (int i = 0; i < clickCursorFrames.Length; i++)
            {
                cursorImage.sprite = clickCursorFrames[i];
                yield return new WaitForSecondsRealtime(frameDuration);
            }

            cursorImage.sprite = clickCursorFrames[clickCursorFrames.Length - 1];
            cursorAnimation = null;
        }

        void BindTreeNodes()
        {
            if (evolutionUI == null) return;
            var content = evolutionUI.transform.Find("FrameBorder/Frame/Tab_Tree/TreeBorder/TreeCanvas/TreeContent");
            if (content == null) return;

            foreach (Transform child in content)
            {
                if (!child.name.StartsWith("Node_", StringComparison.Ordinal)) continue;

                GameObject node = child.gameObject;
                Image background = EnsureNodeBackground(node);
                nodeBackgrounds[node] = background;
                AddTriggerEntry(node, EventTriggerType.PointerClick, nodeClickEntries, _ => PlayNodeClick(node));
                BindHover(node);
            }
        }

        Image EnsureNodeBackground(GameObject node)
        {
            if (nodeBackgrounds.TryGetValue(node, out Image existing) && existing != null) return existing;

            Transform child = node.transform.Find("TravelBookBrownBackground");
            GameObject backgroundGo;
            if (child != null)
            {
                backgroundGo = child.gameObject;
            }
            else
            {
                backgroundGo = new GameObject("TravelBookBrownBackground", typeof(RectTransform), typeof(Image));
                backgroundGo.transform.SetParent(node.transform, false);
                backgroundGo.transform.SetAsFirstSibling();
            }

            var rect = backgroundGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = backgroundGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = false;
            image.color = Color.white;
            if (nodeBackgroundFrames != null && nodeBackgroundFrames.Length > 0)
                image.sprite = nodeBackgroundFrames[0];
            return image;
        }

        void PlayNodeClick(GameObject node)
        {
            if (nodeBackgroundFrames == null || nodeBackgroundFrames.Length == 0) return;
            if (!nodeBackgrounds.TryGetValue(node, out Image background) || background == null) return;
            if (nodeAnimations.TryGetValue(node, out Coroutine current) && current != null) StopCoroutine(current);
            nodeAnimations[node] = StartCoroutine(AnimateNodeBackground(node, background));
        }

        IEnumerator AnimateNodeBackground(GameObject node, Image background)
        {
            for (int i = 0; i < nodeBackgroundFrames.Length; i++)
            {
                if (background == null) yield break;
                background.sprite = nodeBackgroundFrames[i];
                yield return new WaitForSecondsRealtime(frameDuration);
            }

            if (background != null) background.sprite = nodeBackgroundFrames[0];
            nodeAnimations.Remove(node);
        }

        void BindButtonsAndTabs()
        {
            if (evolutionUI == null) return;

            foreach (var button in evolutionUI.GetComponentsInChildren<Button>(true))
            {
                BindHover(button.gameObject);
            }

            var treeButton = evolutionUI.transform.Find("FrameBorder/Frame/Header/Btn_Tree")?.GetComponent<Button>();
            var staffButton = evolutionUI.transform.Find("FrameBorder/Frame/Header/Btn_Staff")?.GetComponent<Button>();
            treeIndicator = evolutionUI.transform.Find("FrameBorder/Frame/Header/Btn_Tree/Indicator")?.GetComponent<Image>();
            staffIndicator = evolutionUI.transform.Find("FrameBorder/Frame/Header/Btn_Staff/Indicator")?.GetComponent<Image>();

            if (treeIndicator != null) treeIndicatorScale = treeIndicator.transform.localScale;
            if (staffIndicator != null) staffIndicatorScale = staffIndicator.transform.localScale;
            if (tabIndicatorFrames != null && tabIndicatorFrames.Length > 0)
            {
                if (treeIndicator != null) treeIndicator.sprite = tabIndicatorFrames[0];
                if (staffIndicator != null) staffIndicator.sprite = tabIndicatorFrames[0];
            }
            AddTabClickListener(treeButton);
            AddTabClickListener(staffButton);
        }

        void AddTabClickListener(Button button)
        {
            if (button == null || tabClickListeners.ContainsKey(button)) return;
            tabButtonScales[button] = button.transform.localScale;
            UnityEngine.Events.UnityAction listener = () => PlayTabClick(button);
            tabClickListeners.Add(button, listener);
            button.onClick.AddListener(listener);
        }

        void PlayTabClick(Button button)
        {
            if (button == null) return;
            if (tabAnimations.TryGetValue(button, out Coroutine current) && current != null) StopCoroutine(current);
            tabAnimations[button] = StartCoroutine(PulseTab(button));
        }

        IEnumerator PulseTab(Button button)
        {
            if (button == null) yield break;
            Vector3 original = tabButtonScales.TryGetValue(button, out Vector3 baseScale) ? baseScale : button.transform.localScale;
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                button.transform.localScale = original * scale;
                yield return null;
            }
            if (button != null) button.transform.localScale = original;
            tabAnimations.Remove(button);
        }

        void UpdateActiveTabIndicator()
        {
            Transform next = null;
            if (treeIndicator != null && treeIndicator.color.a > 0.1f) next = treeIndicator.transform;
            else if (staffIndicator != null && staffIndicator.color.a > 0.1f) next = staffIndicator.transform;

            if (next != activeIndicator)
            {
                if (activeIndicator != null)
                {
                    if (activeIndicator == (treeIndicator != null ? treeIndicator.transform : null))
                        activeIndicator.localScale = treeIndicatorScale;
                    else if (activeIndicator == (staffIndicator != null ? staffIndicator.transform : null))
                        activeIndicator.localScale = staffIndicatorScale;
                }
                activeIndicator = next;
                indicatorPulseStart = Time.unscaledTime;
            }

            if (activeIndicator == null) return;

            Vector3 baseScale = activeIndicator == (treeIndicator != null ? treeIndicator.transform : null)
                ? treeIndicatorScale
                : staffIndicatorScale;
            float pulse = 1f + Mathf.Sin((Time.unscaledTime - indicatorPulseStart) * 7f) * 0.08f;
            activeIndicator.localScale = baseScale * pulse;

            if (tabIndicatorFrames != null && tabIndicatorFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt((Time.unscaledTime - indicatorPulseStart) / 0.16f) % tabIndicatorFrames.Length;
                var image = activeIndicator.GetComponent<Image>();
                if (image != null) image.sprite = tabIndicatorFrames[frame];
            }
        }

        void BindHover(GameObject target)
        {
            if (target == null) return;
            AddTriggerEntry(target, EventTriggerType.PointerEnter, hoverEnterEntries, _ => ShowHover(target));
            AddTriggerEntry(target, EventTriggerType.PointerExit, hoverExitEntries, _ => HideHover(target));
        }

        static void AddTriggerEntry(
            GameObject target,
            EventTriggerType eventType,
            Dictionary<GameObject, EventTrigger.Entry> entryMap,
            Action<BaseEventData> callback)
        {
            var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            if (entryMap.TryGetValue(target, out EventTrigger.Entry existing))
            {
                if (!trigger.triggers.Contains(existing)) trigger.triggers.Add(existing);
                return;
            }

            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
            entryMap.Add(target, entry);
        }

        void ShowHover(GameObject target)
        {
            if (target == null) return;
            if (hoveredObject != null && hoveredObject != target) HideHover(hoveredObject);
            hoveredObject = target;

            if (!hoverFrames.TryGetValue(target, out RectTransform frame) || frame == null)
            {
                frame = CreateHoverFrame(target);
                if (frame == null) return;
                hoverFrames[target] = frame;
            }

            frame.gameObject.SetActive(true);
            StartCoroutine(AnimateHoverFrame(target, frame));
        }

        RectTransform CreateHoverFrame(GameObject target)
        {
            if (!(target.transform is RectTransform)) return null;

            var frameGo = new GameObject("TravelBookHoverFrame", typeof(RectTransform));
            frameGo.transform.SetParent(target.transform, false);
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.offsetMin = new Vector2(-hoverFrameMargin, -hoverFrameMargin);
            frameRect.offsetMax = new Vector2(hoverFrameMargin, hoverFrameMargin);
            frameGo.transform.SetAsLastSibling();

            AddCorner(frameGo.transform, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, hoverTopLeftFrames);
            AddCorner(frameGo.transform, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, hoverTopRightFrames);
            AddCorner(frameGo.transform, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, hoverBottomLeftFrames);
            AddCorner(frameGo.transform, "BottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, hoverBottomRightFrames);
            frameGo.SetActive(false);
            return frameRect;
        }

        void AddCorner(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Sprite[] frames)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(hoverCornerSize, hoverCornerSize);

            var image = go.GetComponent<Image>();
            image.sprite = FirstSprite(frames);
            image.raycastTarget = false;
            image.preserveAspect = false;
        }

        IEnumerator AnimateHoverFrame(GameObject target, RectTransform frame)
        {
            int frameCount = Mathf.Min(
                FrameCount(hoverTopLeftFrames),
                FrameCount(hoverTopRightFrames),
                FrameCount(hoverBottomLeftFrames),
                FrameCount(hoverBottomRightFrames));

            for (int i = 0; i < frameCount; i++)
            {
                if (frame == null || !frame.gameObject.activeInHierarchy) yield break;
                SetCornerSprite(frame, "TopLeft", hoverTopLeftFrames[i]);
                SetCornerSprite(frame, "TopRight", hoverTopRightFrames[i]);
                SetCornerSprite(frame, "BottomLeft", hoverBottomLeftFrames[i]);
                SetCornerSprite(frame, "BottomRight", hoverBottomRightFrames[i]);
                yield return new WaitForSecondsRealtime(frameDuration);
            }

            if (target == null || frame == null) yield break;
            // Keep the final frame while hovered; the four independent corners stay pixel crisp.
        }

        void HideHover(GameObject target)
        {
            if (target == null) return;
            if (hoverFrames.TryGetValue(target, out RectTransform frame) && frame != null)
                frame.gameObject.SetActive(false);
            if (hoveredObject == target) hoveredObject = null;
        }

        static int FrameCount(Sprite[] frames) => frames == null ? 0 : frames.Length;

        static Sprite FirstSprite(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        static void SetCornerSprite(Transform frame, string cornerName, Sprite sprite)
        {
            var image = frame.Find(cornerName)?.GetComponent<Image>();
            if (image != null) image.sprite = sprite;
        }
    }
}
