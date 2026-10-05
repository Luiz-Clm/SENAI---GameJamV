using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using WitchShmup.CameraSystem;

namespace WitchShmup.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 40f;

        [Header("Flying Tilt (Juice)")]
        [SerializeField] private bool enableTilt = true;
        [SerializeField] private float tiltAngle = 14f;
        [SerializeField] private float tiltSpeed = 10f;

        [Header("Boundary Padding")]
        [SerializeField] private float paddingX = 0.6f;
        [SerializeField] private float paddingY = 0.6f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 currentVelocity;
        private float currentTiltZ = 0f;

        public Vector2 MoveInput => moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            ReadInput();
            HandleTilt();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ClampToScreen();
        }

        private void ReadInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                Vector2 dpad = Gamepad.current.dpad.ReadValue();
                Vector2 combined = stick + dpad;
                if (combined.sqrMagnitude > 0.04f)
                {
                    input = combined;
                }
            }
#endif

            // Fallback for Legacy Input if active
            if (input == Vector2.zero)
            {
                input.x = Input.GetAxisRaw("Horizontal");
                input.y = Input.GetAxisRaw("Vertical");
            }

            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void ApplyMovement()
        {
            Vector2 targetVelocity = moveInput * moveSpeed;
            float accelRate = (moveInput.sqrMagnitude > 0.01f) ? acceleration : deceleration;

            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = currentVelocity;
        }

        private void HandleTilt()
        {
            if (!enableTilt) return;

            // Tilt up when moving up, tilt down when moving down
            float targetTilt = -moveInput.y * tiltAngle;
            currentTiltZ = Mathf.Lerp(currentTiltZ, targetTilt, Time.deltaTime * tiltSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, currentTiltZ);
        }

        private void ClampToScreen()
        {
            if (CameraBounds.Instance != null)
            {
                Vector3 clamped = CameraBounds.Instance.ClampPosition(transform.position, paddingX, paddingY);
                transform.position = clamped;
            }
            else
            {
                // Fallback direct camera clamp
                Camera cam = Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float vertExtent = cam.orthographicSize;
                    float horizExtent = vertExtent * cam.aspect;
                    Vector3 camPos = cam.transform.position;

                    float minX = camPos.x - horizExtent + paddingX;
                    float maxX = camPos.x + horizExtent - paddingX;
                    float minY = camPos.y - vertExtent + paddingY;
                    float maxY = camPos.y + vertExtent - paddingY;

                    float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
                    float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
                    transform.position = new Vector3(clampedX, clampedY, transform.position.z);
                }
            }
        }
    }
}
