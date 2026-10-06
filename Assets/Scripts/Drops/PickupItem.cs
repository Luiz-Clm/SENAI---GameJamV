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

        private Transform playerTarget;
        private bool isAttracted = false;
        private float baseHeight;

        private void Start()
        {
            baseHeight = transform.position.y;
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

            // Checa raio magnético até o jogador
            if (playerTarget != null)
            {
                float dist = Vector2.Distance(transform.position, playerTarget.position);
                if (dist <= magnetRadius)
                {
                    isAttracted = true;
                }
            }

            if (isAttracted && playerTarget != null)
            {
                // Move-se acelerando até o player
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, magnetSpeed * Time.deltaTime);
            }
            else
            {
                // Flutua suavemente para a esquerda com oscilação
                float newX = transform.position.x - (floatSpeed * Time.deltaTime);
                float newY = baseHeight + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
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
