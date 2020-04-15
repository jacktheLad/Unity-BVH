// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Tracer/Preview/Diffuse" {
    Properties{
        _Type("Type", Int) = 0
        _DiffuseTex("DiffuseTex", 2D) = "white" {}
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _DiffuseTex;

        struct Input {
            float2 uv_DiffuseTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_DiffuseTex, IN.uv_DiffuseTex);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Legacy Shaders/VertexLit"
}
