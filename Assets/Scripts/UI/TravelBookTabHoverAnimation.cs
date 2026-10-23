using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitchShmup.UI
{
    /// <summary>Shows the TravelBook corner-frame animation while a side tab is hovered.</summary>
    [DisallowMultipleComponent]
    public sealed class TravelBookTabHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Sprite[][] frameSets;
        private Image[] corners;
        private GameObject frameRoot;
        private Coroutine animation;
        private float frameDuration = 0.055f;

        public void Configure(Sprite[][] spriteFrameSets, float cornerSize = 14f, float margin = 5f, float duration = 0.055f)
        {
            frameSets = spriteFrameSets;
            frameDuration = duration;
            if (frameSets == null || frameSets.Length == 0) return;

            if (frameRoot == null) CreateFrame(cornerSize, margin);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (frameRoot == null || frameSets == null || frameSets.Length == 0) return;
            frameRoot.SetActive(true);
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(AnimateFrame());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (frameRoot != null) frameRoot.SetActive(false);
        }

        private void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (frameRoot != null) frameRoot.SetActive(false);
        }

        private void CreateFrame(float cornerSize, float margin)
        {
            frameRoot = new GameObject("TravelBookTabHoverFrame", typeof(RectTransform));
            frameRoot.transform.SetParent(transform, false);
            var frameRect = frameRoot.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.offsetMin = new Vector2(-margin, -margin);
            frameRect.offsetMax = new Vector2(margin, margin);
            frameRoot.transform.SetAsLastSibling();

            corners = new Image[4];
            for (int i = 0; i < corners.Length; i++)
            {
                var corner = new GameObject($"Corner_{i}", typeof(RectTransform), typeof(Image));
                corner.transform.SetParent(frameRoot.transform, false);
                var rect = corner.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = GetAnchor(i);
                rect.pivot = GetAnchor(i);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(cornerSize, cornerSize);

                var image = corner.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = false;
                corners[i] = image;
            }

            frameRoot.SetActive(false);
        }

        private IEnumerator AnimateFrame()
        {
            for (int frame = 0; frame < frameSets.Length; frame++)
            {
                if (frameRoot == null || !frameRoot.activeInHierarchy) yield break;
                Sprite[] sprites = frameSets[frame];
                if (sprites != null)
                {
                    for (int corner = 0; corner < corners.Length; corner++)
                    {
                        if (corners[corner] != null && corner < sprites.Length)
                            corners[corner].sprite = sprites[corner];
                    }
                }
                yield return new WaitForSecondsRealtime(frameDuration);
            }

            animation = null;
        }

        private static Vector2 GetAnchor(int corner)
        {
            switch (corner)
            {
                case 0: return new Vector2(0f, 1f);
                case 1: return new Vector2(1f, 1f);
                case 2: return new Vector2(0f, 0f);
                default: return new Vector2(1f, 0f);
            }
        }
    }
}
