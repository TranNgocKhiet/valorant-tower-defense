using System.Collections;
using UnityEngine;
using UnityEditor;

public class SlowTurret : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Attributes")]
    [SerializeField] private float targetingRange = 1f;
    // aps: attacks per second
    [SerializeField] private float aps = 1f;
    [SerializeField] private float slowDuration = 1f;
    [SerializeField] private float slowSpeedPercent = 0.2f;

    private Transform target;
    private float timeUntilFire;

    private void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        if (!CheckTartetIsInRange())
        {
            return;
        }
        else
        {
            timeUntilFire += Time.deltaTime;

            if (timeUntilFire >= 1f / aps)
            {
                SlowEnemies();
                timeUntilFire = 0f;
            }
        }
    }

    private bool CheckTartetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= targetingRange;
    }

    private void FindTarget()
    {
        // RaycastHit2D[] hits: array to store all detected objects within the circle cast
        // Physics2D.CircleCastAll(): performs a circle cast in 2D space and returns all hits
        // (cast start point, cast radius, cast direction, distance of the cast, layer to cast on)
        // 0f: means only check the area around the point
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, targetingRange, (Vector2)transform.position, 0f, enemyMask);

        if (hits.Length > 0)
        {
            target = hits[0].transform;
        }
    }

    private void SlowEnemies()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, targetingRange, (Vector2)transform.position, 0f, enemyMask);
        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                EnemyMovement em = hit.transform.GetComponent<EnemyMovement>();

                em.SlowSpeed(slowSpeedPercent);

                StartCoroutine(ResetEnemySpeed(em));
            }
        }
    }

    private IEnumerator ResetEnemySpeed(EnemyMovement em)
    {
        yield return new WaitForSeconds(slowDuration);
        em.ResetSpeed();
    }

    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(transform.position, transform.forward, targetingRange);
    }
}
