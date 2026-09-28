// WP-1b SPIKE shader'ı (kalıcı değil). Amaç: RenderMeshIndirect + procedural instancing matris override'ının
// URP 17 Forward+ altında ForwardLit / DepthOnly / DepthNormals'ta çalıştığını göstermek.
// ShadowCaster pass'i BİLEREK yok (çim gölge atmaz).
Shader "AdanBye/Spike/GrassSpikeLit"
{
    Properties
    {
        _ColorRoot("Root Color", Color) = (0.08, 0.20, 0.04, 1)
        _ColorTip("Tip Color", Color) = (0.45, 0.65, 0.18, 1)
        [HideInInspector] _GrassContractVersion("", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex GrassVert
            #pragma fragment GrassFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            // Sözleşme: kullanıcı shader'ı bu iki satırı doğrudan yazar.
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:GrassInstancingSetup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/Grass/GrassInstancing.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorRoot;
                half4 _ColorTip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half3  tint       : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings GrassVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                GrassInstanceData d = Grass_GetCurrentInstance();
                o.tint = d.color.rgb;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 GrassFrag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // Çift taraflı blade: arka yüzde normali çevir, yoksa ışık ters gelir.
                float3 n = normalize(input.normalWS) * IS_FRONT_VFACE(facing, 1.0, -1.0);
                // Çim yumuşak aydınlansın diye normali yukarıya doğru çek (ucuz "yuvarlak yüzey" hilesi).
                n = normalize(lerp(n, float3(0, 1, 0), 0.5));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = n;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(n);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = lerp(_ColorRoot.rgb, _ColorTip.rgb, saturate(input.uv.y)) * input.tint;
                s.metallic = 0;
                s.specular = 0;
                s.smoothness = 0.1;
                s.occlusion = lerp(0.5, 1.0, saturate(input.uv.y)); // kökte ucuz kontakt gölgesi
                s.normalTS = half3(0, 0, 1);
                s.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, s);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:GrassInstancingSetup

            // URP'nin hazır pass'i: aynı UNITY_SETUP_INSTANCE_ID + TransformObjectToHClip yolunu kullanır,
            // yani matris override'ı bu pass'te de geçerli mi sorusunu tam da test eder.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Grass/GrassInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:GrassInstancingSetup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Grass/GrassInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
