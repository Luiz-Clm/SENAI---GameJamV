using System.Collections;
using UnityEngine;

namespace WitchShmup.Enemies
{
    public class RusherEnemy : EnemyBase
    {
        private enum RusherState
        {
            Approaching,
            LockingOn,
            Charging,
            PastPlayer
        }

        [Header("Movement Speeds")]
        [SerializeField] private float approachSpeed = 3f;
        [SerializeField] private float chargeSpeed = 16f;

        [Header("Timing")]
        [SerializeField] private float approachDuration = 1.2f;
        [SerializeField] private float telegraphDuration = 0.5f;

        [Header("Telegraph Visuals")]
        [SerializeField] private Color telegraphColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float shakeIntensity = 0.08f;

        private RusherState currentState = RusherState.Approaching;
        private Transform playerTarget;
        private Vector2 chargeDirection = Vector2.left;
        private Vector3 baseTelegraphPos;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 2f;
            currentHealth = 2f;
            scoreValue = 150;
            contactDamage = 1f;
        }

        private void Start()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }

            StartCoroutine(BehaviorSequence());
        }

        private IEnumerator BehaviorSequence()
        {
            // 1. Aproximação inicial suave pela direita
            currentState = RusherState.Approaching;
            float timer = 0f;
            while (timer < approachDuration)
            {
                transform.Translate(Vector3.left * (approachSpeed * Time.deltaTime), Space.World);
                timer += Time.deltaTime;
                yield return null;
            }

            // 2. Lock-on & Telegraph (Aviso visual para o player reagir)
            currentState = RusherState.LockingOn;
            baseTelegraphPos = transform.position;

            if (playerTarget != null)
            {
                // Calcula direção em direção ao jogador no momento do lock-on
                chargeDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
            }
            else
            {
                chargeDirection = Vector2.left;
            }

            // Rotaciona para apontar na direção da investida
            float angle = Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + 180f); // 180f se o sprite aponta para esquerda

            if (spriteRenderer != null)
            {
                spriteRenderer.color = telegraphColor;
            }

            timer = 0f;
            while (timer < telegraphDuration)
            {
                // Leve tremor no local para indicar que vai disparar
                Vector2 shake = Random.insideUnitCircle * shakeIntensity;
                transform.position = baseTelegraphPos + new Vector3(shake.x, shake.y, 0f);
                timer += Time.deltaTime;
                yield return null;
            }

            transform.position = baseTelegraphPos;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            // 3. Investida furiosa (Charge!)
            currentState = RusherState.Charging;
        }

        protected override void Update()
        {
            base.Update();

            if (currentState == RusherState.Charging)
            {
                transform.Translate(chargeDirection * (chargeSpeed * Time.deltaTime), Space.World);
            }
        }
    }
}
