using UnityEngine;

// Edit > Project Settings > Player > Active Input Handling > CHOOSE Input Manager (Old)
public class TowerPlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color hoverColor;

    private GameObject towerObj;
    private Turret turret;
    private Color startColor;
    private GameObject currentGhost; // To keep track of the ghost instance
    private float currentRotation = 180f;

    private void Start()
    {
        startColor = sr.color;
    }

    private void OnMouseEnter()
    {
        sr.color = hoverColor;

        if (towerObj == null)
        {
            Tower towerToBuild = BuildManager.main.GetSelectedTower();
            if (towerToBuild != null)
            {
                currentGhost = Instantiate(towerToBuild.prefab, transform.position, Quaternion.identity);
                SetGhostOpacity(currentGhost, 0.5f);

                if (currentGhost.TryGetComponent(out Turret ghostTurret))
                {
                    ghostTurret.enabled = false;

                    // --- NEW LOGIC: SHOW FULL CIRCLE DURING GHOST ---
                    // This calls the helper function we added to Turret.cs earlier
                    ghostTurret.TogglePlacementCircle(true);

                    // Hide the cone on the ghost so it's not messy
                    RangeConeRenderer range = currentGhost.GetComponentInChildren<RangeConeRenderer>();
                    if (range != null) range.gameObject.SetActive(false);
                }
            }
        }

        if (currentGhost != null)
        {
            // 1. Set the ghost and all its children to Layer 2 (Ignore Raycast)
            SetLayerRecursive(currentGhost, 2);

            // 2. Ensure its collider is off just in case
            if (currentGhost.TryGetComponent(out Collider2D col)) col.enabled = false;
        }
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void OnMouseExit()
    {
        sr.color = startColor;

        // Remove the ghost when the mouse leaves the plot
        if (currentGhost != null)
        {
            Destroy(currentGhost);
        }
    }

    private void OnMouseDown()
    {
        if (UIManager.main.IsHoveringUI()) return;

        if (towerObj != null)
        {
            turret.OpenUpgradeUI();
            return;
        }

        Tower towerToBuild = BuildManager.main.GetSelectedTower();
        if (towerToBuild.cost > LevelManager.main.currency) return;

        LevelManager.main.SpendCurrency(towerToBuild.cost);

        // --- PLACING THE ACTUAL TOWER ---
        towerObj = Instantiate(towerToBuild.prefab, transform.position, Quaternion.identity);
        turret = towerObj.GetComponent<Turret>();

        // 1. Show the Arrow UI and the Cone Preview immediately
        turret.directionUI.SetActive(true);

        // 2. Hide the Full Circle (it's no longer a ghost)
        turret.TogglePlacementCircle(false);

        // 3. Set the default rotation
        float finalAngle = BuildManager.main.GetCurrentRotation();
        turret.baseRotationAngle = finalAngle;

        Transform pivot = towerObj.transform.Find("TurretRotationPoint");
        if (pivot != null) pivot.rotation = Quaternion.Euler(0, 0, finalAngle);

        if (currentGhost != null) Destroy(currentGhost);
    }

    public float GetCurrentRotation()
    {
        return currentRotation;
    }

    void Update()
    {
        if (towerObj == null && turret != null)
        {
            turret = null;
        }

        if (currentGhost != null && Input.GetKeyDown(KeyCode.R))
        {
            // Toggle local rotation logic or sync with BuildManager
            float rotation = BuildManager.main.GetCurrentRotation();
            Transform pivot = currentGhost.transform.Find("TurretRotationPoint");
            if (pivot != null) pivot.rotation = Quaternion.Euler(0, 0, rotation);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (currentRotation == 0f) ? 180f : 0f;
        }
    }

    public void SetGhostOpacity(GameObject ghost, float alphaValue)
    {
        // Get the SpriteRenderer from the ghost or its children
        SpriteRenderer ghostSR = ghost.GetComponentInChildren<SpriteRenderer>();

        if (ghostSR != null)
        {
            Color color = ghostSR.color;
            color.a = alphaValue; // Value between 0.0f (invisible) and 1.0f (solid)
            ghostSR.color = color;
        }
    }
}
