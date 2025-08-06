using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public InteractableData interactionData;


    public string GetPromptText()
    {
        return interactionData != null ? interactionData.promptText : "Etki Yok";
    }

    public InteractionType GetInteractionType()
    {
        return interactionData != null ? interactionData.interactionType : InteractionType.Look;
    }

    public string GetDescription()
    {
        return interactionData != null ? interactionData.interactionDescription : "";
    }

    public AudioClip GetVoiceLine()
    {
        return interactionData != null ? interactionData.voiceLine : null;
    }
}
