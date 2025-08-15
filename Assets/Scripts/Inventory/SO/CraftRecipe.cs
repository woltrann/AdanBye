using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftRequirement
{
    public ItemData item;
    public int amount;
}

[CreateAssetMenu(fileName = "NewCraftRecipe", menuName = "Inventory/Craft Recipe")]
public class CraftRecipe : ScriptableObject
{
    public string recipeName;
    public List<CraftRequirement> requirements;
    public ItemData resultItem;
    public int resultAmount = 1;
}
