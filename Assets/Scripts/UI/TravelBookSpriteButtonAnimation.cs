using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitchShmup.UI
{
    /// <summary>Reuses TravelBook sprite sequences for clickable menu controls.</summary>
    [DisallowMultipleComponent]
    public sealed class TravelBookSpriteButtonAnimation : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;
        private Image targetImage;
        private Sprite[] frames;
        private Coroutine animation;
        private bool loopWhileSelected;
        private bool selected;
        private float frameDuration = 0.055f;

        public void Configure(Image image, Sprite[] spriteFrames, bool loopWhenSelected)
        {
            targetImage = image;
            frames = spriteFrames;
            loopWhileSelected = loopWhenSelected;
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickAnimation);
                button.onClick.AddListener(PlayClickAnimation);
            }
            if (targetImage != null)
            {
                targetImage.raycastTarget = false;
                if (loopWhileSelected) targetImage.enabled = false;
            }
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (!loopWhileSelected || frames == null || frames.Length == 0 || targetImage == null) return;
            if (animation != null) StopCoroutine(animation);
            if (selected)
            {
                targetImage.enabled = true;
                animation = StartCoroutine(AnimateLoop());
            }
            else
            {
                animation = null;
                targetImage.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
        }

        private void PlayClickAnimation()
        {
            if (loopWhileSelected)
            {
                SetSelected(selected);
                return;
            }
            if (frames == null || frames.Length == 0 || targetImage == null) return;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(AnimateOnce());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!loopWhileSelected) PlayClickAnimation();
        }

        private IEnumerator AnimateOnce()
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (targetImage == null) yield break;
                targetImage.sprite = frames[i];
                yield return new WaitForSecondsRealtime(frameDuration);
            }
            if (targetImage != null) targetImage.sprite = frames[0];
            animation = null;
        }

        private IEnumerator AnimateLoop()
        {
            int index = 0;
            while (selected && targetImage != null)
            {
                targetImage.sprite = frames[index];
                index = (index + 1) % frames.Length;
                yield return new WaitForSecondsRealtime(frameDuration * 4f);
            }
            animation = null;
        }
    }
}
