using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public string dialogueName;
    [TextArea(3, 10)]
    public string dialogueText;

    public AudioClip voiceLine;
    public DialogueData nextDialogue;
    public Condition condition;

}

public enum Condition
{
    Random,
    LowHealth,
    LowStamina,
    HighHunger,
    HighThristy,
    HighToxicity,
}
