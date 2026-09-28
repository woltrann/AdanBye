using UnityEngine;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// Kodla üretilen tek blade mesh'i: pivot kökte, +Y yukarı, ~1 birim yükseklik ve 1 birim taban genişliği
    /// (instance yüksekliği/genişliği matriste ölçeklenir), uca doğru daralan, hafif öne kavisli.
    /// </summary>
    public static class GrassSpikeBladeMesh
    {
        public static Mesh Create(int segments = 4)
        {
            segments = Mathf.Max(1, segments);
            int vertexCount = segments * 2 + 1; // her segment seviyesinde sol/sağ + tek uç vertex
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var indices = new int[(segments - 1) * 6 + 3];

            const float bend = 0.25f; // z = bend * y^2
            for (int i = 0; i < segments; i++)
            {
                float y = (float)i / segments;
                float halfWidth = 0.5f * (1f - y * 0.8f);
                float z = bend * y * y;
                Vector3 n = new Vector3(0f, -2f * bend * y, 1f).normalized;

                int l = i * 2, r = i * 2 + 1;
                vertices[l] = new Vector3(-halfWidth, y, z);
                vertices[r] = new Vector3(halfWidth, y, z);
                normals[l] = normals[r] = n;
                uvs[l] = new Vector2(0f, y);
                uvs[r] = new Vector2(1f, y);
            }

            int tip = segments * 2;
            vertices[tip] = new Vector3(0f, 1f, bend);
            normals[tip] = new Vector3(0f, -2f * bend, 1f).normalized;
            uvs[tip] = new Vector2(0.5f, 1f);

            int idx = 0;
            for (int i = 0; i < segments - 1; i++)
            {
                int l0 = i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                indices[idx++] = l0; indices[idx++] = l1; indices[idx++] = r0;
                indices[idx++] = r0; indices[idx++] = l1; indices[idx++] = r1;
            }
            int lt = (segments - 1) * 2, rt = lt + 1;
            indices[idx++] = lt; indices[idx++] = tip; indices[idx++] = rt;

            var mesh = new Mesh { name = "GRASS_SPIKE_Blade", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            // Gerçek sınır matriste değişir; RenderParams.worldBounds cull'u ayrı yönetir.
            mesh.bounds = new Bounds(new Vector3(0f, 0.5f, 0.1f), new Vector3(1f, 1f, 0.3f));
            return mesh;
        }
    }
}
