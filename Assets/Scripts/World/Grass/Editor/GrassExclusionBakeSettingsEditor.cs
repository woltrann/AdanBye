using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>Bake ayarı Inspector'ı: varsayılan alanlar + stale kontrolü + "Bake" butonu. Mantık baker'da.</summary>
    [CustomEditor(typeof(GrassExclusionBakeSettings))]
    public sealed class GrassExclusionBakeSettingsEditor : UnityEditor.Editor // 'Editor' adı namespace ile çakıştığı için tam ad.
    {
        public override void OnInspectorGUI()
        {
            var settings = (GrassExclusionBakeSettings)target;
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (settings.targetMask != null)
            {
                // Hash yalnızca butonla hesaplanır: her repaint'te tüm ağaç/mesh okumak pahalı.
                if (GUILayout.Button("Güncel mi? (hash kontrol)"))
                {
                    string h = GrassExclusionBaker.ComputeCurrentHash(settings);
                    if (h == null) Debug.LogWarning("Aktif terrain yok; kontrol edilemedi.");
                    else Debug.Log(settings.targetMask.IsStale(h) ? "Mask ESKİ: yeniden bake edin." : "Mask güncel.");
                }
            }

            if (GUILayout.Button("Bake")) GrassExclusionBaker.Bake(settings);
        }
    }
}
