using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform gridParent;

    public int width = 7;
    public int height = 5;

    [HideInInspector]
    public InventoryCell[,] gridCells;

    void Start()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        gridCells = new InventoryCell[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridParent);
                InventoryCell cell = cellObj.GetComponent<InventoryCell>();
                cell.Init(x, y);
                gridCells[x, y] = cell;
            }
        }
    }
}
