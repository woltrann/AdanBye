// WP-1b SPIKE debug: ekranı kaplayan quad ile URP'nin _CameraNormalsTexture / _CameraDepthTexture içeriğini
// renk olarak gösterir. Amaç: çimin DepthNormals prepass'inin gerçekten yazıp yazmadığına kanıt
// (materyalde DepthNormals pass'ini açıp kapatarak A/B). Doğrulamadan sonra silinecek.
Shader "AdanBye/Spike/GrassSpikeDebugView"
{
    Properties { _Mode("Mode (0 normals, 1 linear depth/50)", Float) = 0 }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "DebugView"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                // Quad'ın UV'sinden doğrudan ekran kaplayan konum (nesne konumu yalnızca cull'u geçmek için).
                o.positionCS = float4(i.uv * 2.0 - 1.0, 0.0, 1.0);
#if UNITY_UV_STARTS_AT_TOP
                o.positionCS.y = -o.positionCS.y;
#endif
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy / _ScaledScreenParams.xy;
                if (_Mode < 0.5)
                {
                    float3 n = SampleSceneNormals(uv);
                    return half4(n * 0.5 + 0.5, 1);
                }
                float d = SampleSceneDepth(uv);
                float lin = LinearEyeDepth(d, _ZBufferParams);
                return half4(lin.xxx / 50.0, 1);
            }
            ENDHLSL
        }
    }
}
