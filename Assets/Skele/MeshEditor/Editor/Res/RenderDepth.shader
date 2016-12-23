Shader "MH/RenderDepth" {
SubShader {
    Tags { "RenderType"="Opaque" }
    Pass {
        Fog { Mode Off }
        CGPROGRAM
        
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        
        struct v2f {
            float4 pos : SV_POSITION;
            float2 depth : TEXCOORD0;
        };
        
        v2f vert (appdata_base v) {
            v2f o;
            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
            o.depth = o.pos.zw;
            //UNITY_TRANSFER_DEPTH(o.depth); //the behaviour is different under DX9 & DX11... dont use it
            return o;
        }
        
        float4 frag(v2f i) : SV_Target {
            //UNITY_OUTPUT_DEPTH(i.depth);  //dont use it
            float d = min(i.depth.x / i.depth.y, 0.99999999);
            return EncodeFloatRGBA(d);
        }
        ENDCG
    }
}
}