using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Etkileşimci kayıt defteri (saf C#). Neden statik Default + ayrı instance: sahnedeki bileşenler tek noktaya
    /// kaydolsun, ama testler kendi instance'ını kullanıp global duruma dokunmasın.
    /// Kayıt sırası kararlıdır (List, araya ekleme yok): packer eşit mesafede sırayı belirleyici saydığı için
    /// kareden kareye seçim titremesin.
    /// </summary>
    public sealed class GrassInteractorRegistry
    {
        public static GrassInteractorRegistry Default { get; private set; } = new GrassInteractorRegistry();

        readonly List<IGrassInteractor> _items = new List<IGrassInteractor>();

        public int Count => _items.Count;

        /// <summary>Budanmamış ham liste; okuma amaçlı. Budamak için <see cref="Prune"/> çağır.</summary>
        public IReadOnlyList<IGrassInteractor> Items => _items;

        /// <summary>Idempotent: aynı nesne ikinci kez eklenmez (OnEnable çift çağrısı sırayı bozmasın). Null ve zaten ölü kayıt reddedilir.</summary>
        public bool Register(IGrassInteractor interactor)
        {
            if (!IsUsable(interactor) || _items.Contains(interactor)) return false;
            _items.Add(interactor);
            return true;
        }

        /// <summary>Idempotent: olmayan kayıt sessizce yok sayılır (OnDisable, kayıt hiç olmadıysa da güvenli). Kalanların sırası korunur.</summary>
        public bool Unregister(IGrassInteractor interactor)
        {
            if (interactor == null) return false;
            return _items.Remove(interactor);
        }

        /// <summary>
        /// IsAlive=false veya Unity-null kayıtları atar, sırayı korur. Neden Unity-null ayrıca: destroy edilmiş
        /// bir MonoBehaviour için C# referansı null değildir, IsAlive'a dokunmak ise MissingReferenceException atabilir.
        /// </summary>
        public int Prune()
        {
            return _items.RemoveAll(i => !IsUsable(i));
        }

        public void Clear() => _items.Clear();

        /// <summary>Kullanılabilirlik: null değil, Unity-null değil, IsAlive.</summary>
        public static bool IsUsable(IGrassInteractor interactor)
        {
            if (interactor == null) return false;
            // Unity'nin overload'lu == operatörü yalnızca UnityEngine.Object tipine indirgenince devreye girer.
            if (interactor is Object unityObject && unityObject == null) return false;
            return interactor.IsAlive;
        }

        // Domain reload kapalıyken statik alan Play'ler arası sızar; eski oturumun ölü kayıtları yenisine taşınmasın.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDefault() => Default = new GrassInteractorRegistry();
    }
}
