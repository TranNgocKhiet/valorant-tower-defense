using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EnemySpawnData
{
    public GameObject prefab;

    [Header("Attributes")]
    [Tooltip("Movement speed of this enemy type")]
    public float moveSpeed = 2f;
    [Tooltip("Hit points for this enemy type")]
    public int health = 2;
    [Tooltip("Damage dealt to the base when this enemy reaches the end")]
    public int attackDamage = 1;
    [Tooltip("Relative spawn weight (higher = spawns more often)")]
    public int spawnWeight = 1;
    [Tooltip("Currency rewarded to the player when this enemy is destroyed")]
    public int currencyReward = 50;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnData[] enemyData;

    [Header("Attributes")]
    [SerializeField] private int baseEnemies = 8;
    [SerializeField] private float enemiesPerSec = 0.5f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyScalingFactor = 0.75f;
    [SerializeField] private float enemiesPerSecCap = 15f;

    [Header("Events")]
    public static UnityEvent onEnemyDestroy = new UnityEvent();

    private int currentWave = 1;
    private float timeSinceLastSpawn;
    private int enemiesAlive;
    private int enemiesLeftToSpawn;
    private bool isSpawning = false;
    private float eps;

    private void Awake()
    {
        onEnemyDestroy.AddListener(EnemyDestroyed);
    }

    public void Start()
    {
        StartCoroutine(StartWave());
    }

    private void Update()
    {
        if (!isSpawning) return;

        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= (1f / eps) && enemiesLeftToSpawn > 0)
        {
            SpawnEnemy();
            enemiesLeftToSpawn--;
            enemiesAlive++;
            timeSinceLastSpawn = 0f;
        }

        if (enemiesAlive == 0 && enemiesLeftToSpawn == 0)
        {
            EndWave();
        }
    }

    private void EnemyDestroyed()
    {
        enemiesAlive--;
    }

    private IEnumerator StartWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        isSpawning = true;
        enemiesLeftToSpawn = EnemiesPerWave();
        eps = EnemiesPerSecond();
    }

    private void EndWave()
    {
        isSpawning = false;
        timeSinceLastSpawn = 0f;
        currentWave++;
        StartCoroutine(StartWave());
    }

    private void SpawnEnemy()
    {
        // 1. Get a random path group from the updated LevelManager
        EnemyPath selectedPath = LevelManager.main.GetPath(currentWave);

        if (selectedPath == null)
        {
            Debug.LogError("No paths defined in LevelManager!");
            return;
        }

        // 2. Select enemy type: always use first entry for wave 1, weighted random afterwards
        EnemySpawnData selectedData = (currentWave < 2) ? enemyData[0] : GetWeightedRandomEnemy();
        if (selectedData == null || selectedData.prefab == null)
        {
            Debug.LogError("No valid enemy data configured in EnemySpawner!");
            return;
        }

        // 3. Instantiate at the specific path's start point
        GameObject enemy = Instantiate(selectedData.prefab, selectedPath.startPoint.position, Quaternion.identity);

        // 4. Apply per-type attributes
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.SetPath(selectedPath.waypoints);
            movement.SetAttributes(selectedData.moveSpeed, selectedData.attackDamage);
        }

        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.Initialize(selectedData.health, selectedData.currencyReward);
        }
    }

    private EnemySpawnData GetWeightedRandomEnemy()
    {
        if (enemyData == null || enemyData.Length == 0) return null;

        int totalWeight = 0;
        foreach (EnemySpawnData data in enemyData)
            totalWeight += Mathf.Max(0, data.spawnWeight);

        if (totalWeight <= 0)
            return enemyData[Random.Range(0, enemyData.Length)];

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (EnemySpawnData data in enemyData)
        {
            cumulative += Mathf.Max(0, data.spawnWeight);
            if (roll < cumulative)
                return data;
        }
        return enemyData[enemyData.Length - 1];
    }

    private int EnemiesPerWave()
    {
        return Mathf.RoundToInt(baseEnemies * Mathf.Pow(currentWave, difficultyScalingFactor));
    }

    private float EnemiesPerSecond()
    {
        return Mathf.Clamp(enemiesPerSec * Mathf.Pow(currentWave, difficultyScalingFactor), 0f, enemiesPerSecCap);
    }
}