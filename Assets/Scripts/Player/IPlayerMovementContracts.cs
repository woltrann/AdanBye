using UnityEngine;

// Küçük, tek-amaçlı arayüzler (ISP): her biri tek bir soru cevaplar.
// Bileşenler birbirine bu arayüzler üzerinden bağlanır, somut sınıflara değil (DIP).

public interface IGroundedProvider
{
    bool IsGrounded { get; }
}

public interface IWaterProvider
{
    bool IsInWater { get; }
}

public interface IVelocityProvider
{
    Vector3 CurrentVelocity { get; }
    float MoveSpeed { get; }
}

public interface IElevationOffsetProvider
{
    // Bu FixedUpdate'te transform'un ne kadar yükseldiği/alçaldığı (swim rise gibi efektler için)
    float ElevationDelta { get; }
    // O anki toplam yükseklik ofseti (raycast mesafelerini düzeltmek için)
    float CurrentElevation { get; }
}
