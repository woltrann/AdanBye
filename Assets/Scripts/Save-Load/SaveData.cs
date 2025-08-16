using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // Karakter bilgileri
    public int level;

    // Body parts
    public float headHealth, torsoHealth, armsHealth, legsHealth;
    public float maxHeadHealth, maxTorsoHealth, maxArmsHealth, maxLegsHealth;

    // Statlar
    public float currentHunger, currentThirst, currentPoison;
    public float maxHunger, maxThirst, maxPoison;
    public float currentWeight, maxWeight;
    public float droidCharge, maxDroidCharge;

    public float SpawnPointX, SpawnPointY, SpawnPointZ; // Karakterin spawn noktasý
    public float DroidSpawnPointX, DroidSpawnPointY, DroidSpawnPointZ; // Droidin spawn noktasý
    

    // Inventory
    public List<int> inventoryItemIDs; // ItemData ID'sini sakla

    // Gün saati
    public float timeOfDay; // DayCycle scriptindeki float timeOfDay
}
