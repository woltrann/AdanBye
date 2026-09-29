using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdanBye.Grass.Editor
{
    /// <summary>
    /// Editor katmanı: Terrain/sahneden girdiyi toplar, çekirdeği (<see cref="ExclusionBakeCore"/>) çalıştırır,
    /// texture asset'ini ve mask SO'sunu yazar. Mantık çekirdekte; burada yalnızca I/O.
    /// </summary>
    public static class GrassExclusionBaker
    {
        const string MenuPath = "Tools/AdanBye/Grass/Bake Exclusion Mask";

        [MenuItem(MenuPath)]
        static void BakeSelected()
        {
            var settings = Selection.activeObject as GrassExclusionBakeSettings;
            if (settings == null)
            {
                Debug.LogWarning("Önce Project'te bir GrassExclusionBakeSettings asset'i seçin.");
                return;
            }
            Bake(settings);
        }

        [MenuItem(MenuPath, true)]
        static bool BakeSelectedValidate() => Selection.activeObject is GrassExclusionBakeSettings;

        /// <summary>Bake eder ve mask asset'ini günceller; başarıda true.</summary>
        public static bool Bake(GrassExclusionBakeSettings settings)
        {
            if (settings == null || settings.targetMask == null)
            {
                Debug.LogError("Bake ayarında hedef GrassExclusionMask atanmamış.");
                return false;
            }
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("Aktif Terrain yok; terrain içeren sahneyi açın.");
                return false;
            }

            ExclusionBakeInput input = BuildInput(settings, terrain);
            ExclusionBakeResult result;
            try
            {
                result = ExclusionBakeCore.Bake(input);
            }
            catch (Exception e)
            {
                // Örn. göl mesh'i Read/Write kapalı: asset'e dokunmadan nedenini göster.
                Debug.LogError($"Exclusion bake başarısız: {e.Message}");
                return false;
            }

            Texture2D tex = WriteTexture(settings.targetMask, result.Canvas);
            settings.targetMask.Apply(tex, input.OriginXZ, input.SizeXZ, input.Resolution, result.SourceHash);
            EditorUtility.SetDirty(settings.targetMask);
            AssetDatabase.SaveAssets();
            Debug.Log($"Exclusion mask bake edildi ({input.Resolution}x{input.Resolution}, hash {result.SourceHash}).");
            return true;
        }

        /// <summary>Şu anki kaynaklara göre hash (stale kontrolü); girdi kurulamazsa null.</summary>
        public static string ComputeCurrentHash(GrassExclusionBakeSettings settings)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (settings == null || terrain == null || terrain.terrainData == null) return null;
            return ExclusionBakeCore.ComputeHash(BuildInput(settings, terrain));
        }

        static ExclusionBakeInput BuildInput(GrassExclusionBakeSettings s, Terrain terrain)
        {
            Vector3 pos = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            var overrides = new Dictionary<int, float>();
            if (s.treeRadiusPerPrototype != null)
                for (int i = 0; i < s.treeRadiusPerPrototype.Length; i++)
                    if (s.treeRadiusPerPrototype[i] > 0f) overrides[i] = s.treeRadiusPerPrototype[i];

            return new ExclusionBakeInput
            {
                OriginXZ = new Vector2(pos.x, pos.z),
                SizeXZ = new Vector2(size.x, size.z),
                Resolution = s.resolution,
                Trees = new TerrainTreeProvider(terrain),
                TreeRadiusOverrides = overrides,
                TreeMargin = s.treeMargin,
                Lakes = FindLakes(s.lakeObjectNames),
                LakeMargin = s.lakeMargin,
                ClipLakeToTerrainHeight = s.clipLakeToTerrainHeight,
                TerrainHeights = new TerrainHeightProvider(terrain),
            };
        }

        static List<MeshExclusionEntry> FindLakes(string[] names)
        {
            var list = new List<MeshExclusionEntry>();
            if (names == null || names.Length == 0) return list;

            var seen = new HashSet<MeshFilter>();
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // includeInactive: kapalı göl objesi de bake'e girsin.
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!MatchesAny(t.name, names)) continue;
                    foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
                        if (mf.sharedMesh != null && seen.Add(mf))
                            list.Add(new MeshExclusionEntry(mf.sharedMesh, mf.transform.localToWorldMatrix));
                }
            }
            return list;
        }

        static bool MatchesAny(string name, string[] names)
        {
            foreach (string n in names)
                if (!string.IsNullOrEmpty(n) && string.Equals(name, n, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Texture, mask asset'inin yanına "<ad>_Texture.asset" olarak yazılır; uygunsa yerinde güncellenir (GUID korunur).
        static Texture2D WriteTexture(GrassExclusionMask mask, ExclusionMaskCanvas canvas)
        {
            string maskPath = AssetDatabase.GetAssetPath(mask);
            string texPath = Path.Combine(Path.GetDirectoryName(maskPath) ?? "Assets",
                Path.GetFileNameWithoutExtension(maskPath) + "_Texture.asset").Replace('\\', '/');

            int res = canvas.Resolution;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null || tex.width != res || tex.height != res || tex.format != TextureFormat.R8)
            {
                // Boyut/format değişti: yeniden oluştur (CreateAsset mevcut dosyanın .meta GUID'ini korur).
                var fresh = new Texture2D(res, res, TextureFormat.R8, false, true)
                {
                    name = Path.GetFileNameWithoutExtension(texPath)
                };
                AssetDatabase.CreateAsset(fresh, texPath);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixelData(canvas.Data, 0);
            tex.Apply(false, false); // Okunabilir kalsın (Editor doğrulaması); mip yok.
            EditorUtility.SetDirty(tex);
            return tex;
        }
    }
}
