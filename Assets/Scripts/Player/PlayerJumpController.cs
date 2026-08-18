using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Tek iş: zıplama input'unu dinlemek ve zıplatmak.
// Animator'a doğrudan bağımlı DEĞİL - OnJumped event'i yayınlar,
// isteyen dinler (OCP: yeni bir "zıplama tepkisi" eklemek için bu sınıfı değiştirmen gerekmez).
[DefaultExecutionOrder(0)]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerJumpController : MonoBehaviour
{
    [SerializeField] private float jumpForce = 5f;

    private PlayerMotor motor;
    private IGroundedProvider groundedProvider;
    private InputAction jumpAction;

    public event Action OnJumped;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        groundedProvider = GetComponent<IGroundedProvider>();

        if (groundedProvider == null)
            Debug.LogWarning($"{nameof(PlayerJumpController)}: IGroundedProvider bulunamadı (PlayerGroundWaterSensor eksik mi?).");

        var input = GetComponent<PlayerManager>().InputActions;
        jumpAction = input.FindAction("PlayerController/Jump");
    }

    private void OnEnable()
    {
        jumpAction.Enable();
        jumpAction.performed += HandleJumpInput;
    }

    private void OnDisable()
    {
        jumpAction.Disable();
        jumpAction.performed -= HandleJumpInput;
    }

    private void HandleJumpInput(InputAction.CallbackContext ctx)
    {
        if (groundedProvider == null || !groundedProvider.IsGrounded) return;

        motor.ApplyImpulse(Vector3.up * jumpForce);
        OnJumped?.Invoke();
    }
}
