Shader "Skele/VertBillboard" {
    Properties {
        _PSize ("Point size", float) = 3
    }
    
    Subshader{ //DX11 shader
    Tags {"Queue"="Overlay+100" "IgnoreProjector"="True" "RenderType"="Transparent"}
    Pass{
        
        LOD 200
         
        Offset -1, -1
        
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
         #pragma geometry geom
         #include "UnityCG.cginc"
   
         struct VertexInput {
             float4 v : POSITION;
             float4 color: COLOR;
         };
          
         struct VertexOutput {
             float4 pos : SV_POSITION;
             float4 col : COLOR;
         };
         
         float _PSize;
          
         VertexOutput vert(VertexInput v) {
          
             VertexOutput o;
             o.pos = mul(UNITY_MATRIX_MVP, v.v);
             o.col = v.color;
             return o;
         }
         
         [maxvertexcount(4)]
         void geom(point VertexOutput input[1], inout TriangleStream<VertexOutput> OutputStream)
         {
             float dx = _PSize / _ScreenParams.x;
             float dy = _PSize / _ScreenParams.y;
             float4 pt = input[0].pos / input[0].pos.w; //important! add this line, or multiple w on each elements below.
             
             float4 vert[4];
             vert[0] = pt + float4(-dx,  dy, /*-0.0002f,*/0, 0);
             vert[1] = pt + float4( dx,  dy, /*-0.0002f,*/0, 0);
             vert[2] = pt + float4(-dx, -dy, /*-0.0002f,*/0, 0);
             vert[3] = pt + float4( dx, -dy, /*-0.0002f,*/0, 0);
             
             // Now we "append" or add the vertices to the outgoing stream list
             VertexOutput outputVert;
             for(int i = 0; i < 4; i++)
             {
                 outputVert.pos = vert[i];
                 outputVert.col = input[0].col;
                 
                 OutputStream.Append(outputVert);
             }
             
         }         
          
         float4 frag(VertexOutput o) : COLOR {
             return o.col;
         }
  
         ENDCG 
         }
    }
    
    SubShader { //DX9 shader
     Tags {"Queue"="Overlay+100" "IgnoreProjector"="True" "RenderType"="Transparent"}
     Pass {

         LOD 200
         Offset -1, -1
                  
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
         #pragma exclude_renderers d3d11
         #pragma target 3.0
         #include "UnityCG.cginc"
   
         struct VertexInput {
             float4 v : POSITION;
             float4 color: COLOR;
         };
          
         struct VertexOutput {
             float4 pos : SV_POSITION;
             float4 col : COLOR;
             float size : PSIZE;
         };
         
         float _PSize;
          
         VertexOutput vert(VertexInput v) {
          
             VertexOutput o;
             o.pos = mul(UNITY_MATRIX_MVP, v.v);
             //o.pos.z -= 0.0002f;
             o.col = v.color;
             o.size = _PSize;
             return o;
         }
          
         float4 frag(VertexOutput o) : COLOR {
             return o.col;
         }
  
         ENDCG
         } 
     }
  
 }