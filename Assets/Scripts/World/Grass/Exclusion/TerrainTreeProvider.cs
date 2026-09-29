using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Terrain.terrainData.treeInstances'ı okur. TreeInstance.position normalize (0..1) olduğu için
    /// dünya XZ = normalize * size + terrain.position. Terrain rotasyonsuz/birim ölçekli varsayılır
    /// (GrassTerrainSamplingInfo ile aynı sözleşme).
    /// </summary>
    public sealed class TerrainTreeProvider : ITreeInstanceProvider
    {
        readonly Terrain _terrain;

        public TerrainTreeProvider(Terrain terrain)
        {
            _terrain = terrain != null ? terrain : throw new ArgumentNullException(nameof(terrain));
        }

        public IReadOnlyList<TreeInstanceInfo> GetInstances()
        {
            TerrainData data = _terrain.terrainData;
            if (data == null) return Array.Empty<TreeInstanceInfo>();

            TreeInstance[] trees = data.treeInstances; // Kopya döner; baker tek seferlik çağırır.
            var result = new TreeInstanceInfo[trees.Length];
            Vector3 size = data.size;
            Vector3 origin = _terrain.transform.position;
            for (int i = 0; i < trees.Length; i++)
            {
                TreeInstance t = trees[i];
                result[i] = new TreeInstanceInfo(
                    new Vector2(t.position.x * size.x + origin.x, t.position.z * size.z + origin.z),
                    t.prototypeIndex,
                    t.widthScale);
            }
            return result;
        }

        public float GetPrefabRadius(int prototypeIndex)
        {
            TerrainData data = _terrain.terrainData;
            if (data == null) return 0f;
            TreePrototype[] prototypes = data.treePrototypes;
            if (prototypeIndex < 0 || prototypeIndex >= prototypes.Length) return 0f;

            GameObject prefab = prototypes[prototypeIndex].prefab;
            if (prefab == null) return 0f;

            // Tüm renderer'ları kapsayan yatay yarı-genişlik. Not: taç geniş ağaçlarda gövdeden büyük çıkar;
            // gövde-only istenirse TreeExclusionSource'a prototip başına override verilir.
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return Mathf.Max(b.extents.x, b.extents.z);
        }
    }
}
