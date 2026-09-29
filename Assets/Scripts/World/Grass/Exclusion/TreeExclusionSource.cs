using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Her ağaç için disk basar. Yarıçap = (prototip override'ı ?? prefab bounds yarıçapı) * widthScale + margin.
    /// Override ölçek 1'deki gövde yarıçapıdır; margin ölçekten bağımsız sabit metre (çimin gövdeye yapışmaması için).
    /// </summary>
    public sealed class TreeExclusionSource : IGrassExclusionSource
    {
        readonly ITreeInstanceProvider _provider;
        readonly IReadOnlyDictionary<int, float> _radiusPerPrototype;
        readonly float _margin;
        readonly float _falloff;

        public TreeExclusionSource(ITreeInstanceProvider provider,
            IReadOnlyDictionary<int, float> radiusPerPrototype = null,
            float margin = 1.5f, float falloff = 0f)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _radiusPerPrototype = radiusPerPrototype;
            _margin = Mathf.Max(0f, margin);
            _falloff = Mathf.Max(0f, falloff);
        }

        public void Rasterize(ExclusionMaskCanvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            // Prototip başına yarıçapı bir kez çöz: binlerce ağaçta prefab bounds'u tekrar hesaplanmasın.
            var baseRadius = new Dictionary<int, float>();
            foreach (TreeInstanceInfo tree in _provider.GetInstances())
            {
                if (!baseRadius.TryGetValue(tree.PrototypeIndex, out float r))
                {
                    if (_radiusPerPrototype == null || !_radiusPerPrototype.TryGetValue(tree.PrototypeIndex, out r))
                        r = _provider.GetPrefabRadius(tree.PrototypeIndex);
                    baseRadius[tree.PrototypeIndex] = r;
                }
                float scale = tree.WidthScale > 0f ? tree.WidthScale : 1f;
                canvas.StampDisk(tree.PositionXZ, r * scale + _margin, _falloff);
            }
        }
    }
}
