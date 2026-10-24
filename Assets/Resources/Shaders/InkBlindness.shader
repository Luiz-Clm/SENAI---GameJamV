Shader "UI/InkBlindness"
{
    Properties
    {
        _Color ("Tint", Color) = (0.01, 0.015, 0.025, 0.96)
        _RevealRadius ("Reveal Radius", Float) = 0
        _Center ("Reveal Center", Vector) = (0.5, 0.5, 0, 0)
        _Feather ("Edge Softness", Float) = 0.04
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _RevealRadius;
            float4 _Center;
            float _Feather;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenDelta = (i.uv - _Center.xy) * _ScreenParams.xy;
                float distanceFromCenter = length(screenDelta) / max(_ScreenParams.y, 1.0);
                float visible = smoothstep(_RevealRadius, _RevealRadius + max(_Feather, 0.001), distanceFromCenter);
                return fixed4(_Color.rgb, _Color.a * visible);
            }
            ENDCG
        }
    }
}
