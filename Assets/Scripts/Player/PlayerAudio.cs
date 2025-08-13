using UnityEditor.Rendering;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource audioSource;

    private MainCharacter mainCharacter;

    private void Start()
    {
        mainCharacter = GetComponent<PlayerManager>().mainCharacter;
    }

    public void PlayHighToxicityDialogue()
    {
        if (mainCharacter.HighToxicityDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.RandomDialogues.Count);
            DialogueData dialogue = mainCharacter.HighToxicityDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayHighThirstyDialogue()
    {
        if (mainCharacter.HighThirstyDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.HighThirstyDialogues.Count);
            DialogueData dialogue = mainCharacter.HighThirstyDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayHighHungerDialogue()
    {
        if (mainCharacter.HighHungerDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.HighHungerDialogues.Count);
            DialogueData dialogue = mainCharacter.HighHungerDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayLowStaminaDialogue()
    {
        if (mainCharacter.LowStaminaDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.LowStaminaDialogues.Count);
            DialogueData dialogue = mainCharacter.LowStaminaDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayLowHealthDialogue()
    {
        if (mainCharacter.LowHealthDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.LowHealthDialogues.Count);
            DialogueData dialogue = mainCharacter.LowHealthDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayRandomDialogue()
    {
        if (mainCharacter.RandomDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, mainCharacter.RandomDialogues.Count);
            DialogueData dialogue = mainCharacter.RandomDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayScenarioDialogue(string dialogueName)
    {
        if (mainCharacter.ScenerioDialogues.Count > 0)
        {
            DialogueData dialogue = mainCharacter.ScenerioDialogues.Find(d => d.dialogueName == dialogueName);
            PlayDialogueClip(dialogue);
        }
    }
    /*
    public void PlayExamineDialogue(int itemID)
    {
        if (mainCharacter.ScenerioDialogues.Count > 0)
        {
            DialogueData dialogue = mainCharacter.ScenerioDialogues.Find(d => d.dialogueName == dialogueName);
            PlayDialogueClip(dialogue);
        }
    }
    */
    private void PlayDialogueClip(DialogueData dialogue)
    {
        audioSource.clip = dialogue.voiceLine;
        audioSource.Play();
        Debug.Log($"Playing dialogue: {dialogue.dialogueName} - {dialogue.dialogueText}");
        if (dialogue.nextDialogue != null)
        {
            PlayDialogueClip(dialogue.nextDialogue);
        }
    }
}
