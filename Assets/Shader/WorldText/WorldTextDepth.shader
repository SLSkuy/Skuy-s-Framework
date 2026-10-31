Shader "Skuy/WorldTextDepth"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "WorldText"
            Tags { "LightMode"="SRPDefaultUnlit" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual
            
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            struct Attributes
            {
                float4 positionOS : POSITION; 
                float2 uv : TEXCOORD0; 
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION; 
                float2 uv : TEXCOORD0; 
                float4 color : COLOR;
            };
            
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = input.color;
                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return color;
            }
            
            ENDHLSL
        }
    }
}