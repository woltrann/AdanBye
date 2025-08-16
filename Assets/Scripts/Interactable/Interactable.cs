using UnityEngine;

public class Interactable : MonoBehaviour
{
    public ItemData itemData; // ScriptableObject verisi

    public void Interact()
    {
        if (itemData != null && InteractionManager.Instance != null)
        {
            if (itemData.itemID == 50)
            {
                // Find the sibling SpawnPoint object
                Transform spawnPoint = transform.parent.Find("SpawnPoint");
                Transform droidSpawnPoint = transform.parent.Find("DroidSpawnPoint");
                if (spawnPoint != null && droidSpawnPoint != null)
                {
                    SaveManager.Instance.spawnPoint = spawnPoint;
                    SaveManager.Instance.droidSpawnPoint = droidSpawnPoint;
                }
                else
                {
                    Debug.LogWarning("SpawnPoint not found as a sibling of Campfire!");
                }
            }
            InteractionManager.Instance.Interact(itemData);
        }
        else
        {
            Debug.LogWarning("ItemData or InteractionManager is missing!");
        }
    }

    public AudioClip GetVoiceLine()
    {
        return itemData != null ? itemData.voiceLine : null;
    }
}
