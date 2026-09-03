using UnityEngine;
using UnityEngine.InputSystem;

// Tek iş: oyuncunun baktığı Interactable'ı bulmak ve Interact input'unda tetiklemek.
// Kendi raycast'ini yazmaz - nereye bakıldığını IAimPointProvider'dan (AimTargetFollower)
// okur, böylece aynı raycast iki yerde tekrar edilmez.
public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("IAimPointProvider implement eden bileşen (örn. AimTargetFollower).")]
    [SerializeField] private MonoBehaviour aimPointProviderSource;
    private IAimPointProvider aimPointProvider;

    [Header("Settings")]
    public float interactionRange = 2.5f;

    private InputAction interactAction;
    private Interactable currentInteractable;
    private CanvasLookAtCamera currentCanvas;

    private void Awake()
    {
        aimPointProvider = aimPointProviderSource as IAimPointProvider;
        if (aimPointProvider == null)
        {
            Debug.LogError($"{nameof(PlayerInteraction)}: aimPointProviderSource, IAimPointProvider implement etmiyor.");
        }

        var input = GetComponent<PlayerManager>().InputActions;
        interactAction = input.FindAction("PlayerController/Interact");
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (aimPointProvider == null) return;

        if (currentCanvas != null)
        {
            currentCanvas.SetPressMode(false);
            currentCanvas = null;
        }
        currentInteractable = null;

        if (!aimPointProvider.HasHit) return;
        if (aimPointProvider.LastHit.distance > interactionRange) return;

        Interactable interactable = aimPointProvider.LastHit.collider.GetComponent<Interactable>();
        if (interactable == null) return;

        currentInteractable = interactable;

        CanvasLookAtCamera canvas = aimPointProvider.LastHit.collider.GetComponentInChildren<CanvasLookAtCamera>();
        if (canvas != null)
        {
            canvas.SetPressMode(true);
            currentCanvas = canvas;
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (currentInteractable == null) return;

        currentInteractable.Interact();

        AudioClip voice = currentInteractable.GetVoiceLine();
        if (voice != null)
        {
            AudioSource.PlayClipAtPoint(voice, transform.position);
        }
    }
}
