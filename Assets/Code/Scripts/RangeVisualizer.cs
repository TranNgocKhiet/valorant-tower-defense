using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeConeRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    [SerializeField] private int segments = 50; // Smoothness of the arc

    void Awake() => lineRenderer = GetComponent<LineRenderer>();

    // This is called by the Turret script to update the visuals
    public void DrawRange(float range, float fov, float baseAngle)
    {
        // +3: Center (0), Start of Arc (1), End of Arc (segments + 1), Return to Center (segments + 2)
        lineRenderer.positionCount = segments + 3;
        Vector3[] points = new Vector3[segments + 3];

        points[0] = transform.position; // Start at center

        float startAngle = baseAngle - (fov / 2f);

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = startAngle + (i * (fov / segments));
            float rad = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * range;
            float y = Mathf.Sin(rad) * range;

            points[i + 1] = transform.position + new Vector3(x, y, 0);
        }

        // NEW: Set the very last point back to the center to close the bottom line
        points[segments + 2] = transform.position;

        lineRenderer.SetPositions(points);
    }
}