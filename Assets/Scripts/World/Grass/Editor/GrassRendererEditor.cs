using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>
    /// GrassRenderer Inspector'ı: eksik referans açıklamaları, durum (HelpBox), aktif kamera sayısı, kamera başına bellek tahmini
    /// ve "Yeniden Kur". Neden mantık taşımaz: durum/tahmin GrassRenderer/GrassDrawSystem'den okunur; Core'a Editor bağımlılığı sızmaz.
    /// </summary>
    [CustomEditor(typeof(GrassRenderer))]
    public sealed class GrassRendererEditor : UnityEditor.Editor // 'Editor' adı bu namespace ile çakıştığı için tam ad.
    {
        SerializedProperty _settings;
        SerializedProperty _material;
        SerializedProperty _terrain;
        SerializedProperty _exclusionMask;

        void OnEnable()
        {
            _settings = serializedObject.FindProperty("settings");
            _material = serializedObject.FindProperty("material");
            _terrain = serializedObject.FindProperty("terrain");
            _exclusionMask = serializedObject.FindProperty("exclusionMask");
        }

        public override void OnInspectorGUI()
        {
            var renderer = (GrassRenderer)target;

            DrawDefaultInspector();
            EditorGUILayout.Space();

            DrawMissingReferenceHints();
            DrawExclusionStatus();
            DrawStatus(renderer);

            if (GUILayout.Button("Yeniden Kur")) renderer.Rebuild();
        }

        void DrawMissingReferenceHints()
        {
            if (_settings.objectReferenceValue == null)
                EditorGUILayout.HelpBox("GrassSettings atanmamış: 'Adan Bye/Grass/Grass Settings' ile bir asset oluşturup atayın.", MessageType.Warning);
            if (_material.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Çim materyali atanmamış: GrassBladeLit Shader Graph materyalini atayın.", MessageType.Warning);
            if (_terrain.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Terrain boş: Terrain.activeTerrain kullanılır (sahnede aktif Terrain olmalı). Birden fazla Terrain varsa elle atayın.", MessageType.Info);
        }

        // Stale sonucu Inspector ömrü boyunca (domain reload'a kadar) tutulur; hash pahalı olduğu için yalnız butonla hesaplanır.
        string _staleResult;

        void DrawExclusionStatus()
        {
            var mask = _exclusionMask.objectReferenceValue as GrassExclusionMask;
            var terrain = _terrain.objectReferenceValue as Terrain;
            if (terrain == null) terrain = Terrain.activeTerrain;

            Vector3 tPos = terrain != null ? terrain.transform.position : default;
            Vector3 tSize = terrain != null && terrain.terrainData != null ? terrain.terrainData.size : default;

            switch (GrassExclusionStatus.Classify(mask, tPos, tSize))
            {
                case ExclusionMaskState.None:
                    EditorGUILayout.HelpBox("Exclusion mask atanmadı (ağaç/göl altında çim engellenmez).", MessageType.Info);
                    return;
                case ExclusionMaskState.NotBaked:
                    EditorGUILayout.HelpBox("Exclusion mask bake edilmemiş (texture yok); exclusion uygulanmıyor.", MessageType.Warning);
                    break;
                case ExclusionMaskState.GeometryMismatch:
                    EditorGUILayout.HelpBox(terrain == null
                        ? "Terrain bulunamadı; mask geometrisi doğrulanamadı."
                        : "Exclusion mask terrain origin/size ile uyuşmuyor; yeniden bake edin.", MessageType.Warning);
                    break;
                case ExclusionMaskState.Ok:
                    EditorGUILayout.HelpBox("Exclusion mask terrain ile uyumlu.", MessageType.Info);
                    break;
            }

            // Settings: önce hedef maskesi bu mask olan asset, yoksa projedeki ilk GrassExclusionBakeSettings.
            var settings = FindBakeSettings(mask, out bool exact);
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Projede GrassExclusionBakeSettings asset'i yok; kontrol/bake için oluşturun (Adan Bye/Grass/Exclusion Bake Settings).", MessageType.None);
                return;
            }
            if (!exact)
                EditorGUILayout.HelpBox($"Bu mask'ı hedefleyen ayar bulunamadı; ilk bulunan kullanılıyor: '{settings.name}'.", MessageType.None);

            if (!string.IsNullOrEmpty(_staleResult)) EditorGUILayout.LabelField("Bake durumu", _staleResult);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bake güncel mi? (kontrol)"))
                {
                    string hash = GrassExclusionBaker.ComputeCurrentHash(settings);
                    string when = System.DateTime.Now.ToString("HH:mm:ss");
                    _staleResult = hash == null
                        ? $"Hesaplanamadı (aktif Terrain yok) - {when}"
                        : (mask.IsStale(hash) ? "ESKİ (kaynaklar değişti, yeniden bake edin)" : "Güncel") + $" - {when}";
                }
                if (GUILayout.Button("Bake"))
                {
                    _staleResult = GrassExclusionBaker.Bake(settings)
                        ? $"Bake edildi - {System.DateTime.Now:HH:mm:ss}"
                        : "Bake başarısız (Console'a bakın)";
                }
            }
        }

        static GrassExclusionBakeSettings FindBakeSettings(GrassExclusionMask mask, out bool exact)
        {
            exact = false;
            GrassExclusionBakeSettings first = null;
            foreach (string guid in AssetDatabase.FindAssets("t:GrassExclusionBakeSettings"))
            {
                var s = AssetDatabase.LoadAssetAtPath<GrassExclusionBakeSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (s == null) continue;
                if (s.targetMask == mask) { exact = true; return s; }
                if (first == null) first = s;
            }
            return first;
        }

        static void DrawStatus(GrassRenderer renderer)
        {
            // Durum metni çalışmıyorsa hata, çalışıyorsa (uyarı satırları içerse de) bilgi olarak gösterilir.
            EditorGUILayout.HelpBox(renderer.Status, renderer.IsRunning ? MessageType.Info : MessageType.Error);

            if (!renderer.IsRunning) return;

            int cameras = renderer.ActiveCameraCount;
            long perCamera = renderer.EstimatedBytesPerCamera;
            EditorGUILayout.LabelField("Aktif kamera", cameras.ToString());
            // Sayaç yalnızca Play'de canlıdır; Edit modunda Repaint tetikleyicisi yok, değer bayat kalırdı.
            if (Application.isPlaying)
                EditorGUILayout.LabelField("Chunk", $"Seçilen chunk: {renderer.LastSelectedChunks} / maks {renderer.MaxChunks}");
            EditorGUILayout.LabelField("Bellek (tahmin)",
                $"~{perCamera / (1024f * 1024f):0} MB / kamera, toplam ~{perCamera * cameras / (1024f * 1024f):0} MB");
            EditorGUILayout.HelpBox("Bellek değeri tahmindir (instance + chunk buffer'ları; küçük buffer'lar ve sürücü payı hariç). " +
                                    "Game + Scene view birlikte açıksa iki kamera sayılır.", MessageType.None);
        }
    }
}
