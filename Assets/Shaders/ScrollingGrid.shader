// ScrollingGrid.shader
// URP-compatible Unlit shader that draws a scrolling neon grid.
// Uses frac/step math — no texture required.
// Pans automatically using Unity's built-in _Time variable.

Shader "Custom/ScrollingGrid"
{
    Properties
    {
        _GridColor   ("Grid Line Color",  Color)  = (0.7, 0.0, 1.0, 1.0)   // neon purple default
        _BgColor     ("Background Color", Color)  = (0.01, 0.0, 0.06, 1.0) // near-black navy
        _GridScale   ("Grid Scale",       Float)  = 20.0    // higher = more lines (finer grid)
        _LineWidth   ("Line Width",       Range(0.01, 0.5)) = 0.04
        _ScrollSpeed ("Scroll Speed",     Float)  = 0.3     // units per second along +Z
        _Alpha       ("Opacity",          Range(0, 1)) = 1.0
    }

    SubShader
    {
        // Render on the transparent queue
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off   // visible from both sides so the camera behind it still sees the grid

        Pass
        {
            Name "UnlitScrollingGrid"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ---- Uniform declarations ----
            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _BgColor;
                float  _GridScale;
                float  _LineWidth;
                float  _ScrollSpeed;
                float  _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Scroll UV over time along the V axis (simulates flying forward)
                float2 uv = IN.uv * _GridScale;
                uv.y     += _Time.y * _ScrollSpeed * _GridScale;

                // frac() maps UV to [0,1) within each grid cell
                float2 cell = frac(uv);

                // step() returns 1 where cell < lineWidth or cell > (1 - lineWidth)
                // This creates a line at each cell boundary on both axes
                float lineX = step(cell.x, _LineWidth) + step(1.0 - _LineWidth, cell.x);
                float lineY = step(cell.y, _LineWidth) + step(1.0 - _LineWidth, cell.y);

                // Combine: 1 if on a line, 0 otherwise
                float onLine = saturate(lineX + lineY);

                // Blend between background and grid colour
                half4 col = lerp(_BgColor, _GridColor, onLine);

                // Fade out towards the top of the UV (V -> 1)
                float fade = saturate(1.0 - IN.uv.y);
                col.a *= fade * _Alpha;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
