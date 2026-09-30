using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Registry -> packer -> shader globals hattı (saf C#). Neden diziler hep Max uzunlukta: Unity global vektör dizisini
    /// ilk set edildiği uzunlukta tahsis eder; uzunluk değişirse yeniden tahsis olur / eski veri kalır. Sabit uzunluk hem
    /// GC'siz hem shader'daki [16] ile uyumlu.
    /// </summary>
    public sealed class GrassInteractionPublisher
    {
        readonly GrassInteractorRegistry _registry;
        readonly IGrassShaderGlobals _globals;
        readonly string _countName;
        readonly string _posRadiusName;
        readonly string _paramsName;
        readonly Vector4[] _posRadius = new Vector4[GrassInteractionContract.MaxInteractors];
        readonly Vector4[] _parameters = new Vector4[GrassInteractionContract.MaxInteractors];

        /// <summary>Son Publish'te shader'a giden dolu slot sayısı.</summary>
        public int ActiveCount { get; private set; }
        /// <summary>Son Publish'te pakete girmeyen kayıt sayısı (geçersiz + Max aşımı).</summary>
        public int DroppedCount { get; private set; }

        public GrassInteractionPublisher(GrassInteractorRegistry registry, IGrassShaderGlobals globals,
                                         string countName = GrassInteractionContract.CountName,
                                         string posRadiusName = GrassInteractionContract.PosRadiusName,
                                         string paramsName = GrassInteractionContract.ParamsName)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _globals = globals ?? throw new ArgumentNullException(nameof(globals));
            _countName = countName;
            _posRadiusName = posRadiusName;
            _paramsName = paramsName;
        }

        public void Publish(Vector3? focus)
        {
            _registry.Prune();
            ActiveCount = GrassInteractionPacker.Pack(_registry.Items, focus, _posRadius, _parameters, out int dropped);
            DroppedCount = dropped;
            Write();
        }

        /// <summary>Etkiyi kapatır: count=0 ve diziler sıfır (shader count'a güvenmese bile artık veri okumaz).</summary>
        public void Clear()
        {
            Array.Clear(_posRadius, 0, _posRadius.Length);
            Array.Clear(_parameters, 0, _parameters.Length);
            ActiveCount = 0;
            DroppedCount = 0;
            Write();
        }

        void Write()
        {
            _globals.SetFloat(_countName, ActiveCount);
            _globals.SetVectorArray(_posRadiusName, _posRadius);
            _globals.SetVectorArray(_paramsName, _parameters);
        }
    }
}
