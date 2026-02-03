using UnityEngine;

public class FOVVisualizer : MonoBehaviour
{
    public float range = 3f;
    public float fov = 120f;
    public Transform pivot;

    void OnDrawGizmos()
    {
        if (pivot == null) return;

        Gizmos.color = new Color(0, 1, 1, 0.3f); // Transparent Cyan
        float angle = pivot.eulerAngles.z;

        Vector3 leftDir = Quaternion.AngleAxis(-fov / 2f, Vector3.forward) * Quaternion.Euler(0, 0, angle) * Vector3.right;
        Vector3 rightDir = Quaternion.AngleAxis(fov / 2f, Vector3.forward) * Quaternion.Euler(0, 0, angle) * Vector3.right;

        Gizmos.DrawRay(transform.position, leftDir * range);
        Gizmos.DrawRay(transform.position, rightDir * range);
    }
}