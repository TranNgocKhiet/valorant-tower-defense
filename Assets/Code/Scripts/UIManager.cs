using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager main;

    private bool isHoveringUI;
    private GameObject currentActiveUI; // Track the currently open menu

    private void Awake()
    {
        main = this;
    }

    public void SetHoveringState(bool state)
    {
        isHoveringUI = state;
    }

    public bool IsHoveringUI()
    {
        return isHoveringUI;
    }

    // NEW: Central logic to handle opening menus
    public void OpenUpgradeMenu(GameObject menu)
    {
        // Close the previous menu if a different one is already open
        if (currentActiveUI != null && currentActiveUI != menu)
        {
            currentActiveUI.SetActive(false);
        }

        currentActiveUI = menu;
        currentActiveUI.SetActive(true);
    }

    // NEW: Central logic to close the menu
    public void CloseActiveMenu()
    {
        if (currentActiveUI != null)
        {
            currentActiveUI.SetActive(false);
            currentActiveUI = null;
            isHoveringUI = false;
        }
    }
}