using System;
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
        private Action onPlayOnceComplete;
        private bool playingOnce = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Define os frames de animação com loop infinito (comportamento padrão).
        /// </summary>
        public void SetFrames(Sprite[] newFrames, float fps = 8f)
        {
            frames = newFrames;
            frameRate = fps;
            loop = true;
            playingOnce = false;
            onPlayOnceComplete = null;
            currentFrame = 0;
            timer = 0f;
            if (frames != null && frames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        /// <summary>
        /// Executa a animação UMA vez e chama o callback ao terminar.
        /// Ideal para animações de ataque que não devem ficar em loop.
        /// </summary>
        public void PlayOnce(Sprite[] newFrames, float fps = 10f, Action onComplete = null)
        {
            frames = newFrames;
            frameRate = fps;
            loop = false;
            playingOnce = true;
            onPlayOnceComplete = onComplete;
            currentFrame = 0;
            timer = 0f;
            if (frames != null && frames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        /// <summary>
        /// Pausa a animação no frame atual.
        /// </summary>
        public void Pause()
        {
            loop = false;
            playingOnce = false;
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
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        // Fica no último frame
                        currentFrame = frames.Length - 1;
                        if (playingOnce)
                        {
                            playingOnce = false;
                            // Invoca callback (ex: voltar para idle)
                            onPlayOnceComplete?.Invoke();
                            onPlayOnceComplete = null;
                        }
                        return;
                    }
                }
                spriteRenderer.sprite = frames[currentFrame];
            }
        }
    }
}
