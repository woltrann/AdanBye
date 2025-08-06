using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Interaction/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int width;
    public int height;
    public bool[,] shape; // [width, height]
}
