using UnityEngine;

// Tek iş: kurt sürüsünün terk etmemesi gereken alanı (merkez + yarıçap) tanımlamak.
// WolfPack altına child olarak eklenip sahnede elle konumlandırılır/boyutlandırılır -
// sürünün "sınırı" tek bir kurdun spawn noktasına değil, bu objeye bağlıdır, bu yüzden
// birden fazla kurt aynı Territory'yi referans gösterebilir.
public class WolfTerritory : MonoBehaviour
{
    [SerializeField] private float radius = 20f;

    public Vector3 Center => transform.position;
    public float Radius => radius;

    public bool IsOutside(Vector3 position) => Vector3.Distance(position, Center) > radius;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
