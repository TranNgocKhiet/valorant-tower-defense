using System.Diagnostics;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager main;

    [Header("References")]
    [SerializeField] private Tower[] towers;

    private GameObject ghostObj;
    private int SelectedTower = 0;
    private float currentRotation = 180f;

    private void Awake()
    {
        main = this;
    }

    private void Update()
    {
        // Inside BuildManager.cs Update()
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (currentRotation == 0f) ? 180f : 0f;

            // If a ghost is currently active, rotate it visually
            if (ghostObj != null)
            {
                // We rotate the whole ghost so the player sees the 120-degree cone flip
                ghostObj.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }
        }
    }

    public float GetCurrentRotation()
    {
        return currentRotation;
    }

    public Tower GetSelectedTower()
    {
        return towers[SelectedTower];
    }

    public void SetSelectedTower(int _selectedTower)
    {
        SelectedTower = _selectedTower;
        if (ghostObj != null) Destroy(ghostObj);
    }
}
