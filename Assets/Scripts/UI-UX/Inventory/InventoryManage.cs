using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public InventoryUI inventoryUI;

    void Awake()
    {
        Instance = this;
    }

    public bool TryPlaceItem(InventoryItemDragHandler item)
    {
        for (int y = 0; y <= inventoryUI.height - item.size.y; y++)
        {
            for (int x = 0; x <= inventoryUI.width - item.size.x; x++)
            {
                if (CheckSpaceAvailable(x, y, item.size))
                {
                    PlaceItem(item, x, y);
                    return true;
                }
            }
        }

        return false;
    }

    private bool CheckSpaceAvailable(int startX, int startY, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                var cell = inventoryUI.gridCells[startX + x, startY + y];
                if (cell.IsOccupied)
                    return false;
            }
        }

        return true;
    }

    private void PlaceItem(InventoryItemDragHandler item, int startX, int startY)
    {
        for (int y = 0; y < item.size.y; y++)
        {
            for (int x = 0; x < item.size.x; x++)
            {
                inventoryUI.gridCells[startX + x, startY + y].SetOccupied(true);
            }
        }

        // UI konumunu yerleþtir
        var targetCell = inventoryUI.gridCells[startX, startY];
        RectTransform itemRect = item.GetComponent<RectTransform>();
        RectTransform cellRect = targetCell.GetComponent<RectTransform>();
        itemRect.SetParent(inventoryUI.gridParent);
        itemRect.anchoredPosition = cellRect.anchoredPosition;
    }
}
