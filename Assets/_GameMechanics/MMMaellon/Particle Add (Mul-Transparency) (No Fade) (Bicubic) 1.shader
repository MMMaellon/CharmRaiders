// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Particles/Additive + Options (3050)" {
Properties {
    [HDR]_TintColor ("Tint Color", Color) = (1,1,1,1)
    _MainTex ("Particle Texture", 2D) = "white" {}
    _TimePow ("Scroll Power", Vector) = (0, 0, 0, 0)
    [Toggle(SOFTPARTICLES_ON)]_SoftParticles ("Use Soft Particle", Float) = 0
    [Toggle(_)]_ApplyFog ("Use Fog", Float) = 0
    _InvFade ("Soft Particles Factor", Range(0.001,3.0)) = 1.0
    [Enum(Off, 0, Far, 1, Near, 2)] _ZEdge ("Render at Clip Plane", Float) = 0
    _VisDistance ("Visibility Range", Float) = 0

    [Header(Advanced)]
    [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 0
    _Offset("Offset", float) = 0
    [Enum(Off,0,On,1)] _ZWrite("ZWrite", Int) = 1
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Int) = 4
    [Header(Stencil)]
    [Enum(None,0,Alpha,1,Red,8,Green,4,Blue,2,RGB,14,RGBA,15)] _colormask("Color Mask", Int) = 15 
    [IntRange] _Stencil ("Stencil ID [0;255]", Range(0,255)) = 0
    _ReadMask ("ReadMask [0;255]", Int) = 255
    _WriteMask ("WriteMask [0;255]", Int) = 255
    [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 0
    [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Int) = 0
    [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Int) = 0
    [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Int) = 0
}

Category {
    Tags { "Queue"="Transparent+50" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
    Blend SrcAlpha One
    ColorMask [_colormask]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Stencil
        {
            Ref [_Stencil]
            ReadMask [_ReadMask]
            WriteMask [_WriteMask]
            Comp [_StencilComp]
            Pass [_StencilOp]
            Fail [_StencilFail]
            ZFail [_StencilZFail]
        }
    Cull [_CullMode]

    SubShader {
        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_particles
            #pragma multi_compile_fog
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _TintColor;

            struct appdata_t {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                #ifdef SOFTPARTICLES_ON
                float4 projPos : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _MainTex_ST; float4 _MainTex_TexelSize;
            float4 _TimePow;
            float _ZEdge;
            float _SoftParticles;
            float _VisDistance;
            float _ApplyFog;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                if (_ZEdge==1) {
                    #if defined(UNITY_REVERSED_Z)
                    // when using reversed-Z, make the Z be just a tiny
                    // bit above 0.0
                    //o.vertex.z = 1.0e-9f;
                    o.vertex.z = 1.0e-8f;
                    #else
                    // when not using reversed-Z, make Z/W be just a tiny
                    // bit below 1.0
                    //o.vertex.z = o.vertex.w - 1.0e-6f;
                    o.vertex.z = o.vertex.w - 1.0e-5f;
                    #endif
                }
                if (_ZEdge==2) {
                    #if !defined(UNITY_REVERSED_Z)
                    // when using reversed-Z, make the Z be just a tiny
                    // bit above 0.0
                    //o.vertex.z = 1.0e-9f;
                    o.vertex.z = 1.0e-8f;
                    #else
                    // when not using reversed-Z, make Z/W be just a tiny
                    // bit below 1.0
                    //o.vertex.z = o.vertex.w - 1.0e-6f;
                    o.vertex.z = o.vertex.w - 1.0e-5f;
                    #endif
                }
                #ifdef SOFTPARTICLES_ON
                o.projPos = ComputeScreenPos (o.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
                #endif
                o.color = v.color * _TintColor;

                if (_VisDistance) {
                    fixed3 baseWorldPos = unity_ObjectToWorld._m03_m13_m23;
                    const float scale = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                    float closeDist = distance(_WorldSpaceCameraPos, baseWorldPos);
                    o.color *= saturate((_VisDistance*scale)-closeDist);
                }
                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex)+(_Time.x*_TimePow.xy+_TimePow.zw);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _InvFade;
        
            float4 cubic(float v)
            {
                float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
                float4 s = n * n * n;
                float x = s.x;
                float y = s.y - 4.0 * s.x;
                float z = s.z - 4.0 * s.y + 6.0 * s.x;
                float w = 6.0 - x - y - z;
                return float4(x, y, z, w);
            }

            float4 bicubicFilter(sampler2D inTex, float2 texcoord, float2 texscale)
            {
                float fx = frac(texcoord.x);
                float fy = frac(texcoord.y);
                texcoord.x -= fx;
                texcoord.y -= fy;
            
                float4 xcubic = cubic(fx);
                float4 ycubic = cubic(fy);
            
                float4 c = float4(texcoord.x - 0.5, texcoord.x + 1.5, texcoord.y -
            0.5, texcoord.y + 1.5);
                float4 s = float4(xcubic.x + xcubic.y, xcubic.z + xcubic.w, ycubic.x +
            ycubic.y, ycubic.z + ycubic.w);
                float4 offset = c + float4(xcubic.y, xcubic.w, ycubic.y, ycubic.w) /
            s;
            
                float4 sample0 = tex2D(inTex, float2(offset.x, offset.z) *
            texscale);
                float4 sample1 = tex2D(inTex, float2(offset.y, offset.z) *
            texscale);
                float4 sample2 = tex2D(inTex, float2(offset.x, offset.w) *
            texscale);
                float4 sample3 = tex2D(inTex, float2(offset.y, offset.w) *
            texscale);
            
                float sx = s.x / (s.x + s.y);
                float sy = s.z / (s.z + s.w);
            
                return lerp(
                    lerp(sample3, sample2, sx),
                    lerp(sample1, sample0, sx), sy);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                #ifdef SOFTPARTICLES_ON
                if (_SoftParticles)
                {
                float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                float partZ = i.projPos.z;
                float fade = saturate (_InvFade * (sceneZ-partZ));
                i.color.a *= fade;
                }
                #endif

                #ifdef LOD_FADE_CROSSFADE
                i.color.a = i.color.a * unity_LODFade.x;
                #endif

                fixed4 col = i.color * bicubicFilter(_MainTex, i.texcoord*_MainTex_TexelSize.zw, _MainTex_TexelSize.xy);

                col.rgb *= col.a;
                if (_ApplyFog) UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
                return col;
            }
            ENDCG
        }
    }
}
}
