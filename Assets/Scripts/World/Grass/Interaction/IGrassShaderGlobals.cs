using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Shader global yazma soyutlaması. Neden interface: publisher, Shader statik API'sine bağlanmadan sahte sink ile test edilsin.
    /// </summary>
    public interface IGrassShaderGlobals
    {
        void SetFloat(string name, float value);
        void SetVectorArray(string name, Vector4[] values);
    }
}
