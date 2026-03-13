using UnityEngine;

/* 
 * MonoBehaviour: base class every Unity script inherits from,
 * allows to attach this script to a GameObject
 * and gives access to "Events" like Start() and Update()
 */
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Attributes")]
    [SerializeField] public float moveSpeed = 2f;
    [SerializeField] private int attackDamage = 1;

    private Transform[] waypoints; // Local copy of the specific path for THIS enemy
    private Transform target;
    private int pathIndex = 0;
    private float baseSpeed;
    private bool isSlowActive = false;

    // 1. This method is called by the EnemySpawner immediately after spawning
    public void SetPath(Transform[] _waypoints)
    {
        waypoints = _waypoints;

        // Start moving toward the first waypoint in the local path
        if (waypoints != null && waypoints.Length > 0)
        {
            target = waypoints[pathIndex];
        }
    }

    private void Start()
    {
        baseSpeed = moveSpeed;
        // We no longer set the target here because waypoints are assigned via SetPath()
    }

    private void Update()
    {
        // Safety check to ensure waypoints have been assigned
        if (target == null) return;

        if (Vector2.Distance(target.position, transform.position) <= 0.1f)
        {
            pathIndex++;

            // 2. Check against the local waypoints length instead of LevelManager
            if (pathIndex == waypoints.Length)
            {
                EnemySpawner.onEnemyDestroy.Invoke();
                Destroy(gameObject);
                LevelManager.main.DecreaseHealth(attackDamage);
                return;
            }
            else
            {
                target = waypoints[pathIndex];
            }
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    public void SetAttributes(float speed, int damage)
    {
        moveSpeed = speed;
        baseSpeed = speed;
        attackDamage = damage;
    }

    public void UpdateSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void SlowSpeed(float slowPercent)
    {
        if (isSlowActive) return;
        moveSpeed = Mathf.Max(0f, moveSpeed * (1- slowPercent));
        isSlowActive = true;
    }

    public void ResetSpeed()
    {
        moveSpeed = baseSpeed;
    }
}
