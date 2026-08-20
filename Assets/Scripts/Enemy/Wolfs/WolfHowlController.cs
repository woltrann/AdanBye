using System;
using UnityEngine;

// Tek iş: howl cooldown'ını takip etmek ve OnHowl event'ini fırlatmak.
// Animasyon tetikleme WolfAnimatorSync'in işi - burası sadece "ne zaman ulunabilir"
// sorusuna cevap verir.
public class WolfHowlController : MonoBehaviour, IWolfHowler
{
    [Header("Howl")]
    [SerializeField] private float howlCooldown = 8f;

    private float lastHowlTime = -999f;

    public event Action OnHowl;

    public void TryHowl()
    {
        if (Time.time < lastHowlTime + howlCooldown) return;
        lastHowlTime = Time.time;
        OnHowl?.Invoke();
    }
}
