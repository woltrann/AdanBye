using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Kodla üretilen, uzak LOD için en ucuz blade mesh'i. LOD0/1 mesh'i kullanıcıdan (customMesh) ya da varsayılan
    /// blade'den gelir; uzakta ise siluet önemsizdir ve köşe sayısı maliyetin baskın kısmıdır, o yüzden tek üçgen.
    /// Sözleşme diğer blade mesh'leriyle aynı: pivot kökte, +Y yukarı, 1 birim yükseklik, 1 birim taban genişliği
    /// (instance yüksekliği/genişliği compute'tan gelen boyla ölçeklenir).
    /// </summary>
    public static class GrassLodMeshFactory
    {
        public static Mesh CreateTriangle()
        {
            var mesh = new Mesh { name = "GRASS_LOD_Triangle", hideFlags = HideFlags.DontSave };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 1f, 0f),
            };
            // Düz üçgen: normal +Z (yaw ile döner); arka yüz shader'ın çift yüz/normal işlemesine bırakılır.
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f) };
            mesh.SetIndices(new[] { 0, 2, 1 }, MeshTopology.Triangles, 0);
            mesh.bounds = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.1f));
            return mesh;
        }
    }
}
