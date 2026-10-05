using UnityEngine;

namespace WitchShmup.CameraSystem
{
    [ExecuteAlways]
    public class CameraBounds : MonoBehaviour
    {
        public static CameraBounds Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Extra Margins (Optional)")]
        [SerializeField] private float marginHorizontal = 0.5f;
        [SerializeField] private float marginVertical = 0.5f;

        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                if (targetCamera == null) targetCamera = Camera.main;
            }

            RecalculateBounds();
        }

        private void Update()
        {
            RecalculateBounds();
        }

        public void RecalculateBounds()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            float verticalExtent = targetCamera.orthographicSize;
            float horizontalExtent = verticalExtent * targetCamera.aspect;
            Vector3 camPos = targetCamera.transform.position;

            MinX = camPos.x - horizontalExtent;
            MaxX = camPos.x + horizontalExtent;
            MinY = camPos.y - verticalExtent;
            MaxY = camPos.y + verticalExtent;
        }

        public Vector3 ClampPosition(Vector3 position, float paddingX = 0f, float paddingY = 0f)
        {
            float clampedX = Mathf.Clamp(position.x, MinX + paddingX + marginHorizontal, MaxX - paddingX - marginHorizontal);
            float clampedY = Mathf.Clamp(position.y, MinY + paddingY + marginVertical, MaxY - paddingY - marginVertical);
            return new Vector3(clampedX, clampedY, position.z);
        }

        public bool IsOffscreenLeft(Vector3 position, float buffer = 2f)
        {
            return position.x < (MinX - buffer);
        }

        public bool IsOffscreenRight(Vector3 position, float buffer = 2f)
        {
            return position.x > (MaxX + buffer);
        }

        private void OnDrawGizmos()
        {
            RecalculateBounds();
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, 0f);
            Vector3 size = new Vector3(MaxX - MinX, MaxY - MinY, 1f);
            Gizmos.DrawWireCube(center, size);

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 innerCenter = center;
            Vector3 innerSize = new Vector3(
                Mathf.Max(0, (MaxX - MinX) - 2 * marginHorizontal),
                Mathf.Max(0, (MaxY - MinY) - 2 * marginVertical),
                1f
            );
            Gizmos.DrawWireCube(innerCenter, innerSize);
        }
    }
}
