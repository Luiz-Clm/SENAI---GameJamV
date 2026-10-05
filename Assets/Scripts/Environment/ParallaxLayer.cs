using UnityEngine;
using WitchShmup.CameraSystem;

namespace WitchShmup.Environment
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [Tooltip("1 = normal speed, 0.5 = distant background, 1.2 = close foreground")]
        [SerializeField] private float parallaxFactor = 1f;

        [Header("Sprite Parts")]
        [Tooltip("Two or three sprite renderers arranged horizontally side by side")]
        [SerializeField] private SpriteRenderer[] segments;
        [SerializeField] private float segmentWidth = 20f;

        private void Start()
        {
            AutoSetupSegments();
        }

        public void AutoSetupSegments()
        {
            if (segments == null || segments.Length == 0)
            {
                segments = GetComponentsInChildren<SpriteRenderer>();
            }

            if (segments != null && segments.Length > 0 && segments[0] != null)
            {
                if (segments[0].sprite != null)
                {
                    segmentWidth = segments[0].bounds.size.x;
                }
            }
        }

        public void Scroll(float baseSpeed, float deltaTime)
        {
            float moveAmount = baseSpeed * parallaxFactor * deltaTime;

            // Move each segment to the left
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                Vector3 pos = segments[i].transform.position;
                pos.x -= moveAmount;
                segments[i].transform.position = pos;
            }

            // Check if any segment has scrolled off-screen left and wrap it to the right
            float minScreenX = (CameraBounds.Instance != null) ? CameraBounds.Instance.MinX : -12f;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                // If segment's right edge is behind the left edge of the camera
                if (segments[i].transform.position.x + (segmentWidth * 0.5f) < minScreenX)
                {
                    // Find the rightmost segment to attach behind
                    float rightmostX = GetRightmostSegmentX();
                    Vector3 pos = segments[i].transform.position;
                    pos.x = rightmostX + segmentWidth;
                    segments[i].transform.position = pos;
                }
            }
        }

        private float GetRightmostSegmentX()
        {
            float maxX = float.MinValue;
            foreach (var seg in segments)
            {
                if (seg != null && seg.transform.position.x > maxX)
                {
                    maxX = seg.transform.position.x;
                }
            }
            return (maxX == float.MinValue) ? transform.position.x : maxX;
        }
    }
}
