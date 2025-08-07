using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private InventoryData inventoryData;
    public GameObject InventoryCanvas;
    public Transform contentParent; // ScrollView -> Content
    public GameObject itemPrefab;
    public TextMeshProUGUI weightText;

    private void Awake()
    {

        inventoryData = GameObject.FindWithTag("Player").GetComponent<PlayerManager>().InventoryData;
    }

    public void RefreshUI()
    {
        if (InventoryCanvas.activeSelf)
        {
            InventoryCanvas.SetActive(false);
            return;
        }
        else
            InventoryCanvas.SetActive(true);


        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var item in inventoryData.items)
        {
            var itemUI = Instantiate(itemPrefab, contentParent);
            itemUI.GetComponent<InventoryItemUI>().Setup(item);
        }

        weightText.text = $"Weight: {inventoryData.CurrentWeight}/{inventoryData.MaxWeight} kg";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            RefreshUI();
        }
    }
}
