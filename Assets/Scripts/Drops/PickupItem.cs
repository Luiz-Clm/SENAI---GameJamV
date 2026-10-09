using UnityEngine;
using WitchShmup.CameraSystem;
using WitchShmup.Core;
using WitchShmup.Player;

namespace WitchShmup.Drops
{
    [RequireComponent(typeof(Collider2D))]
    public class PickupItem : MonoBehaviour
    {
        [Header("Item Type")]
        [SerializeField] private PickupType pickupType = PickupType.SkillPoint;
        [SerializeField] private int value = 1;

        [Header("Floating / Magnet Movement")]
        [SerializeField] private float floatSpeed = 1.2f;
        [SerializeField] private float magnetRadius = 3.5f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float bobbingFrequency = 4f;
        [SerializeField] private float bobbingAmplitude = 0.2f;

        [Header("Audio / FX")]
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private GameObject collectEffectPrefab;

        private static System.Collections.Generic.List<PickupItem> allPickups = new System.Collections.Generic.List<PickupItem>();

        private Transform playerTarget;
        private bool isAttracted = false;
        private float baseHeight;
        private float bobbingPhaseOffset;
        private Vector2 ejectionVelocity;
        private float ejectionDrag = 4.5f;

        private void OnEnable()
        {
            if (!allPickups.Contains(this))
            {
                allPickups.Add(this);
            }
        }

        private void OnDisable()
        {
            allPickups.Remove(this);
        }

        private void Start()
        {
            baseHeight = transform.position.y;
            bobbingPhaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            bobbingFrequency += UnityEngine.Random.Range(-0.4f, 0.4f);

            // Dispersão suave ao dropar para não nascer colado
            Vector2 randDir = UnityEngine.Random.insideUnitCircle.normalized;
            if (randDir == Vector2.zero) randDir = Vector2.up;
            ejectionVelocity = randDir * UnityEngine.Random.Range(1.8f, 3.2f);

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void Update()
        {
            if (playerTarget == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTarget = p.transform;
            }

            // Escala do ímã de almas
            float magnetMult = (GameManager.Instance != null) ? GameManager.Instance.SoulMagnetMultiplier : 1f;
            float effectiveMagnetRadius = magnetRadius * magnetMult;

            // Checa raio magnético até o jogador
            if (playerTarget != null)
            {
                float dist = Vector2.Distance(transform.position, playerTarget.position);
                if (dist <= effectiveMagnetRadius)
                {
                    isAttracted = true;
                }
            }

            if (isAttracted && playerTarget != null)
            {
                // Move-se acelerando até o player
                float speed = magnetSpeed * magnetMult;
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, speed * Time.deltaTime);
            }
            else
            {
                // Desacelera a ejeção inicial
                if (ejectionVelocity.sqrMagnitude > 0.01f)
                {
                    transform.position += (Vector3)(ejectionVelocity * Time.deltaTime);
                    baseHeight = transform.position.y;
                    ejectionVelocity = Vector2.MoveTowards(ejectionVelocity, Vector2.zero, ejectionDrag * Time.deltaTime);
                }

                // Repulsão entre drops próximos para mantê-los separados visualmente
                float separationRadius = 0.85f;
                for (int i = 0; i < allPickups.Count; i++)
                {
                    var other = allPickups[i];
                    if (other != null && other != this && !other.isAttracted)
                    {
                        Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
                        float dist = diff.magnitude;
                        if (dist < separationRadius && dist > 0.001f)
                        {
                            Vector2 push = (diff / dist) * ((separationRadius - dist) * 3f * Time.deltaTime);
                            transform.position += (Vector3)push;
                            baseHeight = transform.position.y;
                        }
                    }
                }

                // Flutua suavemente para a esquerda com oscilação dessincronizada
                float newX = transform.position.x - (floatSpeed * Time.deltaTime);
                float newY = baseHeight + Mathf.Sin((Time.time * bobbingFrequency) + bobbingPhaseOffset) * bobbingAmplitude;
                transform.position = new Vector3(newX, newY, transform.position.z);
            }

            // Despawn se sair muito da tela à esquerda
            if (CameraBounds.Instance != null && CameraBounds.Instance.IsOffscreenLeft(transform.position, 3f))
            {
                Destroy(gameObject);
            }
            else if (transform.position.x < -16f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
            {
                Collect(other.gameObject);
            }
        }

        private void Collect(GameObject player)
        {
            switch (pickupType)
            {
                case PickupType.FireHeart:
                    if (GameManager.Instance != null) GameManager.Instance.AddFireHeart(value);
                    break;

                case PickupType.HealthPotion:
                    var health = player.GetComponentInParent<PlayerHealth>();
                    if (health != null) health.Heal(value);
                    break;

                case PickupType.SkillPoint:
                    if (GameManager.Instance != null) GameManager.Instance.AddSkillPoints(value);
                    break;

                case PickupType.BossCore:
                    if (GameManager.Instance != null) GameManager.Instance.AddBossCore(value);
                    break;
            }

            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
