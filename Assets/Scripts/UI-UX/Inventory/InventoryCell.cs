using UnityEngine;
using UnityEngine.UI;

public class InventoryCell : MonoBehaviour
{
    public int x, y;
    public Image image;
    public bool IsOccupied = false;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        image = GetComponent<Image>();
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
        image.color = occupied ? Color.gray : Color.white;
    }

}
