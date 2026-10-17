using UnityEngine;
using WitchShmup.CameraSystem;

namespace WitchShmup.Environment
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [Tooltip("1 = normal speed, 0.5 = distant background, 1.2 = close foreground")]
        [SerializeField] private float parallaxFactor = 1f;

        [Header("Segments")]
        [Tooltip("Direct segment transforms arranged horizontally side by side")]
        [SerializeField] private Transform[] segments;
        [SerializeField] private float segmentWidth = 24f;

        public float ParallaxFactor
        {
            get => parallaxFactor;
            set => parallaxFactor = value;
        }

        public float SegmentWidth
        {
            get => segmentWidth;
            set => segmentWidth = value;
        }

        private void Start()
        {
            AutoSetupSegments();
        }

        public void SetSegments(Transform[] newSegments, float width)
        {
            segments = newSegments;
            segmentWidth = width;
        }

        public void AutoSetupSegments()
        {
            if (segments == null || segments.Length == 0)
            {
                // Only take DIRECT children as segments to prevent picking up torch sub-objects
                int childCount = transform.childCount;
                if (childCount > 0)
                {
                    segments = new Transform[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        segments[i] = transform.GetChild(i);
                    }
                }
            }

            if (segments != null && segments.Length > 0 && segments[0] != null)
            {
                var sr = segments[0].GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    if (sr.drawMode == SpriteDrawMode.Tiled)
                    {
                        segmentWidth = sr.size.x;
                    }
                    else
                    {
                        segmentWidth = sr.bounds.size.x;
                    }
                }

                // Garante que todos os segmentos estejam na layer Background e com cor visível
                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i] == null) continue;
                    var segSr = segments[i].GetComponent<SpriteRenderer>();
                    if (segSr != null)
                    {
                        segSr.sortingLayerName = "Background";
                        // Se a cor do segmento estiver escura demais, clareia para a textura de tijolos da masmorra aparecer
                        if (segSr.color.r < 0.35f && segSr.color.g < 0.35f)
                        {
                            segSr.color = new Color(0.55f, 0.52f, 0.68f, 1f);
                        }
                    }
                }
            }
        }

        public void Scroll(float baseSpeed, float deltaTime)
        {
            if (segments == null || segments.Length == 0) return;

            float moveAmount = baseSpeed * parallaxFactor * deltaTime;

            // Move each segment to the left
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                Vector3 pos = segments[i].position;
                pos.x -= moveAmount;
                segments[i].position = pos;
            }

            // Check if any segment has scrolled off-screen left and wrap it to the right
            float minScreenX = (CameraBounds.Instance != null) ? CameraBounds.Instance.MinX : -12f;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                // If segment's right edge is behind the left edge of the camera
                if (segments[i].position.x + (segmentWidth * 0.5f) < minScreenX)
                {
                    float rightmostX = GetRightmostSegmentX();
                    Vector3 pos = segments[i].position;
                    pos.x = rightmostX + segmentWidth;
                    segments[i].position = pos;
                }
            }
        }

        private float GetRightmostSegmentX()
        {
            float maxX = float.MinValue;
            foreach (var seg in segments)
            {
                if (seg != null && seg.position.x > maxX)
                {
                    maxX = seg.position.x;
                }
            }
            return (maxX == float.MinValue) ? transform.position.x : maxX;
        }
    }
}
