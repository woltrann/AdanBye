using UnityEngine;

public class AimTargetFollower : MonoBehaviour
{
    [Header("References")]
    public Transform aimTarget; // Sphere objesi
    private Camera mainCam;

    [Header("Settings")]
    public float distance = 10f;
    public LayerMask aimLayerMask = ~0;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(distance);
        }

        aimTarget.position = targetPoint;
    }
}