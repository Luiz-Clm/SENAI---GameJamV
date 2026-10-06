using System;
using UnityEngine;

/// <summary>
/// Classe base para os inimigos do jogo.
/// Move-se para a esquerda e atira na direção do jogador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Enemy : Character
{
    [Header("Inimigo Config")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int scoreValue = 100;
    
    // Ponto onde o inimigo é destruído por sair da tela
    [SerializeField] protected float destroyOffScreenX = -12f; 
    
    // Tempo para destruir após a morte (para tocar som/animação)
    [SerializeField] protected float destroyDelay = 1f;

    // Evento estático para notificar o GameManager quando um inimigo morre
    public static event Action<int> OnEnemyKilled;

    protected Rigidbody2D rb;

    protected override void Awake()
    {
        base.Awake(); // Inicializa a vida máxima da classe Character
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        Move();
        
        // Tenta atirar para a esquerda (direção do jogador)
        // O método TryShoot na classe Character já lida com o cooldown
        TryShoot(Vector2.left);

        // Destrói o inimigo caso ele saia da tela
        if (transform.position.x < destroyOffScreenX)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lógica de movimentação básica (linha reta para a esquerda).
    /// Pode ser sobrescrita por inimigos com padrões diferentes.
    /// </summary>
    protected virtual void Move()
    {
        transform.Translate(Vector2.left * (moveSpeed * Time.deltaTime), Space.World);
    }

    protected override void OnDeath()
    {
        base.OnDeath(); // Caso a classe base tenha alguma lógica

        // Desativa a física para parar de colidir ou se mover
        if (rb != null)
        {
            rb.simulated = false;
        }

        // Dispara o evento de pontuação para os sistemas que estão escutando (ex: GameManager)
        OnEnemyKilled?.Invoke(scoreValue);

        // Destrói o GameObject após um atraso para permitir a finalização de efeitos visuais ou sonoros
        Destroy(gameObject, destroyDelay);
    }
}
