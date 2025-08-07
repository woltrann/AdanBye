using UnityEngine;
using UnityEngine.TextCore.Text;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public float weight;

    public Sprite sprite;

}
