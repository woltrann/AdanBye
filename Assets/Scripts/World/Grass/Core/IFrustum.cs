using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Seçicinin ihtiyaç duyduğu tek şey: "bu AABB görüş hacmine değebilir mi". Neden interface: seçici
    /// Camera'ya bağımlı olmadan test edilir (sahte frustum) ve ileride farklı bir cull kaynağı (ör. gölge
    /// cascade'i) takılabilir. Muhafazakâr olmalı: emin değilse true (çim kaybolmasın).
    /// </summary>
    public interface IFrustum
    {
        bool Intersects(Bounds bounds);
    }
}
