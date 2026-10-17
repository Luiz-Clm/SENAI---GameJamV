using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchShmup.Player
{
    /// <summary>
    /// Controla as animações visuais do Mago:
    /// - Idle: Flutuação suave padrão em loop (MAGOidle)
    /// - Ataque: Conjuração e disparo mágico (MAGOattak)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerVisualController : MonoBehaviour
    {
        [Header("Animation Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] attackFrames;

        [Header("Animation Speeds (FPS)")]
        [SerializeField] private float idleFps = 6f;
        [SerializeField] private float attackFps = 12f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerShooting shooting;

        private enum State { Idle, Attacking }
        private State currentState = State.Idle;
        private float frameTimer = 0f;
        private int currentFrame = 0;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = "Player";
                spriteRenderer.sortingOrder = 10;
            }
            if (shooting == null) shooting = GetComponent<PlayerShooting>();

            AutoLoadFramesIfEmpty();
        }

        private void OnEnable()
        {
            if (shooting != null)
            {
                shooting.OnShotFired += HandleShotFired;
            }
        }

        private void OnDisable()
        {
            if (shooting != null)
            {
                shooting.OnShotFired -= HandleShotFired;
            }
        }

        private void Start()
        {
            if (idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private void Update()
        {
            UpdateAnimation(Time.deltaTime);
        }

        private void HandleShotFired()
        {
            PlayAttackAnimation();
        }

        public void PlayAttackAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0) return;

            currentState = State.Attacking;
            currentFrame = 0;
            frameTimer = 0f;
            if (spriteRenderer != null && attackFrames.Length > 0)
            {
                spriteRenderer.sprite = attackFrames[0];
            }
        }

        private void UpdateAnimation(float dt)
        {
            if (spriteRenderer == null) return;

            Sprite[] currentFrames = (currentState == State.Attacking) ? attackFrames : idleFrames;
            float fps = (currentState == State.Attacking) ? attackFps : idleFps;

            if (currentFrames == null || currentFrames.Length == 0) return;

            frameTimer += dt;
            if (frameTimer >= (1f / fps))
            {
                frameTimer = 0f;
                currentFrame++;

                if (currentFrame >= currentFrames.Length)
                {
                    if (currentState == State.Attacking)
                    {
                        // Se o jogador ainda estiver segurando o botão de tiro, repete o ataque
                        if (shooting != null && shooting.IsFiring)
                        {
                            currentFrame = 0;
                        }
                        else
                        {
                            // Retorna suavemente para o idle
                            currentState = State.Idle;
                            currentFrame = 0;
                            currentFrames = idleFrames;
                        }
                    }
                    else
                    {
                        // Loop infinito de idle
                        currentFrame = 0;
                    }
                }

                if (currentFrames != null && currentFrame < currentFrames.Length && currentFrames[currentFrame] != null)
                {
                    spriteRenderer.sprite = currentFrames[currentFrame];
                }
            }
        }

        public void SetFrames(Sprite[] idle, Sprite[] attack)
        {
            idleFrames = idle;
            attackFrames = attack;
            currentFrame = 0;
            frameTimer = 0f;
            if (idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private void AutoLoadFramesIfEmpty()
        {
            if (idleFrames == null || idleFrames.Length == 0)
            {
                idleFrames = Resources.LoadAll<Sprite>("Sprites/MAGOidle");
#if UNITY_EDITOR
                if (idleFrames == null || idleFrames.Length == 0)
                {
                    idleFrames = LoadSpritesFromPath("Assets/Sprites/MAGOidle.png");
                }
#endif
            }

            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = Resources.LoadAll<Sprite>("Sprites/MAGOattak");
#if UNITY_EDITOR
                if (attackFrames == null || attackFrames.Length == 0)
                {
                    attackFrames = LoadSpritesFromPath("Assets/Sprites/MAGOattak.png");
                }
#endif
            }

            if (spriteRenderer != null && idleFrames != null && idleFrames.Length > 0 && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

#if UNITY_EDITOR
        private static Sprite[] LoadSpritesFromPath(string path)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s) list.Add(s);
            }
            return list.ToArray();
        }
#endif
    }
}
