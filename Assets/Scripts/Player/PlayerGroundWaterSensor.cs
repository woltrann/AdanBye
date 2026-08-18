using UnityEngine;

// Tek iş: karakterin zeminde mi, suda mı olduğunu raycast ile tespit etmek.
// Başka hiçbir şey bilmez - hareket, animasyon, yüzme mantığı burada yok.
[DefaultExecutionOrder(-100)] // diğer bileşenlerden ÖNCE çalışmalı ki onlar güncel veriyi okusun
public class PlayerGroundWaterSensor : MonoBehaviour, IGroundedProvider, IWaterProvider
{
    [Header("Ground Settings")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Water Settings")]
    [SerializeField] private float waterCheckDistance = 0.2f;
    [SerializeField] private LayerMask waterLayer;

    // Opsiyonel: swim rise gibi bir efekt varsa raycast mesafesini onun kadar uzatırız.
    // Yoksa (interface bulunamazsa) sıfır kabul edilir - sensor bunun kim olduğunu bilmez.
    private IElevationOffsetProvider elevationProvider;

    public bool IsGrounded { get; private set; }
    public bool IsInWater { get; private set; }

    private void Awake()
    {
        elevationProvider = GetComponent<IElevationOffsetProvider>();
    }

    private void Update()
    {
        float extraDistance = elevationProvider != null ? elevationProvider.CurrentElevation : 0f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        IsGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + extraDistance, groundLayer);

        // Orijinal davranış korunuyor: sadece zeminde değilken suya bakılıyor.
        IsInWater = !IsGrounded && Physics.Raycast(origin, Vector3.down, waterCheckDistance + extraDistance, waterLayer);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.down * groundCheckDistance);
    }
#endif
}
