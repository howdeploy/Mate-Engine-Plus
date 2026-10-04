#ifndef POI_PASS_OUTLINE
#define POI_PASS_OUTLINE

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMacros.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDefines.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_FunctionsArtistic.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_Poicludes.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiHelpers.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBlending.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiPenetration.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVertexManipulations.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiOutlineVert.cginc"
#ifdef TESSELATION
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiTessellation.cginc"
#endif
#ifdef _REQUIRE_UV2
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMirror.cginc"
#endif
#ifdef DISTORT
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDissolve.cginc"
#endif
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiLighting.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMainTex.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiData.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDithering.cginc"
#ifdef _COLOROVERLAY_ON
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDebug.cginc"
#endif
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiOutlineFrag.cginc"
#endif