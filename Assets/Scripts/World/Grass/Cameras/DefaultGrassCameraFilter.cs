using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Varsayılan kural: yalnızca Game (ve isteğe bağlı SceneView) kameraları; Preview/Reflection reddedilir;
    /// çim layer'ı kameranın cullingMask'inde olmalı (Unity'nin kendi MeshRenderer davranışıyla aynı; minimap
    /// kamerası dar mask kullandığı için burada kendiliğinden elenir); ayrıca açıkça dışlanan kameralar.
    /// </summary>
    public sealed class DefaultGrassCameraFilter : IGrassCameraFilter
    {
        readonly bool _allowSceneView;
        readonly int _grassLayer;
        readonly HashSet<Camera> _excluded = new HashSet<Camera>();

        public DefaultGrassCameraFilter(int grassLayer, bool allowSceneView = true)
        {
            _grassLayer = grassLayer;
            _allowSceneView = allowSceneView;
        }

        /// <summary>Kamerayı çimden kalıcı olarak dışlar (ör. runtime'da oluşan minimap kamerası).</summary>
        public void Exclude(Camera camera)
        {
            if (camera != null) _excluded.Add(camera);
        }

        public bool ShouldRender(Camera camera)
        {
            if (camera == null) return false;

            switch (camera.cameraType)
            {
                case CameraType.Game: break;
                case CameraType.SceneView:
                    if (!_allowSceneView) return false;
                    break;
                default: return false; // Preview, Reflection, VR
            }

            if ((camera.cullingMask & (1 << _grassLayer)) == 0) return false;
            return !_excluded.Contains(camera);
        }
    }
}
