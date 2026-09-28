using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Kamera -> kamera-başına-context eşlemesi. Neden var: çim kaynakları (GPU buffer'ları, chunk seçici) kamera başına
    /// tutulur; kamera sayısı değişkendir (Game + SceneView + geçici kameralar) ve yok olan kameranın context'i sızmamalı.
    /// Generic + Unity render bağımsız: birim testte sahte context'lerle doğrulanır.
    /// Context <see cref="IDisposable"/> ise kayıttan çıkarken / DisposeAll'da Dispose edilir.
    /// </summary>
    public sealed class GrassCameraRegistry<TContext> where TContext : class
    {
        readonly Dictionary<Camera, TContext> _contexts = new Dictionary<Camera, TContext>();
        readonly List<Camera> _deadScratch = new List<Camera>(); // hot path'te allocation olmasın diye yeniden kullanılır

        public int Count => _contexts.Count;

        public bool TryGet(Camera camera, out TContext context)
        {
            context = null;
            return camera != null && _contexts.TryGetValue(camera, out context);
        }

        /// <summary>
        /// Varsa mevcut context'i, yoksa <paramref name="factory"/> ile üretileni döner. Factory null dönerse (kurulum
        /// başarısız) hiçbir şey kaydedilmez ve null döner: çağıran hatayı kendi politikasıyla ele alır.
        /// </summary>
        public TContext GetOrCreate(Camera camera, Func<Camera, TContext> factory)
        {
            if (camera == null) return null;
            if (_contexts.TryGetValue(camera, out TContext existing)) return existing;

            TContext created = factory(camera);
            if (created != null) _contexts.Add(camera, created);
            return created;
        }

        /// <summary>
        /// Yok edilmiş (Unity-null) kameraların context'lerini Dispose edip kayıttan çıkarır; budanan sayıyı döner.
        /// Neden anahtarda `== null`: Object.Equals/== Unity'nin yıkılmış nesne semantiğini uygular.
        /// </summary>
        public int PruneDestroyed()
        {
            _deadScratch.Clear();
            foreach (KeyValuePair<Camera, TContext> pair in _contexts)
                if (pair.Key == null) _deadScratch.Add(pair.Key);

            for (int i = 0; i < _deadScratch.Count; i++)
            {
                Camera key = _deadScratch[i];
                DisposeContext(_contexts[key]);
                _contexts.Remove(key);
            }
            int removed = _deadScratch.Count;
            _deadScratch.Clear();
            return removed;
        }

        /// <summary>Idempotent: tüm context'leri Dispose edip kaydı boşaltır.</summary>
        public void DisposeAll()
        {
            foreach (TContext context in _contexts.Values) DisposeContext(context);
            _contexts.Clear();
        }

        static void DisposeContext(TContext context)
        {
            if (context is IDisposable disposable) disposable.Dispose();
        }
    }
}
