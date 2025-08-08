using System.Collections.Generic;
using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public List<GameObject> panels; // Inspector'dan atayacaksýn

    public void ShowFirstPanel()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == 0);
        }
    }

    public void ShowLastPanel()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == panels.Count - 1);
        }
    }
}
