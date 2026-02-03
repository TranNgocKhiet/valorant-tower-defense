using System.Diagnostics;
using UnityEngine;
using TMPro;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthUI;

    private void OnGUI()
    {
        healthUI.text = LevelManager.main.levelHealth.ToString();
    }
}
