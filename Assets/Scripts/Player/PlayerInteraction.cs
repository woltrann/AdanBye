using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private InputAction interactAction;
    private Interactable currentInteractable;

    [Header("Settings")]
    public float interactionRange = 3f;
    public LayerMask interactionLayer;

    private Camera mainCam;

    private void Awake()
    {
        var input = GetComponent<PlayerManager>().InputActions;
        interactAction = input.FindAction("PlayerController/Interact");

        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera bulunamadý. Cinemachine Main Camera etiketli mi?");
        }
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

    void CheckForInteractable()
    {
        if (mainCam == null) return;

        // 1- Önce tüm CanvasLookAtCamera objelerini "pointImage açýk / pressImage kapalý" yap
        foreach (CanvasLookAtCamera canvas in FindObjectsOfType<CanvasLookAtCamera>())
        {
            canvas.SetPressMode(false);
        }

        // 2- Ray at
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentInteractable = interactable;

                // Çocuklarda CanvasLookAtCamera ara
                CanvasLookAtCamera canvasScript = hit.collider.GetComponentInChildren<CanvasLookAtCamera>();
                if (canvasScript != null)
                {
                    canvasScript.SetPressMode(true); // Sadece baktýðýn objeyi press moduna al
                }
                return;
            }
        }

        currentInteractable = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (mainCam == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(mainCam.transform.position, mainCam.transform.position + mainCam.transform.forward * interactionRange);
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (currentInteractable == null) return;

        // Artýk Interactable içindeki Interact fonksiyonunu çaðýrýyoruz
        currentInteractable.Interact();

        // Ses varsa çal
        var voice = currentInteractable.GetVoiceLine();
        if (voice != null)
        {
            AudioSource.PlayClipAtPoint(voice, transform.position);
        }
    }
}
