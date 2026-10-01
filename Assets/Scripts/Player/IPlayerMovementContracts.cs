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

// --- Stamina/hareket kilidi sözleşmeleri (WP-4) ---
// Motor stamina'yı bilmez; sadece "koşabilir miyim / hız çarpanı / kilitli miyim" sorar.

public interface IRunGate
{
    bool CanRun { get; }
    // Yürüme ve koşu hızına birlikte uygulanır.
    float SpeedMultiplier { get; }
}

public interface IMovementLock
{
    bool IsMovementLocked { get; }
}

public interface IStaminaReadout
{
    float Current { get; }
    float Ceiling { get; }
    float Max { get; }
    bool IsCollapsed { get; }
    event System.Action Collapsed;
}

public interface ICampRestReceiver
{
    void RestAtCamp();
}
