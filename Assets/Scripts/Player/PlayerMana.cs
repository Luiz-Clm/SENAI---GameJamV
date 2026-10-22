using System;
using System.Collections;
using UnityEngine;

namespace WitchShmup.Player
{
    /// <summary>
    /// Gerencia o sistema de mana do mago.
    /// Limita os disparos continuos, exigindo que o jogador gerencie sua magia.
    /// Quando a mana zera ou fica insuficiente, o mago entra em estado de "Cansado" (Exhaustion)
    /// e so volta a conseguir atirar apos recarregar uma porcentagem minima (fatigueRecoveryThreshold).
    /// </summary>
    public class PlayerMana : MonoBehaviour
    {
        [Header("Status de Mana (Inspector)")]
        [Tooltip("Mana maxima do mago")]
        [SerializeField] private float maxMana = 100f;

        [Tooltip("Mana atual do mago (somente leitura / debug)")]
        [SerializeField] private float currentMana = 100f;

        [Tooltip("Custo base de mana por disparo")]
        [SerializeField] private float manaCostPerShot = 5f;

        [Tooltip("Taxa de regeneracao de mana por segundo")]
        [SerializeField] private float manaRechargeRate = 22f;

        [Tooltip("Tempo de espera (em segundos) apos o disparo para a mana comecar a recarregar")]
        [SerializeField] private float rechargeDelayAfterShot = 0.45f;

        [Header("Mecanica de Cansaco (Fadiga)")]
        [Tooltip("Porcentagem minima de mana (0.1 = 10%, 0.30 = 30%) para o mago deixar de ficar cansado e voltar a atirar")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float fatigueRecoveryThreshold = 0.30f;

        [Tooltip("Indica se o mago esta cansado e incapaz de atirar")]
        [SerializeField] private bool isExhausted = false;

        [Header("Feedback Visual")]
        [Tooltip("SpriteRenderer do mago para piscar quando tentar atirar cansado")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Cor de aviso no mago ao tentar atirar cansado")]
        [SerializeField] private Color exhaustedAttemptTint = new Color(0.65f, 0.75f, 1f, 0.7f);

        [Header("Feedback Sonoro (Opcional)")]
        [Tooltip("Som ao tentar atirar sem mana")]
        [SerializeField] private AudioClip dryFireSound;
        [SerializeField] private AudioSource audioSource;

        private float lastShotTime = -99f;
        private float lastFeedbackTime = -99f;

        // Propriedades publicas
        public float MaxMana => maxMana;
        public float CurrentMana => currentMana;
        public float ManaCostPerShot => manaCostPerShot;
        public float FatigueRecoveryThreshold => fatigueRecoveryThreshold;
        public bool IsExhausted => isExhausted;
        public float ManaNormalized => (maxMana > 0f) ? Mathf.Clamp01(currentMana / maxMana) : 0f;

        // Eventos
        public event Action<float, float> OnManaChanged;       // (currentMana, maxMana)
        public event Action<bool> OnExhaustionChanged;         // (isExhausted)
        public event Action OnShootAttemptFailed;

        private void Awake()
        {
            currentMana = maxMana;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            OnManaChanged?.Invoke(currentMana, maxMana);
            OnExhaustionChanged?.Invoke(isExhausted);
        }

        private void Update()
        {
            // Regeneracao de mana apos delay do ultimo tiro
            if (Time.time >= lastShotTime + rechargeDelayAfterShot)
            {
                if (currentMana < maxMana)
                {
                    currentMana = Mathf.Min(maxMana, currentMana + manaRechargeRate * Time.deltaTime);

                    // Sai do estado cansado se recuperar ate o limiar minimo
                    if (isExhausted && currentMana >= (maxMana * fatigueRecoveryThreshold))
                    {
                        isExhausted = false;
                        OnExhaustionChanged?.Invoke(false);
                    }

                    OnManaChanged?.Invoke(currentMana, maxMana);
                }
            }
        }

        /// <summary>
        /// Verifica se o mago tem condicoes de atirar (nao esta cansado e tem mana suficiente).
        /// </summary>
        public bool CanShoot(float cost = -1f)
        {
            float required = (cost > 0f) ? cost : manaCostPerShot;
            return !isExhausted && currentMana >= required;
        }

        /// <summary>
        /// Consome mana para atirar. Se a mana esgotar, entra em estado de cansaco.
        /// </summary>
        public bool TryConsumeMana(float cost = -1f)
        {
            float required = (cost > 0f) ? cost : manaCostPerShot;

            if (isExhausted || currentMana < required)
            {
                NotifyShootAttemptFailed();
                return false;
            }

            currentMana = Mathf.Max(0f, currentMana - required);
            lastShotTime = Time.time;

            // Se a mana restante for menor do que o necessario para atirar de novo, entra em cansaco
            if (currentMana < manaCostPerShot)
            {
                if (!isExhausted)
                {
                    isExhausted = true;
                    OnExhaustionChanged?.Invoke(true);
                }
            }

            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }

        /// <summary>
        /// Alerta visual/sonoro quando o jogador pressiona para atirar mas esta sem mana.
        /// </summary>
        public void NotifyShootAttemptFailed()
        {
            if (Time.time - lastFeedbackTime < 0.25f) return;
            lastFeedbackTime = Time.time;

            OnShootAttemptFailed?.Invoke();

            if (audioSource != null && dryFireSound != null)
            {
                audioSource.PlayOneShot(dryFireSound, 0.7f);
            }

            if (spriteRenderer != null)
            {
                StopCoroutine(nameof(FlashExhaustedRoutine));
                StartCoroutine(nameof(FlashExhaustedRoutine));
            }
        }

        private IEnumerator FlashExhaustedRoutine()
        {
            if (spriteRenderer == null) yield break;
            Color orig = spriteRenderer.color;
            spriteRenderer.color = exhaustedAttemptTint;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }

        public void RestoreMana(float amount)
        {
            currentMana = Mathf.Clamp(currentMana + amount, 0f, maxMana);
            if (isExhausted && currentMana >= (maxMana * fatigueRecoveryThreshold))
            {
                isExhausted = false;
                OnExhaustionChanged?.Invoke(false);
            }
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        public void IncreaseMaxMana(float amount)
        {
            maxMana += amount;
            currentMana = Mathf.Min(currentMana + amount, maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        public void IncreaseManaRechargeRate(float amount)
        {
            manaRechargeRate = Mathf.Max(0f, manaRechargeRate + amount);
        }

        public void ReduceRechargeDelay(float amount)
        {
            rechargeDelayAfterShot = Mathf.Max(0f, rechargeDelayAfterShot - amount);
        }

        public void SetFatigueRecoveryThreshold(float threshold)
        {
            fatigueRecoveryThreshold = Mathf.Clamp(threshold, 0.1f, 0.9f);
            if (isExhausted && currentMana >= maxMana * fatigueRecoveryThreshold)
            {
                isExhausted = false;
                OnExhaustionChanged?.Invoke(false);
            }
        }

        public void SetManaCostPerShot(float amount)
        {
            manaCostPerShot = Mathf.Max(0f, amount);
            if (isExhausted && currentMana >= manaCostPerShot)
            {
                isExhausted = false;
                OnExhaustionChanged?.Invoke(false);
            }
        }
    }
}
