using UnityEngine;

/// <summary>
/// Inimigo voador que se move com um padrão senoidal (ondas).
/// </summary>
public sealed class FlyingEnemy : Enemy
{
    [Header("Oscilação")]
    [Tooltip("A amplitude do movimento da onda (altura).")]
    [SerializeField] float amplitude = 2f; 
    
    [Tooltip("A frequência da onda (quão rápido ela sobe e desce).")]
    [SerializeField] float frequency = 2f; 

    private float startY;
    private float elapsedTime;

    protected override void Awake()
    {
        base.Awake();
        // Armazena a posição Y inicial para calcular a onda em torno dela
        startY = transform.position.y;
    }

    protected override void Move()
    {
        // Primeiro realiza o movimento padrão para a esquerda da classe Enemy
        base.Move();
        
        // Em seguida, adiciona o deslocamento vertical usando a função seno
        elapsedTime += Time.deltaTime;
        float newY = startY + Mathf.Sin(elapsedTime * frequency) * amplitude;
        
        // Aplica a nova posição mantendo o X atualizado pela classe base
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
