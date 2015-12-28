//Code by Bac9
//Heavily modified by Lithane
using KSP;
using KSP.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace WingProcedural
{
    public class WingProcedural : PartModule, IPartCostModifier, IPartSizeModifier, IPartMassModifier
    {
        // Some handy bools
        [KSPField] public bool isCtrlSrf = false;
        [KSPField] public bool isWingAsCtrlSrf = false;
        [KSPField] public bool isPanel = false;

        [KSPField (isPersistant = true)]
        public bool isAttached = false;
        [KSPField (isPersistant = true)]
        public bool isSetToDefaultValues = false;

        #region Debug

        private struct DebugMessage
        {
            public string message;
            public string interval;

            public DebugMessage (string m, string i)
            {
                message = m;
                interval = i;
            }
        }

        private DateTime debugTime;
        private DateTime debugTimeLast;
        private List<DebugMessage> debugMessageList = new List<DebugMessage> ();

        private void DebugTimerUpdate ()
        {
            debugTime = DateTime.UtcNow;
        }

        private void DebugLogWithID (string method, string message)
        {
            debugTime = DateTime.UtcNow;
            string m = "WP | ID: " + part.gameObject.GetInstanceID () + " | " + method + " | " + message;
            string i = (debugTime - debugTimeLast).TotalMilliseconds + " ms.";
            if (debugMessageList.Count <= 150) debugMessageList.Add (new DebugMessage (m, i));
            debugTimeLast = DateTime.UtcNow;
            Debug.Log (m);
        }

        private string DebugVectorToString (Vector3 v)
        {
            return v.x.ToString ("F2") + ", " + v.y.ToString ("F2") + ", " + v.z.ToString ("F2");
        }

        ArrowPointer pointer;
        void DrawArrow(Vector3 dir)
        {
            if (pointer == null)
                pointer = ArrowPointer.Create(part.partTransform, Vector3.zero, dir, 30, Color.red, true);
            else
                pointer.Direction = dir;
        }

        void destroyArrow()
        {
            if (pointer != null)
            {
                Destroy(pointer);
                pointer = null;
            }
        }

        #endregion

        #region Mesh properties

        [System.Serializable]
        public class MeshReference
        {
            public Vector3[] vp;
            public Vector3[] nm;
            public Vector2[] uv;
        }

        public MeshFilter meshFilterWingSection;
        public MeshFilter meshFilterWingSurface;
        public List<MeshFilter> meshFiltersWingEdgeTrailing = new List<MeshFilter> ();
        public List<MeshFilter> meshFiltersWingEdgeLeading = new List<MeshFilter> ();

        public MeshFilter meshFilterCtrlFrame;
        public MeshFilter meshFilterCtrlSurface;
        public List<MeshFilter> meshFiltersCtrlEdge = new List<MeshFilter> ();

        public static MeshReference meshReferenceWingSection;
        public static MeshReference meshReferenceWingSurface;
        public static List<MeshReference> meshReferencesWingEdge = new List<MeshReference> ();

        public static MeshReference meshReferenceCtrlFrame;
        public static MeshReference meshReferenceCtrlSurface;
        public static List<MeshReference> meshReferencesCtrlEdge = new List<MeshReference> ();

        private static int meshTypeCountEdgeWing = 4;
        private static int meshTypeCountEdgeCtrl = 3;
        #endregion

        #region Shared properties / Limits and increments
        private Vector2 GetLimitsFromType (Vector4 set)
        {
            if (WPDebug.logLimits)
                DebugLogWithID ("GetLimitsFromType", "Using set: " + set);
            if (!isCtrlSrf)
                return new Vector2 (set.x, set.y);
            else
                return new Vector2 (set.z, set.w);
        }

        private float GetIncrementFromType (float incrementWing, float incrementCtrl)
        {
            if (!isCtrlSrf)
                return incrementWing;
            else
                return incrementCtrl;
        }
        
        private static Vector4 sharedBaseLengthLimits = new Vector4 (0.125f, 16f, 0.04f, 8f);
        private static Vector2 sharedBaseThicknessLimits = new Vector2 (0.04f, 1f);
        private static Vector4 sharedBaseWidthRootLimits = new Vector4 (0.125f, 16f, 0.04f, 1.6f);
        private static Vector4 sharedBaseWidthTipLimits = new Vector4(0.0001f, 16f, 0.04f, 1.6f);
        private static Vector4 sharedBaseOffsetLimits = new Vector4 (-8f, 8f, -2f, 2f);
        private static Vector4 sharedEdgeTypeLimits = new Vector4 (1f, 4f, 1f, 3f);
        private static Vector4 sharedEdgeWidthLimits = new Vector4 (0f, 1f, 0f, 1f);
        private static Vector2 sharedMaterialLimits = new Vector2 (0f, 4f);
        private static Vector2 sharedColorLimits = new Vector2 (0f, 1f);
        
        private static float sharedIncrementColor = 0.01f;
        private static float sharedIncrementColorLarge = 0.10f;
        private static float sharedIncrementMain = 0.125f;
        //private static float sharedIncrementPrecise = 0.001f;
        private static float sharedIncrementSmall = 0.04f;
        private static float sharedIncrementInt = 1f;
        public int dummyValueInt = 0;
        #endregion

        #region Shared properties / Base

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Base")]
        public static bool sharedFieldGroupBaseStatic = true;
        private static string[] sharedFieldGroupBaseArray = new string[] { "sharedBaseLength", "sharedBaseWidthRoot", "sharedBaseWidthTip", "sharedBaseThicknessRoot", "sharedBaseThicknessTip", "sharedBaseOffsetTip" };
        private static string[] sharedFieldGroupBaseArrayCtrl = new string[] { "sharedBaseOffsetRoot" };

        [KSPField (isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Length", guiFormat = "S4")]
        public float sharedBaseLength = 4f;
        public float sharedBaseLengthCached = 4f;
        public static Vector4 sharedBaseLengthDefaults = new Vector4 (4f, 1f, 4f, 1f);
        public int sharedBaseLengthInt = 0;

        [KSPField (isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Width (root)", guiFormat = "S4")]
        public float sharedBaseWidthRoot = 4f;
        public float sharedBaseWidthRootCached = 4f;
        public static Vector4 sharedBaseWidthRootDefaults = new Vector4 (4f, 0.5f, 4f, 0.5f);
        public int sharedBaseWidthRInt = 0;

        [KSPField (isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Width (tip)", guiFormat = "S4")]
        public float sharedBaseWidthTip = 4f;
        public float sharedBaseWidthTipCached = 4f;
        public static Vector4 sharedBaseWidthTipDefaults = new Vector4 (4f, 0.5f, 4f, 0.5f);
        public int sharedBaseWidthTInt = 0;

        [KSPField (isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Offset (root)", guiFormat = "S4")]
        public float sharedBaseOffsetRoot = 0f;
        public float sharedBaseOffsetRootCached = 0f;
        public static Vector4 sharedBaseOffsetRootDefaults = new Vector4 (0f, 0f, 0f, 0f);
        public int sharedBaseOffsetRInt = 0;

        [KSPField (isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Offset (tip)", guiFormat = "S4")]
        public float sharedBaseOffsetTip = 0f;
        public float sharedBaseOffsetTipCached = 0f;
        public static Vector4 sharedBaseOffsetTipDefaults = new Vector4 (0f, 0f, 0f, 0f);
        public int sharedBaseOffsetTInt = 0;

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Thickness (root)", guiFormat = "F3")]
        public float sharedBaseThicknessRoot = 0.24f;
        public float sharedBaseThicknessRootCached = 0.24f;
        public static Vector4 sharedBaseThicknessRootDefaults = new Vector4 (0.24f, 0.24f, 0.24f, 0.24f);
        public int sharedBaseThicknessRInt = 0;

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Thickness (tip)", guiFormat = "F3")]
        public float sharedBaseThicknessTip = 0.24f;
        public float sharedBaseThicknessTipCached = 0.24f;
        public static Vector4 sharedBaseThicknessTipDefaults = new Vector4 (0.24f, 0.24f, 0.24f, 0.24f);
        public int sharedBaseThicknessTInt = 0;



        #endregion

        #region Shared properties / Edge / Leading

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Lead. edge")] 
        public static bool sharedFieldGroupEdgeLeadingStatic = false;
        private static string[] sharedFieldGroupEdgeLeadingArray = new string[] { "sharedEdgeTypeLeading", "sharedEdgeWidthLeadingRoot", "sharedEdgeWidthLeadingTip" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Shape", guiFormat = "F3")]
        public float sharedEdgeTypeLeading = 2f;
        public float sharedEdgeTypeLeadingCached = 2f;
        public static Vector4 sharedEdgeTypeLeadingDefaults = new Vector4 (2f, 1f, 2f, 1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Width (root)", guiFormat = "F3")]
        public float sharedEdgeWidthLeadingRoot = 0.24f;
        public float sharedEdgeWidthLeadingRootCached = 0.24f;
        public static Vector4 sharedEdgeWidthLeadingRootDefaults = new Vector4 (0.24f, 0.24f, 0.24f, 0.24f);
        public int sharedEdgeWidthLRInt = 0;

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Width (tip)", guiFormat = "F3")]
        public float sharedEdgeWidthLeadingTip = 0.24f;
        public float sharedEdgeWidthLeadingTipCached = 0.24f;
        public static Vector4 sharedEdgeWidthLeadingTipDefaults = new Vector4 (0.24f, 0.24f, 0.24f, 0.24f);
        public int sharedEdgeWidthLTInt = 0;

        #endregion

        #region Shared properties / Edge / Trailing

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Trail. edge")]
        public static bool sharedFieldGroupEdgeTrailingStatic = false;
        private static string[] sharedFieldGroupEdgeTrailingArray = new string[] { "sharedEdgeTypeTrailing", "sharedEdgeWidthTrailingRoot", "sharedEdgeWidthTrailingTip" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Shape", guiFormat = "F3")]
        public float sharedEdgeTypeTrailing = 3f;
        public float sharedEdgeTypeTrailingCached = 3f;
        public static Vector4 sharedEdgeTypeTrailingDefaults = new Vector4 (3f, 2f, 3f, 2f);

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Width (root)", guiFormat = "F3")]
        public float sharedEdgeWidthTrailingRoot = 0.48f;
        public float sharedEdgeWidthTrailingRootCached = 0.48f;
        public static Vector4 sharedEdgeWidthTrailingRootDefaults = new Vector4(0.48f, 0.48f, 0.48f, 0.48f);
        public int sharedEdgeWidthTRInt = 0;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Width (tip)", guiFormat = "F3")]
        public float sharedEdgeWidthTrailingTip = 0.48f;
        public float sharedEdgeWidthTrailingTipCached = 0.48f;
        public static Vector4 sharedEdgeWidthTrailingTipDefaults = new Vector4(0.48f, 0.48f, 0.48f, 0.48f);
        public int sharedEdgeWidthTTInt = 0;

        #endregion

        #region Shared properties / Surface / Top

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Material A")]
        public static bool sharedFieldGroupColorSTStatic = false;
        private static string[] sharedFieldGroupColorSTArray = new string[] { "sharedMaterialST", "sharedColorSTOpacity", "sharedColorSTHue", "sharedColorSTSaturation", "sharedColorSTBrightness" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Material", guiFormat = "F3")]
        public float sharedMaterialST = 1f;
        public float sharedMaterialSTCached = 1f;
        public static Vector4 sharedMaterialSTDefaults = new Vector4 (1f, 1f, 1f, 1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Opacity", guiFormat = "F3")]
        public float sharedColorSTOpacity = 0f;
        public float sharedColorSTOpacityCached = 0f;
        public static Vector4 sharedColorSTOpacityDefaults = new Vector4 (0f, 0f, 0f, 0f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (H)", guiFormat = "F3")]
        public float sharedColorSTHue = 0.10f;
        public float sharedColorSTHueCached = 0.10f;
        public static Vector4 sharedColorSTHueDefaults = new Vector4 (0.1f, 0.1f, 0.1f, 0.1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (S)", guiFormat = "F3")]
        public float sharedColorSTSaturation = 0.75f;
        public float sharedColorSTSaturationCached = 0.75f;
        public static Vector4 sharedColorSTSaturationDefaults = new Vector4 (0.75f, 0.75f, 0.75f, 0.75f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (B)", guiFormat = "F3")]
        public float sharedColorSTBrightness = 0.6f;
        public float sharedColorSTBrightnessCached = 0.6f;
        public static Vector4 sharedColorSTBrightnessDefaults = new Vector4 (0.6f, 0.6f, 0.6f, 0.6f);

        #endregion

        #region Shared properties / Surface / bottom

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Material B")]
        public static bool sharedFieldGroupColorSBStatic = false;
        private static string[] sharedFieldGroupColorSBArray = new string[] { "sharedMaterialSB", "sharedColorSBOpacity", "sharedColorSBHue", "sharedColorSBSaturation", "sharedColorSBBrightness" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Material", guiFormat = "F3")]
        public float sharedMaterialSB = 4f;
        public float sharedMaterialSBCached = 4f;
        public static Vector4 sharedMaterialSBDefaults = new Vector4 (4f, 4f, 4f, 4f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Opacity", guiFormat = "F3")]
        public float sharedColorSBOpacity = 0f;
        public float sharedColorSBOpacityCached = 0f;
        public static Vector4 sharedColorSBOpacityDefaults = new Vector4 (0f, 0f, 0f, 0f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (H)", guiFormat = "F3")]
        public float sharedColorSBHue = 0.10f;
        public float sharedColorSBHueCached = 0.10f;
        public static Vector4 sharedColorSBHueDefaults = new Vector4 (0.1f, 0.1f, 0.1f, 0.1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (S)", guiFormat = "F3")]
        public float sharedColorSBSaturation = 0.75f;
        public float sharedColorSBSaturationCached = 0.75f;
        public static Vector4 sharedColorSBSaturationDefaults = new Vector4 (0.75f, 0.75f, 0.75f, 0.75f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (B)", guiFormat = "F3")]
        public float sharedColorSBBrightness = 0.6f;
        public float sharedColorSBBrightnessCached = 0.6f;
        public static Vector4 sharedColorSBBrightnessDefaults = new Vector4 (0.6f, 0.6f, 0.6f, 0.6f);
        #endregion

        #region Shared properties / Surface / trailing edge

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Material T")]
        public static bool sharedFieldGroupColorETStatic = false;
        private static string[] sharedFieldGroupColorETArray = new string[] { "sharedMaterialET", "sharedColorETOpacity", "sharedColorETHue", "sharedColorETSaturation", "sharedColorETBrightness" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Material", guiFormat = "F3")]
        public float sharedMaterialET = 4f;
        public float sharedMaterialETCached = 4f;
        public static Vector4 sharedMaterialETDefaults = new Vector4 (4f, 4f, 4f, 4f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Opacity", guiFormat = "F3")]
        public float sharedColorETOpacity = 0f;
        public float sharedColorETOpacityCached = 0f;
        public static Vector4 sharedColorETOpacityDefaults = new Vector4 (0f, 0f, 0f, 0f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (H)", guiFormat = "F3")]
        public float sharedColorETHue = 0.10f;
        public float sharedColorETHueCached = 0.10f;
        public static Vector4 sharedColorETHueDefaults = new Vector4 (0.1f, 0.1f, 0.1f, 0.1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (S)", guiFormat = "F3")]
        public float sharedColorETSaturation = 0.75f;
        public float sharedColorETSaturationCached = 0.75f;
        public static Vector4 sharedColorETSaturationDefaults = new Vector4 (0.75f, 0.75f, 0.75f, 0.75f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (B)", guiFormat = "F3")]
        public float sharedColorETBrightness = 0.6f;
        public float sharedColorETBrightnessCached = 0.6f;
        public static Vector4 sharedColorETBrightnessDefaults = new Vector4 (0.6f, 0.6f, 0.6f, 0.6f);

        #endregion

        #region Shared properties / Surface / leading edge

        [KSPField (guiActiveEditor = false, guiActive = false, guiName = "| Material L")]
        public static bool sharedFieldGroupColorELStatic = false;
        private static string[] sharedFieldGroupColorELArray = new string[] { "sharedMaterialEL", "sharedColorELOpacity", "sharedColorELHue", "sharedColorELSaturation", "sharedColorELBrightness" };

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Material", guiFormat = "F3")]
        public float sharedMaterialEL = 4f;
        public float sharedMaterialELCached = 4f;
        public static Vector4 sharedMaterialELDefaults = new Vector4 (4f, 4f, 4f, 4f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Opacity", guiFormat = "F3")]
        public float sharedColorELOpacity = 0f;
        public float sharedColorELOpacityCached = 0f;
        public static Vector4 sharedColorELOpacityDefaults = new Vector4 (0f, 0f, 0f, 0f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (H)", guiFormat = "F3")]
        public float sharedColorELHue = 0.10f;
        public float sharedColorELHueCached = 0.10f;
        public static Vector4 sharedColorELHueDefaults = new Vector4 (0.1f, 0.1f, 0.1f, 0.1f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (S)", guiFormat = "F3")]
        public float sharedColorELSaturation = 0.75f;
        public float sharedColorELSaturationCached = 0.75f;
        public static Vector4 sharedColorELSaturationDefaults = new Vector4 (0.75f, 0.75f, 0.75f, 0.75f);

        [KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Color (B)", guiFormat = "F3")]
        public float sharedColorELBrightness = 0.6f;
        public float sharedColorELBrightnessCached = 0.6f;
        public static Vector4 sharedColorELBrightnessDefaults = new Vector4 (0.6f, 0.6f, 0.6f, 0.6f);

        #endregion

        #region Shared properties / Misc + Angles
        //Angles
        private static Vector2 sharedSweptAngleLimits = new Vector2(1f, 180f);

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Swept angle(front)", guiFormat = "F3")]
        public float sharedSweptAngleFront = 90f;
        public float sharedSweptAngleFrontCached = 90f;
        public static Vector4 sharedSweptAngleFrontCachedDefaults = new Vector4(90f, 90f, 90f, 90f);

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Swept angle(back)", guiFormat = "F3")]
        public float sharedSweptAngleBack = 90f;
        public float sharedSweptAngleBackCached = 90f;
        public static Vector4 sharedSweptAngleBackDefaults = new Vector4(90f, 90f, 90f, 90f);
        
        
        //Prefs
        public static bool sharedFieldPrefStatic = true;
        public static bool sharedPropAnglePref = false;
        public static bool sharedPropEdgePref = false;
        public static bool sharedPropEThickPref = false;


        #endregion

        #region Default values
        // Vector4 (defaultWing, defaultCtrl, defaultWingBackup, defaultCtrlBackup)

        private void ReplaceDefaults ()
        {
            ReplaceDefault (ref sharedBaseLengthDefaults, sharedBaseLength);
            ReplaceDefault (ref sharedBaseWidthRootDefaults, sharedBaseWidthRoot);
            ReplaceDefault (ref sharedBaseWidthTipDefaults, sharedBaseWidthTip);
            ReplaceDefault (ref sharedBaseOffsetRootDefaults, sharedBaseOffsetRoot);
            ReplaceDefault (ref sharedBaseOffsetTipDefaults, sharedBaseOffsetTip);
            ReplaceDefault (ref sharedBaseThicknessRootDefaults, sharedBaseThicknessRoot);
            ReplaceDefault (ref sharedBaseThicknessTipDefaults, sharedBaseThicknessTip);

            ReplaceDefault (ref sharedEdgeTypeLeadingDefaults, sharedEdgeTypeLeading);
            ReplaceDefault (ref sharedEdgeWidthLeadingRootDefaults, sharedEdgeWidthLeadingRoot);
            ReplaceDefault (ref sharedEdgeWidthLeadingTipDefaults, sharedEdgeWidthLeadingTip);

            ReplaceDefault (ref sharedEdgeTypeTrailingDefaults, sharedEdgeTypeTrailing);
            ReplaceDefault (ref sharedEdgeWidthTrailingRootDefaults, sharedEdgeWidthTrailingRoot);
            ReplaceDefault (ref sharedEdgeWidthTrailingTipDefaults, sharedEdgeWidthTrailingTip);

            ReplaceDefault (ref sharedMaterialSTDefaults, sharedMaterialST);
            ReplaceDefault (ref sharedColorSTOpacityDefaults, sharedColorSTOpacity);
            ReplaceDefault (ref sharedColorSTHueDefaults, sharedColorSTHue);
            ReplaceDefault (ref sharedColorSTSaturationDefaults, sharedColorSTSaturation);
            ReplaceDefault (ref sharedColorSTBrightnessDefaults, sharedColorSTBrightness);

            ReplaceDefault (ref sharedMaterialSBDefaults, sharedMaterialSB);
            ReplaceDefault (ref sharedColorSBOpacityDefaults, sharedColorSBOpacity);
            ReplaceDefault (ref sharedColorSBHueDefaults, sharedColorSBHue);
            ReplaceDefault (ref sharedColorSBSaturationDefaults, sharedColorSBSaturation);
            ReplaceDefault (ref sharedColorSBBrightnessDefaults, sharedColorSBBrightness);

            ReplaceDefault (ref sharedMaterialETDefaults, sharedMaterialET);
            ReplaceDefault (ref sharedColorETOpacityDefaults, sharedColorETOpacity);
            ReplaceDefault (ref sharedColorETHueDefaults, sharedColorETHue);
            ReplaceDefault (ref sharedColorETSaturationDefaults, sharedColorETSaturation);
            ReplaceDefault (ref sharedColorETBrightnessDefaults, sharedColorETBrightness);

            ReplaceDefault (ref sharedMaterialELDefaults, sharedMaterialEL);
            ReplaceDefault (ref sharedColorELOpacityDefaults, sharedColorELOpacity);
            ReplaceDefault (ref sharedColorELHueDefaults, sharedColorELHue);
            ReplaceDefault (ref sharedColorELSaturationDefaults, sharedColorELSaturation);
            ReplaceDefault (ref sharedColorELBrightnessDefaults, sharedColorELBrightness);
        }

        private void ReplaceDefault (ref Vector4 set, float value)
        {
            if (!isCtrlSrf)
                set = new Vector4 (value, set.w, set.z, set.w);
            else
                set = new Vector4 (set.z, value, set.z, set.w);
        }

        private void RestoreDefaults ()
        {
            RestoreDefault (ref sharedBaseLengthDefaults);
            RestoreDefault (ref sharedBaseWidthRootDefaults);
            RestoreDefault (ref sharedBaseWidthTipDefaults);
            RestoreDefault (ref sharedBaseOffsetRootDefaults);
            RestoreDefault (ref sharedBaseOffsetTipDefaults);
            RestoreDefault (ref sharedBaseThicknessRootDefaults);
            RestoreDefault (ref sharedBaseThicknessTipDefaults);

            RestoreDefault (ref sharedEdgeTypeLeadingDefaults);
            RestoreDefault (ref sharedEdgeWidthLeadingRootDefaults);
            RestoreDefault (ref sharedEdgeWidthLeadingTipDefaults);

            RestoreDefault (ref sharedEdgeTypeTrailingDefaults);
            RestoreDefault (ref sharedEdgeWidthTrailingRootDefaults);
            RestoreDefault (ref sharedEdgeWidthTrailingTipDefaults);

            RestoreDefault (ref sharedMaterialSTDefaults);
            RestoreDefault (ref sharedColorSTOpacityDefaults);
            RestoreDefault (ref sharedColorSTHueDefaults);
            RestoreDefault (ref sharedColorSTSaturationDefaults);
            RestoreDefault (ref sharedColorSTBrightnessDefaults);

            RestoreDefault (ref sharedMaterialSBDefaults);
            RestoreDefault (ref sharedColorSBOpacityDefaults);
            RestoreDefault (ref sharedColorSBHueDefaults);
            RestoreDefault (ref sharedColorSBSaturationDefaults);
            RestoreDefault (ref sharedColorSBBrightnessDefaults);

            RestoreDefault (ref sharedMaterialETDefaults);
            RestoreDefault (ref sharedColorETOpacityDefaults);
            RestoreDefault (ref sharedColorETHueDefaults);
            RestoreDefault (ref sharedColorETSaturationDefaults);
            RestoreDefault (ref sharedColorETBrightnessDefaults);

            RestoreDefault (ref sharedMaterialELDefaults);
            RestoreDefault (ref sharedColorELOpacityDefaults);
            RestoreDefault (ref sharedColorELHueDefaults);
            RestoreDefault (ref sharedColorELSaturationDefaults);
            RestoreDefault (ref sharedColorELBrightnessDefaults);
        }

        private void RestoreDefault (ref Vector4 set)
        {
            set = new Vector4 (set.z, set.w, set.z, set.w);
        }

        private float GetDefault (Vector4 set)
        {
            if (!isCtrlSrf) return set.x;
            else return set.y;
        }
        #endregion

        #region Fuel configuration switching
        // Has to be situated here as this KSPEvent is not correctly added Part.Events otherwise
        [KSPEvent (guiActive = false, guiActiveEditor = true, guiName = "Next configuration", active = true)]
        public void NextConfiguration ()
        {
            if (WPDebug.logFuel)
                DebugLogWithID ("NextConfiguration", "Started");
            
            if (!(canBeFueled && useStockFuel))
                return;
            
            fuelSelectedTankSetup++;

            if (fuelSelectedTankSetup >= fuelConfigurationsList.Count)
                fuelSelectedTankSetup = 0;
            if (HighLogic.LoadedSceneIsFlight)
                fuelCurrentAmount = Vector4.zero;

            FuelSetConfigurationToParts (true);
        }
        #endregion

        #region Inheritance
        private bool inheritancePossibleOnShape = false;
        private bool inheritancePossibleOnMaterials = false;
        private void InheritanceStatusUpdate ()
        {
            if (this.part.parent == null)
                return;

            if (!part.parent.Modules.Contains("WingProcedural"))
                return;

            WingProcedural parentModule = part.parent.Modules.OfType<WingProcedural> ().FirstOrDefault ();
            if (parentModule != null)
            {
                if (!parentModule.isCtrlSrf)
                {
                    inheritancePossibleOnMaterials = true;
                    if (!isCtrlSrf)
                        inheritancePossibleOnShape = true;
                }
            }
        }

        private void InheritParentValues (int mode)
        {
            if (this.part.parent == null)
                return;

            if (!part.parent.Modules.Contains("WingProcedural"))
                return;

            WingProcedural parentModule = part.parent.Modules.OfType<WingProcedural> ().FirstOrDefault ();
            if (parentModule == null)
                return;

            switch (mode)
            {
                case 0:
                    inheritShape(parentModule);
                    break;
                case 1:
                    inheritBase(parentModule);
                    break;
                case 2:
                    inheritEdges(parentModule);
                    break;
                case 3:
                    inheritColours(parentModule);
                    break;
            }
        }

        private void inheritShape(WingProcedural parent)
        {
            if (parent.isCtrlSrf || isCtrlSrf)
                return;

            if (Input.GetMouseButtonUp(0))
                inheritBase(parent);
            sharedBaseThicknessRoot = parent.sharedBaseThicknessTip;

            float tip = sharedBaseWidthRoot + ((parent.sharedBaseWidthTip - parent.sharedBaseWidthRoot) / (parent.sharedBaseLength)) * sharedBaseLength;
            if (sharedBaseWidthTip < 0)
                sharedBaseLength *= sharedBaseWidthRoot  / (sharedBaseWidthRoot - sharedBaseWidthTip);
            //else if (sharedBaseWidthTip > sharedBaseWidthTipLimits.y)
            //    sharedBaseLength *= sharedBaseWidthTipLimits.y / sharedBaseWidthTip;

            float offset = sharedBaseLength / parent.sharedBaseLength * parent.sharedBaseOffsetTip;
            /*
            if (offset > sharedBaseOffsetLimits.y)
                sharedBaseLength *= sharedBaseOffsetLimits.y / offset;
            else if (offset < sharedBaseOffsetLimits.x)
                sharedBaseLength *= sharedBaseOffsetLimits.x / offset;
                */
            
            //sharedBaseLength = Mathf.Clamp(sharedBaseLength, sharedBaseLengthLimits.x, sharedBaseLengthLimits.y);
            sharedBaseWidthTip =tip;
            sharedBaseOffsetTip = offset;
                //Mathf.Clamp(offset, sharedBaseOffsetLimits.x, sharedBaseOffsetLimits.y);
            sharedBaseThicknessTip = min(sharedBaseThicknessRoot + (float)(sharedBaseLength / parent.sharedBaseLength) * (float)(parent.sharedBaseThicknessTip - parent.sharedBaseThicknessRoot), 0);

            if (Input.GetMouseButtonUp(0))
                inheritEdges(parent);
        }

        private float min(float v1, float v2)
        {
            if (v1 <= v2)
                return v1;
            else
                return v2;
        }

        private void inheritBase(WingProcedural parent)
        {
            if (parent.isCtrlSrf || isCtrlSrf)
                return;

            sharedBaseWidthRoot = parent.sharedBaseWidthTip;
            sharedBaseThicknessRoot = parent.sharedBaseThicknessTip;

            sharedEdgeTypeLeading = parent.sharedEdgeTypeLeading;
            sharedEdgeWidthLeadingRoot = parent.sharedEdgeWidthLeadingTip;

            sharedEdgeTypeTrailing = parent.sharedEdgeTypeTrailing;
            sharedEdgeWidthTrailingRoot = parent.sharedEdgeWidthTrailingTip;
        }

        private void inheritEdges(WingProcedural parent)
        {
            if (parent.isCtrlSrf || isCtrlSrf)
                return;

            sharedEdgeTypeLeading = parent.sharedEdgeTypeLeading;
            sharedEdgeWidthLeadingRoot = parent.sharedEdgeWidthLeadingTip;
            sharedEdgeWidthLeadingTip = Mathf.Clamp(sharedEdgeWidthLeadingRoot + ((parent.sharedEdgeWidthLeadingTip - parent.sharedEdgeWidthLeadingRoot) / parent.sharedBaseLength) * sharedBaseLength, sharedEdgeWidthLimits.x, sharedEdgeWidthLimits.y);

            sharedEdgeTypeTrailing = parent.sharedEdgeTypeTrailing;
            sharedEdgeWidthTrailingRoot = parent.sharedEdgeWidthTrailingTip;
            sharedEdgeWidthTrailingTip = Mathf.Clamp(sharedEdgeWidthTrailingRoot + ((parent.sharedEdgeWidthTrailingTip - parent.sharedEdgeWidthTrailingRoot) / parent.sharedBaseLength) * sharedBaseLength, sharedEdgeWidthLimits.x, sharedEdgeWidthLimits.y);
        }

        private void inheritColours(WingProcedural parent)
        {
            sharedMaterialST = parent.sharedMaterialST;
            sharedColorSTOpacity = parent.sharedColorSTOpacity;
            sharedColorSTHue = parent.sharedColorSTHue;
            sharedColorSTSaturation = parent.sharedColorSTSaturation;
            sharedColorSTBrightness = parent.sharedColorSTBrightness;

            sharedMaterialSB = parent.sharedMaterialSB;
            sharedColorSBOpacity = parent.sharedColorSBOpacity;
            sharedColorSBHue = parent.sharedColorSBHue;
            sharedColorSBSaturation = parent.sharedColorSBSaturation;
            sharedColorSBBrightness = parent.sharedColorSBBrightness;

            sharedMaterialET = parent.sharedMaterialET;
            sharedColorETOpacity = parent.sharedColorETOpacity;
            sharedColorETHue = parent.sharedColorETHue;
            sharedColorETSaturation = parent.sharedColorETSaturation;
            sharedColorETBrightness = parent.sharedColorETBrightness;

            sharedMaterialEL = parent.sharedMaterialEL;
            sharedColorELOpacity = parent.sharedColorELOpacity;
            sharedColorELHue = parent.sharedColorELHue;
            sharedColorELSaturation = parent.sharedColorELSaturation;
            sharedColorELBrightness = parent.sharedColorELBrightness;
        }

        #endregion

        #region Mod detection

        public static bool assembliesChecked = false;
        public static bool assemblyFARUsed = false;
        public static bool assemblyDREUsed = false;
        public static bool assemblyRFUsed = false;
        public static bool assemblyMFTUsed = false;

        public void CheckAssemblies (bool forced)
        {
            if (!assembliesChecked || forced)
            {
                assemblyFARUsed = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("FerramAerospaceResearch", StringComparison.InvariantCultureIgnoreCase));
                assemblyRFUsed = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("RealFuels", StringComparison.InvariantCultureIgnoreCase));
                assemblyMFTUsed = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("modularFuelTanks", StringComparison.InvariantCultureIgnoreCase));
                assemblyDREUsed = AssemblyLoader.loadedAssemblies.Any (a => a.assembly.GetName ().Name.Equals ("DeadlyReentry", StringComparison.InvariantCultureIgnoreCase));

                if (WPDebug.logEvents)
                    DebugLogWithID ("CheckAssemblies", "Search results | FAR: " + assemblyFARUsed + " | DRE: " + assemblyDREUsed + " | RF: " + assemblyRFUsed + " | MFT: " + assemblyMFTUsed);
                if (isCtrlSrf && isWingAsCtrlSrf && WPDebug.logEvents)
                    DebugLogWithID ("CheckAssemblies", "WARNING | PART IS CONFIGURED INCORRECTLY, BOTH BOOL PROPERTIES SHOULD NEVER BE SET TO TRUE");
                if (assemblyRFUsed && assemblyMFTUsed && WPDebug.logEvents) 
                    DebugLogWithID ("CheckAssemblies", "WARNING | Both RF and MFT mods detected, this should not be the case");
                assembliesChecked = true;
            }
        }
        #endregion

        #region Unity stuff and Callbacks/events

        public bool isStarted = false;
        /// <summary>
        /// run when part is created in editor, and when part is created in flight. Why is OnStart and Start both being used other than to sparate flight and editor startup?
        /// </summary>
        public override void OnStart (PartModule.StartState state)
        {
            if (WPDebug.logEvents)
                DebugLogWithID ("OnStart", "Invoked");
            base.OnStart (state);
            CheckAssemblies (false);
            
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            
            DebugLogWithID ("OnStart", "Setup started");
            StartCoroutine (SetupReorderedForFlight ()); // does all setup neccesary for flight
            isStarted = true;
            GameEvents.onGameSceneLoadRequested.Add(OnSceneSwitch);
        }

        /// <summary>
        /// run whenever part is created (used in editor), which in the editor is as soon as part list is clicked or symmetry count increases
        /// </summary>
        public void Start ()
        {
            if (WPDebug.logEvents)
                DebugLogWithID ("Start", "Invoked");

            if (!HighLogic.LoadedSceneIsEditor)
                return;

            uiInstanceIDLocal = uiInstanceIDTarget = 0;

            Setup();
            this.part.OnEditorAttach += new Callback(UpdateOnEditorAttach);
            this.part.OnEditorDetach += new Callback(UpdateOnEditorDetach);

            if (!WingProceduralManager.uiStyleConfigured)
                WingProceduralManager.ConfigureStyles ();
            isStarted = true;
        }

        // unnecesary save/load. config is static so it will be initialised as you pass through the space center, and there is no way to change options in the editor scene
        // may resolve errors reported by Hodo
        public override void OnSave(ConfigNode node)
        {
            if (WPDebug.logEvents)
                DebugLogWithID("OnSave", "Invoked");
            try
            {
                vesselList.FirstOrDefault(vs => vs.vessel == vessel).isUpdated = false;
                WingProceduralManager.SaveConfigs();
            }
            catch
            {
                Debug.Log("B9 PWings - Failed to save settings");
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (WPDebug.logEvents)
                DebugLogWithID("OnLoad", "Invoked");
            //WingProceduralManager.LoadConfigs();
        }

        public void OnDestroy()
        {
            editorAppDestroy();
            GameEvents.onGameSceneLoadRequested.Remove(OnSceneSwitch);
        }

        public void CalcBase()
        {
            sharedBaseWidthTip = sharedBaseWidthRoot - 1 / (float)(Math.Tan(Mathf.Deg2Rad * sharedSweptAngleFront)) * sharedBaseLength + 1 / (float)(Math.Tan(Mathf.Deg2Rad * sharedSweptAngleBack)) * sharedBaseLength;
            sharedBaseOffsetTip = ( 1 / (float)(Math.Tan(Mathf.Deg2Rad * sharedSweptAngleFront)) * sharedBaseLength + 1 / (float)(Math.Tan(Mathf.Deg2Rad * sharedSweptAngleBack)) * sharedBaseLength ) / 2;
        }

        public void CalcAngle()
        {
            sharedSweptAngleBack = (float)Math.Atan(sharedBaseLength / ( - sharedBaseWidthRoot/2 + sharedBaseWidthTip/2 + sharedBaseOffsetTip)) / Mathf.Deg2Rad;
            sharedSweptAngleFront = (float)Math.Atan(sharedBaseLength / (sharedBaseWidthRoot/2 - sharedBaseWidthTip/2 + sharedBaseOffsetTip)) / Mathf.Deg2Rad;
            if (sharedSweptAngleFront < 0)
                sharedSweptAngleFront += 180;
            if (sharedSweptAngleBack < 0)
                sharedSweptAngleBack += 180;
        }

        public void Update()
        {
            if (canBeFueled)
                FuelOnUpdate();
            if (!HighLogic.LoadedSceneIsEditor || !isStarted)
                return;

            if (sharedPropAnglePref)
                CalcBase();
            else
                CalcAngle();

            DebugTimerUpdate();

            UpdateUI();

            DeformWing();
            bool updateGeo, updateAero;
            CheckAllFieldValues(out updateGeo, out updateAero);

            if (updateGeo)
            {
                UpdateGeometry(updateAero);
                UpdateCounterparts();
            }
        }

        // Attachment handling
        public void UpdateOnEditorAttach ()
        {
            if (WPDebug.logEvents)
                DebugLogWithID("UpdateOnEditorAttach", "Setup started");

            isAttached = true;
            UpdateGeometry(true);
            if (WPDebug.logEvents)
                DebugLogWithID ("UpdateOnEditorAttach", "Setup ended");
        }

        public void UpdateOnEditorDetach ()
        {
            if (this.part.parent != null && this.part.parent.Modules.Contains("WingProcedural"))
            {
                WingProcedural parentModule = this.part.parent.Modules.OfType<WingProcedural>().FirstOrDefault();
                if (parentModule != null)
                {
                    parentModule.CalculateVolume();
                    parentModule.CalculateAerodynamicValues();
                }
            }
            isAttached = false;
            uiEditMode = false;
        }

        public void OnSceneSwitch(GameScenes scene)
        {
            isStarted = false; // fixes nullrefs when switching scenes and things haven't been destroyed yet
        }

        /// <summary>
        /// called by Start routines of editor and flight scenes
        /// </summary>
        public void Setup()
        {
            SetupMeshFilters();
            SetupFields();
            SetupMeshReferences();
            ReportOnMeshReferences();
            FuelStart(); // shifted from Setup() to fix NRE caused by reattaching a single part that wasn't originally mirrored. Shifted back now Setup is called from Start
            RefreshGeometry();
        }

        /// <summary>
        /// called from setup and when updating clones
        /// </summary>
        public void RefreshGeometry()
        {
            UpdateMaterials();
            UpdateGeometry(true);
            UpdateWindow();
        }
        #endregion

        #region Geometry
        
        
        public void UpdateGeometry (bool updateAerodynamics)
        {
            if (WPDebug.logUpdateGeometry)
                DebugLogWithID ("UpdateGeometry", "Started | isCtrlSrf: " + isCtrlSrf);
            
            

            if (!isCtrlSrf)
            {
                float wingThicknessDeviationRoot = sharedBaseThicknessRoot / 0.24f;
                float wingThicknessDeviationTip = sharedBaseThicknessTip / 0.24f;
                float wingWidthTipBasedOffsetTrailing = sharedBaseWidthTip / 2f + sharedBaseOffsetTip;
                float wingWidthTipBasedOffsetLeading = -sharedBaseWidthTip / 2f + sharedBaseOffsetTip;
                float wingWidthRootBasedOffset = sharedBaseWidthRoot / 2f;
            
                // First, wing cross section
                // No need to filter vertices by normals

                if (meshFilterWingSection != null)
                {
                    int length = meshReferenceWingSection.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReferenceWingSection.vp, vp, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReferenceWingSection.uv, uv, length);
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing section | Passed array setup");

                    for (int i = 0; i < length; ++i)
                    {
                        // Root/tip filtering followed by leading/trailing filtering
                        if (vp[i].x < -0.05f)
                        {
                            if (vp[i].z < 0f)
                            {
                                vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, wingWidthTipBasedOffsetLeading);
                                uv[i] = new Vector2 (sharedBaseWidthTip, uv[i].y);
                            }
                            else
                            {
                                vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, wingWidthTipBasedOffsetTrailing);
                                uv[i] = new Vector2 (0f, uv[i].y);
                            }
                        }
                        else
                        {
                            if (vp[i].z < 0f)
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y * wingThicknessDeviationRoot, -wingWidthRootBasedOffset);
                                uv[i] = new Vector2 (sharedBaseWidthRoot, uv[i].y);
                            }
                            else
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y * wingThicknessDeviationRoot, wingWidthRootBasedOffset);
                                uv[i] = new Vector2 (0f, uv[i].y);
                            }
                        }
                    }

                    meshFilterWingSection.mesh.vertices = vp;
                    meshFilterWingSection.mesh.uv = uv;
                    meshFilterWingSection.mesh.RecalculateBounds ();

                    MeshCollider meshCollider = meshFilterWingSection.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                        meshCollider = meshFilterWingSection.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = meshFilterWingSection.mesh;
                    meshCollider.convex = true;
                    
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing section | Finished");
                }

                // Second, wing surfaces
                // Again, no need for filtering by normals

                if (meshFilterWingSurface != null)
                {
                    meshFilterWingSurface.transform.localPosition = Vector3.zero;
                    meshFilterWingSurface.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                    int length = meshReferenceWingSurface.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReferenceWingSurface.vp, vp, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReferenceWingSurface.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing surface top | Passed array setup");
                    for (int i = 0; i < length; ++i)
                    {
                        // Root/tip filtering followed by leading/trailing filtering
                        if (vp[i].x < -0.05f)
                        {
                            if (vp[i].z < 0f)
                            {
                                vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, wingWidthTipBasedOffsetLeading);
                                uv[i] = new Vector2 (sharedBaseLength / 4f, 1f - 0.5f + sharedBaseWidthTip / 8f - sharedBaseOffsetTip / 4f);
                            }
                            else
                            {
                                vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, wingWidthTipBasedOffsetTrailing);
                                uv[i] = new Vector2 (sharedBaseLength / 4f, 0f + 0.5f - sharedBaseWidthTip / 8f - sharedBaseOffsetTip / 4f);
                            }
                        }
                        else
                        {
                            if (vp[i].z < 0f)
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y * wingThicknessDeviationRoot, -wingWidthRootBasedOffset);
                                uv[i] = new Vector2 (0.0f, 1f - 0.5f + sharedBaseWidthRoot / 8f);
                            }
                            else
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y * wingThicknessDeviationRoot, wingWidthRootBasedOffset);
                                uv[i] = new Vector2 (0f, 0f + 0.5f - sharedBaseWidthRoot / 8f);
                            }
                        }

                        // Top/bottom filtering
                        if (vp[i].y > 0f)
                        {
                            cl[i] = GetVertexColor (0);
                            uv2[i] = GetVertexUV2 (sharedMaterialST);
                        }
                        else
                        {
                            cl[i] = GetVertexColor (1);
                            uv2[i] = GetVertexUV2 (sharedMaterialSB);
                        }
                    }                    

                    meshFilterWingSurface.mesh.vertices = vp;
                    meshFilterWingSurface.mesh.uv = uv;
                    meshFilterWingSurface.mesh.uv2 = uv2;
                    meshFilterWingSurface.mesh.colors = cl;
                    meshFilterWingSurface.mesh.RecalculateBounds ();

                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing surface | Finished");

                }

                // Next, time for leading and trailing edges
                // Before modifying geometry, we have to show the correct objects for the current selection
                // As UI only works with floats, we have to cast selections into ints too

                int wingEdgeTypeTrailingInt = Mathf.RoundToInt (sharedEdgeTypeTrailing - 1);
                int wingEdgeTypeLeadingInt = Mathf.RoundToInt (sharedEdgeTypeLeading - 1);

                for (int i = 0; i < meshTypeCountEdgeWing; ++i)
                {
                    if (i != wingEdgeTypeTrailingInt)
                        meshFiltersWingEdgeTrailing[i].gameObject.SetActive (false);
                    else
                        meshFiltersWingEdgeTrailing[i].gameObject.SetActive (true);

                    if (i != wingEdgeTypeLeadingInt)
                        meshFiltersWingEdgeLeading[i].gameObject.SetActive (false);
                    else
                        meshFiltersWingEdgeLeading[i].gameObject.SetActive (true);
                }

                // Next we calculate some values reused for all edge geometry

                float wingEdgeWidthLeadingRootDeviation = sharedEdgeWidthLeadingRoot / 0.24f;
                float wingEdgeWidthLeadingTipDeviation = sharedEdgeWidthLeadingTip / 0.24f;

                float wingEdgeWidthTrailingRootDeviation = sharedEdgeWidthTrailingRoot / 0.24f;
                float wingEdgeWidthTrailingTipDeviation = sharedEdgeWidthTrailingTip / 0.24f;

                // Next, we fetch appropriate mesh reference and mesh filter for the edges and modify the meshes
                // Geometry is split into groups through simple vertex normal filtering 

                if (meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt] != null)
                {
                    MeshReference meshReference = meshReferencesWingEdge[wingEdgeTypeTrailingInt];
                    int length = meshReference.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReference.vp, vp, length);
                    Vector3[] nm = new Vector3[length];
                    Array.Copy (meshReference.nm, nm, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReference.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing edge trailing | Passed array setup");
                    for (int i = 0; i < vp.Length; ++i)
                    {
                        if (vp[i].x < -0.1f)
                        {
                            vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, vp[i].z * wingEdgeWidthTrailingTipDeviation + sharedBaseWidthTip / 2f + sharedBaseOffsetTip); // Tip edge
                            if (nm[i].x == 0f) uv[i] = new Vector2 (sharedBaseLength, uv[i].y);
                        }
                        else
                            vp[i] = new Vector3 (0f, vp[i].y * wingThicknessDeviationRoot, vp[i].z * wingEdgeWidthTrailingRootDeviation + sharedBaseWidthRoot / 2f); // Root edge
                        if (nm[i].x == 0f && sharedEdgeTypeTrailing != 1)
                        {
                            cl[i] = GetVertexColor (2);
                            uv2[i] = GetVertexUV2 (sharedMaterialET);
                        }
                    }

                    meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt].mesh.vertices = vp;
                    meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt].mesh.uv = uv;
                    meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt].mesh.uv2 = uv2;
                    meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt].mesh.colors = cl;
                    meshFiltersWingEdgeTrailing[wingEdgeTypeTrailingInt].mesh.RecalculateBounds();
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing edge trailing | Finished");
                }
                if (meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt] != null)
                {
                    MeshReference meshReference = meshReferencesWingEdge [wingEdgeTypeLeadingInt];
                    int length = meshReference.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReference.vp, vp, length);
                    Vector3[] nm = new Vector3[length];
                    Array.Copy (meshReference.nm, nm, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReference.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing edge leading | Passed array setup");
                    for (int i = 0; i < vp.Length; ++i)
                    {
                        if (vp[i].x < -0.1f)
                        {
                            vp[i] = new Vector3 (-sharedBaseLength, vp[i].y * wingThicknessDeviationTip, vp[i].z * wingEdgeWidthLeadingTipDeviation + sharedBaseWidthTip / 2f - sharedBaseOffsetTip); // Tip edge
                            if (nm[i].x == 0f)
                                uv[i] = new Vector2 (sharedBaseLength, uv[i].y);
                        }
                        else
                            vp[i] = new Vector3 (0f, vp[i].y * wingThicknessDeviationRoot, vp[i].z * wingEdgeWidthLeadingRootDeviation + sharedBaseWidthRoot / 2f); // Root edge
                        if (nm[i].x == 0f && sharedEdgeTypeLeading != 1)
                        {
                            cl[i] = GetVertexColor (3);
                            uv2[i] = GetVertexUV2 (sharedMaterialEL);
                        }
                    }

                    meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt].mesh.vertices = vp;
                    meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt].mesh.uv = uv;
                    meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt].mesh.uv2 = uv2;
                    meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt].mesh.colors = cl;
                    meshFiltersWingEdgeLeading[wingEdgeTypeLeadingInt].mesh.RecalculateBounds ();
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Wing edge leading | Finished");
                }
            }
            else
            {
                // Some reusable values

                // float ctrlOffsetRootLimit = (sharedBaseLength / 2f) / (sharedBaseWidthRoot + sharedEdgeWidthTrailingRoot);
                // float ctrlOffsetTipLimit = (sharedBaseLength / 2f) / (sharedBaseWidthTip + sharedEdgeWidthTrailingTip);

                float ctrlOffsetRootClamped = Mathf.Clamp (sharedBaseOffsetRoot, sharedBaseOffsetLimits.z, sharedBaseOffsetLimits.w + 0.15f); // Mathf.Clamp (sharedBaseOffsetRoot, sharedBaseOffsetLimits.z, ctrlOffsetRootLimit - 0.075f);
                float ctrlOffsetTipClamped = Mathf.Clamp (sharedBaseOffsetTip, Mathf.Max (sharedBaseOffsetLimits.z - 0.15f, ctrlOffsetRootClamped - sharedBaseLength), sharedBaseOffsetLimits.w); // Mathf.Clamp (sharedBaseOffsetTip, -ctrlOffsetTipLimit + 0.075f, sharedBaseOffsetLimits.w);

                float ctrlThicknessDeviationRoot = sharedBaseThicknessRoot / 0.24f;
                float ctrlThicknessDeviationTip = sharedBaseThicknessTip / 0.24f;

                float ctrlEdgeWidthDeviationRoot = sharedEdgeWidthTrailingRoot / 0.24f;
                float ctrlEdgeWidthDeviationTip = sharedEdgeWidthTrailingTip / 0.24f;

                // float widthDifference = sharedBaseWidthRoot - sharedBaseWidthTip;
                // float edgeLengthTrailing = Mathf.Sqrt (Mathf.Pow (sharedBaseLength, 2) + Mathf.Pow (widthDifference, 2));
                // float sweepTrailing = 90f - Mathf.Atan (sharedBaseLength / widthDifference) * Mathf.Rad2Deg;

                if (meshFilterCtrlFrame != null)
                {
                    int length = meshReferenceCtrlFrame.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReferenceCtrlFrame.vp, vp, length);
                    Vector3[] nm = new Vector3[length];
                    Array.Copy (meshReferenceCtrlFrame.nm, nm, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReferenceCtrlFrame.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry) DebugLogWithID ("UpdateGeometry", "Control surface frame | Passed array setup");
                    for (int i = 0; i < vp.Length; ++i)
                    {
                        // Thickness correction (X), edge width correction (Y) and span-based offset (Z)
                        if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationTip, vp[i].y, vp[i].z + 0.5f - sharedBaseLength / 2f); // if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationTip, ((vp[i].y + 0.5f) * ctrlEdgeWidthDeviationTip), vp[i].z + 0.5f - sharedBaseLength / 2f);
                        else vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationRoot, vp[i].y, vp[i].z - 0.5f + sharedBaseLength / 2f); // else vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationRoot, ((vp[i].y + 0.5f) * ctrlEdgeWidthDeviationRoot), vp[i].z - 0.5f + sharedBaseLength / 2f);

                        // Left/right sides
                        if (nm[i] == new Vector3 (0f, 0f, 1f) || nm[i] == new Vector3 (0f, 0f, -1f))
                        {
                            // Filtering out trailing edge cross sections
                            if (uv[i].y > 0.185f)
                            {
                                // Filtering out root neighbours
                                if (vp[i].y < -0.01f)
                                {
                                    if (vp[i].z < 0f)
                                    {
                                        vp[i] = new Vector3 (vp[i].x, -sharedBaseWidthTip, vp[i].z);
                                        uv[i] = new Vector2 (sharedBaseWidthTip, uv[i].y);
                                    }
                                    else
                                    {
                                        vp[i] = new Vector3 (vp[i].x, -sharedBaseWidthRoot, vp[i].z);
                                        uv[i] = new Vector2 (sharedBaseWidthRoot, uv[i].y);
                                    }
                                }
                            }
                        }

                        // Root (only needs UV adjustment)
                        else if (nm[i] == new Vector3 (0f, 1f, 0f))
                        {
                            if (vp[i].z < 0f) uv[i] = new Vector2 (sharedBaseLength, uv[i].y);
                        }

                        // Trailing edge
                        else
                        {
                            // Filtering out root neighbours
                            if (vp[i].y < -0.1f)
                            {
                                if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthTip, vp[i].z);
                                else vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthRoot, vp[i].z);
                            }
                        }

                        // Offset-based distortion
                        if (vp[i].z < 0f)
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetTipClamped);
                            if (nm[i] != new Vector3 (0f, 0f, 1f) && nm[i] != new Vector3 (0f, 0f, -1f)) uv[i] = new Vector2 (uv[i].x - (vp[i].y * ctrlOffsetTipClamped) / 4f, uv[i].y);
                        }
                        else
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetRootClamped);
                            if (nm[i] != new Vector3 (0f, 0f, 1f) && nm[i] != new Vector3 (0f, 0f, -1f)) uv[i] = new Vector2 (uv[i].x - (vp[i].y * ctrlOffsetRootClamped) / 4f, uv[i].y);
                        }

                        // Just blanks
                        cl[i] = new Color (0f, 0f, 0f, 0f);
                        uv2[i] = Vector2.zero;
                    }

                    meshFilterCtrlFrame.mesh.vertices = vp;
                    meshFilterCtrlFrame.mesh.uv = uv;
                    meshFilterCtrlFrame.mesh.uv2 = uv2;
                    meshFilterCtrlFrame.mesh.colors = cl;
                    meshFilterCtrlFrame.mesh.RecalculateBounds ();

                    MeshCollider meshCollider = meshFilterCtrlFrame.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                        meshCollider = meshFilterCtrlFrame.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = meshFilterCtrlFrame.mesh;
                    meshCollider.convex = true;
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Control surface frame | Finished");
                }

                // Next, time for edge types
                // Before modifying geometry, we have to show the correct objects for the current selection
                // As UI only works with floats, we have to cast selections into ints too

                int ctrlEdgeTypeInt = Mathf.RoundToInt (sharedEdgeTypeTrailing - 1);
                for (int i = 0; i < meshTypeCountEdgeCtrl; ++i)
                {
                    if (i != ctrlEdgeTypeInt)
                        meshFiltersCtrlEdge[i].gameObject.SetActive (false);
                    else meshFiltersCtrlEdge[i].gameObject.SetActive (true);
                }

                // Now we can modify geometry
                // Copy-pasted frame deformation sequence at the moment, to be pruned later

                if (meshFiltersCtrlEdge[ctrlEdgeTypeInt] != null)
                {
                    MeshReference meshReference = meshReferencesCtrlEdge[ctrlEdgeTypeInt];
                    int length = meshReference.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReference.vp, vp, length);
                    Vector3[] nm = new Vector3[length];
                    Array.Copy (meshReference.nm, nm, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReference.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry) DebugLogWithID ("UpdateGeometry", "Control surface edge | Passed array setup");
                    for (int i = 0; i < vp.Length; ++i)
                    {
                        // Thickness correction (X), edge width correction (Y) and span-based offset (Z)
                        if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationTip, ((vp[i].y + 0.5f) * ctrlEdgeWidthDeviationTip) - 0.5f, vp[i].z + 0.5f - sharedBaseLength / 2f);
                        else vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationRoot, ((vp[i].y + 0.5f) * ctrlEdgeWidthDeviationRoot) - 0.5f, vp[i].z - 0.5f + sharedBaseLength / 2f);

                        // Left/right sides
                        if (nm[i] == new Vector3 (0f, 0f, 1f) || nm[i] == new Vector3 (0f, 0f, -1f))
                        {
                            if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthTip, vp[i].z);
                            else vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthRoot, vp[i].z);
                        }

                        // Trailing edge
                        else
                        {
                            // Filtering out root neighbours
                            if (vp[i].y < -0.1f)
                            {
                                if (vp[i].z < 0f) vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthTip, vp[i].z);
                                else vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthRoot, vp[i].z);
                            }
                        }

                        // Offset-based distortion
                        if (vp[i].z < 0f)
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetTipClamped);
                            if (nm[i] != new Vector3 (0f, 0f, 1f) && nm[i] != new Vector3 (0f, 0f, -1f)) uv[i] = new Vector2 (uv[i].x - (vp[i].y * ctrlOffsetTipClamped) / 4f, uv[i].y);
                        }
                        else
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetRootClamped);
                            if (nm[i] != new Vector3 (0f, 0f, 1f) && nm[i] != new Vector3 (0f, 0f, -1f)) uv[i] = new Vector2 (uv[i].x - (vp[i].y * ctrlOffsetRootClamped) / 4f, uv[i].y);
                        }

                        // Trailing edge (UV adjustment, has to be the last as it's based on cumulative vertex positions)
                        if (nm[i] != new Vector3 (0f, 1f, 0f) && nm[i] != new Vector3 (0f, 0f, 1f) && nm[i] != new Vector3 (0f, 0f, -1f) && uv[i].y < 0.3f)
                        {
                            if (vp[i].z < 0f) uv[i] = new Vector2 (vp[i].z, uv[i].y);
                            else uv[i] = new Vector2 (vp[i].z, uv[i].y);

                            // Color has to be applied there to avoid blanking out cross sections
                            cl[i] = GetVertexColor (2);
                            uv2[i] = GetVertexUV2 (sharedMaterialET);
                        }
                    }

                    meshFiltersCtrlEdge[ctrlEdgeTypeInt].mesh.vertices = vp;
                    meshFiltersCtrlEdge[ctrlEdgeTypeInt].mesh.uv = uv;
                    meshFiltersCtrlEdge[ctrlEdgeTypeInt].mesh.uv2 = uv2;
                    meshFiltersCtrlEdge[ctrlEdgeTypeInt].mesh.colors = cl;
                    meshFiltersCtrlEdge[ctrlEdgeTypeInt].mesh.RecalculateBounds ();
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Control surface edge | Finished");
                }

                // Finally, simple top/bottom surface changes

                if (meshFilterCtrlSurface != null)
                {
                    int length = meshReferenceCtrlSurface.vp.Length;
                    Vector3[] vp = new Vector3[length];
                    Array.Copy (meshReferenceCtrlSurface.vp, vp, length);
                    Vector2[] uv = new Vector2[length];
                    Array.Copy (meshReferenceCtrlSurface.uv, uv, length);
                    Color[] cl = new Color[length];
                    Vector2[] uv2 = new Vector2[length];

                    if (WPDebug.logUpdateGeometry) DebugLogWithID ("UpdateGeometry", "Control surface top | Passed array setup");
                    for (int i = 0; i < vp.Length; ++i)
                    {
                        // Span-based shift
                        if (vp[i].z < 0f)
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z + 0.5f - sharedBaseLength / 2f);
                            uv[i] = new Vector2 (0f, uv[i].y);
                        }
                        else
                        {
                            vp[i] = new Vector3 (vp[i].x, vp[i].y, vp[i].z - 0.5f + sharedBaseLength / 2f);
                            uv[i] = new Vector2 (sharedBaseLength / 4f, uv[i].y);
                        }

                        // Width-based shift
                        if (vp[i].y < -0.1f)
                        {
                            if (vp[i].z < 0f)
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthTip, vp[i].z);
                                uv[i] = new Vector2 (uv[i].x, sharedBaseWidthTip / 4f);
                            }
                            else
                            {
                                vp[i] = new Vector3 (vp[i].x, vp[i].y + 0.5f - sharedBaseWidthRoot, vp[i].z);
                                uv[i] = new Vector2 (uv[i].x, sharedBaseWidthRoot / 4f);
                            }
                        }
                        else uv[i] = new Vector2 (uv[i].x, 0f);

                        // Offsets & thickness
                        if (vp[i].z < 0f)
                        {
                            vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationTip, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetTipClamped);
                            uv[i] = new Vector2 (uv[i].x + (vp[i].y * ctrlOffsetTipClamped) / 4f, uv[i].y);
                        }
                        else
                        {
                            vp[i] = new Vector3 (vp[i].x * ctrlThicknessDeviationRoot, vp[i].y, vp[i].z + vp[i].y * ctrlOffsetRootClamped);
                            uv[i] = new Vector2 (uv[i].x + (vp[i].y * ctrlOffsetRootClamped) / 4f, uv[i].y);
                        }

                        // Colors
                        if (vp[i].x > 0f)
                        {
                            cl[i] = GetVertexColor (0);
                            uv2[i] = GetVertexUV2 (sharedMaterialST);
                        }
                        else
                        {
                            cl[i] = GetVertexColor (1);
                            uv2[i] = GetVertexUV2 (sharedMaterialSB);
                        }
                    }
                    meshFilterCtrlSurface.mesh.vertices = vp;
                    meshFilterCtrlSurface.mesh.uv = uv;
                    meshFilterCtrlSurface.mesh.uv2 = uv2;
                    meshFilterCtrlSurface.mesh.colors = cl;
                    meshFilterCtrlSurface.mesh.RecalculateBounds ();
                    if (WPDebug.logUpdateGeometry)
                        DebugLogWithID ("UpdateGeometry", "Control surface top | Finished");
                }
            }
            if (WPDebug.logUpdateGeometry)
                DebugLogWithID ("UpdateGeometry", "Finished");
            if (HighLogic.LoadedSceneIsEditor)
                CalculateVolume ();
            if (updateAerodynamics)
                CalculateAerodynamicValues();
        }

        public void UpdateCounterparts()
        {
            for (int i = 0; i < this.part.symmetryCounterparts.Count; ++i)
            {
                WingProcedural clone = this.part.symmetryCounterparts[i].Modules.OfType<WingProcedural>().FirstOrDefault();

                clone.sharedBaseLength = clone.sharedBaseLengthCached = sharedBaseLength;
                clone.sharedBaseWidthRoot = clone.sharedBaseWidthRootCached = sharedBaseWidthRoot;
                clone.sharedBaseWidthTip = clone.sharedBaseWidthTipCached = sharedBaseWidthTip;
                clone.sharedBaseThicknessRoot = clone.sharedBaseThicknessRootCached = sharedBaseThicknessRoot;
                clone.sharedBaseThicknessTip = clone.sharedBaseThicknessTipCached = sharedBaseThicknessTip;
                clone.sharedBaseOffsetRoot = clone.sharedBaseOffsetRootCached = sharedBaseOffsetRoot;
                clone.sharedBaseOffsetTip = clone.sharedBaseOffsetTipCached = sharedBaseOffsetTip;

                clone.sharedEdgeTypeLeading = clone.sharedEdgeTypeLeadingCached = sharedEdgeTypeLeading;
                clone.sharedEdgeWidthLeadingRoot = clone.sharedEdgeWidthLeadingRootCached = sharedEdgeWidthLeadingRoot;
                clone.sharedEdgeWidthLeadingTip = clone.sharedEdgeWidthLeadingTipCached = sharedEdgeWidthLeadingTip;

                clone.sharedEdgeTypeTrailing = clone.sharedEdgeTypeTrailingCached = sharedEdgeTypeTrailing;
                clone.sharedEdgeWidthTrailingRoot = clone.sharedEdgeWidthTrailingRootCached = sharedEdgeWidthTrailingRoot;
                clone.sharedEdgeWidthTrailingTip = clone.sharedEdgeWidthTrailingTipCached = sharedEdgeWidthTrailingTip;

                clone.sharedMaterialST = clone.sharedMaterialSTCached = sharedMaterialST;
                clone.sharedMaterialSB = clone.sharedMaterialSBCached = sharedMaterialSB;
                clone.sharedMaterialET = clone.sharedMaterialETCached = sharedMaterialET;
                clone.sharedMaterialEL = clone.sharedMaterialELCached = sharedMaterialEL;

                clone.sharedColorSTBrightness = clone.sharedColorSTBrightnessCached = sharedColorSTBrightness;
                clone.sharedColorSBBrightness = clone.sharedColorSBBrightnessCached = sharedColorSBBrightness;
                clone.sharedColorETBrightness = clone.sharedColorETBrightnessCached = sharedColorETBrightness;
                clone.sharedColorELBrightness = clone.sharedColorELBrightnessCached = sharedColorELBrightness;

                clone.sharedColorSTOpacity = clone.sharedColorSTOpacityCached = sharedColorSTOpacity;
                clone.sharedColorSBOpacity = clone.sharedColorSBOpacityCached = sharedColorSBOpacity;
                clone.sharedColorETOpacity = clone.sharedColorETOpacityCached = sharedColorETOpacity;
                clone.sharedColorELOpacity = clone.sharedColorELOpacityCached = sharedColorELOpacity;

                clone.sharedColorSTHue = clone.sharedColorSTHueCached = sharedColorSTHue;
                clone.sharedColorSBHue = clone.sharedColorSBHueCached = sharedColorSBHue;
                clone.sharedColorETHue = clone.sharedColorETHueCached = sharedColorETHue;
                clone.sharedColorELHue = clone.sharedColorELHueCached = sharedColorELHue;

                clone.sharedColorSTSaturation = clone.sharedColorSTSaturationCached = sharedColorSTSaturation;
                clone.sharedColorSBSaturation = clone.sharedColorSBSaturationCached = sharedColorSBSaturation;
                clone.sharedColorETSaturation = clone.sharedColorETSaturationCached = sharedColorETSaturation;
                clone.sharedColorELSaturation = clone.sharedColorELSaturationCached = sharedColorELSaturation;

                clone.RefreshGeometry();
            }
        }

        // Edge geometry
        public Vector3[] GetReferenceVertices (MeshFilter source)
        {
            Vector3[] positions = new Vector3[0];
            if (source != null)
            {
                if (source.mesh != null)
                {
                    positions = source.mesh.vertices;
                    return positions;
                }
            }
            return positions;
        }

        #endregion

        #region Mesh Setup and Checking
        private void SetupMeshFilters()
        {
            if (!isCtrlSrf)
            {
                meshFilterWingSurface = CheckMeshFilter(meshFilterWingSurface, "surface");
                meshFilterWingSection = CheckMeshFilter(meshFilterWingSection, "section");
                for (int i = 0; i < meshTypeCountEdgeWing; ++i)
                {
                    MeshFilter meshFilterWingEdgeTrailing = CheckMeshFilter("edge_trailing_type" + i);
                    meshFiltersWingEdgeTrailing.Add(meshFilterWingEdgeTrailing);

                    MeshFilter meshFilterWingEdgeLeading = CheckMeshFilter("edge_leading_type" + i);
                    meshFiltersWingEdgeLeading.Add(meshFilterWingEdgeLeading);
                }
            }
            else
            {
                meshFilterCtrlFrame = CheckMeshFilter(meshFilterCtrlFrame, "frame");
                meshFilterCtrlSurface = CheckMeshFilter(meshFilterCtrlSurface, "surface");
                for (int i = 0; i < meshTypeCountEdgeCtrl; ++i)
                {
                    MeshFilter meshFilterCtrlEdge = CheckMeshFilter("edge_type" + i);
                    meshFiltersCtrlEdge.Add(meshFilterCtrlEdge);
                }
            }
        }

        public void SetupMeshReferences()
        {
            bool required = true;
            if (!isCtrlSrf)
            {
                if (meshReferenceWingSection != null && meshReferenceWingSurface != null && meshReferencesWingEdge[meshTypeCountEdgeWing - 1] != null)
                {
                    if (meshReferenceWingSection.vp.Length > 0 && meshReferenceWingSurface.vp.Length > 0 && meshReferencesWingEdge[meshTypeCountEdgeWing - 1].vp.Length > 0)
                    {
                        required = false;
                    }
                }
            }
            else
            {
                if (meshReferenceCtrlFrame != null && meshReferenceCtrlSurface != null && meshReferencesCtrlEdge[meshTypeCountEdgeCtrl - 1] != null)
                {
                    if (meshReferenceCtrlFrame.vp.Length > 0 && meshReferenceCtrlSurface.vp.Length > 0 && meshReferencesCtrlEdge[meshTypeCountEdgeCtrl - 1].vp.Length > 0)
                    {
                        required = false;
                    }
                }
            }
            if (required)
            {
                if (WPDebug.logMeshReferences) DebugLogWithID("SetupMeshReferences", "References missing | isCtrlSrf: " + isCtrlSrf);
                SetupMeshReferencesFromScratch();
            }
            else
            {
                if (WPDebug.logMeshReferences) DebugLogWithID("SetupMeshReferences", "Skipped, all references seem to be in order");
            }
        }

        public void ReportOnMeshReferences()
        {
            if (isCtrlSrf)
            {
                if (WPDebug.logMeshReferences)
                    DebugLogWithID("ReportOnMeshReferences", "Control surface reference length check" + " | Edge: " + meshReferenceCtrlFrame.vp.Length
                                        + " | Surface: " + meshReferenceCtrlSurface.vp.Length);
            }
            else
            {
                if (WPDebug.logMeshReferences)
                    DebugLogWithID("ReportOnMeshReferences", "Wing reference length check" + " | Section: " + meshReferenceWingSection.vp.Length
                                        + " | Surface: " + meshReferenceWingSurface.vp.Length
                );
            }
        }

        private void SetupMeshReferencesFromScratch()
        {
            if (WPDebug.logMeshReferences)
                DebugLogWithID("SetupMeshReferencesFromScratch", "No sources found, creating new references");
            if (!isCtrlSrf)
            {
                WingProcedural.meshReferenceWingSection = FillMeshRefererence(meshFilterWingSection);
                WingProcedural.meshReferenceWingSurface = FillMeshRefererence(meshFilterWingSurface);
                for (int i = 0; i < meshTypeCountEdgeWing; ++i)
                {
                    MeshReference meshReferenceWingEdge = FillMeshRefererence(meshFiltersWingEdgeTrailing[i]);
                    meshReferencesWingEdge.Add(meshReferenceWingEdge);
                }
            }
            else
            {
                WingProcedural.meshReferenceCtrlFrame = FillMeshRefererence(meshFilterCtrlFrame);
                WingProcedural.meshReferenceCtrlSurface = FillMeshRefererence(meshFilterCtrlSurface);
                for (int i = 0; i < meshTypeCountEdgeCtrl; ++i)
                {
                    MeshReference meshReferenceCtrlEdge = FillMeshRefererence(meshFiltersCtrlEdge[i]);
                    meshReferencesCtrlEdge.Add(meshReferenceCtrlEdge);
                }
            }
        }

        // Reference fetching

        private MeshFilter CheckMeshFilter(string name) { return CheckMeshFilter(null, name, false); }
        private MeshFilter CheckMeshFilter(MeshFilter reference, string name) { return CheckMeshFilter(reference, name, false); }
        private MeshFilter CheckMeshFilter(MeshFilter reference, string name, bool disable)
        {
            if (reference == null)
            {
                if (WPDebug.logCheckMeshFilter)
                    DebugLogWithID("CheckMeshFilter", "Looking for object: " + name);
                Transform parent = part.transform.GetChild(0).GetChild(0).GetChild(0).Find(name);
                if (parent != null)
                {
                    parent.localPosition = Vector3.zero;
                    if (WPDebug.logCheckMeshFilter)
                        DebugLogWithID("CheckMeshFilter", "Object " + name + " was found");
                    reference = parent.gameObject.GetComponent<MeshFilter>();
                    if (disable)
                        parent.gameObject.SetActive(false);
                }
                else if (WPDebug.logCheckMeshFilter)
                    DebugLogWithID("CheckMeshFilter", "Object " + name + " was not found!");
            }
            return reference;
        }

        private Transform CheckTransform(string name)
        {
            Transform t = part.transform.GetChild(0).GetChild(0).GetChild(0).Find(name);
            return t;
        }

        private MeshReference FillMeshRefererence(MeshFilter source)
        {
            MeshReference reference = new MeshReference();
            if (source != null)
            {
                int length = source.mesh.vertices.Length;
                reference.vp = new Vector3[length];
                Array.Copy(source.mesh.vertices, reference.vp, length);
                reference.nm = new Vector3[length];
                Array.Copy(source.mesh.normals, reference.nm, length);
                reference.uv = new Vector2[length];
                Array.Copy(source.mesh.uv, reference.uv, length);
            }
            else if (WPDebug.logMeshReferences)
                DebugLogWithID("FillMeshReference", "Mesh filter reference is null, unable to set up reference arrays");
            return reference;
        }
        #endregion

        #region Materials
        public static Material materialLayeredSurface;
        public static Texture materialLayeredSurfaceTextureMain;
        public static Texture materialLayeredSurfaceTextureMask;

        public static Material materialLayeredEdge;
        public static Texture materialLayeredEdgeTextureMain;
        public static Texture materialLayeredEdgeTextureMask;

        private float materialPropertyShininess = 0.4f;
        private Color materialPropertySpecular = new Color(0.62109375f, 0.62109375f, 0.62109375f, 1.0f);

        public void UpdateMaterials ()
        {
            if (materialLayeredSurface == null || materialLayeredEdge == null)
                SetMaterialReferences ();
            if (materialLayeredSurface != null)
            {
                if (!isCtrlSrf)
                {
                    SetMaterial (meshFilterWingSurface, materialLayeredSurface);
                    for (int i = 0; i < meshTypeCountEdgeWing; ++i)
                    {
                        SetMaterial (meshFiltersWingEdgeTrailing[i], materialLayeredEdge);
                        SetMaterial (meshFiltersWingEdgeLeading[i], materialLayeredEdge);
                    }
                }
                else
                {
                    SetMaterial (meshFilterCtrlSurface, materialLayeredSurface);
                    SetMaterial (meshFilterCtrlFrame, materialLayeredEdge);
                    for (int i = 0; i < meshTypeCountEdgeCtrl; ++i)
                    {
                        SetMaterial(meshFiltersCtrlEdge[i], materialLayeredEdge);
                    }
                }
            }
            else if (WPDebug.logUpdateMaterials)
                DebugLogWithID ("UpdateMaterials", "Material creation failed");
        }

        private void SetMaterialReferences ()
        {
            if (materialLayeredSurface == null)
                materialLayeredSurface = ResourceExtractor.GetEmbeddedMaterial ("B9_Aerospace_WingStuff.SpecularLayered.txt");
            if (materialLayeredEdge == null)
                materialLayeredEdge = ResourceExtractor.GetEmbeddedMaterial ("B9_Aerospace_WingStuff.SpecularLayered.txt");

            if (!isCtrlSrf) SetTextures (meshFilterWingSurface, meshFiltersWingEdgeTrailing[0]);
            else SetTextures (meshFilterCtrlSurface, meshFilterCtrlFrame);

            if (materialLayeredSurfaceTextureMain != null && materialLayeredSurfaceTextureMask != null)
            {
                materialLayeredSurface.SetTexture ("_MainTex", materialLayeredSurfaceTextureMain);
                materialLayeredSurface.SetTexture ("_Emissive", materialLayeredSurfaceTextureMask);
                materialLayeredSurface.SetFloat ("_Shininess", materialPropertyShininess);
                materialLayeredSurface.SetColor ("_SpecColor", materialPropertySpecular);
            }
            else if (WPDebug.logUpdateMaterials) DebugLogWithID ("SetMaterialReferences", "Surface textures not found");

            if (materialLayeredEdgeTextureMain != null && materialLayeredEdgeTextureMask != null)
            {
                materialLayeredEdge.SetTexture ("_MainTex", materialLayeredEdgeTextureMain);
                materialLayeredEdge.SetTexture ("_Emissive", materialLayeredEdgeTextureMask);
                materialLayeredEdge.SetFloat ("_Shininess", materialPropertyShininess);
                materialLayeredEdge.SetColor ("_SpecColor", materialPropertySpecular);
            }
            else if (WPDebug.logUpdateMaterials) DebugLogWithID ("SetMaterialReferences", "Edge textures not found");
        }

        private void SetMaterial (MeshFilter target, Material material)
        {
            if (target != null)
            {
                Renderer r = target.gameObject.GetComponent<Renderer> ();
                if (r != null)
                    r.sharedMaterial = material;
            }
        }

        private void SetTextures (MeshFilter sourceSurface, MeshFilter sourceEdge)
        {
            if (sourceSurface != null)
            {
                Renderer r = sourceSurface.gameObject.GetComponent<Renderer> ();
                if (r != null)
                {
                    materialLayeredSurfaceTextureMain = r.sharedMaterial.GetTexture ("_MainTex");
                    materialLayeredSurfaceTextureMask = r.sharedMaterial.GetTexture ("_Emissive");
                    if (WPDebug.logUpdateMaterials)
                        DebugLogWithID ("SetTextures", "Main: " + materialLayeredSurfaceTextureMain.ToString () + " | Mask: " + materialLayeredSurfaceTextureMask);
                }
            }
            if (sourceEdge != null)
            {
                Renderer r = sourceEdge.gameObject.GetComponent<Renderer> ();
                if (r != null)
                {
                    materialLayeredEdgeTextureMain = r.sharedMaterial.GetTexture ("_MainTex");
                    materialLayeredEdgeTextureMask = r.sharedMaterial.GetTexture ("_Emissive");
                    if (WPDebug.logUpdateMaterials)
                        DebugLogWithID ("SetTextures", "Main: " + materialLayeredEdgeTextureMain.ToString () + " | Mask: " + materialLayeredEdgeTextureMask);
                }
            }
        }

        #endregion

        #region Aero

        public class VesselStatus
        {
            public Vessel vessel = null;
            public bool isUpdated = false;

            public VesselStatus (Vessel v, bool state)
            {
                vessel = v;
                isUpdated = state;
            }
        }
        public static List<VesselStatus> vesselList = new List<VesselStatus> ();

        // Delayed aero value setup
        // Must be run after all geometry setups, otherwise FAR checks will be done before surrounding parts take shape, producing incorrect results
        public IEnumerator SetupReorderedForFlight ()
        {
            // First we need to determine whether the vessel this part is attached to is included into the status list
            // If it's included, we need to fetch it's index in that list

            bool vesselListInclusive = false;
            int vesselID = vessel.GetInstanceID ();
            int vesselStatusIndex = 0;
            int vesselListCount = vesselList.Count;
            for (int i = 0; i < vesselListCount; ++i)
            {
                if (vesselList[i].vessel.GetInstanceID () == vesselID)
                {
                    if (WPDebug.logFlightSetup)
                        DebugLogWithID ("SetupReorderedForFlight", "Vessel " + vesselID + " found in the status list");
                    vesselListInclusive = true;
                    vesselStatusIndex = i;
                }
            }

            // If it was not included, we add it to the list
            // Correct index is then fairly obvious

            if (!vesselListInclusive)
            {
                if (WPDebug.logFlightSetup)
                    DebugLogWithID ("SetupReorderedForFlight", "Vessel " + vesselID + " was not found in the status list, adding it");
                vesselList.Add (new VesselStatus (vessel, false));
                vesselStatusIndex = vesselList.Count - 1;
            }

            // Using the index for the status list we obtained, we check whether it was updated yet
            // So that only one part can run the following part

            if (!vesselList[vesselStatusIndex].isUpdated)
            {
                if (WPDebug.logFlightSetup)
                    DebugLogWithID ("SetupReorderedForFlight", "Vessel " + vesselID + " was not updated yet (this message should only appear once)");
                vesselList[vesselStatusIndex].isUpdated = true;
                List<WingProcedural> moduleList = new List<WingProcedural> ();

                // First we get a list of all relevant parts in the vessel
                // Found modules are added to a list

                int vesselPartsCount = vessel.parts.Count;
                for (int i = 0; i < vesselPartsCount; ++i)
                {
                    if (vessel.parts[i].Modules.Contains ("WingProcedural"))
                        moduleList.Add ((WingProcedural) vessel.parts[i].Modules["WingProcedural"]);
                }

                // After that we make two separate runs through that list
                // First one setting up all geometry and second one setting up aerodynamic values

                if (WPDebug.logFlightSetup)
                    DebugLogWithID ("SetupReorderedForFlight", "Vessel " + vesselID + " contained " + vesselPartsCount + " parts, of which " + moduleList.Count + " should be set up");
                int moduleListCount = moduleList.Count;
                for (int i = 0; i < moduleListCount; ++i)
                {
                    moduleList[i].Setup ();
                }

                yield return new WaitForFixedUpdate ();
                yield return new WaitForFixedUpdate ();

                if (WPDebug.logFlightSetup)
                    DebugLogWithID ("SetupReorderedForFlight", "Vessel " + vesselID + " waited for updates, starting aero value calculation");
                for (int i = 0; i < moduleListCount; ++i)
                {
                    moduleList[i].CalculateAerodynamicValues ();
                }
            }
        }




        // Aerodynamics value calculation
        // More or less lifted from pWings, so credit goes to DYJ and Taverius

        [KSPField] public float aeroConstLiftFudgeNumber = 0.0775f;
        [KSPField] public float aeroConstMassFudgeNumber = 0.015f;
        [KSPField] public float aeroConstDragBaseValue = 0.6f;
        [KSPField] public float aeroConstDragMultiplier = 3.3939f;
        [KSPField] public float aeroConstConnectionFactor = 150f;
        [KSPField] public float aeroConstConnectionMinimum = 50f; 
        [KSPField] public float aeroConstCostDensity = 5300f;
        [KSPField] public float aeroConstCostDensityControl = 6500f;
        [KSPField] public float aeroConstControlSurfaceFraction = 1f;

        [KSPField (guiActiveEditor = false, guiName = "Coefficient of drag", guiFormat = "F3")]
        public float aeroUICd;

        [KSPField (guiActiveEditor = false, guiName = "Coefficient of lift", guiFormat = "F3")]
        public float aeroUICl;

        [KSPField (guiActiveEditor = false, guiName = "Mass", guiFormat = "F3", guiUnits = "t")]
        public float aeroUIMass;

        [KSPField (guiActiveEditor = false, guiName = "Cost")] 
        public float aeroUICost;

        [KSPField (guiActiveEditor = false, guiName = "Mean aerodynamic chord", guiFormat = "F3", guiUnits = "m")]
        public float aeroUIMeanAerodynamicChord;

        [KSPField (guiActiveEditor = false, guiName = "Semispan", guiFormat = "F3", guiUnits = "m")]
        public float aeroUISemispan;

        [KSPField (guiActiveEditor = false, guiName = "Mid-chord sweep", guiFormat = "F3", guiUnits = "deg.")]
        public float aeroUIMidChordSweep;

        [KSPField (guiActiveEditor = false, guiName = "Taper ratio", guiFormat = "F3")]
        public float aeroUITaperRatio;

        [KSPField (guiActiveEditor = false, guiName = "Surface area", guiFormat = "F3", guiUnits = "m²")]
        public float aeroUISurfaceArea;

        [KSPField (guiActiveEditor = false, guiName = "Aspect ratio", guiFormat = "F3")]
        public float aeroUIAspectRatio;

        [KSPField (guiActiveEditor = false, guiName = "Volume", guiFormat = "F3")]
        public float aeroStatVolume = 3.84f;

        public double aeroStatCd;
        public double aeroStatCl;
        public double aeroStatClChildren;
        public double aeroStatMass;
        public double aeroStatConnectionForce;

        public double aeroStatMeanAerodynamicChord;
        public double aeroStatSemispan;
        public double aeroStatMidChordSweep;
        public Vector3d aeroStatRootMidChordOffsetFromOrigin;
        public double aeroStatTaperRatio;
        public double aeroStatSurfaceArea;
        public double aeroStatAspectRatio;
        public double aeroStatAspectRatioSweepScale;

        private PartModule aeroFARModuleReference;
        private Type       aeroFARModuleType;

        private FieldInfo  aeroFARFieldInfoSemispan;
        private FieldInfo aeroFARFieldInfoSemispan_Actual; // to handle tweakscale, wings have semispan (unscaled) and semispan_actual (tweakscaled). Need to set both (actual is the important one, and tweakscale isn't needed here, so only _actual actually needs to be set, but it would be silly to not set it)
        private FieldInfo  aeroFARFieldInfoMAC;
        private FieldInfo  aeroFARFieldInfoMAC_Actual; //  to handle tweakscale, wings have MAC (unscaled) and MAC_actual (tweakscaled). Need to set both (actual is the important one, and tweakscale isn't needed here, so only _actual actually needs to be set, but it would be silly to not set it)
        private FieldInfo aeroFARFieldInfoSurfaceArea; // calculated internally from b_2_actual and MAC_actual
        private FieldInfo  aeroFARFieldInfoMidChordSweep;
        private FieldInfo  aeroFARFieldInfoTaperRatio;
        private FieldInfo  aeroFARFieldInfoControlSurfaceFraction;
        private FieldInfo  aeroFARFieldInfoRootChordOffset;
        private MethodInfo aeroFARMethodInfoUsed;

        public void CalculateVolume ()
        {
            if (!canBeFueled || isPanel)
                return;

            aeroStatVolume = (sharedBaseWidthTip * sharedBaseThicknessTip * sharedBaseLength) + ((sharedBaseWidthRoot - sharedBaseWidthTip) / 2f * sharedBaseThicknessTip * sharedBaseLength)
                                + (sharedBaseWidthTip * (sharedBaseThicknessRoot - sharedBaseThicknessTip) / 2f * sharedBaseLength)
                                + ((sharedBaseWidthRoot - sharedBaseWidthTip) / 2f * (sharedBaseThicknessRoot - sharedBaseThicknessTip) / 2f * sharedBaseLength);

            FuelUpdateAmountsFromVolume (aeroStatVolume, true);
        }

        public void CalculateAerodynamicValues ()
        {
            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Started");
            CheckAssemblies (false);

            float sharedWidthTipSum = sharedBaseWidthTip;
            float sharedWidthRootSum = sharedBaseWidthRoot;

            if (!isCtrlSrf)
            {
                double offset = 0;
                if (sharedEdgeTypeLeading != 1)
                {
                    sharedWidthTipSum += sharedEdgeWidthLeadingTip;
                    sharedWidthRootSum += sharedEdgeWidthLeadingRoot;
                    offset += 0.2 * (sharedEdgeWidthLeadingRoot + sharedEdgeWidthLeadingTip);
                }
                if (sharedEdgeTypeTrailing != 1)
                {
                    sharedWidthTipSum += sharedEdgeWidthTrailingTip;
                    sharedWidthRootSum += sharedEdgeWidthTrailingRoot;
                    offset -= 0.25 * (sharedEdgeWidthTrailingRoot + sharedEdgeWidthTrailingTip);
                }
                aeroStatRootMidChordOffsetFromOrigin = offset * Vector3d.up;
            }
            else
            {
                sharedWidthTipSum += sharedEdgeWidthTrailingTip;
                sharedWidthRootSum += sharedEdgeWidthTrailingRoot;
            }

            float ctrlOffsetRootLimit = (sharedBaseLength / 2f) / (sharedBaseWidthRoot + sharedEdgeWidthTrailingRoot);
            float ctrlOffsetTipLimit = (sharedBaseLength / 2f) / (sharedBaseWidthTip + sharedEdgeWidthTrailingTip);

            float ctrlOffsetRootClamped = Mathf.Clamp (sharedBaseOffsetRoot, -ctrlOffsetRootLimit, ctrlOffsetRootLimit);
            float ctrlOffsetTipClamped = Mathf.Clamp (sharedBaseOffsetTip, -ctrlOffsetTipLimit, ctrlOffsetTipLimit);

            // Base four values

            if (!isCtrlSrf)
            {
                aeroStatSemispan = (double) sharedBaseLength;
                aeroStatTaperRatio = (double) sharedWidthTipSum / (double) sharedWidthRootSum;
                aeroStatMeanAerodynamicChord = (double) (sharedWidthTipSum + sharedWidthRootSum) / 2.0;
                aeroStatMidChordSweep = MathD.Atan ((double) sharedBaseOffsetTip / (double) sharedBaseLength) * MathD.Rad2Deg;
            }
            else
            {
                aeroStatSemispan = (double) sharedBaseLength;
                aeroStatTaperRatio = (double) (sharedBaseLength + sharedWidthTipSum * ctrlOffsetTipClamped - sharedWidthRootSum * ctrlOffsetRootClamped) / (double) sharedBaseLength;
                aeroStatMeanAerodynamicChord = (double) (sharedWidthTipSum + sharedWidthRootSum) / 2.0;
                aeroStatMidChordSweep = MathD.Atan ((double) Mathf.Abs (sharedWidthRootSum - sharedWidthTipSum) / (double) sharedBaseLength) * MathD.Rad2Deg;
            }
            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Passed B2/TR/MAC/MCS");

            // Derived values

            aeroStatSurfaceArea = aeroStatMeanAerodynamicChord * aeroStatSemispan;
            aeroStatAspectRatio = 2.0f * aeroStatSemispan / aeroStatMeanAerodynamicChord;

            aeroStatAspectRatioSweepScale = MathD.Pow (aeroStatAspectRatio / MathD.Cos (MathD.Deg2Rad * aeroStatMidChordSweep), 2.0f) + 4.0f;
            aeroStatAspectRatioSweepScale = 2.0f + MathD.Sqrt (aeroStatAspectRatioSweepScale);
            aeroStatAspectRatioSweepScale = (2.0f * MathD.PI) / aeroStatAspectRatioSweepScale * aeroStatAspectRatio;

            aeroStatMass = MathD.Clamp (aeroConstMassFudgeNumber * aeroStatSurfaceArea * ((aeroStatAspectRatioSweepScale * 2.0) / (3.0 + aeroStatAspectRatioSweepScale)) * ((1.0 + aeroStatTaperRatio) / 2), 0.01, double.MaxValue);
            aeroStatCd = aeroConstDragBaseValue / aeroStatAspectRatioSweepScale * aeroConstDragMultiplier;
            aeroStatCl = aeroConstLiftFudgeNumber * aeroStatSurfaceArea * aeroStatAspectRatioSweepScale;
            GatherChildrenCl();
            aeroStatConnectionForce = MathD.Round (MathD.Clamp (MathD.Sqrt (aeroStatCl + aeroStatClChildren) * (double) aeroConstConnectionFactor, (double) aeroConstConnectionMinimum, double.MaxValue));
            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Passed SR/AR/ARSS/mass/Cl/Cd/connection");

            // Shared parameters

            if (!isCtrlSrf)
            {
                aeroUICost = (float) aeroStatMass * (1f + (float) aeroStatAspectRatioSweepScale / 4f) * aeroConstCostDensity;
                aeroUICost = Mathf.Round (aeroUICost / 5f) * 5f;
                part.CoMOffset = new Vector3 (sharedBaseLength / 2f, -sharedBaseOffsetTip / 2f, 0f);
            }
            else
            {
                aeroUICost = (float) aeroStatMass * (1f + (float) aeroStatAspectRatioSweepScale / 4f) * aeroConstCostDensity * (1f - aeroConstControlSurfaceFraction);
                aeroUICost += (float) aeroStatMass * (1f + (float) aeroStatAspectRatioSweepScale / 4f) * aeroConstCostDensityControl * aeroConstControlSurfaceFraction;
                aeroUICost = Mathf.Round (aeroUICost / 5f) * 5f;
                part.CoMOffset = new Vector3 (0f, -(sharedWidthRootSum + sharedWidthTipSum) / 4f, 0f);
            }
            part.breakingForce = Mathf.Round ((float) aeroStatConnectionForce);
            part.breakingTorque = Mathf.Round ((float) aeroStatConnectionForce);
            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Passed cost/force/torque");

            // Stock-only values
            if (!assemblyFARUsed)
            {
                float stockLiftCoefficient = (float)aeroStatSurfaceArea / 3.52f;
                if (!isCtrlSrf && !isWingAsCtrlSrf)
                {
                    if (WPDebug.logCAV)
                        DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR is inactive, calculating values for winglet part type");
                    ((ModuleLiftingSurface)this.part.Modules["ModuleLiftingSurface"]).deflectionLiftCoeff = (float)Math.Round(stockLiftCoefficient, 2);
                    part.mass = stockLiftCoefficient * 0.1f;
                }
                else
                {
                    if (WPDebug.logCAV)
                        DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR is inactive, calculating stock control surface module values");
                    ModuleControlSurface mCtrlSrf = part.Modules.OfType<ModuleControlSurface> ().FirstOrDefault ();
                    mCtrlSrf.deflectionLiftCoeff = (float)Math.Round(stockLiftCoefficient, 2);
                    mCtrlSrf.ctrlSurfaceArea = aeroConstControlSurfaceFraction;
                    part.mass = stockLiftCoefficient * (1 + mCtrlSrf.ctrlSurfaceArea) * 0.1f;
                }
            }

            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Passed stock drag/deflection/area");

            // FAR values

            if (assemblyFARUsed)
            {
                if (WPDebug.logCAV)
                    DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Entered segment");
                if (aeroFARModuleReference == null)
                {
                    if (part.Modules.Contains ("FARControllableSurface"))
                        aeroFARModuleReference = part.Modules["FARControllableSurface"];
                    else if (part.Modules.Contains ("FARWingAerodynamicModel"))
                        aeroFARModuleReference = part.Modules["FARWingAerodynamicModel"];
                    if (WPDebug.logCAV)
                        DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Module reference was null, search performed, recheck result was " + (aeroFARModuleReference == null).ToString ());
                }
                if (aeroFARModuleReference != null)
                {
                    if (WPDebug.logCAV)
                        DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Module reference present");
                    if (aeroFARModuleType == null)
                        aeroFARModuleType = aeroFARModuleReference.GetType ();
                    if (aeroFARModuleType != null) 
                    {
                        if (WPDebug.logCAV)
                            DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Module type present");
                        if (aeroFARFieldInfoSemispan == null)
                            aeroFARFieldInfoSemispan = aeroFARModuleType.GetField ("b_2");
                        if (aeroFARFieldInfoSemispan_Actual == null)
                            aeroFARFieldInfoSemispan_Actual = aeroFARModuleType.GetField("b_2_actual");
                        if (aeroFARFieldInfoMAC == null)
                            aeroFARFieldInfoMAC = aeroFARModuleType.GetField ("MAC");
                        if (aeroFARFieldInfoMAC_Actual == null)
                            aeroFARFieldInfoMAC_Actual = aeroFARModuleType.GetField("MAC_actual");
                        if (aeroFARFieldInfoSurfaceArea == null)
                            aeroFARFieldInfoSurfaceArea = aeroFARModuleType.GetField("S");
                        if (aeroFARFieldInfoMidChordSweep == null)
                            aeroFARFieldInfoMidChordSweep = aeroFARModuleType.GetField ("MidChordSweep");
                        if (aeroFARFieldInfoTaperRatio == null)
                            aeroFARFieldInfoTaperRatio = aeroFARModuleType.GetField ("TaperRatio");
                        if (isCtrlSrf)
                        {
                            if (aeroFARFieldInfoControlSurfaceFraction == null)
                                aeroFARFieldInfoControlSurfaceFraction = aeroFARModuleType.GetField ("ctrlSurfFrac");
                        }
                        else
                        {
                            if (aeroFARFieldInfoRootChordOffset == null)
                                aeroFARFieldInfoRootChordOffset = aeroFARModuleType.GetField("rootMidChordOffsetFromOrig");
                        }
                        if (WPDebug.logCAV)
                            DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Field checks and fetching passed");

                        if (aeroFARMethodInfoUsed == null)
                        {
                            aeroFARMethodInfoUsed = aeroFARModuleType.GetMethod ("StartInitialization");
                            if (WPDebug.logCAV)
                                DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Method info was null, search performed, recheck result was " + (aeroFARMethodInfoUsed == null).ToString ());
                        }
                        if (aeroFARMethodInfoUsed != null)
                        {
                            if (WPDebug.logCAV)
                                DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Method info present");
                            aeroFARFieldInfoSemispan.SetValue (aeroFARModuleReference, aeroStatSemispan);
                            aeroFARFieldInfoSemispan_Actual.SetValue(aeroFARModuleReference, aeroStatSemispan);
                            aeroFARFieldInfoMAC.SetValue (aeroFARModuleReference, aeroStatMeanAerodynamicChord);
                            aeroFARFieldInfoMAC_Actual.SetValue(aeroFARModuleReference, aeroStatMeanAerodynamicChord);
                            //aeroFARFieldInfoSurfaceArea.SetValue (aeroFARModuleReference, aeroStatSurfaceArea);
                            aeroFARFieldInfoMidChordSweep.SetValue (aeroFARModuleReference, aeroStatMidChordSweep);
                            aeroFARFieldInfoTaperRatio.SetValue (aeroFARModuleReference, aeroStatTaperRatio);
                            if (isCtrlSrf)
                                aeroFARFieldInfoControlSurfaceFraction.SetValue (aeroFARModuleReference, aeroConstControlSurfaceFraction);
                            else
                                aeroFARFieldInfoRootChordOffset.SetValue(aeroFARModuleReference, (Vector3)aeroStatRootMidChordOffsetFromOrigin);

                            if (WPDebug.logCAV)
                                DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | All values set, invoking the method");
                            aeroFARMethodInfoUsed.Invoke (aeroFARModuleReference, null);
                        }
                    }
                }
            }
            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "FAR/NEAR | Segment ended");

            // Update GUI values and finish

            if (!assemblyFARUsed)
            {
                aeroUICd = Mathf.Round ((float) aeroStatCd * 100f) / 100f;
                aeroUICl = Mathf.Round ((float) aeroStatCl * 100f) / 100f;
            }
            if (!assemblyFARUsed)
                aeroUIMass = part.mass;

            aeroUIMeanAerodynamicChord = (float) aeroStatMeanAerodynamicChord;
            aeroUISemispan = (float) aeroStatSemispan;
            aeroUIMidChordSweep = (float) aeroStatMidChordSweep;
            aeroUITaperRatio = (float) aeroStatTaperRatio;
            aeroUISurfaceArea = (float) aeroStatSurfaceArea;
            aeroUIAspectRatio = (float) aeroStatAspectRatio;

            if (WPDebug.logCAV)
                DebugLogWithID ("CalculateAerodynamicValues", "Finished");

            StartCoroutine(updateAeroDelayed());
        }

        float updateTimeDelay = 0;
        IEnumerator updateAeroDelayed()
        {
            bool running = updateTimeDelay > 0;
            updateTimeDelay = 0.5f;
            if (running)
                yield break;
            while (updateTimeDelay > 0)
            {
                updateTimeDelay -= TimeWarp.deltaTime;
                yield return null;
            }
            if (assemblyFARUsed)
                part.SendMessage("GeometryPartModuleRebuildMeshData"); // notify FAR that geometry has changed
            else
            {
                DragCube DragCube = DragCubeSystem.Instance.RenderProceduralDragCube(part);
                part.DragCubes.ClearCubes();
                part.DragCubes.Cubes.Add(DragCube);
                part.DragCubes.ResetCubeWeights();
            }
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            updateTimeDelay = 0;
        }

        public void GatherChildrenCl ()
        {
            aeroStatClChildren = 0;

            // Add up the Cl and ChildrenCl of all our children to our ChildrenCl
            for (int i = 0; i < part.children.Count; ++i)
            {
                if (part.children[i] == null)
                    continue;
                if (part.children[i].Modules.Contains ("WingProcedural"))
                {
                    WingProcedural child = part.children[i].Modules.OfType<WingProcedural> ().FirstOrDefault ();
                    if (child == null)
                        continue;
                    aeroStatClChildren += child.aeroStatCl;
                    aeroStatClChildren += child.aeroStatClChildren;
                }
            }

            // If parent is a pWing, trickle the call to gather ChildrenCl up to them.
            if (this.part.parent != null && this.part.parent.Modules.Contains ("WingProcedural"))
                this.part.parent.Modules.OfType<WingProcedural> ().FirstOrDefault ().GatherChildrenCl();
        }

        public bool showWingData = false;
        [KSPEvent (guiActiveEditor = true, guiName = "Show wing data")]
        public void InfoToggleEvent ()
        {
            if (isAttached && this.part.parent != null)
            {
                showWingData = !showWingData;
                if (showWingData) Events["InfoToggleEvent"].guiName = "Hide wing data";
                else Events["InfoToggleEvent"].guiName = "Show wing data";

                // If FAR/NEAR aren't present, toggle Cl/Cd
                if (!assemblyFARUsed)
                {
                    Fields["aeroUICd"].guiActiveEditor = showWingData;
                    Fields["aeroUICl"].guiActiveEditor = showWingData;
                    Fields["aeroUIMass"].guiActive = showWingData;
                }
                // Toggle the rest of the info values
                Fields["aeroUICost"].guiActiveEditor = showWingData;
                Fields["aeroUIMeanAerodynamicChord"].guiActiveEditor = showWingData;
                Fields["aeroUISemispan"].guiActiveEditor = showWingData;
                Fields["aeroUIMidChordSweep"].guiActiveEditor = showWingData;
                Fields["aeroUITaperRatio"].guiActiveEditor = showWingData;
                Fields["aeroUISurfaceArea"].guiActiveEditor = showWingData;
                Fields["aeroUIAspectRatio"].guiActiveEditor = showWingData;
                Fields["aeroStatVolume"].guiActiveEditor = showWingData;

                // Force tweakable window to refresh
                UpdateWindow();
            }
        }

        // [KSPEvent (guiActive = true, guiActiveEditor = true, guiName = "Dump interaction data")]
        public void DumpInteractionData ()
        {
            if (part.Modules.Contains ("FARWingAerodynamicModel"))
            {
                PartModule moduleFAR = part.Modules["FARWingAerodynamicModel"];
                Type typeFAR = moduleFAR.GetType ();

                var referenceInteraction = typeFAR.GetField ("wingInteraction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (moduleFAR);
                if (referenceInteraction != null)
                {
                    string report = "";
                    Type typeInteraction = referenceInteraction.GetType ();
                    Type runtimeListType = typeof (List<>).MakeGenericType (typeFAR);

                    FieldInfo forwardExposureInfo = typeInteraction.GetField ("forwardExposure", BindingFlags.NonPublic | BindingFlags.Instance);
                    double forwardExposure = (double) forwardExposureInfo.GetValue (referenceInteraction);
                    FieldInfo backwardExposureInfo = typeInteraction.GetField ("backwardExposure", BindingFlags.NonPublic | BindingFlags.Instance);
                    double backwardExposure = (double) backwardExposureInfo.GetValue (referenceInteraction);
                    FieldInfo leftwardExposureInfo = typeInteraction.GetField ("leftwardExposure", BindingFlags.NonPublic | BindingFlags.Instance);
                    double leftwardExposure = (double) leftwardExposureInfo.GetValue (referenceInteraction);
                    FieldInfo rightwardExposureInfo = typeInteraction.GetField ("rightwardExposure", BindingFlags.NonPublic | BindingFlags.Instance);
                    double rightwardExposure = (double) rightwardExposureInfo.GetValue (referenceInteraction);
                    report += "Exposure (fwd/back/left/right): " + forwardExposure.ToString ("F2") + ", " + backwardExposure.ToString ("F2") + ", " + leftwardExposure.ToString ("F2") + ", " + rightwardExposure.ToString ("F2");
                    DebugLogWithID ("DumpInteractionData", report);
                }
                else DebugLogWithID ("DumpInteractionData", "Interaction reference is null, report failed");
            }
            else DebugLogWithID ("DumpInteractionData", "FAR module not found, report failed");
        }

        #endregion

        #region Alternative UI/input


        public KeyCode uiKeyCodeEdit = KeyCode.J;
        public static bool uiWindowActive = true;
        public static float uiMouseDeltaCache = 0f;

        public static int uiInstanceIDTarget = 0;
        private int uiInstanceIDLocal = 0;

        public static int uiPropertySelectionWing = 0;
        public static int uiPropertySelectionSurface = 0;

        public static bool uiEditMode = false;
        public static bool uiAdjustWindow = true;
        public static bool uiEditModeTimeout = false;
        private float uiEditModeTimeoutDuration = 0.25f;
        private float uiEditModeTimer = 0f;

        public Vector2 getLimits(double value, double step, int i = 0)
        {
            if (value % step != 0 || ((int)(value / step) != i & (int)((value / step)-1) != i))
                i = (int) (value / step);
            float x = (float)(i * step);
            float y = (float)((i + 1) * step);
            Vector2 limits = new Vector2(x, y);
            return limits;
        }

        public Vector2 getOffsetLimits(float value, float step, int i = 0)
        {
            value -= step / 2;
            if (value % step != 0 || ((int)(value / step) != i & (int)((value / step) - 1) != i))
                i = (int)(value / step);
            float x = i * step - step / 2;
            float y = (i + 1) * step - step / 2;
            Vector2 limits = new Vector2(x, y);
            return limits;
        }
        /*
        public Vector2 switchVector(Vector2 value)
        {
            Vector2 ret;
            ret.x = value.y;
            ret.y = value.x;
            return ret;
        }
        */
        public float getStep(Vector4 limits)
        {
            float step;
            if (!isCtrlSrf)
                step = limits.y;
            else
                step = limits.w;
            return step;
        }
        public float getStep2(Vector2 limits)
        {
            return limits.y;
        }
        

        // Supposed to fix context menu updates
        // Proposed by NathanKell, if I'm not mistaken
        UIPartActionWindow _myWindow = null;
        UIPartActionWindow myWindow
        {
            get
            {
                if (_myWindow == null)
                {
                    UIPartActionWindow[] windows = (UIPartActionWindow[])FindObjectsOfType(typeof(UIPartActionWindow));
                    for (int i = 0; i < windows.Length; ++i)
                    {
                        if (windows[i].part == part)
                            _myWindow = windows[i];
                    }
                }
                return _myWindow;
            }
        }

        private void UpdateWindow()
        {
            if (myWindow != null)
                myWindow.displayDirty = true;
        }

        private void OnGUI ()
        {
            if (!isStarted || !HighLogic.LoadedSceneIsEditor || !uiWindowActive)
                return;

            if (uiInstanceIDLocal == 0)
                uiInstanceIDLocal = part.GetInstanceID ();
            if (uiInstanceIDTarget == uiInstanceIDLocal || uiInstanceIDTarget == 0)
            {
                if (!WingProceduralManager.uiStyleConfigured)
                    WingProceduralManager.ConfigureStyles ();

                if (uiAdjustWindow)
                {
                    uiAdjustWindow = false;
                    if (WPDebug.logPropertyWindow)
                        DebugLogWithID ("OnGUI", "Window forced to adjust");
                    WingProceduralManager.uiRectWindowEditor = GUILayout.Window (273, WingProceduralManager.uiRectWindowEditor, OnWindow, GetWindowTitle (), WingProceduralManager.uiStyleWindow, GUILayout.Height (0));
                }
                else
                    WingProceduralManager.uiRectWindowEditor = GUILayout.Window (273, WingProceduralManager.uiRectWindowEditor, OnWindow, GetWindowTitle (), WingProceduralManager.uiStyleWindow);

                // Thanks to ferram4
                // Following section lock the editor, preventing window clickthrough

                if (WingProceduralManager.uiRectWindowEditor.Contains(UIUtility.GetMousePos()))
                {
                    EditorLogic.fetch.Lock(false, false, false, "WingProceduralWindow");
                    EditorTooltip.Instance.HideToolTip ();
                }
                else
                    EditorLogic.fetch.Unlock("WingProceduralWindow");
            }
        }

        public static Vector4 uiColorSliderBase = new Vector4 (0.25f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderEdgeL = new Vector4 (0.20f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderEdgeT = new Vector4 (0.15f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderColorsST = new Vector4 (0.10f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderColorsSB = new Vector4 (0.05f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderColorsET = new Vector4 (0.00f, 0.5f, 0.4f, 1f);
        public static Vector4 uiColorSliderColorsEL = new Vector4 (0.95f, 0.5f, 0.4f, 1f);

        private void OnWindow (int window)
        {
            if (uiEditMode)
            {

                bool returnEarly = false;
                GUILayout.BeginHorizontal ();
                GUILayout.BeginVertical ();
                if (uiLastFieldName.Length > 0) GUILayout.Label ("Last: " + uiLastFieldName, WingProceduralManager.uiStyleLabelMedium);
                else GUILayout.Label ("Property editor", WingProceduralManager.uiStyleLabelMedium);
                if (uiLastFieldTooltip.Length > 0) GUILayout.Label (uiLastFieldTooltip + "\n_________________________", WingProceduralManager.uiStyleLabelHint, GUILayout.MaxHeight (44f), GUILayout.MinHeight (44f)); // 58f for four lines
                GUILayout.EndVertical ();
                if (GUILayout.Button ("Close", WingProceduralManager.uiStyleButton, GUILayout.MaxWidth (50f)))
                {
                    EditorLogic.fetch.Unlock ("WingProceduralWindow");
                    uiWindowActive = false;
                    stockButton.SetFalse(false);
                    returnEarly = true;
                }
                GUILayout.EndHorizontal ();
                if (returnEarly) return;
                DrawFieldGroupHeader(ref sharedFieldPrefStatic, "Preference");
                if(sharedFieldPrefStatic)
                {
                    DrawCheck(ref sharedPropAnglePref, "Use angles to define the wing", "No", "Yes", "AngleDefine", 101);
                    DrawCheck(ref sharedPropEdgePref, "[Disabled]Include edges in definitions", "No", "Yes", "EdgeIncluded", 102);
                    DrawCheck(ref sharedPropEThickPref, "[Disabled]Scale edges to thickness ", "No", "Yes", "ThickScale", 103);
                }
                DrawFieldGroupHeader (ref sharedFieldGroupBaseStatic, "Base");
                if (sharedFieldGroupBaseStatic && !isCtrlSrf)
                {
                    DrawField(ref sharedBaseLength, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseLength, getStep(sharedBaseLengthLimits), sharedBaseLengthInt), "Length", uiColorSliderBase, 0, 0, ref sharedBaseLengthInt);
                    DrawField(ref sharedBaseWidthRoot, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseWidthRoot, getStep(sharedBaseWidthRootLimits), sharedBaseWidthRInt), "Width (root)", uiColorSliderBase, 1, 0, ref sharedBaseWidthRInt);
                    if (!sharedPropAnglePref)
                    {                        
                        DrawField(ref sharedBaseWidthTip, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseWidthTip, getStep(sharedBaseWidthTipLimits), sharedBaseWidthTInt), "Width (tip)", uiColorSliderBase, 2, 0, ref sharedBaseWidthTInt, true);
                        DrawField(ref sharedBaseOffsetTip, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), 1f, getOffsetLimits(sharedBaseOffsetTip, getStep(sharedBaseOffsetLimits)), "Offset (tip)", uiColorSliderBase, 4, 0, ref sharedBaseOffsetTInt, true, 1);
                        
                    }
                    else
                    {
                        DrawField(ref sharedSweptAngleFront, 1f, 1f, sharedSweptAngleLimits, "Swept angle(front)", uiColorSliderBase, 201, 0, ref dummyValueInt);
                        DrawField(ref sharedSweptAngleBack, 1f, 1f, sharedSweptAngleLimits, "Swept angle(back)", uiColorSliderBase, 202, 0, ref dummyValueInt);
                        
                    }
                    DrawField(ref sharedBaseThicknessRoot, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedBaseThicknessRoot, getStep2(sharedBaseThicknessLimits)), "Thickness (root)", uiColorSliderBase, 5, 0, ref sharedBaseThicknessRInt);
                    DrawField(ref sharedBaseThicknessTip, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedBaseThicknessTip, getStep2(sharedBaseThicknessLimits)), "Thickness (tip)", uiColorSliderBase, 6, 0, ref sharedBaseThicknessTInt);
                    //Debug.Log("B9PW: base complete");
                }
                else
                {
                    DrawField(ref sharedBaseLength, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseLength, getStep(sharedBaseLengthLimits)), "Length", uiColorSliderBase, 0, 0, ref sharedBaseLengthInt);
                    DrawField(ref sharedBaseWidthRoot, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseWidthRoot, getStep(sharedBaseWidthRootLimits)), "Width (root)", uiColorSliderBase, 1, 0, ref sharedBaseWidthRInt);
                    DrawField(ref sharedBaseWidthTip, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), GetIncrementFromType(1f, 0.24f), getLimits(sharedBaseWidthTip, getStep(sharedBaseWidthTipLimits)), "Width (tip)", uiColorSliderBase, 2, 0, ref sharedBaseWidthTInt);
                   DrawField(ref sharedBaseOffsetRoot, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), 1f, getOffsetLimits(sharedBaseOffsetRoot, getStep(sharedBaseOffsetLimits)), "Offset (root)", uiColorSliderBase, 3, 0, ref sharedBaseOffsetRInt, true, 1);
                    DrawField(ref sharedBaseOffsetTip, GetIncrementFromType(sharedIncrementMain, sharedIncrementSmall), 1f, getOffsetLimits(sharedBaseOffsetTip, getStep(sharedBaseOffsetLimits)), "Offset (tip)", uiColorSliderBase, 4, 0, ref sharedBaseOffsetTInt, true, 1);
                    DrawField(ref sharedBaseThicknessRoot, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedBaseThicknessRoot, getStep2(sharedBaseThicknessLimits)), "Thickness (root)", uiColorSliderBase, 5, 0, ref sharedBaseThicknessRInt);
                    DrawField(ref sharedBaseThicknessTip, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedBaseThicknessTip, getStep2(sharedBaseThicknessLimits)), "Thickness (tip)", uiColorSliderBase, 6, 0, ref sharedBaseThicknessTInt);
                }

                if (!isCtrlSrf)
                {
                    DrawFieldGroupHeader (ref sharedFieldGroupEdgeLeadingStatic, "Edge (leading)");
                    if (sharedFieldGroupEdgeLeadingStatic)
                    {
                        DrawField (ref sharedEdgeTypeLeading, sharedIncrementInt, sharedIncrementInt, GetLimitsFromType (sharedEdgeTypeLimits), "Shape", uiColorSliderEdgeL, 7, 2, ref dummyValueInt, false);
                        DrawField (ref sharedEdgeWidthLeadingRoot, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedEdgeWidthLeadingRoot, getStep(sharedEdgeWidthLimits)), "Width (root)", uiColorSliderEdgeL, 8, 0, ref sharedEdgeWidthLRInt);
                        DrawField (ref sharedEdgeWidthLeadingTip, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedEdgeWidthLeadingTip, getStep(sharedEdgeWidthLimits)), "Width (tip)", uiColorSliderEdgeL, 9, 0, ref sharedEdgeWidthLTInt);
                    }
                    
                }

                DrawFieldGroupHeader (ref sharedFieldGroupEdgeTrailingStatic, "Edge (trailing)");
                if (sharedFieldGroupEdgeTrailingStatic)
                {
                    DrawField (ref sharedEdgeTypeTrailing, sharedIncrementInt, sharedIncrementInt, GetLimitsFromType (sharedEdgeTypeLimits), "Shape", uiColorSliderEdgeT, 10, isCtrlSrf ? 3 : 2, ref dummyValueInt, false);
                    DrawField (ref sharedEdgeWidthTrailingRoot, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedEdgeWidthTrailingRoot, getStep(sharedEdgeWidthLimits)), "Width (root)", uiColorSliderEdgeT, 11, 0, ref sharedEdgeWidthTRInt);
                    DrawField (ref sharedEdgeWidthTrailingTip, sharedIncrementSmall, sharedIncrementSmall, getLimits(sharedEdgeWidthTrailingTip, getStep(sharedEdgeWidthLimits)), "Width (tip)", uiColorSliderEdgeT, 12, 0, ref sharedEdgeWidthTTInt);
                }

                DrawFieldGroupHeader (ref sharedFieldGroupColorSTStatic, "Surface (top)");
                if (sharedFieldGroupColorSTStatic)
                {
                    DrawField (ref sharedMaterialST, sharedIncrementInt, sharedIncrementInt, sharedMaterialLimits, "Material", uiColorSliderColorsST, 13, 1, ref dummyValueInt, false);
                    DrawField (ref sharedColorSTOpacity, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Opacity", uiColorSliderColorsST, 14, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSTHue, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Hue", uiColorSliderColorsST, 15, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSTSaturation, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Saturation", uiColorSliderColorsST, 16, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSTBrightness, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Brightness", uiColorSliderColorsST, 17, 0, ref dummyValueInt);
                }

                DrawFieldGroupHeader (ref sharedFieldGroupColorSBStatic, "Surface (bottom)");
                if (sharedFieldGroupColorSBStatic)
                {
                    DrawField (ref sharedMaterialSB, sharedIncrementInt, sharedIncrementInt, sharedMaterialLimits, "Material", uiColorSliderColorsSB, 13, 1, ref dummyValueInt, false);
                    DrawField (ref sharedColorSBOpacity, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Opacity", uiColorSliderColorsSB, 14, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSBHue, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Hue", uiColorSliderColorsSB, 15, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSBSaturation, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Saturation", uiColorSliderColorsSB, 16, 0, ref dummyValueInt);
                    DrawField (ref sharedColorSBBrightness, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Brightness", uiColorSliderColorsSB, 17, 0, ref dummyValueInt);
                }

                DrawFieldGroupHeader (ref sharedFieldGroupColorETStatic, "Surface (trailing edge)");
                if (sharedFieldGroupColorETStatic)
                {
                    DrawField (ref sharedMaterialET, sharedIncrementInt, sharedIncrementInt, sharedMaterialLimits, "Material", uiColorSliderColorsET, 13, 1, ref dummyValueInt, false);
                    DrawField (ref sharedColorETOpacity, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Opacity", uiColorSliderColorsET, 14, 0, ref dummyValueInt);
                    DrawField (ref sharedColorETHue, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Hue", uiColorSliderColorsET, 15, 0, ref dummyValueInt);
                    DrawField (ref sharedColorETSaturation, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Saturation", uiColorSliderColorsET, 16, 0, ref dummyValueInt);
                    DrawField (ref sharedColorETBrightness, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Brightness", uiColorSliderColorsET, 17, 0, ref dummyValueInt);
                }

                if (!isCtrlSrf)
                {
                    DrawFieldGroupHeader (ref sharedFieldGroupColorELStatic, "Surface (leading edge)");
                    if (sharedFieldGroupColorELStatic)
                    {
                        DrawField (ref sharedMaterialEL, sharedIncrementInt, sharedIncrementInt, sharedMaterialLimits, "Material", uiColorSliderColorsEL, 13, 1, ref dummyValueInt, false);
                        DrawField (ref sharedColorELOpacity, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Opacity", uiColorSliderColorsEL, 14, 0, ref dummyValueInt);
                        DrawField (ref sharedColorELHue, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Hue", uiColorSliderColorsEL, 15, 0, ref dummyValueInt);
                        DrawField (ref sharedColorELSaturation, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Saturation", uiColorSliderColorsEL, 16, 0, ref dummyValueInt);
                        DrawField (ref sharedColorELBrightness, sharedIncrementColor, sharedIncrementColorLarge, sharedColorLimits, "Brightness", uiColorSliderColorsEL, 17, 0, ref dummyValueInt);
                    }
                }

                GUILayout.Label ("_________________________\n\nPress J to exit edit mode\nOptions below allow you to change default values", WingProceduralManager.uiStyleLabelHint);
                if (canBeFueled && useStockFuel)
                {
                    if (GUILayout.Button (FuelGUIGetConfigDesc () + " | Next tank setup", WingProceduralManager.uiStyleButton)) NextConfiguration ();
                }

                GUILayout.BeginHorizontal ();
                if (GUILayout.Button ("Save as default", WingProceduralManager.uiStyleButton))
                    ReplaceDefaults ();
                if (GUILayout.Button ("Restore default", WingProceduralManager.uiStyleButton))
                    RestoreDefaults ();
                GUILayout.EndHorizontal ();
                if (inheritancePossibleOnShape || inheritancePossibleOnMaterials)
                {
                    GUILayout.Label ("_________________________\n\nOptions options allow you to match the part properties to it's parent", WingProceduralManager.uiStyleLabelHint);
                    GUILayout.BeginHorizontal ();
                    if (inheritancePossibleOnShape) 
                    { 
                        if (GUILayout.Button ("Shape", WingProceduralManager.uiStyleButton))
                            InheritParentValues (0);
                        if (GUILayout.Button ("Base", WingProceduralManager.uiStyleButton))
                            InheritParentValues (1);
                        if (GUILayout.Button ("Edges", WingProceduralManager.uiStyleButton))
                            InheritParentValues (2); 
                    }
                    if (inheritancePossibleOnMaterials)
                    {
                        if (GUILayout.Button ("Color", WingProceduralManager.uiStyleButton)) InheritParentValues (3);
                    }
                    GUILayout.EndHorizontal ();
                }
            }
            else
            {
                if (uiEditModeTimeout)
                    GUILayout.Label("Exiting edit mode...\n", WingProceduralManager.uiStyleLabelMedium);
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Press J while pointing at a\nprocedural part to edit it", WingProceduralManager.uiStyleLabelHint);
                    if (GUILayout.Button("Close", WingProceduralManager.uiStyleButton, GUILayout.MaxWidth(50f)))
                    {
                        uiWindowActive = false;
                        stockButton.SetFalse(false);
                        uiAdjustWindow = true;
                        EditorLogic.fetch.Unlock("WingProceduralWindow");
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUI.DragWindow ();
        }

        private void SetupFields()
        {
            

            sharedBaseLength = SetupFieldValue(sharedBaseLength, getLimits(sharedBaseLength,getStep(sharedBaseLengthLimits)), GetDefault(sharedBaseLengthDefaults));
            sharedBaseWidthRoot = SetupFieldValue(sharedBaseWidthRoot, getLimits(sharedBaseWidthRoot, getStep(sharedBaseWidthRootLimits)), GetDefault(sharedBaseWidthRootDefaults));
            sharedBaseWidthTip = SetupFieldValue(sharedBaseWidthTip, getLimits(sharedBaseWidthTip, getStep(sharedBaseWidthTipLimits)), GetDefault(sharedBaseWidthTipDefaults));
            sharedBaseThicknessRoot = SetupFieldValue(sharedBaseThicknessRoot, getLimits(sharedBaseThicknessRoot, getStep(sharedBaseThicknessLimits)), GetDefault(sharedBaseThicknessRootDefaults));
            sharedBaseThicknessTip = SetupFieldValue(sharedBaseThicknessTip, getLimits(sharedBaseThicknessTip, getStep(sharedBaseThicknessLimits)) ,GetDefault(sharedBaseThicknessTipDefaults));
            sharedBaseOffsetRoot = SetupFieldValue(sharedBaseOffsetRoot, getLimits(sharedBaseOffsetRoot, getStep(sharedBaseOffsetLimits)), GetDefault(sharedBaseOffsetRootDefaults));
            sharedBaseOffsetTip = SetupFieldValue(sharedBaseOffsetTip, getLimits(sharedBaseOffsetTip, getStep(sharedBaseOffsetLimits)), GetDefault(sharedBaseOffsetTipDefaults));

            sharedEdgeTypeTrailing = SetupFieldValue(sharedEdgeTypeTrailing, sharedEdgeTypeLimits, GetDefault(sharedEdgeTypeTrailingDefaults));
            sharedEdgeWidthTrailingRoot = SetupFieldValue(sharedEdgeWidthTrailingRoot, getLimits(sharedEdgeWidthTrailingRoot, getStep(sharedEdgeWidthLimits)), GetDefault(sharedEdgeWidthTrailingRootDefaults));
            sharedEdgeWidthTrailingTip = SetupFieldValue(sharedEdgeWidthTrailingTip, getLimits(sharedEdgeWidthTrailingTip, getStep(sharedEdgeWidthLimits)), GetDefault(sharedEdgeWidthTrailingTipDefaults));

            sharedEdgeTypeLeading = SetupFieldValue(sharedEdgeTypeLeading, sharedEdgeTypeLimits, GetDefault(sharedEdgeTypeLeadingDefaults));
            sharedEdgeWidthLeadingRoot = SetupFieldValue(sharedEdgeWidthLeadingRoot, getLimits(sharedEdgeWidthLeadingRoot, getStep(sharedBaseOffsetLimits)), GetDefault(sharedEdgeWidthLeadingRootDefaults));
            sharedEdgeWidthLeadingTip = SetupFieldValue(sharedEdgeWidthLeadingTip, getLimits(sharedEdgeWidthLeadingTip, getStep(sharedBaseOffsetLimits)), GetDefault(sharedEdgeWidthLeadingTipDefaults));

            sharedMaterialST = SetupFieldValue(sharedMaterialST, sharedMaterialLimits, GetDefault(sharedMaterialSTDefaults));
            sharedColorSTOpacity = SetupFieldValue(sharedColorSTOpacity, sharedColorLimits, GetDefault(sharedColorSTOpacityDefaults));
            sharedColorSTHue = SetupFieldValue(sharedColorSTHue, sharedColorLimits, GetDefault(sharedColorSTHueDefaults));
            sharedColorSTSaturation = SetupFieldValue(sharedColorSTSaturation, sharedColorLimits, GetDefault(sharedColorSTSaturationDefaults));
            sharedColorSTBrightness = SetupFieldValue(sharedColorSTBrightness, sharedColorLimits, GetDefault(sharedColorSTBrightnessDefaults));

            sharedMaterialSB = SetupFieldValue(sharedMaterialSB, sharedMaterialLimits, GetDefault(sharedMaterialSBDefaults));
            sharedColorSBOpacity = SetupFieldValue(sharedColorSBOpacity, sharedColorLimits, GetDefault(sharedColorSBOpacityDefaults));
            sharedColorSBHue = SetupFieldValue(sharedColorSBHue, sharedColorLimits, GetDefault(sharedColorSBHueDefaults));
            sharedColorSBSaturation = SetupFieldValue(sharedColorSBSaturation, sharedColorLimits, GetDefault(sharedColorSBSaturationDefaults));
            sharedColorSBBrightness = SetupFieldValue(sharedColorSBBrightness, sharedColorLimits, GetDefault(sharedColorSBBrightnessDefaults));

            sharedMaterialET = SetupFieldValue(sharedMaterialET, sharedMaterialLimits, GetDefault(sharedMaterialETDefaults));
            sharedColorETOpacity = SetupFieldValue(sharedColorETOpacity, sharedColorLimits, GetDefault(sharedColorETOpacityDefaults));
            sharedColorETHue = SetupFieldValue(sharedColorETHue, sharedColorLimits, GetDefault(sharedColorETHueDefaults));
            sharedColorETSaturation = SetupFieldValue(sharedColorETSaturation, sharedColorLimits, GetDefault(sharedColorETSaturationDefaults));
            sharedColorETBrightness = SetupFieldValue(sharedColorETBrightness, sharedColorLimits, GetDefault(sharedColorETBrightnessDefaults));

            sharedMaterialEL = SetupFieldValue(sharedMaterialEL, sharedMaterialLimits, GetDefault(sharedMaterialELDefaults));
            sharedColorELOpacity = SetupFieldValue(sharedColorELOpacity, sharedColorLimits, GetDefault(sharedColorELOpacityDefaults));
            sharedColorELHue = SetupFieldValue(sharedColorELHue, sharedColorLimits, GetDefault(sharedColorELHueDefaults));
            sharedColorELSaturation = SetupFieldValue(sharedColorELSaturation, sharedColorLimits, GetDefault(sharedColorELSaturationDefaults));
            sharedColorELBrightness = SetupFieldValue(sharedColorELBrightness, sharedColorLimits, GetDefault(sharedColorELBrightnessDefaults));

            UpdateWindow();
            isSetToDefaultValues = true;
        }

        private int GetFieldMode()
        {
            if (!isCtrlSrf) return 1;
            else return 2;
        }

        private float SetupFieldValue(float value, Vector2 limits, float defaultValue)
        {
            if (!isSetToDefaultValues)
                return defaultValue;
            else
                return Mathf.Clamp(value, limits.x, limits.y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">the value to draw</param>
        /// <param name="increment">mouse drag increment</param>
        /// <param name="incrementLarge">button increment</param>
        /// <param name="limits">min/max value</param>
        /// <param name="name">the field name to display</param>
        /// <param name="hsbColor">field colour</param>
        /// <param name="fieldID">tooltip stuff</param>
        /// <param name="fieldType">tooltip stuff</param>
        /// <param name="allowFine">Whether right click drag behaves as fine control or not</param>
        //Vector2 field0;  //it need definition
        private void DrawField ( ref float field, float increment, float incrementLarge, Vector2 limits, string name, Vector4 hsbColor, int fieldID, int fieldType, ref int delta, bool allowFine = true, int isOffset = 0)
        {
            bool changed = false;
            field = UIUtility.FieldSlider (field, increment, incrementLarge, limits, name, out changed, ColorHSBToRGB (hsbColor), fieldType, ref delta, allowFine, isOffset);
            
            if (changed)
            {
                uiLastFieldName = name;
                uiLastFieldTooltip = UpdateTooltipText (fieldID);
                //Debug.Log("B9PW:" + name  + " Value changed to " + field);
            }
        }

        private void DrawCheck(ref bool value, string desc, string choice1, string choice2, string name, int fieldID)
        {
            bool changed = false;
            value = UIUtility.CheckBox(desc, choice1, choice2, value, out changed);
            if (changed)
            {
                uiLastFieldName = name;
                uiLastFieldTooltip = UpdateTooltipText(fieldID);
                //Debug.Log("B9PW:" + value + " Value changed to " + value);
                WingProceduralManager.SaveConfigs();
            }
        }
        

        private void DrawFieldGroupHeader (ref bool fieldGroupBoolStatic, string header)
        {
            GUILayout.BeginHorizontal ();
            if (GUILayout.Button (header, WingProceduralManager.uiStyleLabelHint))
            {
                fieldGroupBoolStatic = !fieldGroupBoolStatic;
                if (WPDebug.logPropertyWindow) DebugLogWithID ("DrawFieldGroupHeader", "Header of " + header + " pressed | Group state: " + fieldGroupBoolStatic);
                uiAdjustWindow = true;
            }
            if (fieldGroupBoolStatic) GUILayout.Label ("|", WingProceduralManager.uiStyleLabelHint, GUILayout.MaxWidth (15f));
            else GUILayout.Label ("+", WingProceduralManager.uiStyleLabelHint, GUILayout.MaxWidth (15f));
            GUILayout.EndHorizontal ();
        }

        private static string uiLastFieldName = "";
        private static string uiLastFieldTooltip = "Additional info on edited \nproperties is displayed here";

        private string UpdateTooltipText (int fieldID)
        {
            // Base descriptions
            if (fieldID == 0) // sharedBaseLength))
            {
                if (!isCtrlSrf) return "Lateral measurement of the wing, \nalso referred to as semispan";
                else return "Lateral measurement of the control \nsurface at it's root";
            }
            else if (fieldID == 1) // sharedBaseWidthRoot))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the wing \nat the root cross section";
                else return "Longitudinal measurement of \nthe root chord";
            }
            else if (fieldID == 2) // sharedBaseWidthTip))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the wing \nat the tip cross section";
                else return "Longitudinal measurement of \nthe tip chord";
            }
            else if (fieldID == 3) // sharedBaseOffsetRoot))
            {
                if (!isCtrlSrf) return "This property shouldn't be accessible \non a wing";
                else return "Offset of the trailing edge \nroot corner on the lateral axis";
            }
            else if (fieldID == 4) // sharedBaseOffsetTip))
            {
                if (!isCtrlSrf) return "Distance between midpoints of the cross \nsections on the longitudinal axis";
                else return "Offset of the trailing edge \ntip corner on the lateral axis";
            }
            else if (fieldID == 5) // sharedBaseThicknessRoot))
            {
                if (!isCtrlSrf) return "Thickness at the root cross section \nUsually kept proportional to edge width";
                else return "Thickness at the root cross section \nUsually kept proportional to edge width";
            }
            else if (fieldID == 6) // sharedBaseThicknessTip))
            {
                if (!isCtrlSrf) return "Thickness at the tip cross section \nUsually kept proportional to edge width";
                else return "Thickness at the tip cross section \nUsually kept proportional to edge width";
            }

            // Edge descriptions
            else if (fieldID == 7) // sharedEdgeTypeTrailing))
            {
                if (!isCtrlSrf) return "Shape of the trailing edge cross \nsection (round/biconvex/sharp)";
                else return "Shape of the trailing edge cross \nsection (round/biconvex/sharp)";
            }
            else if (fieldID == 8) // sharedEdgeWidthTrailingRoot))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the trailing \nedge cross section at wing root";
                else return "Longitudinal measurement of the trailing \nedge cross section at with root";
            }
            else if (fieldID == 9) // sharedEdgeWidthTrailingTip))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the trailing \nedge cross section at wing tip";
                else return "Longitudinal measurement of the trailing \nedge cross section at with tip";
            }
            else if (fieldID == 10) // sharedEdgeTypeLeading))
            {
                if (!isCtrlSrf) return "Shape of the leading edge cross \nsection (round/biconvex/sharp)";
                else return "Shape of the leading edge cross \nsection (round/biconvex/sharp)";
            }
            else if (fieldID == 11) // sharedEdgeWidthLeadingRoot))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the leading \nedge cross section at wing root";
                else return "Longitudinal measurement of the leading \nedge cross section at wing root";
            }
            else if (fieldID == 12) // sharedEdgeWidthLeadingTip))
            {
                if (!isCtrlSrf) return "Longitudinal measurement of the leading \nedge cross section at with tip";
                else return "Longitudinal measurement of the leading \nedge cross section at with tip";
            }

            // Surface descriptions
            else if (fieldID == 13)
            {
                if (!isCtrlSrf) return "Surface material (uniform fill, plating, \nLRSI/HRSI tiles and so on)";
                else return "Surface material (uniform fill, plating, \nLRSI/HRSI tiles and so on)";
            }
            else if (fieldID == 14)
            {
                if (!isCtrlSrf) return "Fairly self-explanatory, controls the paint \nopacity: no paint at 0, full coverage at 1";
                else return "Fairly self-explanatory, controls the paint \nopacity: no paint at 0, full coverage at 1";
            }
            else if (fieldID == 15)
            {
                if (!isCtrlSrf) return "Controls the paint hue (HSB axis): \nvalues from zero to one make full circle";
                else return "Controls the paint hue (HSB axis): \nvalues from zero to one make full circle";
            }
            else if (fieldID == 16)
            {
                if (!isCtrlSrf) return "Controls the paint saturation (HSB axis): \ncolorless at 0, full color at 1";
                else return "Controls the paint saturation (HSB axis): \ncolorless at 0, full color at 1";
            }
            else if (fieldID == 17)
            {
                if (!isCtrlSrf) return "Controls the paint brightness (HSB axis): black at 0, white at 1, primary at 0.5";
                else return "Controls the paint brightness (HSB axis): black at 0, white at 1, primary at 0.5";
            }//Modified part start
            else if (fieldID == 101)
                return "Use front and back sweptback angles to define wings,\nor just select no to use the good old lengths.";
            else if (fieldID == 102)
                return "Include or exclude edges \nwhen changing propertiesof the wing.";
            else if (fieldID == 103)
                return "Scale edge lengths when changing thickness.";
            else if (fieldID == 201)
                return "Angle between front edge and root.\n<90 deg is to the back";
            else if (fieldID == 202)
                return "Angle between back edge and root.\n<90 deg is to the back.";

            // This should not really happen
            else return "Unknown field\n";
        }

        private void OnMouseOver ()
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            
            if (this.part.parent != null && isAttached && !uiEditModeTimeout)
            {
                if (uiEditMode)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        uiEditMode = false;
                        uiEditModeTimeout = true;
                    }
                }
                if (Input.GetKeyDown(uiKeyCodeEdit))
                {
                    uiInstanceIDTarget = part.GetInstanceID();
                    uiEditMode = true;
                    uiEditModeTimeout = true;
                    uiAdjustWindow = true;
                    uiWindowActive = true;
                    stockButton.SetTrue(false);
                    InheritanceStatusUpdate();
                }
            }
            if (state == 0)
            {
                lastMousePos = Input.mousePosition;
                if (Input.GetKeyDown(keyTranslation))
                    state = 1;
                else if (Input.GetKeyDown(keyTipWidth))
                    state = 2;
                else if (Input.GetKeyDown(keyRootWidth))
                    state = 3;
            }
        }

        static KeyCode keyTranslation = KeyCode.G, keyTipWidth = KeyCode.T, keyRootWidth = KeyCode.B, keyLeading = KeyCode.LeftAlt, keyTrailing = KeyCode.LeftControl;
        Vector3 lastMousePos;
        int state = 0; // 0 == nothing, 1 == translate, 2 == tipScale, 3 == rootScale
        public void DeformWing()
        {
            if (!isAttached || state == 0)
                return;

            float depth = EditorCamera.Instance.camera.WorldToScreenPoint(part.transform.position).z;
            Vector3 diff = depth * (Input.mousePosition - lastMousePos) / 1000;
            lastMousePos = Input.mousePosition;
            switch (state)
            {
                case 1:
                    if (!Input.GetKey(keyTranslation))
                    {
                        state = 0;
                        return;
                    }

                    sharedBaseLength += (isCtrlSrf ? 2 : 1) * diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, part.transform.right) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.right);
                    //sharedBaseLength = Mathf.Clamp(sharedBaseLength, GetLimitsFromType(sharedBaseLengthLimits).x, GetLimitsFromType(sharedBaseLengthLimits).y);
                    if (!isCtrlSrf)
                    {
                        sharedBaseOffsetTip -= diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.up);
                        //sharedBaseOffsetTip = Mathf.Clamp(sharedBaseOffsetTip, GetLimitsFromType(sharedBaseOffsetLimits).x, GetLimitsFromType(sharedBaseOffsetLimits).y);
                    }
                    break;
                case 2:
                    if (!Input.GetKey(keyTipWidth))
                    {
                        state = 0;
                        return;
                    }
                    if (Input.GetKey(keyLeading) && !isCtrlSrf)
                    {
                        sharedEdgeWidthLeadingTip += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.up);
                        //sharedEdgeWidthLeadingTip = Mathf.Clamp(sharedEdgeWidthLeadingTip, GetLimitsFromType(sharedEdgeWidthLimits).x, GetLimitsFromType(sharedEdgeWidthLimits).y);
                    }
                    else if (Input.GetKey(keyTrailing))
                    {
                        sharedEdgeWidthTrailingTip += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, -part.transform.up);
                        //sharedEdgeWidthTrailingTip = Mathf.Clamp(sharedEdgeWidthTrailingTip, GetLimitsFromType(sharedEdgeWidthLimits).x, GetLimitsFromType(sharedEdgeWidthLimits).y);
                    }
                    else
                    {
                        sharedBaseWidthTip += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, -part.transform.up);
                        //sharedBaseWidthTip = Mathf.Clamp(sharedBaseWidthTip, GetLimitsFromType(sharedBaseWidthTipLimits).x, GetLimitsFromType(sharedBaseWidthTipLimits).y);
                        sharedBaseThicknessTip += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.forward) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.forward * (part.isMirrored ? 1 : -1));
                        //sharedBaseThicknessTip = Mathf.Clamp(sharedBaseThicknessTip, sharedBaseThicknessLimits.x, sharedBaseThicknessLimits.y);
                    }
                    break;
                case 3:
                    if (!Input.GetKey(keyRootWidth))
                    {
                        state = 0;
                        return;
                    }
                    if (Input.GetKey(keyLeading) && !isCtrlSrf)
                    {
                        sharedEdgeWidthLeadingRoot += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.up);
                        //sharedEdgeWidthLeadingRoot = Mathf.Clamp(sharedEdgeWidthLeadingRoot, GetLimitsFromType(sharedEdgeWidthLimits).x, GetLimitsFromType(sharedEdgeWidthLimits).y);
                    }
                    else if (Input.GetKey(keyTrailing))
                    {
                        sharedEdgeWidthTrailingRoot += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, -part.transform.up);
                        //sharedEdgeWidthTrailingRoot = Mathf.Clamp(sharedEdgeWidthTrailingRoot, GetLimitsFromType(sharedEdgeWidthLimits).x, GetLimitsFromType(sharedEdgeWidthLimits).y);
                    }
                    else
                    {
                        sharedBaseWidthRoot += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.up) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, -part.transform.up);
                        //sharedBaseWidthRoot = Mathf.Clamp(sharedBaseWidthRoot, GetLimitsFromType(sharedBaseWidthRootLimits).x, GetLimitsFromType(sharedBaseWidthRootLimits).y);
                        sharedBaseThicknessRoot += diff.x * Vector3.Dot(EditorCamera.Instance.camera.transform.right, -part.transform.forward) + diff.y * Vector3.Dot(EditorCamera.Instance.camera.transform.up, part.transform.forward * (part.isMirrored ? 1 : -1));
                        //sharedBaseThicknessRoot = Mathf.Clamp(sharedBaseThicknessRoot, sharedBaseThicknessLimits.x, sharedBaseThicknessLimits.y);
                    }
                    break;
            }
        }

        private void UpdateUI ()
        {
            if (stockButton == null)
                OnStockButtonSetup ();
            if (uiEditModeTimeout && uiInstanceIDTarget == 0)
            {
                if (WPDebug.logPropertyWindow)
                    DebugLogWithID ("UpdateUI", "Window timeout was left active on scene reload, resetting the window state");
                StopWindowTimeout ();
            }
            if (uiInstanceIDLocal != uiInstanceIDTarget)
                return;

            if (uiEditModeTimeout)
            {
                uiEditModeTimer += Time.deltaTime;
                if (uiEditModeTimer > uiEditModeTimeoutDuration)
                    StopWindowTimeout ();
            }
            else if (uiEditMode)
            {
                if (Input.GetKeyDown (uiKeyCodeEdit))
                    ExitEditMode ();
                else
                {
                    bool cursorInGUI = WingProceduralManager.uiRectWindowEditor.Contains (UIUtility.GetMousePos ());
                    if (!cursorInGUI && Input.GetKeyDown(KeyCode.Mouse0))
                        ExitEditMode ();
                }
            }
        }

        private void CheckAllFieldValues(out bool geometryUpdate, out bool aeroUpdate)
        {
            geometryUpdate = aeroUpdate = false;

            // all the fields that affect aero
            geometryUpdate |= CheckFieldValue(sharedBaseLength, ref sharedBaseLengthCached);
            geometryUpdate |= CheckFieldValue(sharedBaseWidthRoot, ref sharedBaseWidthRootCached);
            geometryUpdate |= CheckFieldValue(sharedBaseWidthTip, ref sharedBaseWidthTipCached);
            geometryUpdate |= CheckFieldValue(sharedBaseThicknessRoot, ref sharedBaseThicknessRootCached);
            geometryUpdate |= CheckFieldValue(sharedBaseThicknessTip, ref sharedBaseThicknessTipCached);
            geometryUpdate |= CheckFieldValue(sharedBaseOffsetRoot, ref sharedBaseOffsetRootCached);
            geometryUpdate |= CheckFieldValue(sharedBaseOffsetTip, ref sharedBaseOffsetTipCached);

            geometryUpdate |= CheckFieldValue(sharedEdgeTypeTrailing, ref sharedEdgeTypeTrailingCached);
            geometryUpdate |= CheckFieldValue(sharedEdgeWidthTrailingRoot, ref sharedEdgeWidthTrailingRootCached);
            geometryUpdate |= CheckFieldValue(sharedEdgeWidthTrailingTip, ref sharedEdgeWidthTrailingTipCached);

            geometryUpdate |= CheckFieldValue(sharedEdgeTypeLeading, ref sharedEdgeTypeLeadingCached);
            geometryUpdate |= CheckFieldValue(sharedEdgeWidthLeadingRoot, ref sharedEdgeWidthLeadingRootCached);
            geometryUpdate |= CheckFieldValue(sharedEdgeWidthLeadingTip, ref sharedEdgeWidthLeadingTipCached);

            if (geometryUpdate)
                aeroUpdate = true;

            // all the fields that have no aero effects

            geometryUpdate |= CheckFieldValue(sharedMaterialST, ref sharedMaterialSTCached);
            geometryUpdate |= CheckFieldValue(sharedColorSTOpacity, ref sharedColorSTOpacityCached);
            geometryUpdate |= CheckFieldValue(sharedColorSTHue, ref sharedColorSTHueCached);
            geometryUpdate |= CheckFieldValue(sharedColorSTSaturation, ref sharedColorSTSaturationCached);
            geometryUpdate |= CheckFieldValue(sharedColorSTBrightness, ref sharedColorSTBrightnessCached);

            geometryUpdate |= CheckFieldValue(sharedMaterialSB, ref sharedMaterialSBCached);
            geometryUpdate |= CheckFieldValue(sharedColorSBOpacity, ref sharedColorSBOpacityCached);
            geometryUpdate |= CheckFieldValue(sharedColorSBHue, ref sharedColorSBHueCached);
            geometryUpdate |= CheckFieldValue(sharedColorSBSaturation, ref sharedColorSBSaturationCached);
            geometryUpdate |= CheckFieldValue(sharedColorSBBrightness, ref sharedColorSBBrightnessCached);

            geometryUpdate |= CheckFieldValue(sharedMaterialET, ref sharedMaterialETCached);
            geometryUpdate |= CheckFieldValue(sharedColorETOpacity, ref sharedColorETOpacityCached);
            geometryUpdate |= CheckFieldValue(sharedColorETHue, ref sharedColorETHueCached);
            geometryUpdate |= CheckFieldValue(sharedColorETSaturation, ref sharedColorETSaturationCached);
            geometryUpdate |= CheckFieldValue(sharedColorETBrightness, ref sharedColorETBrightnessCached);

            geometryUpdate |= CheckFieldValue(sharedMaterialEL, ref sharedMaterialELCached);
            geometryUpdate |= CheckFieldValue(sharedColorELOpacity, ref sharedColorELOpacityCached);
            geometryUpdate |= CheckFieldValue(sharedColorELHue, ref sharedColorELHueCached);
            geometryUpdate |= CheckFieldValue(sharedColorELSaturation, ref sharedColorELSaturationCached);
            geometryUpdate |= CheckFieldValue(sharedColorELBrightness, ref sharedColorELBrightnessCached);
        }

        private bool CheckFieldValue(float fieldValue, ref float fieldCache)
        {
            if (fieldValue != fieldCache)
            {
                if (WPDebug.logUpdate)
                    DebugLogWithID("Update", "Detected value change");
                fieldCache = fieldValue;
                return true;
            }

            return false;
        }

        private void StopWindowTimeout ()
        {
            uiAdjustWindow = true;
            uiEditModeTimeout = false;
            uiEditModeTimer = 0.0f;

        }

        private void ExitEditMode ()
        {
            uiEditMode = false;
            uiEditModeTimeout = true;
            uiAdjustWindow = true;
        }

        private string GetWindowTitle ()
        {
            if (uiEditMode)
            {
                if (!isCtrlSrf)
                {
                    if (isWingAsCtrlSrf) return "All-moving control surface";
                    else return "Wing";
                }
                else return "Control surface";
            }
            else return "Inactive";
        }

        #endregion

        #region Coloration

        // XYZ
        // HSB
        // RGB

        private Color GetVertexColor (int side)
        {
            if (side == 0) 
                return ColorHSBToRGB (new Vector4 (sharedColorSTHue, sharedColorSTSaturation, sharedColorSTBrightness, sharedColorSTOpacity));
            else if (side == 1) 
                return ColorHSBToRGB (new Vector4 (sharedColorSBHue, sharedColorSBSaturation, sharedColorSBBrightness, sharedColorSBOpacity));
            else if (side == 2) 
                return ColorHSBToRGB (new Vector4 (sharedColorETHue, sharedColorETSaturation, sharedColorETBrightness, sharedColorETOpacity));
            else 
                return ColorHSBToRGB (new Vector4 (sharedColorELHue, sharedColorELSaturation, sharedColorELBrightness, sharedColorELOpacity));
        }

        private Vector2 GetVertexUV2 (float selectedLayer)
        {
            if (selectedLayer == 0)
                return new Vector2 (0f, 1f);
            else
                return new Vector2 ((selectedLayer - 1f) / 3f, 0f);
        }

        private Color ColorHSBToRGB (Vector4 hsbColor)
        {
            float r = hsbColor.z;
            float g = hsbColor.z;
            float b = hsbColor.z;
            if (hsbColor.y != 0)
            {
                float max = hsbColor.z;
                float dif = hsbColor.z * hsbColor.y;
                float min = hsbColor.z - dif;
                float h = hsbColor.x * 360f;
                if (h < 60f)
                {
                    r = max;
                    g = h * dif / 60f + min;
                    b = min;
                }
                else if (h < 120f)
                {
                    r = -(h - 120f) * dif / 60f + min;
                    g = max;
                    b = min;
                }
                else if (h < 180f)
                {
                    r = min;
                    g = max;
                    b = (h - 120f) * dif / 60f + min;
                }
                else if (h < 240f)
                {
                    r = min;
                    g = -(h - 240f) * dif / 60f + min;
                    b = max;
                }
                else if (h < 300f)
                {
                    r = (h - 240f) * dif / 60f + min;
                    g = min;
                    b = max;
                }
                else if (h <= 360f)
                {
                    r = max;
                    g = min;
                    b = -(h - 360f) * dif / 60 + min;
                }
                else
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }
            }
            return new Color (Mathf.Clamp01 (r), Mathf.Clamp01 (g), Mathf.Clamp01 (b), hsbColor.w);
        }

        #endregion

        #region Resources
        // Original code by Snjo
        // Modified to remove config support and string parsing and to add support for arbitrary volumes

        public class WPResource
        {
            public string name;
            public int ID;
            public float ratio;
            public double currentSupply = 0f;
            public float amount = 0f;
            public float maxAmount = 0f;

            public WPResource(string _name, float _ratio)
            {
                name = _name;
                ID = _name.GetHashCode();
                ratio = _ratio;
            }

            public WPResource(string _name)
            {
                name = _name;
                ID = _name.GetHashCode();
                ratio = 1f;
            }
        }

        public class WPInnerTank
        {
            public List<WPResource> resources = new List<WPResource> ();
        }

        private List<WPInnerTank> fuelConfigurationsList = new List<WPInnerTank> ();

        // Reference values for 3.25m3 tank and 1.0m3 tank
        // LF    | LFO
        // 420.0 | 189.0, 231.0
        // 134.4 | 60.48, 73.92

        public string[][] fuelResourceNames = new string[][] { new string[] { "Structural" }, new string[] { "LiquidFuel" }, new string[] { "LiquidFuel", "Oxidizer"}, new string[] { "MonoPropellant" } };
        public float[][] fuelPerCubicMeter = new float[][] { new float[] { 0.0f }, new float[] { 134.4f }, new float[] { 60.48f, 73.92f }, new float[] { 134.4f } };
        public float[] fuelCostPerUnit = new float[] { 0.0f, 0.6f, 0.875f, 0.750f };

        public bool fuelDisplayCurrentTankCost = false;
        public bool fuelShowInfo = false;

        [KSPField(isPersistant = true)] public Vector4 fuelCurrentAmount = new Vector4(-1, -1, -1, -1); // if val < 0, then fill to max, otherwise don't change
        [KSPField (isPersistant = true)] public int fuelSelectedTankSetup = 0;

        [KSPField (guiActive = false, guiActiveEditor = false, guiName = "Added cost")] public float fuelAddedCost = 0f;
        [KSPField (guiActive = false, guiActiveEditor = false, guiName = "Dry mass")] public float fuelDryMassInfo = 0f;

        private float fuelVolumeOld = 0f;

        /// <summary>
        /// Called from setup (part of Start() for editor and flight)
        /// </summary>
        private void FuelStart()
        {
            if (WPDebug.logFuel)
                DebugLogWithID("FuelStart", "Started");
            if (!(canBeFueled && useStockFuel))
                return;

            fuelConfigurationsList.Clear();
            for (int configID = 0; configID < fuelResourceNames.Length; configID++)
            {
                WPInnerTank newTank = new WPInnerTank();
                for (int nameID = 0; nameID < fuelResourceNames[configID].Length; nameID++)
                {
                    newTank.resources.Add(new WPResource(fuelResourceNames[configID][nameID].Trim(' ')));
                }
                fuelConfigurationsList.Add(newTank);
            }

            if (HighLogic.LoadedSceneIsEditor)
                FuelSetConfigurationToParts(false);
            FuelUpdateAmountsFromVolume(aeroStatVolume, false);
        }

        /// <summary>
        /// called in Update for standard wing panels, sets fuelCurrentAmount to the current part resource volume.
        /// The Vector4 of fuels is required because the stock resources get cleared frequently(?)
        /// </summary>
        private void FuelOnUpdate()
        {
            if (fuelSelectedTankSetup < fuelConfigurationsList.Count && fuelSelectedTankSetup >= 0 && fuelConfigurationsList[fuelSelectedTankSetup] != null)
            {
                for (int i = 0; i < fuelConfigurationsList[fuelSelectedTankSetup].resources.Count; i++)
                {
                    if (fuelConfigurationsList[fuelSelectedTankSetup].resources[i].name != "Structural")
                        FuelSetResource(i, (float)part.Resources[fuelConfigurationsList[fuelSelectedTankSetup].resources[i].name].amount);
                }
            }
        }

        /// <summary>
        /// takes a volume in m^3 and sets up max amounts (and current for stock)
        /// </summary>
        private void FuelUpdateAmountsFromVolume(float volume, bool reassignAfter)
        {
            if (!canBeFueled)
                return;

            if (!useStockFuel)
            {
                if (WPDebug.logFuel)
                    DebugLogWithID("FuelUpdateAmountsFromVolume", "Started for RF or MFT");
                if (part.Modules.Contains("ModuleFuelTanks"))
                {
                    PartModule module = part.Modules["ModuleFuelTanks"];
                    Type type = module.GetType();

                    double volumeRF = (double)volume;
                    if (assemblyRFUsed)
                        volumeRF *= 1000;     // RF requests units in liters instead of cubic meters
                    else if (assemblyMFTUsed)
                        volumeRF *= 173.9;  // MFT requests volume in units
                    type.GetField("volume").SetValue(module, volumeRF);
                    type.GetMethod("ChangeVolume").Invoke(module, new object[] { volumeRF });
                }
                else if (WPDebug.logFuel)
                    DebugLogWithID("FuelUpdateAmountsFromVolume", "Module not found");
            }
            else
            {
                if (WPDebug.logFuel)
                    DebugLogWithID("FuelUpdateAmountsFromVolume", "Started for stock fuel");
                for (int i = 0; i < fuelConfigurationsList.Count; ++i)
                {
                    for (int r = 0; r < fuelConfigurationsList[i].resources.Count; ++r)
                    {
                        float newAmount = fuelPerCubicMeter[i][r] * volume * 0.7f; // since not all volume is used
                        float prevPct = FuelGetResource(r) >= 0 ? FuelGetResource(r) / fuelConfigurationsList[i].resources[r].maxAmount : 1;
                        fuelConfigurationsList[i].resources[r].maxAmount = newAmount;
                        fuelConfigurationsList[i].resources[r].amount = !float.IsNaN(prevPct) ? Mathf.Min(newAmount * prevPct, newAmount) : newAmount;
                    }
                }
                if (reassignAfter)
                    FuelSetConfigurationToParts(false);
            }
            fuelVolumeOld = volume;
        }

        /// <summary>
        /// calls FuelSetResourcesToPart on this part and all it's symmetry counterparts. Only call from the editor
        /// </summary>
        private void FuelSetConfigurationToParts(bool calledByPlayer)
        {
            if (WPDebug.logFuel)
                DebugLogWithID("FuelAssignResourcesToPart", "Started");

            FuelSetResourcesToPart(part, calledByPlayer);
            for (int s = 0; s < part.symmetryCounterparts.Count; s++)
            {
                if (part.symmetryCounterparts[s] == null) // fixes nullref caused by removing mirror sym while hovering over attach location
                    continue;
                FuelSetResourcesToPart(part.symmetryCounterparts[s], calledByPlayer);
                WingProcedural wing = part.symmetryCounterparts[s].Modules.OfType<WingProcedural>().FirstOrDefault();
                if (wing != null)
                    wing.fuelSelectedTankSetup = fuelSelectedTankSetup;
            }

            UpdateWindow();
        }

        /// <summary>
        /// Updates part.Resources to match the changes
        /// </summary>
        private void FuelSetResourcesToPart(Part currentPart, bool calledByPlayer)
        {
            if (WPDebug.logFuel)
                DebugLogWithID("FuelSetupTankInPart", "Started");

            currentPart.Resources.list.Clear();
            PartResource[] partResources = currentPart.GetComponents<PartResource>();
            for (int i = 0; i < partResources.Length; i++)
                DestroyImmediate(partResources[i]);


            if (fuelVolumeOld != aeroStatVolume)
                FuelUpdateAmountsFromVolume(aeroStatVolume, false);
            
            for (int resourceIndex = 0; resourceIndex < fuelConfigurationsList[fuelSelectedTankSetup].resources.Count; resourceIndex++)
            {
                if (fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].name != "Structural")
                {
                    if (WPDebug.logFuel)
                        DebugLogWithID("FuelSetResourcesToPart", "Found wing with fuel | Stored amounts: " + fuelCurrentAmount);

                    ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                    newResourceNode.AddValue("name", fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].name);
                    if (calledByPlayer) // || fuelBrandNewPart)
                    {
                        if (WPDebug.logFuel)
                            DebugLogWithID("FuelSetResourcesToPart", "CBP, setting amount from max of " + fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].maxAmount);
                        FuelSetResource(resourceIndex, fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].amount);
                    }
                    newResourceNode.AddValue("amount", fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].amount);
                    newResourceNode.AddValue("maxAmount", fuelConfigurationsList[fuelSelectedTankSetup].resources[resourceIndex].maxAmount);
                    currentPart.AddResource(newResourceNode);
                }
            }
            currentPart.Resources.UpdateList();
            fuelAddedCost = FuelGetAddedCost();
        }

        /// <summary>
        /// returns cost of max amount of fuel that the tanks can carry with the current loadout
        /// </summary>
        /// <returns></returns>
        private float FuelGetAddedCost ()
        {
            float result = 0f;
            if (fuelSelectedTankSetup < fuelCostPerUnit.Length && fuelSelectedTankSetup < fuelConfigurationsList.Count && fuelSelectedTankSetup >= 0)
            {
                for (int i = 0; i < fuelConfigurationsList[fuelSelectedTankSetup].resources.Count; ++i)
                {
                    result += fuelCostPerUnit[fuelSelectedTankSetup] * fuelConfigurationsList[fuelSelectedTankSetup].resources[i].maxAmount;
                }
            }
            return result;
        }

        /// <summary>
        /// returns the current volume of fuel for internal use at the specified index. Valid indices are 0-3
        /// </summary>
        private float FuelGetResource (int number)
        {
            switch (number)
            {
                case 0:
                    return fuelCurrentAmount.x;
                case 1:
                    return fuelCurrentAmount.y;
                case 2:
                    return fuelCurrentAmount.z;
                case 3:
                    return fuelCurrentAmount.w;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// sets the current volume of fuel for internal use at the specified index. Valid indices are 0-3
        /// </summary>
        private void FuelSetResource (int number, float amount)
        {
            switch (number)
            {
                case 0:
                    fuelCurrentAmount.x = amount;
                    break;
                case 1:
                    fuelCurrentAmount.y = amount;
                    break;
                case 2:
                    fuelCurrentAmount.z = amount;
                    break;
                case 3:
                    fuelCurrentAmount.w = amount;
                    break;
            }
        }

        /// <summary>
        /// returns a string containing an abreviation of the current fuels and the number of units of each. eg LFO (360/420)
        /// </summary>
        private string FuelGUIGetConfigDesc()
        {
            if (fuelSelectedTankSetup == -1)
                return "Invalid";
            else
            {
                string units = "";
                if (fuelSelectedTankSetup == 1)
                    units += "LF (";
                else if (fuelSelectedTankSetup == 2)
                    units += "LFO (";
                else if (fuelSelectedTankSetup == 3)
                    units += "RCS (";
                else
                    units += "STR (";
                if (fuelConfigurationsList.Count > 0)
                {
                    for (int i = 0; i < fuelConfigurationsList[fuelSelectedTankSetup].resources.Count; ++i)
                    {
                        units += ((int)fuelConfigurationsList[fuelSelectedTankSetup].resources[i].maxAmount).ToString();
                        if (i == fuelConfigurationsList[fuelSelectedTankSetup].resources.Count - 1)
                            units += ")";
                        else
                            units += "/";
                    }
                }
                return units;
            }
        }

        public bool canBeFueled
        {
            get
            {
                return !isCtrlSrf && !isWingAsCtrlSrf;
            }
        }

        public bool useStockFuel
        {
            get
            {
                return !assemblyRFUsed && !assemblyMFTUsed;
            }
        }

        #endregion

        #region Interfaces

        public float GetModuleCost ()
        {
            if (!useStockFuel)
                return aeroUICost;
            else
                return FuelGetAddedCost () + aeroUICost;
        }

        public float GetModuleCost (float modifier)
        {
            return GetModuleCost();
        }

        public Vector3 GetModuleSize (Vector3 defaultSize)
        {
            // This is a seriously stupid Interface
            // it is called 4(!) times per part, the first two the vessel size has not changed, the second two it has changed
            // the return value is # meters to add/subtract from the vessel size, which happens even if the part is completely occluded by other parts (seriously, wtf)
            return Vector3.zero;
        }

        public float GetModuleMass(float defaultMass)
        {
            return part.mass - part.partInfo.partPrefab.mass;
        }
        #endregion

        #region Stock toolbar integration

        public static ApplicationLauncherButton stockButton = null;

        private void OnStockButtonSetup ()
        {
            stockButton = ApplicationLauncher.Instance.AddModApplication (OnStockButtonClick, OnStockButtonClick, null, null, null, null, ApplicationLauncher.AppScenes.SPH, (Texture) GameDatabase.Instance.GetTexture ("B9_Aerospace/Plugins/icon_stock", false));
        }

        public void OnStockButtonClick ()
        {
            uiWindowActive = !uiWindowActive;
        }

        public void editorAppDestroy()
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            bool stockButtonCanBeRemoved = true;
            WingProcedural[] components = GameObject.FindObjectsOfType<WingProcedural>();
            if (WPDebug.logEvents)
                DebugLogWithID("OnDestroy", "Invoked, with " + components.Length + " remaining components in the scene");
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] != null)
                    stockButtonCanBeRemoved = false;
            }
            if (stockButtonCanBeRemoved)
            {
                uiInstanceIDTarget = 0;
                if (stockButton != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(stockButton);
                    stockButton = null;
                }
            }
        }
        #endregion

        #region Dump state

        public void DumpState ()
        {
            string report = "State report on part " + this.GetInstanceID () + ":\n\n";
            Type type = this.GetType ();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            List<string> fieldNames = fields.Select(field => field.Name).ToList();
            List<object> fieldValues = fields.Select(field => field.GetValue(this)).ToList();
            if (fieldNames.Count == fieldValues.Count && fieldNames.Count == fields.Length)
            {
                for (int i = 0; i < fields.Length; ++i)
                {
                    if (!string.IsNullOrEmpty (fieldNames[i]))
                    {
                        if (fieldValues[i] != null) report += fieldNames[i] + ": " + fieldValues[i].ToString () + "\n";
                        else report += fieldNames[i] + ": null\n";
                    }
                    else report += "Field " + i.ToString () + " name not available\n";
                }
            }
            else report += "Field info size mismatch, list can't be printed";
            Debug.Log (report);
        }

        public void DumpExecutionTimes ()
        {
            Debug.Log ("Dumping execution time report, message list contains " + debugMessageList.Count);
            string report = "Execution time report on part " + this.GetInstanceID () + ":\n\n";
            int count = debugMessageList.Count;
            for (int i = 0; i < count; ++i)
            {
                report += "I: " + debugMessageList[i].interval + "\n> M: " + (debugMessageList[i].message.Length <= 140 ? (debugMessageList[i].message) : (debugMessageList[i].message.Substring (0, 135) + "(...)")) + "\n";
            }
            Debug.Log (report);
        }
        #endregion
    }
}
