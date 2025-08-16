using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject droidPrefab;
    public MainCharacter mainCharacter;
    public DayCycle dayCycle;

    public Transform spawnPoint;
    public Transform droidSpawnPoint;

    private string savePath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, "save.json");

        // Initialize the ItemDatabase
        ItemDatabase.Initialize();
        LoadGame();
    }

    public void SaveGame()
    {

        SaveData data = new SaveData();

        // Character data
        data.level = mainCharacter.level;
        data.headHealth = mainCharacter.headHealth;
        data.torsoHealth = mainCharacter.torsoHealth;
        data.armsHealth = mainCharacter.armsHealth;
        data.legsHealth = mainCharacter.legsHealth;
        data.maxHeadHealth = mainCharacter.maxHeadHealth;
        data.maxTorsoHealth = mainCharacter.maxTorsoHealth;
        data.maxArmsHealth = mainCharacter.maxArmsHealth;
        data.maxLegsHealth = mainCharacter.maxLegsHealth;

        data.currentHunger = mainCharacter.currentHunger;
        data.currentThirst = mainCharacter.currentThirst;
        data.currentPoison = mainCharacter.currentPoison;
        data.maxHunger = mainCharacter.maxHunger;
        data.maxThirst = mainCharacter.maxThirst;
        data.maxPoison = mainCharacter.maxPoison;
        data.currentWeight = mainCharacter.currentWeight;
        data.maxWeight = mainCharacter.maxWeight;
        data.droidCharge = mainCharacter.droidCharge;
        data.maxDroidCharge = mainCharacter.maxDroidCharge;

        // Inventory - save both item IDs and quantities
        data.inventoryItemIDs = new List<int>();

        foreach (var item in mainCharacter.InventoryData.items)
        {
            data.inventoryItemIDs.Add(item.itemID);
            // If your items have quantities, save them too
            // data.inventoryItemQuantities.Add(item.quantity);
        }

        //Save spawn points
        data.SpawnPointX = spawnPoint.transform.position.x;
        data.SpawnPointY = spawnPoint.transform.position.y;
        data.SpawnPointZ = spawnPoint.transform.position.z;

        data.DroidSpawnPointX = droidSpawnPoint.transform.position.x;
        data.DroidSpawnPointY = droidSpawnPoint.transform.position.y;
        data.DroidSpawnPointZ = droidSpawnPoint.transform.position.z;


        // Day cycle
        data.timeOfDay = dayCycle.CurrentHour / 24f;

        // Write to JSON
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved: " + savePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Save file not found. Starting new game.");
            // Spawn player for new game
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // Ensure mainCharacter exists
            if (mainCharacter == null)
            {
                Debug.LogError("MainCharacter reference is null!");
                return;
            }

            // Load character data
            mainCharacter.level = data.level;
            mainCharacter.headHealth = data.headHealth;
            mainCharacter.torsoHealth = data.torsoHealth;
            mainCharacter.armsHealth = data.armsHealth;
            mainCharacter.legsHealth = data.legsHealth;
            mainCharacter.maxHeadHealth = data.maxHeadHealth;
            mainCharacter.maxTorsoHealth = data.maxTorsoHealth;
            mainCharacter.maxArmsHealth = data.maxArmsHealth;
            mainCharacter.maxLegsHealth = data.maxLegsHealth;

            mainCharacter.currentHunger = data.currentHunger;
            mainCharacter.currentThirst = data.currentThirst;
            mainCharacter.currentPoison = data.currentPoison;
            mainCharacter.maxHunger = data.maxHunger;
            mainCharacter.maxThirst = data.maxThirst;
            mainCharacter.maxPoison = data.maxPoison;
            mainCharacter.currentWeight = data.currentWeight;
            mainCharacter.maxWeight = data.maxWeight;
            mainCharacter.droidCharge = data.droidCharge;
            mainCharacter.maxDroidCharge = data.maxDroidCharge;

            // Load inventory
            if (mainCharacter.InventoryData != null)
            {
                mainCharacter.InventoryData.items.Clear();

                if (data.inventoryItemIDs != null)
                {
                    foreach (var id in data.inventoryItemIDs)
                    {
                        ItemData item = ItemDatabase.GetItemById(id);
                        if (item != null)
                        {
                            mainCharacter.InventoryData.items.Add(item);
                            Debug.Log($"Loaded item: {item.name} (ID: {id})");
                        }
                        else
                        {
                            Debug.LogWarning($"Item with ID {id} not found in database!");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("InventoryData is null!");
            }

            // Load day cycle
            if (dayCycle != null)
            {
                var timeField = typeof(DayCycle).GetField("timeOfDay",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (timeField != null)
                {
                    timeField.SetValue(dayCycle, data.timeOfDay);
                    dayCycle.setIsLoaded(true); // Set loaded state to true
                }
                else
                {
                    Debug.LogWarning("timeOfDay field not found in DayCycle!");
                }
            }

            Vector3 PlayerSpawnPoint = new Vector3(data.SpawnPointX, data.SpawnPointY, data.SpawnPointZ);
            Vector3 DroidSpawnPoint = new Vector3(data.DroidSpawnPointX, data.DroidSpawnPointY, data.DroidSpawnPointZ);
            // Spawn player
            if (playerPrefab != null && PlayerSpawnPoint != null && droidPrefab != null && DroidSpawnPoint != null)
            {
                playerPrefab.transform.position = PlayerSpawnPoint;
                //playerPrefab.transform.rotation = spawnPoint.transform.rotation;
                droidPrefab.transform.position = DroidSpawnPoint;
                //droidPrefab.transform.rotation = droidSpawnPoint.transform.rotation;
            }

            Debug.Log("Game loaded successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading game: {e.Message}");
        }
    }
}

public static class ItemDatabase
{
    private static Dictionary<int, ItemData> itemDictionary;

    public static void Initialize()
    {
        if (itemDictionary != null) return; // Prevent re-initialization

        itemDictionary = new Dictionary<int, ItemData>();

        // Method 1: Try Resources folder first
        ItemData[] items = Resources.LoadAll<ItemData>("Items");

        if (items.Length == 0)
        {
            Debug.LogWarning("No items found in Resources/GameData/Items/.");
        }

        if (items.Length == 0)
        {
            Debug.LogError("No ItemData assets found! Make sure your ItemData assets are either in:" +
                          "\n1. Assets/Resources/GameData/Items/ (for Resources.LoadAll)" +
                          "\n2. Any folder (will be found by AssetDatabase in Editor)");
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                Debug.LogWarning("Null item found in database!");
                continue;
            }

            if (item.itemID <= 0)
            {
                Debug.LogWarning($"Item {item.name} has invalid ID: {item.itemID}");
                continue;
            }

            if (!itemDictionary.ContainsKey(item.itemID))
            {
                itemDictionary.Add(item.itemID, item);
                Debug.Log($"Added item to database: {item.name} (ID: {item.itemID})");
            }
            else
            {
                Debug.LogWarning($"Duplicate item ID detected: {item.itemID} for item {item.name}");
            }
        }

        Debug.Log($"ItemDatabase initialized with {itemDictionary.Count} items.");
    }


    public static ItemData GetItemById(int id)
    {
        if (itemDictionary == null)
        {
            Debug.LogError("ItemDatabase is not initialized. Call ItemDatabase.Initialize() first.");
            return null;
        }

        if (itemDictionary.TryGetValue(id, out var item))
        {
            return item;
        }

        Debug.LogWarning($"Item with ID {id} not found in database. Available IDs: {string.Join(", ", itemDictionary.Keys)}");
        return null;
    }

    // Utility method to debug the database
    public static void DebugDatabase()
    {
        if (itemDictionary == null)
        {
            Debug.Log("ItemDatabase is not initialized.");
            return;
        }

        Debug.Log($"ItemDatabase contains {itemDictionary.Count} items:");
        foreach (var kvp in itemDictionary)
        {
            Debug.Log($"ID: {kvp.Key}, Name: {kvp.Value.name}");
        }
    }
}
