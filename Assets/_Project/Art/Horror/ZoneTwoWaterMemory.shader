Shader "G10/UI/ZoneTwoWaterMemory"
{
    Properties
    {
        [PerRendererData] _MainTex ("Illustrated photograph", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Strength ("Bounded edge motion", Range(0,1)) = 0
        _EffectTime ("Presentation clock", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _EffectTime, _Strength;
            v2f vert(appdata v)
            { v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                // Only the outer22% of the photograph changes. The central subject/data stay exact.
                float edge=(1-smoothstep(.025,.22,min(uv.x,1-uv.x))) *
                    smoothstep(0,.025,min(uv.x,1-uv.x)) * smoothstep(.02,.12,min(uv.y,1-uv.y));
                float2 drift=float2(sin(uv.y*8+_EffectTime*.31),sin(uv.x*13+_EffectTime*.23));
                uv+=drift*float2(.003,.0015)*edge*saturate(_Strength);
                fixed4 c=(tex2D(_MainTex,saturate(uv))+_TextureSampleAdd)*i.color;
                c.rgb*=1-saturate(_Strength)*edge*.018*(.5+.5*sin(uv.y*9-_EffectTime*.19));
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
