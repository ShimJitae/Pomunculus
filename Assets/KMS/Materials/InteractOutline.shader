// 상호작용 대상 테두리용 아웃라인 셰이더 (Inverted Hull 방식)
// - 메시를 한 번 더, 뒤집어서(Cull Front) 화면 기준으로 살짝 부풀려 그린다
// - 두께는 픽셀 단위라 카메라 거리와 상관없이 일정하다
// - Built-in, URP 모두 동작 (HDRP 미지원)
Shader "Custom/InteractOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (pixels)", Range(0, 20)) = 4
        // 0 = 노멀 방향으로 부풀림 (캐릭터처럼 부드러운 메시)
        // 1 = 중심에서 바깥 방향으로 부풀림 (큐브처럼 모서리가 각진 메시, 테두리 끊김 방지)
        _PositionDir ("Expand From Center (hard-edged mesh)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _PositionDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // 부풀릴 방향: 노멀 ↔ 중심에서 정점 방향
                float3 p = v.vertex.xyz;
                float lenP = length(p);
                float3 fromCenter = lenP > 1e-5 ? p / lenP : v.normal;
                float3 dir = lerp(v.normal, fromCenter, _PositionDir);

                o.pos = UnityObjectToClipPos(v.vertex);

                // 방향을 화면 공간으로 옮겨서 픽셀 단위로 밀어냄
                float3 viewDir = mul((float3x3)UNITY_MATRIX_IT_MV, dir);
                float2 offset = TransformViewToProjection(viewDir.xy);
                float lenO = length(offset);
                if (lenO > 1e-5)
                {
                    offset /= lenO;
                    o.pos.xy += offset * (_OutlineWidth * 2.0 / _ScreenParams.xy) * o.pos.w;
                }
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
