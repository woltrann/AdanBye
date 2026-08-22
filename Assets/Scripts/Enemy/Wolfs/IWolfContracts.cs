using System;
using UnityEngine;

// Küçük, tek-amaçlı arayüzler (ISP): her biri tek bir soru cevaplar.
// Bileşenler birbirine bu arayüzler üzerinden bağlanır, somut sınıflara değil (DIP).

// Guard: territory sınırında oyuncuya bakıp uluyarak beklemek - kovalamayı ne sürdürür
// ne de eve döner, oyuncu tekrar sınıra girerse anında Chase'e geri döner.
public enum WolfState { Idle, Chase, Search, Guard, Retreat }

public interface IWolfMover
{
    float CurrentSpeed { get; }
    void MoveTo(Vector3 target, float moveSpeed);
    void MoveAwayFrom(Vector3 target, float moveSpeed);
    void LookAt(Vector3 target);
}

public interface IWolfAttacker
{
    float AttackDistance { get; }
    bool IsPaused { get; }
    bool IsReady { get; }
    void Attack();
    event Action OnAttack;
}

public interface IWolfHowler
{
    void TryHowl();
    event Action OnHowl;
}

// Strategy: Alpha ve Beta kurtların "beyni". Yeni bir rol eklemek (örn. Scout) bu
// arayüzü implement eden yeni bir sınıf demektir - mevcut Alpha/Beta sınıflarına
// dokunulmaz (OCP).
public interface IWolfBehavior
{
    void Tick();
    void OnStateEntered(WolfState newState);
    Vector3 DebugTarget { get; }
}
