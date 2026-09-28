using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bir kameranın çim çizimi alıp almayacağına karar verir. Neden arayüz: minimap / MapBaker / preview /
    /// reflection kameralarını reddetme kuralı proje bazlı değişir; renderer bu kuralı bilmez (OCP/DIP).
    /// beginCameraRendering her kamera için çağrıldığından implementasyon allocation yapmamalı.
    /// </summary>
    public interface IGrassCameraFilter
    {
        bool ShouldRender(Camera camera);
    }
}
