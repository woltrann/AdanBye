using System;
using UnityEngine;

// Küçük, tek-amaçlı arayüzler (ISP): her biri tek bir soru cevaplar.
// Bileşenler birbirine bu arayüzler üzerinden bağlanır, somut sınıflara değil (DIP).

public interface ITargetProvider
{
    Transform Target { get; }
    bool HasTarget { get; }
}

public interface IDistanceProvider
{
    float DistanceToTarget { get; }
}

public interface IEnemyAttacker
{
    bool IsAttackReady { get; }
    void Attack();
    event Action OnAttack;
}
