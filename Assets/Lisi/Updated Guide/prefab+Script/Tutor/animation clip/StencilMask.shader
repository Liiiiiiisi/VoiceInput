Shader "Unlit/StencilMask_URP"
{
    Properties
    {
        [IntRange]_StencilID("Stencil ID", Range(0,255)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline"="UniversalPipeline" }

        Pass
        {

            // 不输出颜色
            ColorMask 0
            ZWrite Off

            Stencil
            {
                Ref [_StencilID]
                Comp Always
                Pass Replace
            }

        }
    }
}


