using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Etkileşimcileri shader dizilerine paketleyen saf fonksiyon. Neden ayrı: seçim/eleme kuralları
    /// (NaN, kırpma, en yakın Max) GPU'ya gitmeden test edilebilsin; çağıran dizileri bir kez ayırıp yeniden kullanır (GC yok).
    /// </summary>
    public static class GrassInteractionPacker
    {
        /// <summary>
        /// Geçerli kayıtları posRadius/parameters dizilerine yazar. İlk Max slotun tamamı her çağrıda üzerine yazılır
        /// (kullanılmayan slotlar sıfır): shader sayıya güvenmese bile eski kareden artık veri okumaz.
        /// Dönüş: dolu slot sayısı. <paramref name="dropped"/>: kaynaktan pakete girmeyen toplam kayıt
        /// (geçersiz + Max aşımı nedeniyle elenen).
        /// </summary>
        /// <param name="focus">Max aşılırsa "en yakın" ölçütünün merkezi (genelde kamera/oyuncu). Null veya NaN/Inf ise
        /// mesafe kullanılmaz, geçerli kayıtlardan sıradaki ilk Max alınır.</param>
        public static int Pack(IReadOnlyList<IGrassInteractor> source, Vector3? focus,
                               Vector4[] posRadius, Vector4[] parameters, out int dropped)
        {
            const int Max = GrassInteractionContract.MaxInteractors;
            if (posRadius == null || posRadius.Length < Max)
                throw new ArgumentException($"posRadius en az {Max} uzunlukta olmalı.", nameof(posRadius));
            if (parameters == null || parameters.Length < Max)
                throw new ArgumentException($"parameters en az {Max} uzunlukta olmalı.", nameof(parameters));

            Array.Clear(posRadius, 0, Max);
            Array.Clear(parameters, 0, Max);

            int sourceCount = source?.Count ?? 0;
            if (sourceCount == 0)
            {
                dropped = 0;
                return 0;
            }

            bool useFocus = focus.HasValue && IsFinite(focus.Value);
            Vector3 f = useFocus ? focus.Value : default;

            int validCount = 0;
            for (int i = 0; i < sourceCount; i++)
                if (IsValid(source[i])) validCount++;

            // Sığıyorsa ya da mesafe ölçütü yoksa seçim gerekmez: kayıt sırasıyla ilk Max.
            bool needsSelection = useFocus && validCount > Max;

            int used = 0;
            for (int i = 0; i < sourceCount && used < Max; i++)
            {
                IGrassInteractor a = source[i];
                if (!IsValid(a)) continue;

                // Sıralama dizisi ayırmamak için sıra (rank) hesaplıyoruz: beni yenen kayıt sayısı Max'tan azsa seçilirim.
                // Eşit mesafede düşük indeks kazanır => kararlı ve deterministik. O(n^2) ama n küçük (onlarca).
                // Çıktı yine kayıt sırasındadır, böylece slot ataması kareler arası oynamaz.
                if (needsSelection && CountBeaten(source, i, f) >= Max) continue;

                Write(a, used++, posRadius, parameters);
            }

            dropped = sourceCount - used;
            return used;
        }

        // 'i' kaydını (mesafe, indeks) sırasında geçen geçerli kayıt sayısı.
        static int CountBeaten(IReadOnlyList<IGrassInteractor> source, int i, Vector3 focus)
        {
            float di = (source[i].Position - focus).sqrMagnitude;
            int beaten = 0;
            for (int j = 0; j < source.Count; j++)
            {
                if (j == i || !IsValid(source[j])) continue;
                float dj = (source[j].Position - focus).sqrMagnitude;
                if (dj < di || (dj == di && j < i)) beaten++;
            }
            return beaten;
        }

        static void Write(IGrassInteractor a, int slot, Vector4[] posRadius, Vector4[] parameters)
        {
            Vector3 p = a.Position;
            posRadius[slot] = new Vector4(p.x, p.y, p.z, a.Radius);
            // Negatif dikey menzil anlamsız; 0'a çekmek etkiyi kapatır, shader tarafında işaret varsayımı gerekmez.
            parameters[slot] = new Vector4(Mathf.Clamp01(a.Strength), Mathf.Max(0f, a.VerticalRange), 0f, 0f);
        }

        // Neden NaN/Inf elemesi: tek bir NaN shader'da mesafe hesabını zehirler ve çevredeki tüm çim yok olur
        // (rüzgar (0,0) normalize sorununun aynısı).
        static bool IsValid(IGrassInteractor a)
        {
            if (!GrassInteractorRegistry.IsUsable(a)) return false;
            if (!IsFinite(a.Position)) return false;
            float r = a.Radius, s = a.Strength, v = a.VerticalRange;
            if (!IsFinite(r) || !IsFinite(s) || !IsFinite(v)) return false;
            return r > 0f && s > 0f;
        }

        static bool IsFinite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }
}
