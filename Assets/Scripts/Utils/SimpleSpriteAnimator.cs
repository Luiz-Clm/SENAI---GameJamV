using UnityEngine;

namespace WitchShmup.Utils
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 8f; // Frames por segundo
        [SerializeField] private bool loop = true;

        private SpriteRenderer spriteRenderer;
        private float timer = 0f;
        private int currentFrame = 0;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetFrames(Sprite[] newFrames, float fps = 8f)
        {
            frames = newFrames;
            frameRate = fps;
            currentFrame = 0;
            timer = 0f;
            if (frames != null && frames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1 || spriteRenderer == null) return;

            timer += Time.deltaTime;
            if (timer >= (1f / frameRate))
            {
                timer = 0f;
                currentFrame++;
                if (currentFrame >= frames.Length)
                {
                    if (loop) currentFrame = 0;
                    else currentFrame = frames.Length - 1;
                }
                spriteRenderer.sprite = frames[currentFrame];
            }
        }
    }
}
