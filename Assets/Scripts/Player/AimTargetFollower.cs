using UnityEngine;

// Tek iş: kameranın baktığı noktayı hesaplayıp hedef objeyi (sphere) oraya taşımak.
// Artık IAimPointProvider implement ediyor - başka sistemler (örn. PlayerInteraction)
// kendi raycast'ini tekrar yazmak yerine bu bileşenden aim noktasını okuyabilir.
public class AimTargetFollower : MonoBehaviour, IAimPointProvider
{
    [Header("References")]
    public Transform aimTarget; // Sphere objesi

    [Header("Settings")]
    public float distance = 10f;
    public LayerMask aimLayerMask = ~0;

    private Camera mainCam;

    public Vector3 AimPoint { get; private set; }
    public bool HasHit { get; private set; }
    public RaycastHit LastHit { get; private set; }

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        // Camera.main bazen sahne yüklenirken henüz hazır olmayabilir (örn. Cinemachine
        // brain bir frame geç kurulursa) - Awake'te null kalırsa burada tekrar dene.
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, aimLayerMask))
        {
            HasHit = true;
            LastHit = hit;
            AimPoint = hit.point;
        }
        else
        {
            HasHit = false;
            AimPoint = ray.GetPoint(distance);
        }

        if (aimTarget != null)
        {
            aimTarget.position = AimPoint;
        }
    }
}
