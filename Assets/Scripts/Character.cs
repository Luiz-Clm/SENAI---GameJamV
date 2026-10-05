using System;
using UnityEngine;

public abstract class Character : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] int maxHealth = 5;
    [SerializeField] float invincibleTime = 0.5f;

    [Header("Tiro")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float shootCooldown = 0.35f;
    [SerializeField] string targetTag; // quem esse personagem pode acertar

    [Header("Feedback")]
    [SerializeField] protected Animator anim;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip shootSfx, hurtSfx, deathSfx;

    public event Action<int, int> OnHealthChanged;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    protected bool IsDead { get; private set; }

    float lastHitTime = float.NegativeInfinity;
    float nextShotTime;

    protected virtual void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(int amount)
    {
        if (IsDead || Time.time < lastHitTime + invincibleTime) return;
        lastHitTime = Time.time;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth > 0)
        {
            anim.SetTrigger("hurt");
            Play(hurtSfx);
            return;
        }

        IsDead = true;
        anim.SetTrigger("die");
        Play(deathSfx);
        OnDeath();
    }

    protected bool TryShoot(Vector2 direction)
    {
        if (IsDead || Time.time < nextShotTime) return false;
        nextShotTime = Time.time + shootCooldown;

        Projectile p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        p.Init(direction, targetTag);
        anim.SetTrigger("attack");
        Play(shootSfx);
        return true;
    }

    // Cada filho decide o que acontece ao morrer (player para, inimigo some)
    protected virtual void OnDeath() { }

    void Play(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }
}