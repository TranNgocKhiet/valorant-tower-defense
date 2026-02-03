using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] enemyPrefabs;

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

        // 2. Select the prefab to spawn
        GameObject prefabToSpawn = (currentWave < 2) ? enemyPrefabs[0] : enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // 3. Instantiate at the specific path's start point
        GameObject enemy = Instantiate(prefabToSpawn, selectedPath.startPoint.position, Quaternion.identity);

        // 4. Assign the specific waypoints to the enemy movement script
        // Assuming your movement script is named EnemyMovement (or similar)
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.SetPath(selectedPath.waypoints);
        }
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