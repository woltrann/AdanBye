using UnityEngine;

[CreateAssetMenu(fileName = "NewInteraction", menuName = "Interaction/InteractionData")]

public class InteractableData : ScriptableObject
{
    public string promptText;
    public InteractionType interactionType;

    [TextArea(2, 5)]
    public string interactionDescription;

    public AudioClip voiceLine;
}

public enum InteractionType
{
    Look,
    Use,
    Collect
}

