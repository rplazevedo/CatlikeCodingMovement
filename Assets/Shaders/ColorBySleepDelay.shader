Shader "Custom/ColorChangeShader"
{
    Properties
    {
        _FloatDelay ("Float Delay", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        float _FloatDelay;

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            if (_FloatDelay == 0)
            {
                o.Albedo = float3(0.2, 0.2, 0.2); // Dark Grey
            }
            else if (_FloatDelay < 1)
            {
                o.Albedo = float3(1, 1, 0); // Yellow
            }
            else
            {
                o.Albedo = float3(1, 0, 0); // Red
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}