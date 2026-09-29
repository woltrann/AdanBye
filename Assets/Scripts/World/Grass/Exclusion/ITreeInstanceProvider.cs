using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Tek ağaç örneği: dünya XZ konumu, prototip indeksi, yatay ölçek.</summary>
    public readonly struct TreeInstanceInfo
    {
        public Vector2 PositionXZ { get; }
        public int PrototypeIndex { get; }
        public float WidthScale { get; }

        public TreeInstanceInfo(Vector2 positionXZ, int prototypeIndex, float widthScale)
        {
            PositionXZ = positionXZ;
            PrototypeIndex = prototypeIndex;
            WidthScale = widthScale;
        }
    }

    /// <summary>Ağaç verisinin kaynağı; TreeExclusionSource Terrain'e doğrudan bağımlı olmasın diye.</summary>
    public interface ITreeInstanceProvider
    {
        IReadOnlyList<TreeInstanceInfo> GetInstances();

        /// <summary>Prototip prefab'ının ölçek 1'deki yatay yarıçapı (bounds'tan); bilinmiyorsa 0.</summary>
        float GetPrefabRadius(int prototypeIndex);
    }
}
