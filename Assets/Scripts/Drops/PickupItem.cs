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
        private float despawnTimer = 25f; // Itens somem após 25s se não coletados

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

        private Vector3 spawnPosition;

        private void Start()
        {
            spawnPosition = transform.position;
            baseHeight = spawnPosition.y;
            bobbingPhaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            bobbingFrequency += UnityEngine.Random.Range(-0.4f, 0.4f);

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }

            // Fallback de Sprite caso esteja nulo
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                switch (pickupType)
                {
                    case PickupType.FireHeart:
                        sr.sprite = Resources.Load<Sprite>("Sprites/FireHeart");
                        break;
                    case PickupType.HealthPotion:
                        sr.sprite = Resources.Load<Sprite>("Sprites/HealthPotion");
                        break;
                    case PickupType.SkillPoint:
                        sr.sprite = Resources.Load<Sprite>("Sprites/SkillPoint");
                        break;
                    case PickupType.BossCore:
                        sr.sprite = Resources.Load<Sprite>("Sprites/BossCore");
                        break;
                }
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
                // Voa em direção ao jogador ao ser atraído
                float speed = magnetSpeed * magnetMult;
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, speed * Time.deltaTime);
            }
            else
            {
                // Permanece exatamente onde o inimigo morreu, apenas flutuando/oscilando levemente na vertical
                float newY = baseHeight + Mathf.Sin((Time.time * bobbingFrequency) + bobbingPhaseOffset) * bobbingAmplitude;
                transform.position = new Vector3(spawnPosition.x, newY, spawnPosition.z);
            }


            // Despawn por timer (item não se move mais, então não sai da tela sozinho)
            if (!isAttracted)
            {
                despawnTimer -= Time.deltaTime;
                if (despawnTimer <= 0f)
                {
                    Destroy(gameObject);
                }
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
                    // Busca o PlayerHealth de forma robusta
                    var health = player.GetComponentInParent<PlayerHealth>() ?? player.GetComponent<PlayerHealth>();
                    if (health == null) health = FindFirstObjectByType<PlayerHealth>();

                    if (health != null)
                    {
                        // Se a vida estiver cheia, guarda no inventário
                        if (health.CurrentHealth >= health.MaxHealth)
                        {
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.AddHealthPotionToInventory(value);
                            }
                        }
                        else
                        {
                            health.Heal(value);
                        }
                    }
                    break;

                case PickupType.SkillPoint:
                    if (GameManager.Instance != null) GameManager.Instance.AddSkillPoints(value);
                    break;

                case PickupType.BossCore:
                    if (GameManager.Instance != null) GameManager.Instance.AddBossCore(value);
                    break;
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
