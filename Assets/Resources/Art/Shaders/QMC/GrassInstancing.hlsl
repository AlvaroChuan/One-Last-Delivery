#ifndef GRASS_INSTANCING_INCLUDED
#define GRASS_INSTANCING_INCLUDED

StructuredBuffer<float4x4> visibleInstances; //same name as c#

void GetInstancedData_float(uint instanceID, float3 objectPosition, float3 objectNormal, out float3 worldPosition, out float3 worldNormal)
{
#ifdef SHADERGRAPH_PREVIEW
        worldPosition = TransformObjectToWorld(objectPosition);
        worldNormal = TransformObjectToWorldNormal(objectNormal);
#else
    float4x4 mat = visibleInstances[instanceID];
    worldPosition = mul(mat, float4(objectPosition, 1.0)).xyz;
    worldNormal = normalize(mul((float3x3) mat, objectNormal));
#endif
}
#endif