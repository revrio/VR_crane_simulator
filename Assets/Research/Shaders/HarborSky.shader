Shader "PortLab/Harbor Sky"
{
    Properties
    {
        _Zenith ("Upper sky", Color) = (.13,.36,.65,1)
        _Horizon ("Horizon", Color) = (.52,.67,.76,1)
        _SunDirection ("Sun direction", Vector) = (.4,.7,.3,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon;
                float4 _SunDirection;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            float Clouds(float2 p)
            {
                return Noise(p)*.55 + Noise(p*2.1+4)*.28 + Noise(p*4.3+8)*.12 + Noise(p*8.1)*.05;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 d=normalize(input.direction);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(saturate(d.y),.55));
                float sun=saturate(dot(d,normalize(_SunDirection.xyz)));
                color += half3(1,.83,.57)*pow(sun,64)*.18;
                color = lerp(color,half3(1,.96,.84),smoothstep(.99965,.99988,sun));
                float2 cloudUV=d.xz/max(d.y+.12,.12)*2.5+float2(_Time.y*.004,_Time.y*.0015);
                float shape=Clouds(cloudUV);
                float cloud=smoothstep(.55,.75,shape)*smoothstep(.02,.18,d.y);
                half3 cloudColor=lerp(half3(.64,.72,.79),half3(.96,.97,.98),saturate((shape-.48)*4));
                color=lerp(color,cloudColor,cloud*.85);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
