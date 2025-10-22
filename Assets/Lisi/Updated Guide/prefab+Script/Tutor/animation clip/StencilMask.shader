Shader "Unlit/StencilMask"
{
    Properties
    {
        [IntRange]_stencilID("Stencil ID", Range(0,255))= 0
    }

    SubShader
    {
        Tags{"RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline"="UniversalPipline"}

        Pass
        {
            Blend Zero One
            ZWrite Off

            Stencil
            {
                Ref [_stencilID]
                Comp Always
                Pass Replace
    
            }
        }
    }
}
