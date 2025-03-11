Shader "Custom/ColorChangeShader"
{
    Properties
    {
        _FloatDelay ("Float Delay", Float) = 0
        _IsSleeping ("Is Sleeping", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        float _FloatDelay;
        int _IsSleeping;

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            if (_IsSleeping == 1)
            {
                o.Albedo = float3(0.2, 0.2, 0.2); // Grey when sleeping
            }
            else if (_FloatDelay > 0)
            {
                o.Albedo = float3(1, 1, 0); // Yellow when floatDelay > 0
            }
            else
            {
                o.Albedo = float3(1, 0, 0); // Red if awake
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
