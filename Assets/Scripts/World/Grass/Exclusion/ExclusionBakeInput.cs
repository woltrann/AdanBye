using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Göl gibi mesh tabanlı bir exclusion kaynağı: mesh + dünya matrisi.</summary>
    public readonly struct MeshExclusionEntry
    {
        public Mesh Mesh { get; }
        public Matrix4x4 LocalToWorld { get; }

        public MeshExclusionEntry(Mesh mesh, Matrix4x4 localToWorld)
        {
            Mesh = mesh;
            LocalToWorld = localToWorld;
        }
    }

    /// <summary>Baker çekirdeğinin girdisi; Editor/Terrain'den bağımsız düz veri.</summary>
    public sealed class ExclusionBakeInput
    {
        public Vector2 OriginXZ;
        public Vector2 SizeXZ;
        public int Resolution = 2048;
        public ITreeInstanceProvider Trees;
        /// <summary>Prototip indeksi -> ölçek-1 yarıçap; olmayan prototip prefab bounds'a düşer.</summary>
        public IReadOnlyDictionary<int, float> TreeRadiusOverrides;
        public float TreeMargin = 1.5f;
        public IReadOnlyList<MeshExclusionEntry> Lakes;
        public float LakeMargin = 1.0f;
        /// <summary>True ve TerrainHeights doluysa göl yalnız terrain'in su altında kaldığı yerde maskelenir.</summary>
        public bool ClipLakeToTerrainHeight;
        public ITerrainHeightProvider TerrainHeights;
    }
}
