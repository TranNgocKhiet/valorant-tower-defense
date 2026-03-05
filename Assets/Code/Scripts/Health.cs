using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private int hitPoints = 2;
    [SerializeField] int rewardOnDestroy = 50;

    private bool isDestroyed = false;

    public void Initialize(int hp)
    {
        hitPoints = hp;
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;

        if (hitPoints <= 0 && !isDestroyed)
        {
            Die();
            LevelManager.main.IncreaseCurrency(rewardOnDestroy);
        }
    }

    private void Die()
    {
        EnemySpawner.onEnemyDestroy.Invoke();
        isDestroyed = true;
        Destroy(gameObject);
    }
}
