using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryData", menuName = "Inventory/InventoryData")]
public class InventoryData : ScriptableObject
{
    [SerializeField] private float currentWeight;
    public float CurrentWeight => currentWeight;

    public float MaxWeight = 50f;

    public List<ItemData> items;

    private void OnEnable()
    {
        if (items == null)
            items = new List<ItemData>();
        currentWeight = 0f;
        foreach (var item in items)
            currentWeight += item.weight;
    }

    public bool CanAdd(ItemData item) => currentWeight + item.weight <= MaxWeight;

    public bool AddItem(ItemData item)
    {
        if (!CanAdd(item))
            return false;

        items.Add(item);
        currentWeight += item.weight;
        return true;
    }

    public void RemoveItem(ItemData item)
    {
        if (items.Remove(item))
            currentWeight -= item.weight;
    }
}
