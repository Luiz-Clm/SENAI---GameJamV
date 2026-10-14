using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using WitchShmup.Combat;

namespace WitchShmup.Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [System.Serializable]
        public class ElementWeaponConfig
        {
            public ElementType element;
            public GameObject projectilePrefab;
            public float fireRate = 0.15f;      // Seconds between shots
            public float damage = 1f;
            public float projectileSpeed = 16f;
            public int pierceCount = 1;
            public Color elementColor = Color.white;
            public AudioClip shootSound;
        }

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
        [SerializeField] private bool isFireUnlocked = false; // Começa apenas com gelo!
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

        private float nextFireTime = 0f;
        private bool isFiring = false;

        public bool IsFireUnlocked => isFireUnlocked;
        public int MultiShotLevel => multiShotLevel;
        public bool IsDoubleShotUnlocked => multiShotLevel >= 1;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public int BonusPierce => bonusPierce;
        public float BonusDamage => bonusDamage;
        public float FireRateBonusPercent => fireRateBonusPercent;

        public ElementType CurrentElement => currentElement;
        public ElementWeaponConfig CurrentConfig => (currentElement == ElementType.Ice) ? iceConfig : fireConfig;

        public event Action<ElementType> OnElementChanged;
        public event Action OnShotFired;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // O jogador sempre começa com Gelo
            currentElement = ElementType.Ice;
            OnElementChanged?.Invoke(currentElement);
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
                Shoot();
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
                // Disparo Triplo: Centro + Superior + Inferior com leve abertura
                SpawnProjectile(basePos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(new Vector3(basePos.x, basePos.y + 0.32f, basePos.z), new Vector2(1f, 0.12f).normalized, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(new Vector3(basePos.x, basePos.y - 0.32f, basePos.z), new Vector2(1f, -0.12f).normalized, config, effectiveDamage, effectivePierce, effectiveSpeed);
            }
            else if (multiShotLevel == 1)
            {
                // Disparo Duplo: 2 magias paralelas
                Vector3 topPos = new Vector3(basePos.x, basePos.y + 0.22f, basePos.z);
                Vector3 botPos = new Vector3(basePos.x, basePos.y - 0.22f, basePos.z);

                SpawnProjectile(topPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
                SpawnProjectile(botPos, Vector2.right, config, effectiveDamage, effectivePierce, effectiveSpeed);
            }
            else
            {
                // Disparo inicial: 1 projétil central único
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
            if (config.projectilePrefab != null)
            {
                GameObject projObj = Instantiate(config.projectilePrefab, position, Quaternion.identity);
                var proj = projObj.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(direction, speed, dmg, config.element, true, pierce);
                }
            }
            else
            {
                // Runtime fallback projectile if prefab is not yet assigned
                GameObject runtimeProj = CreateFallbackProjectile(position, config);
                var proj = runtimeProj.GetComponent<Projectile>();
                proj.Initialize(direction, speed, dmg, config.element, true, pierce);
            }
        }

        private GameObject CreateFallbackProjectile(Vector3 position, ElementWeaponConfig config)
        {
            GameObject obj = new GameObject($"Projectile_{config.element}");
            obj.transform.position = position;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.color = config.elementColor;
            sr.sortingOrder = 5;

            // Generate small 16x6 pixel bullet sprite
            Texture2D tex = new Texture2D(16, 6, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[16 * 6];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 6), new Vector2(0.5f, 0.5f), 16f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 0.35f);

            obj.AddComponent<Projectile>();
            return obj;
        }
    }
}
