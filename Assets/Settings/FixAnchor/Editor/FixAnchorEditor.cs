using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class FixAnchorEditor
{
    static FixAnchorEditor()
    {
        SceneView.duringSceneGui += ListenForShortcut;
    }

    static void ListenForShortcut(SceneView view)
    {
        if (Event.current == null) return;
        if (Event.current.type != EventType.KeyDown) return;

        if (Event.current.control && Event.current.keyCode == KeyCode.Q)
        {
            ApplyAnchors();
            Event.current.Use();
        }
    }

    [MenuItem("Tools/FixAnchor/Set Anchors To Corners %q")]
    public static void ApplyAnchors()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            RectTransform t = obj.GetComponent<RectTransform>();
            if (t == null) continue;

            RectTransform parent = t.parent as RectTransform;
            if (parent == null) continue;

            Vector2 newAnchorMin = new Vector2(
                t.anchorMin.x + (t.offsetMin.x / parent.rect.width),
                t.anchorMin.y + (t.offsetMin.y / parent.rect.height));

            Vector2 newAnchorMax = new Vector2(
                t.anchorMax.x + (t.offsetMax.x / parent.rect.width),
                t.anchorMax.y + (t.offsetMax.y / parent.rect.height));

            t.anchorMin = newAnchorMin;
            t.anchorMax = newAnchorMax;
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
        }

        Debug.Log("FixAnchor: Anchors updated.");
    }
}
