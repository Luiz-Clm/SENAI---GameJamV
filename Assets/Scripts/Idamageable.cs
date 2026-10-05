// Contrato para qualquer objeto que pode levar dano (Character, barril, parede destrutivel...)
public interface IDamageable
{
    void TakeDamage(int amount);
}
 