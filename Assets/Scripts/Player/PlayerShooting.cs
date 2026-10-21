using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using WitchShmup.Combat;

namespace WitchShmup.Player
{
    [RequireComponent(typeof(PlayerMana))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerShooting : MonoBehaviour
    {
        private void OnValidate()
        {
            if (playerMana == null)
            {
                playerMana = GetComponent<PlayerMana>();
#if UNITY_EDITOR
                if (playerMana == null && !Application.isPlaying)
                {
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (this != null && playerMana == null && gameObject != null)
                        {
                            playerMana = GetComponent<PlayerMana>();
                            if (playerMana == null)
                            {
                                playerMana = UnityEditor.Undo.AddComponent<PlayerMana>(gameObject);
                                UnityEditor.EditorUtility.SetDirty(gameObject);
                            }
                        }
                    };
                }
#endif
            }
        }
        [System.Serializable]
        public class ElementWeaponConfig
        {
            public ElementType element;
            public GameObject projectilePrefab;
            [Tooltip("Tempo em segundos entre cada disparo (cadência). Quanto menor, mais rápido atira.")]
            public float fireRate = 0.16f;
            [Tooltip("Dano base de cada projétil.")]
            public float damage = 1f;
            [Tooltip("Velocidade com que o projétil se move pela tela.")]
            public float projectileSpeed = 18f;
            [Tooltip("Quantos inimigos o projétil pode atravessar.")]
            public int pierceCount = 1;
            public Color elementColor = Color.white;
            public AudioClip shootSound;
            [Tooltip("Custo de mana específico deste elemento (0 = usa o padrão de PlayerMana)")]
            public float manaCost = 0f;
        }

        [Header("Mana Reference")]
        [Tooltip("Componente de Mana do mago (gerenciado automaticamente se vazio)")]
        [SerializeField] private PlayerMana playerMana;

        [Header("Active Element")]
        [SerializeField] private ElementType currentElement = ElementType.Ice;

        [Header("Weapon Configurations")]
        [SerializeField] private ElementWeaponConfig iceConfig = new ElementWeaponConfig
        {
            element = ElementType.Ice,
            fireRate = 0.16f,
            damage = 1f,
            projectileSpeed = 18f,
            pierceCount = 1,
            elementColor = new Color(0.35f, 0.85f, 1f, 1f) // Cyan
        };

        [SerializeField] private ElementWeaponConfig fireConfig = new ElementWeaponConfig
        {
            element = ElementType.Fire,
            fireRate = 0.28f,
            damage = 1.8f,
            projectileSpeed = 14f,
            pierceCount = 1,
            elementColor = new Color(1f, 0.55f, 0.15f, 1f) // Orange
        };

        [Header("Muzzle / Fire Points")]
        [SerializeField] private Transform[] firePoints;
        [SerializeField] private Vector2 defaultMuzzleOffset = new Vector2(0.6f, 0f);

        [Header("Upgrades State")]
        [SerializeField] private bool isFireUnlocked = true; // Tiro de Fogo liberado!
        [SerializeField] private int multiShotLevel = 0;       // 0 = 1 tiro, 1 = 2 tiros, 2 = 3 tiros
        [SerializeField] private float projectileSpeedMultiplier = 1f;
        [SerializeField] private int bonusPierce = 0;
        [SerializeField] private float bonusDamage = 0f;
        [SerializeField] private float fireRateBonusPercent = 0f;
        [SerializeField] private float iceDamageBonus = 0f;
        [SerializeField] private float fireDamageBonus = 0f;
        [SerializeField] private int fireBonusPierce = 0;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Natural Wizard/Staff Light (Ajuste Manual no Inspector)")]
        [Tooltip("Luz natural emanada pelo cajado do mago")]
        [SerializeField] private Light2D staffLight;
        [Tooltip("Intensidade do brilho do cajado")]
        [SerializeField] private float staffLightIntensity = 1.25f;
        [Tooltip("Raio de alcance da luz do mago")]
        [SerializeField] private float staffLightRadius = 5.0f;
        [Tooltip("Cor da aura de Gelo")]
        [SerializeField] private Color iceAuraColor = new Color(0.35f, 0.85f, 1f, 1f);
        [Tooltip("Cor da aura de Fogo")]
        [SerializeField] private Color fireAuraColor = new Color(1f, 0.58f, 0.22f, 1f);

        private float nextFireTime = 0f;
        private bool isFiring = false;

        public bool IsFiring => isFiring;
        public bool IsFireUnlocked => isFireUnlocked;
        public int MultiShotLevel => multiShotLevel;
        public bool IsDoubleShotUnlocked => multiShotLevel >= 1;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public int BonusPierce => bonusPierce;
        public float BonusDamage => bonusDamage;
        public float FireRateBonusPercent => fireRateBonusPercent;

        public PlayerMana Mana => playerMana;
        public ElementType CurrentElement => currentElement;
        public ElementWeaponConfig CurrentConfig => (currentElement == ElementType.Ice) ? iceConfig : fireConfig;

        public event Action<ElementType> OnElementChanged;
        public event Action OnShotFired;

        private void Awake()
        {
            if (playerMana == null)
            {
                playerMana = GetComponent<PlayerMana>();
            }
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D Sound
            }

            // Auto-carrega os sons dos tiros caso não estejam assinalados no Inspector
            if (iceConfig.shootSound == null)
            {
                iceConfig.shootSound = Resources.Load<AudioClip>("Audio/PlayerIce_LancaCongelada");
            }
            if (fireConfig.shootSound == null)
            {
                fireConfig.shootSound = Resources.Load<AudioClip>("Audio/PlayerFire_DardoChamas");
            }
        }

        private void Start()
        {
            // O jogador sempre começa com Gelo
            currentElement = ElementType.Ice;
            SetupStaffLight();
            OnElementChanged?.Invoke(currentElement);
        }

        private void SetupStaffLight()
        {
            if (staffLight == null)
            {
                var lightChild = transform.Find("Staff_NaturalLight");
                if (lightChild != null)
                {
                    staffLight = lightChild.GetComponent<Light2D>();
                }
            }

            if (staffLight != null)
            {
                int pLayer = SortingLayer.NameToID("Player");
                int defLayer = SortingLayer.NameToID("Default");
                if (pLayer != 0)
                {
                    staffLight.targetSortingLayers = new int[] { pLayer, defLayer };
                }
            }
            UpdateStaffLightColor();
        }

        public void UpdateStaffLightColor()
        {
            if (staffLight != null)
            {
                // A luz natural muda de acordo com o elemento do cajado
                Color targetCol = (currentElement == ElementType.Fire) ? fireAuraColor : iceAuraColor;
                staffLight.color = targetCol;
                staffLight.intensity = staffLightIntensity;
                staffLight.pointLightOuterRadius = staffLightRadius;
                staffLight.pointLightInnerRadius = staffLightRadius * 0.1f;
            }
        }

        private void Update()
        {
            HandleElementSwitchInput();
            HandleFireInput();
        }

        private void HandleElementSwitchInput()
        {
            bool switched = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame ||
                    Keyboard.current.tabKey.wasPressedThisFrame ||
                    Keyboard.current.leftShiftKey.wasPressedThisFrame)
                {
                    ToggleElement();
                    switched = true;
                }
                else if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    SetElement(ElementType.Ice);
                    switched = true;
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    SetElement(ElementType.Fire);
                    switched = true;
                }
            }

            if (!switched && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                ToggleElement();
                switched = true;
            }

            if (!switched && Gamepad.current != null)
            {
                if (Gamepad.current.buttonNorth.wasPressedThisFrame ||
                    Gamepad.current.rightShoulder.wasPressedThisFrame ||
                    Gamepad.current.leftShoulder.wasPressedThisFrame)
                {
                    ToggleElement();
                    switched = true;
                }
            }
#endif

            // Fallback for standard input
            if (!switched)
            {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab) || Input.GetMouseButtonDown(1))
                {
                    ToggleElement();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SetElement(ElementType.Ice);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SetElement(ElementType.Fire);
                }
            }
        }

        private void HandleFireInput()
        {
            isFiring = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.jKey.isPressed || Keyboard.current.enterKey.isPressed)
                {
                    isFiring = true;
                }
            }

            if (!isFiring && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                isFiring = true;
            }

            if (!isFiring && Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.rightTrigger.isPressed)
                {
                    isFiring = true;
                }
            }
#endif

            if (!isFiring)
            {
                isFiring = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
            }

            if (isFiring && Time.time >= nextFireTime)
            {
                float cost = (CurrentConfig != null && CurrentConfig.manaCost > 0f) ? CurrentConfig.manaCost : -1f;
                if (playerMana != null)
                {
                    if (playerMana.CanShoot(cost))
                    {
                        if (playerMana.TryConsumeMana(cost))
                        {
                            Shoot();
                        }
                    }
                    else
                    {
                        playerMana.NotifyShootAttemptFailed();
                    }
                }
                else
                {
                    Shoot();
                }
            }
        }

        public void ToggleElement()
        {
            if (currentElement == ElementType.Ice)
            {
                if (isFireUnlocked)
                {
                    SetElement(ElementType.Fire);
                }
                else
                {
                    Debug.Log("[PlayerShooting] Poder de Fogo Bloqueado! Colete 3 Corações de Fogo e 1 Núcleo de Boss para evoluir o cajado!");
                }
            }
            else
            {
                SetElement(ElementType.Ice);
            }
        }

        public void SetElement(ElementType newElement)
        {
            if (newElement == ElementType.Fire && !isFireUnlocked)
            {
                Debug.Log("[PlayerShooting] Poder de Fogo Bloqueado! Colete 3 Corações de Fogo e 1 Núcleo de Boss para evoluir o cajado!");
                return;
            }

            if (currentElement == newElement) return;

            currentElement = newElement;
            UpdateStaffLightColor();
            OnElementChanged?.Invoke(currentElement);
        }

        public void UnlockFireElement()
        {
            isFireUnlocked = true;
            SetElement(ElementType.Fire);
        }

        public void SetMultiShotLevel(int level)
        {
            multiShotLevel = Mathf.Max(multiShotLevel, level);
        }

        public void UnlockDoubleShot()
        {
            SetMultiShotLevel(1);
        }

        public void AddProjectileSpeedBonus(float percent)
        {
            projectileSpeedMultiplier += percent;
        }

        public void AddIceMasteryBonus(float extraDamage)
        {
            iceDamageBonus += extraDamage;
        }

        public void AddFireMasteryBonus(float extraDamage, int extraPierce = 1)
        {
            fireDamageBonus += extraDamage;
            fireBonusPierce += extraPierce;
        }

        public void AddBonusDamage(float amount)
        {
            bonusDamage += amount;
        }

        public void AddBonusPierce(int amount)
        {
            bonusPierce += amount;
        }

        public void AddFireRateBonus(float percent)
        {
            fireRateBonusPercent += percent;
        }

        public void UpgradeStaffBonus(float damageMultiplier)
        {
            iceConfig.damage *= damageMultiplier;
            fireConfig.damage *= damageMultiplier;
            iceConfig.projectileSpeed *= 1.25f;
            fireConfig.projectileSpeed *= 1.25f;
            iceConfig.fireRate *= 0.85f;
            fireConfig.fireRate *= 0.85f;
            UnlockFireElement();
        }

        public void MultiplyFireRate(float rateMultiplier)
        {
            iceConfig.fireRate *= rateMultiplier;
            fireConfig.fireRate *= rateMultiplier;
        }

        public void BoostElementDamage(ElementType element, float mult)
        {
            if (element == ElementType.Ice) iceConfig.damage *= mult;
            else if (element == ElementType.Fire) fireConfig.damage *= mult;
        }

        private void Shoot()
        {
            var config = CurrentConfig;
            float effectiveRate = config.fireRate * (1f - Mathf.Clamp(fireRateBonusPercent, 0f, 0.65f));
            nextFireTime = Time.time + effectiveRate;

            float effectiveDamage = config.damage + bonusDamage + (currentElement == ElementType.Ice ? iceDamageBonus : fireDamageBonus);
            int effectivePierce = config.pierceCount + bonusPierce + (currentElement == ElementType.Fire ? fireBonusPierce : 0);
            float effectiveSpeed = config.projectileSpeed * projectileSpeedMultiplier;

            Vector3 basePos = transform.position + (Vector3)defaultMuzzleOffset;

            if (multiShotLevel >= 2)
            {
                // Disparo Triplo: 3 fileiras paralelas na vertical (superior, centro, inferior)
                Vector3 topPos = new Vector3(basePos.x, basePos.y + 0.28f, basePos.z);
                Vector3 midPos = basePos;
                Vector3 botPos = new Vector3(basePos.x, basePos.y - 0.28f, basePos.z);

                SpawnProjectile(topPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(midPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(botPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
            }
            else if (multiShotLevel == 1)
            {
                // Disparo Duplo: 2 fileiras paralelas na vertical
                Vector3 topPos = new Vector3(basePos.x, basePos.y + 0.20f, basePos.z);
                Vector3 botPos = new Vector3(basePos.x, basePos.y - 0.20f, basePos.z);

                SpawnProjectile(topPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(botPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
            }
            else
            {
                // Disparo inicial: 1 fileira central única
                SpawnProjectile(basePos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
            }

            if (config.shootSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(config.shootSound);
            }

            OnShotFired?.Invoke();
        }

        private void SpawnProjectile(Vector3 position, Vector2 direction, ElementWeaponConfig config, float dmg, int pierce, float speed)
        {
            if (config.projectilePrefab == null) return;

            GameObject projObj = Instantiate(config.projectilePrefab, position, Quaternion.identity);

            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(direction, speed, dmg, config.element, true, pierce);
            }
        }
    }
}
