using System.Collections.Generic;
using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public List<GameObject> panels; // Inspector�dan ekle
    private int currentIndex = 0;

    private void Start()
    {
        // Ba�lang��ta sadece currentIndex'teki panel a��k olsun
        ShowPanel(currentIndex);
    }

    public void NextPanel()
    {
        currentIndex++;
        if (currentIndex >= panels.Count)
            currentIndex = panels.Count - 1; // En sondaysa ileri gitmesin

        ShowPanel(currentIndex);
    }

    public void PreviousPanel()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = 0; // En ba�taysa geri gitmesin

        ShowPanel(currentIndex);
    }

    public void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == index);
        }
    }
}
