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
        public static float FieldSlider (float value, float increment, float incrementLarge, Vector2 limits, string name, out bool changed, Color backgroundColor, int valueType, bool allowFine = true)
        {
            if (!WingProceduralManager.uiStyleConfigured)
                WingProceduralManager.ConfigureStyles ();
            GUILayout.BeginHorizontal ();
            double range = limits.y - limits.x;
            double value01 = (value - limits.x) / range; // rescaling value to be 0-100% of range for convenience
            double increment01 = increment / range;
            double valueOld = value01;
            changed = false;

            GUILayout.Label ("", WingProceduralManager.uiStyleLabelHint);
            Rect rectLast = GUILayoutUtility.GetLastRect (); 
            Rect rectSlider = new Rect (rectLast.xMin + 15f, rectLast.yMin, rectLast.width - 30f, rectLast.height);
            Rect rectSliderValue = new Rect (rectSlider.xMin, rectSlider.yMin, rectSlider.width * (float)value01, rectSlider.height - 3f);
            Rect rectButtonL = new Rect (rectLast.xMin, rectLast.yMin, 12f, rectLast.height);
            Rect rectButtonR = new Rect (rectLast.xMin + rectLast.width - 12f, rectLast.yMin, 12f, rectLast.height);
            Rect rectLabelValue = new Rect (rectSlider.xMin + rectSlider.width * 0.75f, rectSlider.yMin, rectSlider.width * 0.25f, rectSlider.height);

            if (GUI.Button(rectButtonL, new GUIContent(""), WingProceduralManager.uiStyleButton))
            {
                if (Input.GetMouseButtonUp(0))
                    value01 -= 0.0625;
                else if (Input.GetMouseButtonUp(1))
                    value01 -= 0.0625 / 128;

                value = Mathf.Clamp((float)(value01 * range + limits.x), (float)(limits.x * 0.5), limits.y);
                if (valueOld != value)
                    changed = true;
            }
            else if (GUI.Button(rectButtonR, new GUIContent(""), WingProceduralManager.uiStyleButton))
            {
                if (Input.GetMouseButtonUp(0))
                    value01 += 0.0625;
                else if (Input.GetMouseButtonUp(1))
                    value01 += 0.0625 / 128;

                value = Mathf.Clamp((float)(value01 * range + limits.x), (float)(limits.x * 0.5), limits.y);
                if (valueOld != value)
                    changed = true;
            }

            if (rectLast.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag) // right click drag doesn't work without the event check
            {
                value01 = GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, WingProceduralManager.uiStyleSlider, WingProceduralManager.uiStyleSliderThumb);

                if (valueOld != value01 && rectLast.Contains(Event.current.mousePosition))
                {
                    if (Input.GetMouseButton(0))
                    {
                        double excess = value01 % increment01;
                        if (value01 > valueOld)
                        {
                            if (excess > increment01 / 2)
                                value01 = value01 - excess + increment01;
                            else
                                value01 = value01 - excess;
                        }
                        else if (value01 < valueOld)
                        {
                            if (excess < increment01 / 2)
                                value01 = value01 - excess;
                            else
                                value01 = value01 - excess + increment01;
                        }
                    }
                    else if (Input.GetMouseButton(1) && allowFine)
                    {
                        double excess = valueOld / increment01; // modulus is never negative, which makes the match somewhat annoying. Do it manually instead
                        excess = (excess - Math.Round(excess)) * increment01;
                        double valueIncrement = valueOld - excess;
                        value01 = valueOld - excess + Math.Min(value01 - 0.5, 0.499) * increment01;
                    }
                }

                value = Mathf.Clamp((float)(value01 * range + limits.x), (float)(limits.x * 0.5), limits.y);
                if (valueOld != value)
                    changed = true;
            }
            else
                GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, WingProceduralManager.uiStyleSlider, WingProceduralManager.uiStyleSliderThumb);


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

        public static Vector3 GetMouseWindowPos(Rect windowRect)
        {
            Vector3 mousepos = GetMousePos();
            mousepos.x -= windowRect.x;
            mousepos.y -= windowRect.y;
            return mousepos;
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
