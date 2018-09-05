Shader "Custom/WaterShader" 
{
    //////////////////////////////////////
    // Parameters
    // Propertiesブロックに書いた変数がインスペクタから操作できるようになります
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _BumpHeightMaps("BumpHeightMaps", 2DArray) = "" {}
        _RefractiveRatio("RefractiveRatio", Range(0.0,1.0)) = 0.25
        _TextureZ("TextureZ", int) = 0
    }
        //
        //////////////////////////////////////

        SubShader
    {
        //////////////////////////////////////
        // Shader Settings
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        //
        //////////////////////////////////////

        Pass
        {
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4    _Color;
            float     _RefractiveRatio;
            int       _TextureZ;
            UNITY_DECLARE_TEX2DARRAY(_BumpHeightMaps);

            //////////////////////////////////////
            // Vertex Shader
            struct VertexIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;
            };

            struct VertexOut
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 world_vertex : TEXCOORD1;
                float height : TEXCOORD2;
            };

            VertexOut vert(VertexIn v)
            {
                VertexOut o;
                //static float offset_mul = 101.0f / 128.0f;
                //static float offset_add = (128.0f - 101.0f) * 0.5f / 128.0f;

                float2 real_uv = (1.0f - v.uv);// *offset_mul + offset_add;
                float4 bump_height = UNITY_SAMPLE_TEX2DARRAY_LOD(_BumpHeightMaps, float3(real_uv, _TextureZ), 0);
                o.height = bump_height.w;
                bump_height = (bump_height - 0.5f) * 2.0f;

                v.vertex.y += bump_height.w * 1.5f;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world_vertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(bump_height.xyz);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(VertexOut info) : SV_Target
            {
                float light_intensity = dot(normalize(info.normal), _WorldSpaceLightPos0.xyz); //(info.height + 0.25f) * 2.0f;
                float3 diffuse = _Color.rgb * max(0.0f, light_intensity);

                // フレネル反射率計算
                float3 forward = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
                float A = _RefractiveRatio;
                float B = dot(-forward, normalize(info.normal));
                float C = sqrt(1.0f - A * A * (1 - B * B));
                float Rs = (A*B - C) * (A*B - C) / ((A*B + C) * (A*B + C));
                float Rp = (A*C - B) * (A*C - B) / ((A*C + B) * (A*C + B));
                float alpha = (Rs + Rp) / 2.0f;

                //float3 reflect_view = reflect(forward, normalize(info.normal));
                //float view_intensity = max(0.0f, dot(reflect_view, _WorldSpaceLightPos0.xyz));
                float4 color = float4(diffuse, min(alpha + 0.2f, 0.65f));
                return color;
            }
            //
            //////////////////////////////////////
            ENDCG
        }
	}
	FallBack "Diffuse"
}
