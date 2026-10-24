Shader "G10/UI/HorrorDisplay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Display frame", 2D) = "white" {}
        _Strength ("Strength", Range(0,1)) = 0
        _EffectTime ("Effect time", Float) = 0
        _Seed ("Seed", Float) = 0
        _Analog ("Analog", Float) = 0
        _Circle ("Circular display", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _ClipRect;
            float _Strength, _EffectTime, _Seed, _Analog, _Circle;
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))+_Seed)*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float tick=floor(_EffectTime*8);
                float row=floor(uv.y*26);
                float tear=step(.78,hash(float2(row,tick)));
                float displacement=(hash(float2(row+3,tick))-.5)*(.025+.035*_Analog)*tear*_Strength;
                float roll=exp(-pow((uv.y-frac(_EffectTime*.32))/.035,2));
                float2 sampleUV=clamp(uv+float2(displacement+roll*.012*_Strength,0),.001,.999);
                float split=(.003+.007*_Analog)*_Strength*(.3+tear);
                fixed4 c=tex2D(_MainTex,sampleUV);
                c.r=tex2D(_MainTex,sampleUV+float2(split,0)).r;
                c.b=tex2D(_MainTex,sampleUV-float2(split,0)).b;
                float noise=hash(floor(uv*float2(430,260))+float2(tick,tick*7))-.5;
                float scan=sin(uv.y/_MainTex_TexelSize.y*3.14159)*.055;
                c.rgb=c.rgb*(1-_Strength*(tear*.36+roll*.4))+_Strength*(noise*.28+scan);
                c.rgb*=1-_Strength*tear*.22;
                float2 q=abs(uv-.5);
                float rounded=1-smoothstep(.49,.5,max(q.x,q.y)+max(0,min(q.x,q.y)-.46)*.25);
                float circle=1-smoothstep(.482,.495,length(uv-.5));
                c.a=lerp(rounded,circle,_Circle)*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
