using UnityEngine;

public class CanvasLookAtCamera : MonoBehaviour
{
    public GameObject pointImage;
    public GameObject pressImage;
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Sadece Y ekseninde dönmesini istiyorsan þu satýrý kullan:
            Vector3 lookDirection = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
            transform.LookAt(lookDirection);
            transform.Rotate(0, 180, 0);

            // Tamamen kameraya bakmasýný istersen bunu kullan:
            // transform.LookAt(Camera.main.transform);
        }
    }
    public void SetPressMode(bool isPressMode)
    {
        if (pointImage != null) pointImage.SetActive(!isPressMode);
        if (pressImage != null) pressImage.SetActive(isPressMode);
    }
}
