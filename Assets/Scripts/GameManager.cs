using UnityEngine;
using System;

/// <summary>
/// Gerencia a pontuação, estado do jogo e recorde.
/// Usa um padrão Singleton simples (sem DontDestroyOnLoad).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int HiScore { get; private set; }
    public bool IsGameOver { get; private set; }

    // Eventos para a HUD e outros sistemas
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHiScoreChanged;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        HiScore = PlayerPrefs.GetInt("HiScore", 0);
    }

    void OnEnable()
    {
        Enemy.OnEnemyKilled += AddScore;
    }

    void OnDisable()
    {
        Enemy.OnEnemyKilled -= AddScore;
    }

    void Start()
    {
        CurrentScore = 0;
        IsGameOver = false;
        OnScoreChanged?.Invoke(CurrentScore);
        OnHiScoreChanged?.Invoke(HiScore);
    }

    /// <summary>
    /// Adiciona pontuação e atualiza o recorde se necessário.
    /// </summary>
    public void AddScore(int points)
    {
        if (IsGameOver) return;

        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);

        if (CurrentScore > HiScore)
        {
            HiScore = CurrentScore;
            PlayerPrefs.SetInt("HiScore", HiScore);
            PlayerPrefs.Save();
            OnHiScoreChanged?.Invoke(HiScore);
        }
    }

    /// <summary>
    /// Chamado quando o jogador morre. Para o spawner e marca o fim do jogo.
    /// </summary>
    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("Game Over! Score Final: " + CurrentScore);

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
            spawner.SetPaused(true);
    }
}
