using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Composition root: Registry.Default -> Publisher -> Shader globals. Sahnede tek olmalı (globaller paylaşımlıdır,
    /// ikinci feed birincinin yazdığını ezer). Mantık Publisher'da; burada yalnızca yaşam döngüsü ve odak seçimi var.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GrassInteractionFeed : MonoBehaviour
    {
        static int s_enabledCount;
        static bool s_warned;

        [Tooltip("Max aşılınca en yakın 16 ölçütünün merkezi. Boşsa Camera.main, o da yoksa kayıt sırası.")]
        [SerializeField] Transform focus;

        GrassInteractionPublisher _publisher;

        /// <summary>Inspector için: "aktif N/16, M elendi".</summary>
        public string StatusText => _publisher == null
            ? "Yayın yok"
            : $"aktif {_publisher.ActiveCount}/{GrassInteractionContract.MaxInteractors}, {_publisher.DroppedCount} elendi";

        void OnEnable()
        {
            _publisher = new GrassInteractionPublisher(GrassInteractorRegistry.Default, new UnityGrassShaderGlobals());
            if (++s_enabledCount > 1 && !s_warned)
            {
                s_warned = true;
                Debug.LogWarning("Sahnede birden fazla GrassInteractionFeed var; shader globalleri paylaşımlı olduğundan " +
                                 "birbirlerini ezerler. Tek feed bırakın.", this);
            }
        }

        void OnDisable()
        {
            s_enabledCount = Mathf.Max(0, s_enabledCount - 1);
            _publisher?.Clear();
            _publisher = null;
        }

        void LateUpdate()
        {
            if (_publisher == null) return;
            _publisher.Publish(ResolveFocus());
        }

        Vector3? ResolveFocus()
        {
            if (focus != null) return focus.position;
            Camera cam = Camera.main;
            return cam != null ? cam.transform.position : (Vector3?)null;
        }

        // Domain reload kapalıyken statikler Play'ler arası sızar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_enabledCount = 0; s_warned = false; }
    }
}
