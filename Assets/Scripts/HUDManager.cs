using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField, Tooltip("Referência para o texto de pontuação.")] 
    private TMP_Text scoreText;
    
    [SerializeField, Tooltip("Referência para o texto do nome da fase.")] 
    private TMP_Text phaseText;
    
    [SerializeField, Tooltip("Referência para o texto de pontuação máxima (Hi-Score).")] 
    private TMP_Text hiScoreText;

    [Header("Vidas")]
    [SerializeField, Tooltip("Lista de imagens que representam os corações (vidas) do jogador.")] 
    private Image[] hearts;
    
    [SerializeField, Tooltip("Sprite para o coração cheio.")] 
    private Sprite heartFull;
    
    [SerializeField, Tooltip("Sprite para o coração vazio.")] 
    private Sprite heartEmpty;

    [Header("Referências")]
    [SerializeField, Tooltip("Referência ao jogador para atualizar a UI de vida.")] 
    private Player player;

    void Start()
    {
        // Inscreve-se nos eventos para atualizar a interface quando houver mudanças
        if (player != null)
            player.OnHealthChanged += UpdateHearts;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnHiScoreChanged += UpdateHiScore;
        }
        
        // Inicializa a interface com os valores atuais
        UpdateScore(0);
        UpdateHiScore(GameManager.Instance != null ? GameManager.Instance.HiScore : 0);
        UpdateHearts(player != null ? player.CurrentHealth : 5, player != null ? player.MaxHealth : 5);
    }

    /// <summary>
    /// Atualiza o texto da pontuação.
    /// </summary>
    /// <param name="score">Pontuação atual.</param>
    void UpdateScore(int score)
    {
        scoreText.text = $"SCORE {score:D7}";
    }

    /// <summary>
    /// Atualiza o texto da pontuação máxima.
    /// </summary>
    /// <param name="hiScore">Pontuação máxima.</param>
    void UpdateHiScore(int hiScore)
    {
        hiScoreText.text = $"HI {hiScore:D7}";
    }

    /// <summary>
    /// Atualiza os ícones de coração baseando-se na vida atual do jogador.
    /// </summary>
    /// <param name="current">Vida atual.</param>
    /// <param name="max">Vida máxima.</param>
    void UpdateHearts(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            // Ativa apenas a quantidade de corações correspondente à vida máxima
            hearts[i].gameObject.SetActive(i < max);
            
            // Define o sprite correto (cheio ou vazio) dependendo da vida atual
            hearts[i].sprite = i < current ? heartFull : heartEmpty;
        }
    }

    void OnDestroy()
    {
        // Remove a inscrição dos eventos para evitar memory leaks
        if (player != null)
            player.OnHealthChanged -= UpdateHearts;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnHiScoreChanged -= UpdateHiScore;
        }
    }
}
