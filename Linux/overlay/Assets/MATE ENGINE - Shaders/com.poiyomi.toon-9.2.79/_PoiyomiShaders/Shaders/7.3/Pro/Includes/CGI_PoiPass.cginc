/*
USED---------------------------------------------
"_PARALLAXMAP"
"_REQUIRE_UV2"
"_SUNDISK_NONE"
"_DETAIL_MULX2"
"_GLOSSYREFLECTIONS_OFF"
"_METALLICGLOSSMAP"
"_COLORADDSUBDIFF_ON"
"_SPECGLOSSMAP"
"_TERRAIN_NORMAL_MAP"
"_SUNDISK_SIMPLE"
"_EMISSION"
"_COLORCOLOR_ON"
"_COLOROVERLAY_ON"
"_ALPHAMODULATE_ON"
"_SUNDISK_HIGH_QUALITY"
"_MAPPING_6_FRAMES_LAYOUT"
"_NORMALMAP
"EFFECT_BUMP"
"BLOOM"
"BLOOM_LOW"
"GRAIN"
"DEPTH_OF_FIELD"
"USER_LUT"
"CHROMATIC_ABERRATION_LOW"
"BLOOM_LENS_DIRT"
"_FADING_ON"
"CHROMATIC_ABERRATION"
"DISTORT"
"GEOM_TYPE_BRANCH"
"_SPECULARHIGHLIGHTS_OFF"
"_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"
"EFFECT_HUE_VARIATION"
"GEOM_TYPE_LEAF"
"GEOM_TYPE_MESH"
"FINALPASS"
"AUTO_EXPOSURE
"VIGNETTE"
"VIGNETTE_MASKED"
"COLOR_GRADING_HDR"
"COLOR_GRADING_HDR_3D"
"DITHERING"
"VIGNETTE_CLASSIC"
"GEOM_TYPE_BRANCH_DETAIL"
"GEOM_TYPE_FROND"
"DEPTH_OF_FIELD_COC_VIEW"
"COLOR_GRADING_LOG_VIEW"
"TONEMAPPING_CUSTOM"

UNUSED-------------------------------------------
"_ALPHABLEND_ON"
"_ALPHAPREMULTIPLY_ON"
"_ALPHATEST_ON"
"PIXELSNAP_ON"
"TONEMAPPING_FILMIC"
"TONEMAPPING_NEUTRAL"
"TONEMAPPING_ACES"
"COLOR_GRADING"

DO NOT USE -----------------------------------------
"BILLBOARD_FACE_CAMERA_POS"
SOFTPARTICLES_ON
*/


#ifndef POI_PASS
#define POI_PASS

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
#include "UnityShaderVariables.cginc"

#ifdef POI_META_PASS
	#include "UnityMetaPass.cginc"
#endif

//#pragma warning (default : 3206) // implicit truncation

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMacros.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDefines.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_FunctionsArtistic.cginc"

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_Poicludes.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiHelpers.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBlending.cginc"

#ifdef _SUNDISK_NONE
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiRandom.cginc"
#endif

#ifdef _REQUIRE_UV2
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMirror.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiPenetration.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVertexManipulations.cginc"

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSpawnInVert.cginc"

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiV2F.cginc"

#ifdef BLOOM_LOW
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBulge.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVert.cginc"

#ifdef TESSELATION
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiTessellation.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDithering.cginc"

#ifdef _PARALLAXMAP
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiParallax.cginc"
#endif

#ifdef COLOR_GRADING_LOG_VIEW
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiAudioLink.cginc"
#endif

#ifdef USER_LUT
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiUVDistortion.cginc"
#endif

#ifdef VIGNETTE
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiRGBMask.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiData.cginc"

#ifdef _SPECULARHIGHLIGHTS_OFF
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBlackLight.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSpawnInFrag.cginc"

#ifdef WIREFRAME
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiWireframe.cginc"
#endif

#ifdef DISTORT
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDissolve.cginc"
#endif

#ifdef DEPTH_OF_FIELD
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiHologram.cginc"
#endif

#ifdef BLOOM_LENS_DIRT
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiIridescence.cginc"
#endif


#ifdef FUR
	//#include "CGI_PoiFur.cginc"
	//#include "CGI_PoiGeomFur.cginc"
#endif

#ifdef VIGNETTE_MASKED
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiLighting.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMainTex.cginc"

#ifdef TONEMAPPING_CUSTOM
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiPathing.cginc"
#endif

#ifdef GEOM_TYPE_BRANCH
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDecal.cginc"
#endif

#ifdef CHROMATIC_ABERRATION
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVoronoi.cginc"
#endif

#ifdef _DETAIL_MULX2
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiPanosphere.cginc"
#endif

#ifdef EFFECT_BUMP
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMSDF.cginc"
#endif

#ifdef GRAIN
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDepthColor.cginc"
#endif


#ifdef _SUNDISK_HIGH_QUALITY
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiFlipbook.cginc"
#endif

#ifdef _GLOSSYREFLECTIONS_OFF
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiRimLighting.cginc"
#endif

#ifdef _MAPPING_6_FRAMES_LAYOUT
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiEnvironmentalRimLighting.cginc"
#endif

#ifdef VIGNETTE_CLASSIC
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBRDF.cginc"
#endif

#ifdef _METALLICGLOSSMAP
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMetal.cginc"
#endif

#ifdef _COLORADDSUBDIFF_ON
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiMatcap.cginc"
#endif

#ifdef _SPECGLOSSMAP
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSpecular.cginc"
#endif

#ifdef BLOOM
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiVideo.cginc"
#endif

#ifdef _TERRAIN_NORMAL_MAP
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiSubsurfaceScattering.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiBlending.cginc"
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiGrab.cginc"

#ifdef _SUNDISK_SIMPLE
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiGlitter.cginc"
#endif

#ifdef _EMISSION
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiEmission.cginc"
#endif

#ifdef _COLORCOLOR_ON
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiClearCoat.cginc"
#endif

#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiAlphaToCoverage.cginc"

#ifdef _COLOROVERLAY_ON
	#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiDebug.cginc"
#endif
#include "Assets/MATE ENGINE - Shaders/com.poiyomi.toon-9.2.79/_PoiyomiShaders/Shaders/7.3/Pro/Includes/CGI_PoiFrag.cginc"

#endif