#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
	#if SHADERPASS != SHADERPASS_FORWARD && SHADERPASS != SHADERPASS_GBUFFER
		// #if to avoid "duplicate keyword" warnings if this is included in a Lit Graph

    	#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    	#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
		#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
		#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

		// Left some keywords (e.g. light layers, cookies) in subgraphs to help avoid unnecessary shader variants
		// But means if those subgraphs are nested in another, you'll need to copy the keywords from blackboard

	#endif
#endif


void MainLightShadows_float(float3 WorldPos, half4 Shadowmask, out float ShadowAtten)
{
	#ifdef SHADERGRAPH_PREVIEW
			ShadowAtten = 1;
	#else
	#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
			float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
	#else
		float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
	#endif
		ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
	#endif
}

void MainLightShadows_float(float3 WorldPos, out float ShadowAtten)
{
    MainLightShadows_float(WorldPos, half4(1, 1, 1, 1), ShadowAtten);
}

#endif