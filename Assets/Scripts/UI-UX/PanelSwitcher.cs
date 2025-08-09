using UnityEngine;
using UnityEngine.UI;

public class PanelSlider : MonoBehaviour
{
    public GameObject PhonePanel;
    public RectTransform panelParent;  // Panellerin parent objesi
    public float slideSpeed = 5f;      // Kayma h�z�
    public Image image;
    public float width;
    public int panelCount = 4;         // Ka� panel var
    private int currentIndex = 0;
    private Vector2 targetPosition;

    void Start()
    {
        targetPosition = panelParent.anchoredPosition;
        width = image.rectTransform.rect.width; // width de�erini kullanabilirsiniz
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            PhonePanel.SetActive(!PhonePanel.activeSelf);
        }
        // Sa�
        if (Input.GetKeyDown(KeyCode.X) && currentIndex < panelCount - 1)
        {
            currentIndex++;
            targetPosition = new Vector2(-currentIndex * width, 0);
        }

        // Sol
        if (Input.GetKeyDown(KeyCode.Z) && currentIndex > 0)
        {
            currentIndex--;
            targetPosition = new Vector2(-currentIndex * width, 0);
        }

        // Yumu�ak ge�i�
        panelParent.anchoredPosition = Vector2.Lerp(panelParent.anchoredPosition, targetPosition, Time.deltaTime * slideSpeed);
    }
}
