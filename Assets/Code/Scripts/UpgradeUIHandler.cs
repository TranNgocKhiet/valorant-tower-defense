using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeUIHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.main.SetHoveringState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.main.SetHoveringState(false);
    }

    private float openTime;

    private void OnEnable()
    {
        openTime = Time.time; // Record when the menu appeared
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            // If the direction UI is still active, we are in "Placement Mode"
            // We shouldn't try to close menus yet.
            if (GetComponent<Turret>().directionUI.activeSelf) return;

            if (!UIManager.main.IsHoveringUI())
            {
                UIManager.main.CloseActiveMenu();
            }
        }
    }
}