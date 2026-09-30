using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Çimi eğen sahne bileşeni (oyuncu, NPC, hayvan). Neden [ExecuteAlways]: Edit modunda da kayıt olsun ki
    /// Scene view'da etki önizlenebilsin; OnDisable'da silinir, domain reload sonrası OnEnable yeniden kaydeder.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassInteractor : MonoBehaviour, IGrassInteractor
    {
        [Tooltip("Yatay etki yarıçapı (m).")]
        [SerializeField, Min(0f)] float radius = 0.6f;
        [Tooltip("Etki gücü (0..1).")]
        [SerializeField, Range(0f, 1f)] float strength = 1f;
        [Tooltip("Konumun üstünde/altında etkinin geçerli olduğu dikey menzil (m).")]
        [SerializeField, Min(0f)] float verticalRange = 1.5f;
        [Tooltip("Transform konumuna eklenen dikey kaydırma (ör. pivot bel hizasındaysa negatif ver).")]
        [SerializeField] float yOffset;

        public Vector3 Position => transform.position + new Vector3(0f, yOffset, 0f);
        public float Radius => radius;
        public float Strength => strength;
        public float VerticalRange => verticalRange;
        public bool IsAlive => isActiveAndEnabled;

        void OnEnable() => GrassInteractorRegistry.Default.Register(this);
        void OnDisable() => GrassInteractorRegistry.Default.Unregister(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.9f);
            Vector3 p = Position;
            const int Segments = 32;
            Vector3 prev = p + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                Vector3 next = p + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawWireCube(p, new Vector3(radius * 2f, verticalRange * 2f, radius * 2f));
        }
    }
}
