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
    }
}
