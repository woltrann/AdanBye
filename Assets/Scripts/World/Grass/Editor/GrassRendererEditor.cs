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

        void OnEnable()
        {
            _settings = serializedObject.FindProperty("settings");
            _material = serializedObject.FindProperty("material");
            _terrain = serializedObject.FindProperty("terrain");
        }

        public override void OnInspectorGUI()
        {
            var renderer = (GrassRenderer)target;

            DrawDefaultInspector();
            EditorGUILayout.Space();

            DrawMissingReferenceHints();
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
