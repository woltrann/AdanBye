using UnityEngine;

public class Interactable : MonoBehaviour
{
    public ItemData itemData; // ScriptableObject verisi

    public void Interact()
    {
        if (itemData != null && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Interact(itemData);
            Debug.Log(itemData);
        }
        else
        {
            Debug.LogWarning("ItemData veya InteractionManager eksik!");
        }
    }

    public AudioClip GetVoiceLine()
    {
        return itemData != null ? itemData.voiceLine : null;
    }
}
