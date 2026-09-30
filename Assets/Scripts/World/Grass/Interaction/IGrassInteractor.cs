using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Çimi eğen/ezen bir kaynak (oyuncu, NPC, hayvan...). Neden interface: registry ve packer Unity bileşenine
    /// bağımlı olmadan test edilebilsin; gerçek MonoBehaviour'lar bunu uygular.
    /// </summary>
    public interface IGrassInteractor
    {
        /// <summary>Dünya konumu (etki merkezi, genelde ayak).</summary>
        Vector3 Position { get; }
        /// <summary>Etki yarıçapı (m), yatay düzlemde.</summary>
        float Radius { get; }
        /// <summary>Etki gücü; packer 0..1'e kırpar.</summary>
        float Strength { get; }
        /// <summary>Konumun üstünde/altında etkinin geçerli olduğu dikey menzil (m).</summary>
        float VerticalRange { get; }
        /// <summary>False ise registry kaydı budar. Destroy edilmiş Unity nesneleri ayrıca "Unity-null" olarak yakalanır.</summary>
        bool IsAlive { get; }
    }
}
