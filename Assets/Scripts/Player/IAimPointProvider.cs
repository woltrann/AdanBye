using UnityEngine;

// Nereye bakıldığını bilmek isteyen herkes (silah sistemi, etkileşim, UI reticle vs.)
// bu arayüz üzerinden sorar - kendi raycast'ini tekrar yazmak zorunda kalmaz.
public interface IAimPointProvider
{
    Vector3 AimPoint { get; }
    bool HasHit { get; }          // raycast bir şeye çarptı mı, yoksa boşluğa mı bakıyor
    RaycastHit LastHit { get; }   // çarpılan objenin detayı (collider, normal vs.) gerekirse
}
