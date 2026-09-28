using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// GrassInstancing.hlsl'deki GrassInstance ile BİREBİR aynı 32 baytlık düzen (Sequential, 5 x uint + float3).
    /// Alan sırası HLSL struct'ıyla eşleşmek zorunda; değişirse iki tarafı birlikte değiştir.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GrassSpikeInstance
    {
        public Vector3 position;
        public uint yawHeight;
        public uint widthFade;
        public uint colorRGBA8;
        public uint normalOct;
        public uint hash;

        public const int Stride = 32;

        public static GrassSpikeInstance Create(Vector3 position, float yawRadians, float height, float width,
                                                Color32 tint, uint hash)
        {
            uint yaw16 = (uint)Mathf.Clamp(Mathf.RoundToInt(yawRadians / (2f * Mathf.PI) * 65535f), 0, 65535);
            uint height16 = math.f32tof16(height);
            uint width16 = math.f32tof16(width);
            const uint lodFade16 = 65535u; // tek LOD: tam görünür

            return new GrassSpikeInstance
            {
                position = position,
                yawHeight = yaw16 | (height16 << 16),
                widthFade = width16 | (lodFade16 << 16),
                colorRGBA8 = (uint)tint.r | ((uint)tint.g << 8) | ((uint)tint.b << 16) | ((uint)tint.a << 24),
                normalOct = 0u, // rezerve (spike'ta terrain normali yazılmıyor)
                hash = hash,
            };
        }
    }
}
