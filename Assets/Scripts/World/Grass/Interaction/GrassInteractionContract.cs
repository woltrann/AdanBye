namespace AdanBye.Grass
{
    /// <summary>
    /// C# tarafı ile Shader Graph / HLSL tarafının paylaştığı sabitler. Neden tek yerde: dizi uzunluğu ve global adlar
    /// iki tarafta ayrı ayrı yazılırsa sessizce kayar (shader diziyi taşırır ya da yanlış global'i okur).
    /// Shader tarafındaki dizi boyutu (16) bu sabitle elle eşleşmelidir.
    /// </summary>
    public static class GrassInteractionContract
    {
        /// <summary>Aynı anda shader'a gidebilecek en fazla etkileşimci (Vector4[16] x2 = 512 byte, cbuffer için ucuz).</summary>
        public const int MaxInteractors = 16;

        /// <summary>float: bu karede dolu slot sayısı (Shader.SetGlobalFloat ile yazılır).</summary>
        public const string CountName = "_GrassInteractorCount";
        /// <summary>Vector4[]: xyz dünya konumu, w yarıçap (m).</summary>
        public const string PosRadiusName = "_GrassInteractorPosRadius";
        /// <summary>Vector4[]: x güç (0..1), y dikey menzil (m), zw ayrılmış (0).</summary>
        public const string ParamsName = "_GrassInteractorParams";
    }
}
