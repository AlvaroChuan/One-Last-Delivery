#ifndef MAIN_LIGHT_INCLUDED
#define MAIN_LIGHT_INCLUDED

void MainLight_float(float3 worldPos, out float3 lightDirection, out float3 lightColor, out float distanceAttenuation, out float shadowAttenuation)
{
    #ifdef SHADERGRAPH_PREVIEW
            // Valores por defecto para la previsualización dentro de Shader Graph
            lightDirection = normalize(float3(1.0f, 1.0f, 0.0f));
            lightColor = 1.0f;
            distanceAttenuation = 1.0f;
            shadowAttenuation = 1.0f;
    #else

    #if SHADOWS_SCREEN
                float4 clipPos = TransformWorldToHClip(worldPos);
                float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
    #endif
        Light light = GetMainLight(shadowCoord);
        
        lightDirection = light.direction;
        lightColor = light.color;
        distanceAttenuation = light.distanceAttenuation;
        shadowAttenuation = light.shadowAttenuation;
    #endif
}
#endif