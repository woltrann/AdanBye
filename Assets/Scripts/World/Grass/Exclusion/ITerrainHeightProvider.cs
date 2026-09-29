using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Dünya XZ'sinde terrain yüksekliği (world Y); kaynaklar Terrain'e doğrudan bağımlı olmasın diye.</summary>
    public interface ITerrainHeightProvider
    {
        float SampleHeightWorld(float x, float z);
    }

    /// <summary>Terrain.SampleHeight tabanlı sağlayıcı; SampleHeight terrain origin'ine göre döndüğü için Y offset eklenir.</summary>
    public sealed class TerrainHeightProvider : ITerrainHeightProvider
    {
        readonly Terrain _terrain;

        public TerrainHeightProvider(Terrain terrain)
        {
            _terrain = terrain != null ? terrain : throw new System.ArgumentNullException(nameof(terrain));
        }

        public float SampleHeightWorld(float x, float z) =>
            _terrain.SampleHeight(new Vector3(x, 0f, z)) + _terrain.transform.position.y;
    }
}
