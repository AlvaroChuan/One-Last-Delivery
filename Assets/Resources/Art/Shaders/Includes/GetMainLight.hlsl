#ifndef MAIN_LIGHT_INCLUDED
#define MAIN_LIGHT_INCLUDED

void MainLight_float(float3 worldPos, out float3 lightDirection, out float3 lightColor)
{
    #ifdef SHADERGRAPH_PREVIEW
        lightDirection = normalize(float3(1.0f,1.0f,0.0f));
        lightColor = 1.0f;
    #else
        Light light = GetMainLight();
        lightDirection = light.direction;
        lightColor = light.color;
    #endif
}
#endif