using UnityEditor;

namespace AdanBye.Grass.Editor
{
    /// <summary>GrassInteractionFeed Inspector'ı: yalnızca durum HelpBox'ı; mantık Feed'den okunur.</summary>
    [CustomEditor(typeof(GrassInteractionFeed))]
    public sealed class GrassInteractionFeedEditor : UnityEditor.Editor // 'Editor' adı namespace ile çakıştığı için tam ad.
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(((GrassInteractionFeed)target).StatusText, MessageType.Info);
        }

        // Durum her karede değişir; Inspector canlı görünsün.
        public override bool RequiresConstantRepaint() => true;
    }
}
