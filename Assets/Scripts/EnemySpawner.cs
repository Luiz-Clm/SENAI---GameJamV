using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    [SerializeField] private GameObject[] enemyPrefabs; // Lista de inimigos para spawnar
    [SerializeField] private float minY = -4.5f; // Limite inferior da câmera
    [SerializeField] private float maxY = 4.5f;  // Limite superior da câmera
    [SerializeField] private float spawnX = 10f; // Posição fora da tela, à direita

    [Header("Controle de Tempo e Dificuldade")]
    [SerializeField] private float maxSpawnInterval = 3f; // Intervalo máximo no início
    [SerializeField] private float minSpawnInterval = 0.5f; // Intervalo mínimo (mais difícil)
    [SerializeField] private float difficultyRampSpeed = 0.05f; // Quanto diminui por segundo

    private float currentSpawnInterval;
    private float timer;
    private bool isPaused = false;

    void Start()
    {
        currentSpawnInterval = maxSpawnInterval;
        timer = currentSpawnInterval;
    }

    void Update()
    {
        // Se o jogo estiver pausado ou acabar, para de spawnar
        if (isPaused) return;

        // Aumenta a dificuldade com o tempo, diminuindo o intervalo de spawn
        if (currentSpawnInterval > minSpawnInterval)
        {
            currentSpawnInterval -= difficultyRampSpeed * Time.deltaTime;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);
        }

        // Lógica de spawn baseada no tempo
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = currentSpawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Escolhe um prefab aleatório da lista
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        
        // Define a posição de spawn com um Y aleatório
        float randomY = Random.Range(minY, maxY);
        Vector3 spawnPosition = new Vector3(spawnX, randomY, 0f);

        // Cria o inimigo no jogo
        Instantiate(enemyPrefabs[randomIndex], spawnPosition, Quaternion.identity);
    }

    // Método para pausar e despausar o spawner (ex: chamado pelo GameManager ao morrer)
    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }
}
