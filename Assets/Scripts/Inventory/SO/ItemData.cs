using UnityEngine;

public enum InteractionType
{
    Collect, // Toplanabilir
    Examine, // Ýncelenebilir
    Use,      // Kullanýlabilir
    Chip,
    Consumable,
    Unconsumable
}
public enum ConsumableType
{
    None,
    Food,
    ToksinFood,
    Water,
    ToksinRiverWater,
    ToksinPoolWater,
    Medkit,
    Bandage,
    Antidote,
    PoweredHead,
    PoweredTorso,
    PoweredArm,
    PoweredLeg,
    UpgradeWeight,
    UpgradeSpeed,
    ToksinMask,
    WaterCleaner,
    Recharger
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

    [Header("Consumable")]
    public ConsumableType consumableType;
    public float consumableValue; // Ne kadar iyileþtirecek, doyuracak, su verecek vs.

    public ItemData itemAfterUse;
}
