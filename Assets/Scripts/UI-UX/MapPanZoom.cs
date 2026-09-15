using UnityEngine;
using UnityEngine.EventSystems;

public class MapPanZoom : MonoBehaviour, IDragHandler
{
    public RectTransform content;
    public RectTransform viewport;
    public float minScale = 1f;
    public float maxScale = 3f;
    public float scrollZoomSpeed = 0.1f;

    public void OnDrag(PointerEventData eventData)
    {
        content.anchoredPosition += eventData.delta / content.localScale.x;
        ClampPosition();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newScale = Mathf.Clamp(content.localScale.x + scroll * scrollZoomSpeed, minScale, maxScale);
            content.localScale = Vector3.one * newScale;
            ClampPosition();
        }
    }

    void ClampPosition()
    {
        Vector2 maxOffset = (content.rect.size * content.localScale.x - viewport.rect.size) * 0.5f;
        maxOffset.x = Mathf.Max(maxOffset.x, 0);
        maxOffset.y = Mathf.Max(maxOffset.y, 0);

        Vector2 pos = content.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxOffset.x, maxOffset.x);
        pos.y = Mathf.Clamp(pos.y, -maxOffset.y, maxOffset.y);
        content.anchoredPosition = pos;
    }
}