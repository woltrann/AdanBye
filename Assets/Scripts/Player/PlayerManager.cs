using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public InputActionAsset InputActions;

    public PlayerAudio PlayerAudio;

    public MainCharacter mainCharacter;
    
    private string currentScenarioName;


    private void OnEnable()
    {
        InputActions.FindActionMap("PlayerController").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("PlayerController").Disable();
    }

    private void PlayDialogue(Condition condition)
    {
        
        switch(condition) {
            case Condition.Random:
                PlayerAudio.PlayRandomDialogue();
                break;
            case Condition.LowHealth:
                PlayerAudio.PlayLowHealthDialogue();
                break;
            case Condition.LowStamina:
                PlayerAudio.PlayLowStaminaDialogue();
                break;
            case Condition.HighHunger:
                PlayerAudio.PlayHighHungerDialogue();
                break;
            case Condition.HighThristy:
                PlayerAudio.PlayHighThirstyDialogue();
                break;
            case Condition.HighToxicity:
                PlayerAudio.PlayHighToxicityDialogue();
                break;
            case Condition.Scenario:
                PlayerAudio.PlayScenarioDialogue(currentScenarioName);
                break;
        }
    }

    
}
