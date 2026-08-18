using System;
using System.Collections.Generic;
using UnityEngine;

// Not: orijinal dosyada "using UnityEditor.Rendering;" vardı - bu bir editor-only
// namespace, runtime kodda kullanılmıyordu bile ve build'i kırma riski taşıyordu. Kaldırıldı.
public class PlayerAudio : MonoBehaviour
{
    public AudioSource audioSource;

    private MainCharacter mainCharacter;

    // Condition -> hangi metodun çağrılacağı eşlemesi (PlayerManager'daki switch'ten taşındı).
    // Yeni bir Condition eklemek artık burada tek satır eklemek demek; başka bir yeri
    // değiştirmene gerek yok (OCP: switch'e göre genişlemeye daha açık).
    private Dictionary<Condition, Action> dialogueByCondition;

    private void Start()
    {
        mainCharacter = GetComponent<PlayerManager>().mainCharacter;

        dialogueByCondition = new Dictionary<Condition, Action>
        {
            { Condition.Random, PlayRandomDialogue },
            { Condition.LowHealth, PlayLowHealthDialogue },
            { Condition.LowStamina, PlayLowStaminaDialogue },
            { Condition.HighHunger, PlayHighHungerDialogue },
            { Condition.HighThristy, PlayHighThirstyDialogue },
            { Condition.HighToxicity, PlayHighToxicityDialogue },
        };
    }

    // PlayerManager.PlayDialogue'nin yerini alıyor. Scenario ayrı ele alınıyor çünkü
    // o tek başına bir isim (scenarioName) parametresine ihtiyaç duyuyor.
    public void PlayForCondition(Condition condition, string scenarioName = null)
    {
        if (condition == Condition.Scenario)
        {
            PlayScenarioDialogue(scenarioName);
            return;
        }

        if (dialogueByCondition != null && dialogueByCondition.TryGetValue(condition, out var play))
        {
            play.Invoke();
        }
    }

    public void PlayHighToxicityDialogue()
    {
        if (mainCharacter.HighToxicityDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.HighToxicityDialogues.Count);
            DialogueData dialogue = mainCharacter.HighToxicityDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayHighThirstyDialogue()
    {
        if (mainCharacter.HighThirstyDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.HighThirstyDialogues.Count);
            DialogueData dialogue = mainCharacter.HighThirstyDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayHighHungerDialogue()
    {
        if (mainCharacter.HighHungerDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.HighHungerDialogues.Count);
            DialogueData dialogue = mainCharacter.HighHungerDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayLowStaminaDialogue()
    {
        if (mainCharacter.LowStaminaDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.LowStaminaDialogues.Count);
            DialogueData dialogue = mainCharacter.LowStaminaDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayLowHealthDialogue()
    {
        if (mainCharacter.LowHealthDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.LowHealthDialogues.Count);
            DialogueData dialogue = mainCharacter.LowHealthDialogues[randomIndex];
            PlayDialogueClip(dialogue);
        }
    }

    public void PlayRandomDialogue()
    {
        if (mainCharacter.RandomDialogues.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, mainCharacter.RandomDialogues.Count);
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

    private void PlayDialogueClip(DialogueData dialogue)
    {
        if (dialogue == null) return;

        audioSource.clip = dialogue.voiceLine;
        audioSource.Play();
        Debug.Log($"Playing dialogue: {dialogue.dialogueName} - {dialogue.dialogueText}");
        if (dialogue.nextDialogue != null)
        {
            PlayDialogueClip(dialogue.nextDialogue);
        }
    }
}
