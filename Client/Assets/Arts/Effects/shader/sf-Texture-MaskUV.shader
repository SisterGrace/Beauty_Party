// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.30 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.30;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,vlit:False,simplebake:False,suppproj:False,simplepbl:False,sh:False,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:0,dpts:2,wrdp:False,dith:0,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,ugoelinearfog:False,goelinearintensity:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False;n:type:ShaderForge.SFN_Final,id:3138,x:33797,y:33189,varname:node_3138,prsc:2|emission-2155-OUT,alpha-4498-OUT;n:type:ShaderForge.SFN_Color,id:7241,x:32209,y:32512,ptovrint:False,ptlb:TintColor,ptin:_TintColor,varname:node_7241,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Tex2d,id:7323,x:32209,y:32686,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_7323,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False|UVIN-2993-UVOUT;n:type:ShaderForge.SFN_Slider,id:591,x:32148,y:32894,ptovrint:False,ptlb:ColorPar,ptin:_ColorPar,varname:node_591,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:10;n:type:ShaderForge.SFN_ValueProperty,id:1857,x:31627,y:32636,ptovrint:False,ptlb:USpeed,ptin:_USpeed,varname:node_1857,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:5190,x:31627,y:32735,ptovrint:False,ptlb:VSpeed,ptin:_VSpeed,varname:_node_1857_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Color,id:7239,x:32218,y:33141,ptovrint:False,ptlb:MaskTexColor,ptin:_MaskTexColor,varname:node_7239,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:0.5;n:type:ShaderForge.SFN_Tex2d,id:7358,x:32230,y:33341,ptovrint:False,ptlb:MaskTex,ptin:_MaskTex,varname:node_7358,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Slider,id:1563,x:32151,y:33547,ptovrint:False,ptlb:MaskTexColorPar,ptin:_MaskTexColorPar,varname:node_1563,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:10;n:type:ShaderForge.SFN_TexCoord,id:7506,x:31627,y:32451,varname:node_7506,prsc:2,uv:0;n:type:ShaderForge.SFN_UVTranslate,id:2993,x:31873,y:32602,varname:node_2993,prsc:2|UVIN-7506-UVOUT,USP-1857-OUT,VSP-5190-OUT,TIME-7078-TSL;n:type:ShaderForge.SFN_Time,id:7078,x:31627,y:32831,varname:node_7078,prsc:2;n:type:ShaderForge.SFN_Multiply,id:9861,x:32497,y:33242,varname:node_9861,prsc:2|A-7239-RGB,B-7358-RGB,C-1563-OUT;n:type:ShaderForge.SFN_Append,id:4036,x:32665,y:33372,varname:node_4036,prsc:2|A-9861-OUT,B-7358-A;n:type:ShaderForge.SFN_Multiply,id:40,x:32760,y:32728,varname:node_40,prsc:2|A-1700-OUT,B-7731-OUT,C-469-OUT,D-591-OUT,E-4036-OUT;n:type:ShaderForge.SFN_Append,id:469,x:32523,y:32728,varname:node_469,prsc:2|A-7323-RGB,B-7323-A;n:type:ShaderForge.SFN_VertexColor,id:982,x:32209,y:32330,varname:node_982,prsc:2;n:type:ShaderForge.SFN_Append,id:1700,x:32416,y:32376,varname:node_1700,prsc:2|A-982-RGB,B-982-B;n:type:ShaderForge.SFN_Append,id:7731,x:32416,y:32550,varname:node_7731,prsc:2|A-7241-RGB,B-7241-A;n:type:ShaderForge.SFN_ComponentMask,id:2155,x:33028,y:32955,varname:node_2155,prsc:2,cc1:0,cc2:1,cc3:2,cc4:-1|IN-40-OUT;n:type:ShaderForge.SFN_Add,id:4498,x:33416,y:33396,varname:node_4498,prsc:2|A-2155-R,B-2155-G,C-2155-B;proporder:7241-7323-591-1857-5190-7239-7358-1563;pass:END;sub:END;*/

Shader "Shader Forge/Effect/sf-Texture-MaskUV" {
    Properties {
        _TintColor ("TintColor", Color) = (1,1,1,1)
        _MainTex ("MainTex", 2D) = "black" {}
        _ColorPar ("ColorPar", Range(0, 10)) = 1
        _USpeed ("USpeed", Float ) = 0
        _VSpeed ("VSpeed", Float ) = 0
        _MaskTexColor ("MaskTexColor", Color) = (0.5,0.5,0.5,0.5)
        _MaskTex ("MaskTex", 2D) = "white" {}
        _MaskTexColorPar ("MaskTexColorPar", Range(0, 10)) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha One
            ZWrite Off
			cull off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform float4 _TintColor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _ColorPar;
            uniform float _USpeed;
            uniform float _VSpeed;
            uniform float4 _MaskTexColor;
            uniform sampler2D _MaskTex; uniform float4 _MaskTex_ST;
            uniform float _MaskTexColorPar;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
////// Lighting:
////// Emissive:
                float4 node_7078 = _Time + _TimeEditor;
                float2 node_2993_tc_rcp = float2( _USpeed, _VSpeed ) * node_7078.r;
                float2 node_2993 = i.uv0 + node_2993_tc_rcp;
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_2993, _MainTex));
                float4 _MaskTex_var = tex2D(_MaskTex,TRANSFORM_TEX(i.uv0, _MaskTex));
                float3 node_2155 = (float4(i.vertexColor.rgb,i.vertexColor.a)*float4(_TintColor.rgb,_TintColor.a)*float4(_MainTex_var.rgb,_MainTex_var.a)*_ColorPar*float4((_MaskTexColor.rgb*_MaskTex_var.rgb*_MaskTexColorPar),_MaskTex_var.a)).rgb;
                float3 emissive = node_2155;
                float3 finalColor = emissive;
                return fixed4(finalColor,(node_2155.r+node_2155.g+node_2155.b));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
