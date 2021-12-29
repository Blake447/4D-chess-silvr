Shader "Unlit/4D-Chess-Visualizer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ImportScale("Import Scale", float) = 1.0
        _OffsetScale("Offset Scale", float) = 1.0
        _CursorScale("Cursor Scale", float) = 1.0
        _CursorHeight("Cursor Height", float) = 1.0
        _Coordinate("Coordinate", Vector) = (0,0,0,0)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ImportScale;
            float _OffsetScale;
            float _CursorScale;
            float _CursorHeight;
            float4 _Coordinate;

            v2f vert (appdata v)
            {
                int4 root_coordinate = floor(_Coordinate + float4(8.5, 8.5, 8.5, 8.5));
                root_coordinate += -float4(8, 8, 8, 8);

                float xy_offset = 0.00825;
                float z_offset = 0.01235*1.325;
                float z_scaling = 0.9;
                float w_offset = 0.04125;

                


                float import_scale = _ImportScale;

                float3 rt = float3(1, 0, 0);
                float3 up = float3(0, 1, 0);
                float3 fw = float3(0, 0, 1);

                float x_raw = dot(rt, v.vertex) * import_scale;
                float y_raw = dot(up, v.vertex) * import_scale;
                float z_raw = dot(fw, v.vertex) * import_scale;

                float isInBoundsMajor = step(max(max(abs(x_raw), abs(y_raw)), abs(z_raw)), 24);
                

                float4 original = float4(frac(x_raw), frac(y_raw), frac(z_raw), 0) - float4(0.5, 0.5, .25, 0);
                original.z *= _CursorHeight;

                int x_int = (int)(-x_raw + 128) - 128;
                int y_int = (int)(y_raw + 128) - 128;

                int x_proj = x_int % 4 - root_coordinate.x;
                int z_proj = x_int / 4 + root_coordinate.z;


                int y_proj = y_int % 4 + root_coordinate.y;
                int w_proj = y_int / 4 + root_coordinate.w;
                
                float z = dot(fw, v.vertex);


                float scalar = pow(0.9, z_proj);
                float2 center = float2(-1.5, 1.5);
                float2 centered = float2(x_proj, y_proj) - center;
                float2 scaled = centered * scalar;
                float2 offset_scaled = scaled + center;


                //float4 reprojected = float4(x_proj*xy_offset, y_proj*xy_offset +  w_proj*w_offset, z_proj*z_offset, 1);
                float4 reprojected = float4(offset_scaled.x*xy_offset, offset_scaled.y*xy_offset + w_proj*w_offset, z_proj*z_offset, 1);

                float minimum = min(min(-x_proj, z_proj), min(y_proj, w_proj));
                float maximum = max(max(-x_proj, z_proj), max(y_proj, w_proj));

                float isInBounds = step(-0.5, minimum) * step(maximum, 3.5);



                v2f o;
                o.color = reprojected;
                o.vertex = UnityObjectToClipPos( (original*.01*_CursorScale + reprojected*_OffsetScale) * isInBoundsMajor * isInBounds);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(1, 0, 1, 1);
            }
            ENDCG
        }
    }
}
