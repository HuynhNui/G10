Shader "G10/UI/ZoneThreeComposition"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI texture", 2D) = "white" {}
        _Mode ("Composition", Float) = 1
        _Strength ("Strength", Range(0,1)) = 0
        _EffectTime ("Effect time", Float) = 0
        _Seed ("Seed", Float) = 0
        _Aspect ("Display aspect", Float) = 1
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _ClipRect;
            float _Mode, _Strength, _EffectTime, _Seed, _Aspect, _Circle;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))+_Seed*.019)*43758.5453); }
            float noise(float2 p)
            {
                float2 a=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(a),hash21(a+float2(1,0)),f.x),lerp(hash21(a+float2(0,1)),hash21(a+1),f.x),f.y);
            }
            float mist(float2 p) { return noise(p)*.58+noise(p*2.03+7)*.28+noise(p*4.01+19)*.14; }
            float ellipse(float2 p,float2 center,float2 radius,float softness)
            { return 1-smoothstep(1-softness,1+softness,length((p-center)/radius)); }
            float angleDistance(float a,float b) { return abs(frac((a-b)/6.2831853+.5)*6.2831853-3.1415926); }
            fixed4 stone(float2 uv)
            {
                float2 p=uv*float2(max(1,_Aspect),1);
                float n=mist(p*5+float2(_EffectTime*.008,0));
                float edge=min(uv.x,1-uv.x);
                float strata=sin(uv.y*31+n*5+sin(uv.x*9)*1.7)*.5+.5;
                float side=(1-smoothstep(.025,.20+n*.055,edge))*(.45+.55*n);
                float floorMask=(1-smoothstep(.015,.22+n*.075,uv.y))*(.5+.5*strata);
                float ridge=max(side,floorMask);
                float haze=pow(1-saturate(abs(uv.y-.22)*2.2),3)*pow(n,3)*.12;
                float grain=hash21(floor(p*350));
                float alpha=saturate(ridge*.40+haze);
                float3 color=lerp(float3(.025,.050,.058),float3(.14,.205,.205),strata*.5+n*.4+grain*.1);
                return float4(color,alpha);
            }
            fixed4 radar(float2 uv)
            {
                float2 p=(uv-float2(.53,.46))*float2(_Aspect,1);
                float radius=length(p), angle=atan2(p.y,p.x);
                float turn=.50+_EffectTime*.95;
                float da=angleDistance(angle,turn);
                float sweep=exp(-pow(da/.11,2))*smoothstep(.02,.07,radius)*(1-smoothstep(.40,.48,radius));
                float fan=exp(-pow(angleDistance(angle,turn-.13)/.27,2))*.20*(1-smoothstep(.42,.49,radius));
                float echo=0;
                [unroll] for(int j=0;j<3;j++)
                {
                    float2 at=j==0 ? float2(-.21,.17) : j==1 ? float2(.20,-.12) : float2(.10,.29);
                    float birth=j*.42+.18;
                    float age=max(0,_EffectTime-birth);
                    float presence=smoothstep(birth,birth+.15,_EffectTime)*exp(-age*.55);
                    float d=length(p-at);
                    float echoPoint=exp(-d*d/((.013+.003*j)*(.013+.003*j)));
                    float ring=exp(-pow((d-(.023+age*.026))/.006,2))*.45;
                    echo+=presence*(echoPoint+ring);
                }
                float secondary=exp(-pow(angleDistance(angle,turn-1.14)/.025,2))*.20*(1-smoothstep(.38,.46,radius));
                float amount=saturate(sweep*.82+fan+echo*.76+secondary);
                float scan=.90+.10*sin(uv.y*820);
                float3 color=lerp(float3(.24,.72,.62),float3(.84,.90,.67),saturate(echo));
                return float4(color,amount*scan);
            }
            fixed4 reflection(float2 uv)
            {
                // Soft volumes and refracted glass, deliberately no face or painted ghost sprite.
                float t=_EffectTime;
                float2 p=uv-.5;
                float veils=mist(uv*float2(8,5)+float2(t*.05,-t*.015));
                p.x+=sin(p.y*15+t*.67)*.006+(veils-.5)*.018;
                float sway=.055+sin(t*.29)*.014;
                float head=ellipse(p,float2(sway,.17),float2(.052,.082),.40);
                float shoulders=ellipse(p,float2(sway-.009,-.005),float2(.158,.100),.44);
                float trunk=ellipse(p,float2(sway-.014,-.22),float2(.104,.22),.50);
                float arm=ellipse(p,float2(sway-.145,-.17),float2(.035,.15),.65)*.48;
                float shape=max(max(head,shoulders),max(trunk,arm));
                shape*=.56+.44*veils;
                float smear=ellipse(p,float2(sway+.041,-.03),float2(.082,.34),.9)*.25;
                float glass=exp(-pow((uv.x-.79+uv.y*.17)/.033,2))*.16;
                float etching=(.5+.5*sin(uv.y*460+t*.45))*.012;
                float3 dark=float3(.012,.024,.031);
                float3 figure=float3(.17,.235,.245)*(shape+smear);
                float3 color=dark+figure+float3(.15,.20,.21)*glass+veils*.012+etching;
                float alpha=.69+shape*.17;
                return float4(color,alpha);
            }
            fixed4 electric(float2 uv)
            {
                float t=_EffectTime;
                float burst=pow(saturate(sin(t*17+sin(t*6.7)*2.1)),12);
                burst+=pow(saturate(sin(t*29+.9)),20)*.55;
                float2 p=(uv-.5)*float2(_Aspect,1);
                float halo=exp(-pow((length(p)-.33)/.07,2))*.11;
                float arc=0;
                [unroll] for(int j=0;j<5;j++)
                {
                    float turn=j*1.2566+.22;
                    float2 axis=float2(cos(turn),sin(turn)), tangent=float2(-axis.y,axis.x);
                    float along=dot(p,axis), across=dot(p,tangent);
                    float n=noise(float2(along*32+j*13,floor(t*11)))-.5;
                    float path=n*.10+sin(along*64+j)*.008;
                    float band=exp(-pow((across-path)/.0045,2));
                    float reach=smoothstep(.14,.20,along)*(1-smoothstep(.34,.43,along));
                    arc+=band*reach*(.3+.7*hash21(float2(j,floor(t*13))));
                }
                float a=saturate((arc*.9+halo)*burst);
                return float4(lerp(float3(.25,.61,.67),float3(.79,.98,.85),saturate(arc)),a);
            }
            fixed4 debris(float2 uv)
            {
                float t=_EffectTime;
                float2 p=uv*float2(_Aspect,1);
                float n=mist(p*float2(5,8)+float2(t*.045,-t*.10));
                float rise=.04+min(t,4)*.046;
                // A broken, rolling plume stays along the floor and lower corners. Its dark
                // blue-gray wash belongs to the watercolor cabin instead of whitening the viewport.
                float rolling=rise+(n-.5)*.105+sin(uv.x*12+n*3-t*.8)*.018;
                float cloud=exp(-pow((uv.y-rolling)/(.065+t*.014),2))*(.45+.55*n)*.58;
                float lowerMask=1-smoothstep(.30,.50,uv.y);
                float sideMask=1-smoothstep(.025,.14,min(uv.x,1-uv.x));
                cloud+=sideMask*lowerMask*pow(n,2)*.20;
                cloud*=lowerMask*smoothstep(0,.16,t)*(1-smoothstep(4,8,t));
                float particles=0;
                [unroll] for(int j=0;j<22;j++)
                {
                    float2 h=float2(hash21(float2(j,19)),hash21(float2(j,47)));
                    float launch=frac(h.y*7)*1.2;
                    float age=max(0,t-launch);
                    float2 start=float2(h.x*_Aspect,.035+h.y*.04);
                    float2 v=float2((h.x-.5)*.11,.14+h.y*.13);
                    float2 at=start+v*age+float2(0,-.047*age*age);
                    float size=.0034+pow(h.y,3)*.0075;
                    float2 axis=normalize(h-float2(.47,.53)+float2(.001,.003));
                    float2 delta=(p-at)/size;
                    float2 q=float2(dot(delta,axis)/1.35,dot(delta,float2(-axis.y,axis.x)));
                    // Unequal angled cuts produce rough slate fragments, never aligned square confetti.
                    float shardEdge=max(max(abs(q.x)*.70+abs(q.y)*.50,q.x*.45+q.y*.90),-q.x*.90-q.y*.40);
                    float shard=1-smoothstep(.58,1,shardEdge);
                    particles+=shard*step(launch,t)*(1-smoothstep(2,4,age));
                }
                float edge=(1-smoothstep(0,.18,min(uv.x,1-uv.x)))*n*.07;
                float3 color=lerp(float3(.075,.12,.15),float3(.17,.23,.25),saturate(particles));
                return float4(color,saturate(cloud+particles*.65+edge));
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c;
                if(_Mode<1.5) c=stone(i.uv);
                else if(_Mode<2.5) c=radar(i.uv);
                else if(_Mode<3.5) c=reflection(i.uv);
                else if(_Mode<4.5) c=electric(i.uv);
                else c=debris(i.uv);
                float2 q=abs(i.uv-.5);
                float rounded=1-smoothstep(.485,.50,max(q.x,q.y)+max(0,min(q.x,q.y)-.45)*.25);
                float circle=1-smoothstep(.480,.495,length(i.uv-.5));
                c.a*=lerp(rounded,circle,_Circle)*saturate(_Strength)*i.color.a;
                c.rgb*=i.color.rgb;
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
