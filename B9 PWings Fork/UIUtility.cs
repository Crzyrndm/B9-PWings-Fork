using KSP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WingProcedural
{
    public static class UIUtility
    {
        public static float FieldSlider (float value, float increment, float incrementLarge, Vector2 limits, string name, out bool changed, Color backgroundColor, int valueType)
        {
            if (!WingProceduralManager.uiStyleConfigured) WingProceduralManager.ConfigureStyles ();
            GUILayout.BeginHorizontal ();

            float valueOld = value;
            float valueFromButtons = 0f;
            changed = false;

            GUILayout.Label ("", WingProceduralManager.uiStyleLabelHint);
            Rect rectLast = GUILayoutUtility.GetLastRect (); 
            Rect rectSlider = new Rect (rectLast.xMin + 8f, rectLast.yMin, rectLast.width - 16f, rectLast.height);
            Rect rectSliderValue = new Rect (rectSlider.xMin, rectSlider.yMin, rectSlider.width * ((value - limits.x) / (limits.y - limits.x)), rectSlider.height - 3f);
            Rect rectButtonL = new Rect (rectLast.xMin, rectLast.yMin, 6f, rectLast.height);
            Rect rectButtonR = new Rect (rectLast.xMin + rectLast.width - 6f, rectLast.yMin, 6f, rectLast.height);
            Rect rectLabelValue = new Rect (rectSlider.xMin + rectSlider.width * 0.75f, rectSlider.yMin, rectSlider.width * 0.25f, rectSlider.height); 

            if (GUI.Button (rectButtonL, new GUIContent (""), WingProceduralManager.uiStyleButton)) valueFromButtons -= incrementLarge;
            value = GUI.HorizontalSlider (rectSlider, value, limits.x, limits.y, WingProceduralManager.uiStyleSlider, WingProceduralManager.uiStyleSliderThumb);
            if (GUI.Button (rectButtonR, new GUIContent (""), WingProceduralManager.uiStyleButton)) valueFromButtons += incrementLarge;
            value += valueFromButtons;

            if (valueOld != value)
            {
                value = Mathf.Clamp (value, limits.x, limits.y);
                if (valueOld != value)
                {
                    float excess = value % increment;
                    if (value > valueOld)
                    {
                        if (excess > increment / 2f) value = Mathf.Min (value - excess + increment, limits.y);
                        else value = value - excess;
                    }

                    else if (value < valueOld)
                    {
                        if (excess < increment / 2f) value = Mathf.Max (limits.x, value - excess);
                        else value = value - excess + increment;
                    }

                    value = Mathf.Clamp (value, limits.x, limits.y);
                    if (valueOld != value) changed = true;
                }
            }

            GUI.DrawTexture (rectSliderValue, backgroundColor.GetTexture2D ());
            GUI.Label (rectSlider, "  " + name, WingProceduralManager.uiStyleLabelHint);
            GUI.Label (rectLabelValue, GetValueTranslation (value, valueType), WingProceduralManager.uiStyleLabelHint);

            GUILayout.EndHorizontal ();
            return value;
        }

        public static Rect ClampToScreen (Rect window)
        {
            window.x = Mathf.Clamp (window.x, -window.width + 20, Screen.width - 20);
            window.y = Mathf.Clamp (window.y, -window.height + 20, Screen.height - 20);

            return window;
        }

        public static Rect SetToScreenCenter (this Rect r)
        {
            if (r.width > 0 && r.height > 0)
            {
                r.x = Screen.width / 2f - r.width / 2f;
                r.y = Screen.height / 2f - r.height / 2f;
            }
            return r;
        }

        public static double TextEntryForDouble (string label, int labelWidth, double prevValue)
        {
            string valString = prevValue.ToString ();
            UIUtility.TextEntryField (label, labelWidth, ref valString);

            if (!Regex.IsMatch (valString, @"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$"))
                return prevValue;

            return double.Parse (valString);
        }

        public static void TextEntryField (string label, int labelWidth, ref string inputOutput)
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Label (label, GUILayout.Width (labelWidth));
            inputOutput = GUILayout.TextField (inputOutput);
            GUILayout.EndHorizontal ();
        }


        private static Vector3 mousePos = Vector3.zero;

        public static Vector3 GetMousePos ()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            return mousePos;
        }

        public static string GetValueTranslation (float value, int type)
        {
            if (type == 1)
            {
                if (value == 0f) return "Uniform";
                else if (value == 1f) return "Standard";
                else if (value == 2f) return "Reinforced";
                else if (value == 3f) return "LRSI";
                else if (value == 4f) return "HRSI";
                else return "Unknown material";
            }
            else if (type == 2)
            {
                if (value == 1f) return "No edge";
                else if (value == 2f) return "Rounded";
                else if (value == 3f) return "Biconvex";
                else if (value == 4f) return "Triangular";
                else return "Unknown";
            }
            else if (type == 3)
            {
                if (value == 1f) return "Rounded";
                else if (value == 2f) return "Biconvex";
                else if (value == 3f) return "Triangular";
                else return "Unknown";
            }
            else return value.ToString ("F3");
        }
    }
}
