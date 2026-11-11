Shader "PortLab/Harbor Water"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (.018,.15,.21,1)
        _CrestColor ("Wave color", Color) = (.045,.30,.34,1)
        _HorizonColor ("Horizon haze", Color) = (.52,.67,.76,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "HarborWater"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _CrestColor, _HorizonColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float3 NoiseGradient(float2 p)
            {
                float2 cell=floor(p), f=frac(p), u=f*f*(3-2*f), du=6*f*(1-f);
                float a=Hash(cell), b=Hash(cell+float2(1,0)), c=Hash(cell+float2(0,1)), d=Hash(cell+1);
                return float3(lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y),
                    lerp(b-a,d-c,u.y)*du.x,lerp(c-a,d-b,u.x)*du.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 p = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y;
                p.y += .17 * sin(dot(p.xz, float2(.18,.10)) + t*.62)
                     + .08 * sin(dot(p.xz, float2(-.10,.30)) - t*.47);
                output.positionWS = p;
                output.positionCS = TransformWorldToHClip(p);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.positionWS.xz;
                float t = _Time.y;
                float a = dot(p, float2(.18,.10)) + t*.62;
                float b = dot(p, float2(-.10,.30)) - t*.47;
                float2 slope = .17*cos(a)*float2(.18,.10) + .08*cos(b)*float2(-.10,.30);
                // Short crossed ripples move independently of the broad swell. Fade
                // unresolved detail in the distance to avoid sparkling pixels in VR.
                float footprint = max(length(ddx(p)), length(ddy(p)));
                float detail = 1 - smoothstep(.35, 2.5, footprint);
                float3 chop=NoiseGradient(p*.42+float2(t*.12,-t*.08));
                float3 smallChop=NoiseGradient(p*.93+float2(-t*.14,t*.11)+7);
                slope += detail * (chop.yz*.17 + smallChop.yz*.07
                    + .02*cos(dot(p,float2(1.8,.7))+chop.x*5-t*1.7)*float2(1.8,.7));
                half3 n = normalize(float3(-slope.x, 1, -slope.y));
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light sun = GetMainLight();
                half fresnel = pow(1-saturate(dot(n,v)), 4);
                half3 sky = lerp(half3(.12,.36,.49), _HorizonColor.rgb, .65);
                half bands = .35 + .15*sin(a) + .12*sin(b) + .35*NoiseGradient(p*.05+t*.012).x;
                half3 color = lerp(_DeepColor.rgb, _CrestColor.rgb, bands*.65);
                color *= .8 + .2*saturate(dot(n,sun.direction));
                color = lerp(color, sky, .15 + fresnel*.7);
                half3 h = normalize(sun.direction + v);
                half highlight = pow(saturate(dot(n,h)), 110) * .8;
                color += sun.color * highlight;
                // Sparse pale ripple crests, rather than a uniformly flat fill.
                half crest = pow(saturate(sin(a*2.7 + chop.x*5)), 22)
                           * smoothstep(.5,.85,smallChop.x);
                color += half3(.20,.31,.30)*crest*.12;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
