using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Turret : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform turretRotationPoint;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [SerializeField] private GameObject upgradeUI;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private SpriteRenderer sr;

    [Header("Attributes")]
    [SerializeField]  float targetingRange = 3f;
    [SerializeField] private float rotaionSpeed = 5f;
    [SerializeField] public float seeFeePercent = 0.5f; // 50% refund on sell

    // bps: bullets per second
    [SerializeField] private float bps = 1f;
    [SerializeField] private float baseUpgradeCost = 100;

    [Header("FOV Settings")]
    [SerializeField] public float fieldOfView = 120f; // Total angle of the cone
    [Range(0, 360)]
    [SerializeField] public float baseRotationAngle = 180f; // 0 = Right, 180 = Left

    [Header("Visuals")]
    [SerializeField] private RangeConeRenderer rangeRenderer;
    [SerializeField] private GameObject fullRangeCircle;

    [Header("Placement UI")]
    [SerializeField] public GameObject directionUI;

    private Transform target;
    private float timeUntilFire;
    private int upgradeLevel = 1;
    private float bpsBase;
    private float targetingRangeBase;

    private void Start()
    {
        bpsBase = bps;
        targetingRangeBase = targetingRange;

        upgradeButton.onClick.AddListener(Upgrade);

        if (directionUI != null && directionUI.activeSelf)
        {
            TogglePlacementCircle(false); // Hide the full circle
            rangeRenderer.gameObject.SetActive(true); // Show the cone immediately
            UpdateRangeVisuals();
        }
    }

    public void UpdateRangeVisuals()
    {
        if (rangeRenderer != null)
        {
            rangeRenderer.DrawRange(targetingRange, fieldOfView, baseRotationAngle);
        }
    }

    private void Update()
    {
        if (directionUI != null && directionUI.activeSelf) return;

        if (target == null)
        {
            FindTarget();
            return;
        } 

        RotateTorwardsTarget();

        if(!CheckTartetIsInRange())
        {
            target = null;
        } else
        {
            timeUntilFire += Time.deltaTime;

            if (timeUntilFire >= 1f/bps)
            {
                Shoot();
                timeUntilFire = 0f;
            }
        }
    }

    private void Shoot()
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        
        Bullet bulletScript =  bulletObj.GetComponent<Bullet>();
        bulletScript.SetTarget(target);
    }

    private void FindTarget()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, targetingRange, (Vector2)transform.position, 0f, enemyMask);

        foreach (RaycastHit2D hit in hits)
        {
            // 1. Calculate direction to the potential enemy
            Vector2 dirToEnemy = (hit.transform.position - transform.position).normalized;

            // 2. Calculate direction the turret "should" be facing (the center of its cone)
            Vector2 forwardDir = new Vector2(Mathf.Cos(baseRotationAngle * Mathf.Deg2Rad), Mathf.Sin(baseRotationAngle * Mathf.Deg2Rad));

            // 3. Check the angle between the two vectors
            float angleToEnemy = Vector2.Angle(forwardDir, dirToEnemy);

            // 4. If the angle is within half of our FOV (60 degrees on each side), it's a valid target
            if (angleToEnemy <= fieldOfView / 2f)
            {
                target = hit.transform;
                return; // Target found, stop looking
            }
        }
    }

    private bool CheckTartetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= targetingRange;
    }

    private void RotateTorwardsTarget()
    {
        float targetAngle = Mathf.Atan2(target.position.y - transform.position.y, target.position.x - transform.position.x) * Mathf.Rad2Deg;
        float angleDiff = Mathf.DeltaAngle(baseRotationAngle, targetAngle);

        // Clamp to your 120-degree cone (60 degrees each way)
        angleDiff = Mathf.Clamp(angleDiff, -fieldOfView / 2f, fieldOfView / 2f);
        float finalAngle = baseRotationAngle + angleDiff + 180;

        turretRotationPoint.rotation = Quaternion.Euler(0, 0, finalAngle);

        // Logic to keep the gun upright:
        // If facing Left (180), we flip Y to keep the gun from being upside down
        if (finalAngle > 90 && finalAngle < 270)
        {
            sr.flipY = true;
        }
        else
        {
            sr.flipY = false;
        }
    }

    public void Upgrade()
    { 
        if (baseUpgradeCost > LevelManager.main.currency)
        {
            return;
        }

        LevelManager.main.SpendCurrency(CaculateCost());
        upgradeLevel++;

        CaculateBps();
        CaculateRange();

        Debug.Log("New BPS: " + CaculateBps());
        Debug.Log("New Range: " + CaculateRange());
        Debug.Log("New Cost: " + CaculateCost());

        CloseUpgradeUI();
    }

    public void SellTurret()
    {
        // 1. Calculate the refund (e.g., 50% of the base cost)
        // You could also calculate total spent if you track upgrades
        int refundAmount = Mathf.RoundToInt(baseUpgradeCost * seeFeePercent);

        // 2. Add currency back to the player
        LevelManager.main.currency += refundAmount;

        // 3. Clear the UI state in UIManager
        UIManager.main.CloseActiveMenu();

        // 4. Destroy the turret object
        Destroy(gameObject);

        Debug.Log($"Turret sold for {refundAmount} Radianite.");
    }

    private int CaculateCost()
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeLevel, 0.8f));    
    }

    private float CaculateBps()
    {
        return bpsBase * Mathf.Pow(upgradeLevel, 0.6f);
    }

    private float CaculateRange()
    {
        return targetingRangeBase * Mathf.Pow(upgradeLevel, 0.4f);
    }

    public void TogglePlacementCircle(bool isActive)
    {
        if (fullRangeCircle != null)
        {
            fullRangeCircle.SetActive(isActive);

            // Get the SpriteRenderer to check the original size
            SpriteRenderer circleSR = fullRangeCircle.GetComponent<SpriteRenderer>();
            if (circleSR != null)
            {
                // Calculate the diameter we want (Range * 2)
                float desiredDiameter = targetingRange * 2f;

                // Get the current size of the sprite in world units (usually 1 or 2 depending on the asset)
                float spriteSize = circleSR.sprite.bounds.size.x;

                // Set the scale so the resulting world size matches our diameter
                float scaleFactor = desiredDiameter / spriteSize;
                fullRangeCircle.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            }
        }
    }

    // Call this from the "Pointer Enter" event of your buttons
    public void ShowRangePreview(bool isLeft)
    {
        // Hide the full circle while looking at a specific direction
        if (fullRangeCircle != null) fullRangeCircle.SetActive(false);

        if (rangeRenderer != null)
        {
            rangeRenderer.gameObject.SetActive(true);
            float previewAngle = isLeft ? 180f : 0f;
            rangeRenderer.DrawRange(targetingRange, fieldOfView, previewAngle);
        }
    }

    // Call this from the "Pointer Exit" event of your buttons
    public void HideRangePreview()
    {
        if (rangeRenderer != null)
        {
            // Hide it again
            rangeRenderer.gameObject.SetActive(false);

            // Reset the visualizer to the actual current baseRotationAngle 
            // so it's ready for the next time it's shown
            UpdateRangeVisuals();
        }
    }

    // OnDrawGizmosSelected: Unity event called when the object is selected in the editor
    // Handles: part of UnityEditor namespace, used for drawing shapes in the Scene view
    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(transform.position, transform.forward, targetingRange);

        // Draw FOV Cone lines
        Vector3 leftBoundary = Quaternion.AngleAxis(-fieldOfView / 2f, Vector3.forward) * new Vector2(Mathf.Cos(baseRotationAngle * Mathf.Deg2Rad), Mathf.Sin(baseRotationAngle * Mathf.Deg2Rad));
        Vector3 rightBoundary = Quaternion.AngleAxis(fieldOfView / 2f, Vector3.forward) * new Vector2(Mathf.Cos(baseRotationAngle * Mathf.Deg2Rad), Mathf.Sin(baseRotationAngle * Mathf.Deg2Rad));

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftBoundary * targetingRange);
        Gizmos.DrawRay(transform.position, rightBoundary * targetingRange);
    }

    private void OnMouseEnter()
    {
        // Only show the cone if the arrows are gone (placement is finished)
        if (directionUI != null && !directionUI.activeSelf)
        {
            rangeRenderer.gameObject.SetActive(true);
            UpdateRangeVisuals();
        }
    }

    private void OnMouseExit()
    {
        rangeRenderer.gameObject.SetActive(false);
    }

    // Updated function
    public void SetDirectionRight()
    {
        baseRotationAngle = 0f;
        sr.flipX = false;
        UpdateRangeVisuals();

        FinishedPlacement(); // Remove arrows immediately
    }

    // Updated function
    public void SetDirectionLeft()
    {
        baseRotationAngle = 180f;
        sr.flipX = true;
        UpdateRangeVisuals();

        FinishedPlacement(); // Remove arrows immediately
    }

    public void FinishedPlacement()
    {
        // 1. Hide the Arrow UI
        if (directionUI != null) directionUI.SetActive(false);

        // 2. Hide the full circle
        if (fullRangeCircle != null) fullRangeCircle.SetActive(false);

        // 3. ADD THIS: Hide the cone immediately so it only shows on next hover
        if (rangeRenderer != null) rangeRenderer.gameObject.SetActive(false);

        Debug.Log("Turret locked. All visuals hidden until hover.");
    }

    // Inside Turret.cs

    public void CancelPlacement()
    {
        // 1. Refund the cost to the player
        // Note: You might need to store the initial cost if it varies
        LevelManager.main.currency += 100; // Adjust based on your tower's cost variable

        // 2. Reset the UIManager state
        UIManager.main.SetHoveringState(false);

        // 3. Destroy this turret
        Destroy(gameObject);

        Debug.Log("Placement cancelled. Currency refunded.");
    }

    // Inside Turret.cs

    public void OpenUpgradeUI()
    {
        UIManager.main.OpenUpgradeMenu(upgradeUI);
    }

    public void CloseUpgradeUI()
    {
        upgradeUI.SetActive(false);
        UIManager.main.SetHoveringState(false);
    }

    // Add this inside your Turret class
    private void OnMouseDown()
    {
        // Use this to see which turret is actually being hit
        Debug.Log("Attempting to open menu for: " + gameObject.name);

        if (directionUI != null && !directionUI.activeSelf)
        {
            // Close other menus first via UIManager
            UIManager.main.OpenUpgradeMenu(upgradeUI);
        }
    }
}
