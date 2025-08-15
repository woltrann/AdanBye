using UnityEngine;

public enum InteractionType
{
    Collect, // Toplanabilir
    Examine, // Ýncelenebilir
    Use,      // Kullanýlabilir
    Chip
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Interaction/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string itemName;
    public int itemID; // Oyundaki benzersiz ID
    public InteractionType interactionType;

    [Header("Görsel & Ses")]
    public Sprite icon;
    public AudioClip voiceLine;

    [Header("Ek Bilgi")]
    [TextArea] public string description;
    public float weight;
}
