using System.Collections;
using UnityEngine;

namespace WitchShmup.CameraSystem
{
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        private Vector3 originalLocalPos;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            originalLocalPos = transform.localPosition;
        }

        public void Shake(float duration = 0.4f, float intensity = 0.25f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(intensity, 0f, elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * strength;
                transform.localPosition = originalLocalPos + new Vector3(offset.x, offset.y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
            shakeCoroutine = null;
        }
    }
}
