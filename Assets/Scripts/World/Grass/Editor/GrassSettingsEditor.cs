using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>
    /// GrassSettings Inspector'ı: varsayılan alan çizimi + doğrulama raporu (HelpBox) + varsayılana sıfırlama.
    /// Neden mantık taşımaz: doğrulama GrassSettings.TryBuildRuntime'da; burası yalnızca raporu gösterir.
    /// </summary>
    [CustomEditor(typeof(GrassSettings))]
    public sealed class GrassSettingsEditor : UnityEditor.Editor // 'Editor' adı bu namespace ile çakıştığı için tam ad.
    {
        public override void OnInspectorGUI()
        {
            var settings = (GrassSettings)target;

            DrawDefaultInspector();
            EditorGUILayout.Space();

            // Her çizimde yeniden hesaplanır: ayar küçük, Editor'da maliyet önemsiz; önbellek bayatlama riski getirirdi.
            settings.TryBuildRuntime(out _, out ValidationReport report);

            foreach (string error in report.Errors)
                EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (string warning in report.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            if (report.IsValid && report.Warnings.Count == 0)
                EditorGUILayout.HelpBox("Ayarlar geçerli.", MessageType.Info);

            if (GUILayout.Button("Varsayılanlara sıfırla"))
            {
                // Undo: yanlışlıkla basılırsa kullanıcının özel LOD/layer ayarları geri alınabilsin.
                Undo.RecordObject(settings, "Grass Ayarlarını Sıfırla");
                settings.ResetToDefaults();
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
