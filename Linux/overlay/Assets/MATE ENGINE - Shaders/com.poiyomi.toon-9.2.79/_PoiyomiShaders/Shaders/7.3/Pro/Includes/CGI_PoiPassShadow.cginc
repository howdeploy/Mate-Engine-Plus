#ifndef POI_PASS_SHADOW
    #define POI_PASS_SHADOW
    
    #pragma multi_compile_shadowcaster
    #include "UnityCG.cginc"
    #include "UnityShaderVariables.cginc"
    #include "UnityCG.cginc"
    #include "Lighting.cginc"
    #include "UnityPBSLighting.cginc"
    #include "AutoLight.cginc"
    
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMacros.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDefines.cginc"
    
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_Poicludes.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiShadowIncludes.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiHelpers.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMirror.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSpawnInFrag.cginc"
    
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiV2F.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiData.cginc"
    
    #ifdef WIREFRAME
        #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiWireframe.cginc"
    #endif
    
    #ifdef _SUNDISK_HIGH_QUALITY
        #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiFlipbook.cginc"
    #endif
    
    #ifdef _SUNDISK_NONE
        #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiRandom.cginc"
    #endif
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDithering.cginc"
    #ifdef DISTORT
        #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDissolve.cginc"
    #endif
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiPenetration.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVertexManipulations.cginc"
    
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSpawnInVert.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiShadowVert.cginc"
    #include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiShadowFrag.cginc"
    
#endif