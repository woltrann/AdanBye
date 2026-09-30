using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Shader.SetGlobal* üzerine ince sarmalayıcı (gerçek sink).</summary>
    public sealed class UnityGrassShaderGlobals : IGrassShaderGlobals
    {
        public void SetFloat(string name, float value) => Shader.SetGlobalFloat(name, value);
        public void SetVectorArray(string name, Vector4[] values) => Shader.SetGlobalVectorArray(name, values);
    }
}
