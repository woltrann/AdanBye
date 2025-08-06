using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public InputActionAsset InputActions;

    private void OnEnable()
    {
        InputActions.FindActionMap("PlayerController").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("PlayerController").Disable();
    }
}
