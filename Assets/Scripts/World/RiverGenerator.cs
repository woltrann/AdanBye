using UnityEngine;
using UnityEngine.Splines; // Spline kütüphanesi
using Unity.Mathematics; // Matematik iþlemleri için

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode] // Editörde çalýþýrken de mesh'i günceller!
public class RiverGenerator : MonoBehaviour
{
    public SplineContainer splineContainer; // Unity'nin Spline bileþeni

    [Header("Ayarlar")]
    public float width = 5f;        // Nehir geniþliði
    public int resolution = 50;     // Nehir boyunca kaç parça olacak (Yumuþaklýk)
    public float uvScale = 5f;      // Texture tekrar sýklýðý

    private void OnEnable()
    {
        // Spline deðiþtiðinde otomatik güncellemek için event'e abone ol
        Spline.Changed += OnSplineChanged;
        UpdateRiver();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    // Spline üzerinde bir noktayý oynattýðýnda otomatik tetiklenir
    void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
    {
        if (splineContainer != null && spline == splineContainer.Spline)
            UpdateRiver();
    }

    // Inspector'dan bir deðer deðiþtirirsen güncelle
    private void OnValidate()
    {
        UpdateRiver();
    }

    void UpdateRiver()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        Spline spline = splineContainer.Spline;
        Mesh mesh = new Mesh();

        // resolution kadar adýmda spline'ý örnekleyeceðiz
        // Vertex sayýsý: (resolution + 1) * 2 (sað ve sol kýyý)
        Vector3[] vertices = new Vector3[(resolution + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * 6];

        float step = 1f / resolution; // 0 ile 1 arasýnda ilerleme adýmý
        float currentLen = 0f; // UV için uzunluk hesabý

        for (int i = 0; i <= resolution; i++)
        {
            float t = i * step; // Spline üzerindeki konum (0=baþ, 1=son)

            // Spline üzerinden verileri al (Unity.Mathematics float3 döner, Vector3'e çeviririz)
            Vector3 position = (Vector3)spline.EvaluatePosition(t);
            Vector3 tangent = (Vector3)spline.EvaluateTangent(t); // Akýþ yönü
            Vector3 up = (Vector3)spline.EvaluateUpVector(t);     // Yukarý yönü

            // Saðý bulmak için Cross Product (Teðet ile Yukarý'nýn çarpýmý)
            Vector3 right = Vector3.Cross(tangent, up).normalized;

            // Mesh local space'te olmalý, bu yüzden transform.InverseTransformPoint kullanýyoruz
            // NOT: Eðer SplineContainer ile Mesh ayný objedeyse buna gerek kalmayabilir ama güvenli yol budur.
            Vector3 localPos = transform.InverseTransformPoint(transform.position + position);
            // Düzeltme: SplineContainer genelde world pozisyon verir, biz bunu scriptin olduðu objeye göre ayarlamalýyýz.
            // Daha basiti: Scripti SplineContainer ile AYNI objeye koyarsan:
            localPos = transform.InverseTransformPoint(splineContainer.transform.TransformPoint(position));


            // Sol ve Sað Köþeler
            vertices[i * 2] = localPos - (right * width * 0.5f);
            vertices[i * 2 + 1] = localPos + (right * width * 0.5f);

            // UV Hesaplama
            if (i > 0)
            {
                // Önceki noktayla aradaki mesafeyi hesapla
                float dist = Vector3.Distance(vertices[i * 2], vertices[(i - 1) * 2]);
                currentLen += dist;
            }

            uvs[i * 2] = new Vector2(0, currentLen / width * uvScale);     // Sol UV
            uvs[i * 2 + 1] = new Vector2(1, currentLen / width * uvScale); // Sað UV

            // Üçgenleri örme
            if (i < resolution)
            {
                int vertIndex = i * 2;
                int triIndex = i * 6;

                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + 2;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + 2;
                triangles[triIndex + 5] = vertIndex + 3;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}