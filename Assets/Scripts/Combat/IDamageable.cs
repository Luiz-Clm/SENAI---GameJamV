namespace WitchShmup.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, ElementType element);
    }
}
