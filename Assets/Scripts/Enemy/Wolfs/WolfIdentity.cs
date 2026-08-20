using System.Collections.Generic;
using UnityEngine;

// Tek iş: bu kurdun kim olduğunu (alpha mı), kime bağlı olduğunu (alphaWolf,
// packMembers) ve kimi hedeflediğini (playerTransform) bilmek. Diğer bileşenler
// oyuncu/pack bilgisine buradan ulaşır - kendi başlarına referans aramazlar.
public class WolfIdentity : MonoBehaviour
{
    [Header("Pack")]
    [SerializeField] private bool isAlpha; // Lider mi?
    [SerializeField] private Transform playerTransform;
    [SerializeField] private WolfIdentity alphaWolf; // Beta'lar için lider referansı
    [SerializeField] private List<WolfIdentity> packMembers; // Alpha'nın ekibi

    public bool IsAlpha => isAlpha;
    public Transform PlayerTransform => playerTransform;
    public WolfIdentity AlphaWolf => alphaWolf;
    public IReadOnlyList<WolfIdentity> PackMembers => packMembers;
    public Vector3 HomePosition { get; private set; } // Idle wander'ın etrafında döneceği sabit nokta
    public PlayerManager CachedPlayerManager { get; private set; }

    private void Awake()
    {
        HomePosition = transform.position;

        if (playerTransform != null)
            CachedPlayerManager = playerTransform.GetComponent<PlayerManager>();
    }

    public int IndexOfMember(WolfIdentity member) => packMembers != null ? packMembers.IndexOf(member) : -1;
}
