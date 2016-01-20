using KSP;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WingProcedural
{
    public static class UIUtility
    {
        
        public static float FieldSlider (float value, float increment, float incrementLarge, Vector2 limits, string name, out bool changed, Color backgroundColor, int valueType, ref int delta, bool allowFine = true, int isOffset = 0, bool needDelta = true)
        {
            if (!WingProceduralManager.uiStyleConfigured)
                WingProceduralManager.ConfigureStyles ();
            GUILayout.BeginHorizontal ();
            double range = limits.y - limits.x;
            double value01 = (value - limits.x) / range; // rescaling value to be 0-100% of range for convenience
            double increment01 = increment / range;
            double valueOld = value01;
            float buttonWidth = 12, spaceWidth = 3;
            if (!needDelta)
                delta = 0;
         
            GUILayout.Label ("", WingProceduralManager.uiStyleLabelHint);
            Rect rectLast = GUILayoutUtility.GetLastRect ();
            Rect rectSlider = new Rect(rectLast.xMin + buttonWidth + spaceWidth, rectLast.yMin, rectLast.width - 2 * (buttonWidth + spaceWidth), rectLast.height);
            Rect rectSliderValue = new Rect (rectSlider.xMin, rectSlider.yMin, rectSlider.width * (float)value01, rectSlider.height - 3f);
            Rect rectButtonL = new Rect (rectLast.xMin, rectLast.yMin, buttonWidth, rectLast.height);
            Rect rectButtonR = new Rect (rectLast.xMin + rectLast.width - buttonWidth, rectLast.yMin, buttonWidth, rectLast.height);
            Rect rectLabelValue = new Rect (rectSlider.xMin + rectSlider.width * 0.75f, rectSlider.yMin, rectSlider.width * 0.25f, rectSlider.height);

            //Debug.Log("Slider created.");

            if (GUI.Button(rectButtonL, "", WingProceduralManager.uiStyleButton))
            {
                /* if (Input.GetMouseButtonUp(0) || !allowFine)
                     if ( limits.x - range >= 0)
                     {
                         delta -= 1;
                     }
                 else
                     {
                         value01 = limits.x;
                     }
                     else if (Input.GetMouseButtonUp(1))*/
                if (needDelta)
                {
                    if (limits.x - range >= 0 && isOffset != 1)
                    {
                        delta -= 1;
                        value01 = 0;
                    }
                    else if (isOffset == 1)
                    {
                        delta -= 1;
                        value01 = 0;
                    }
                    else
                        value01 = limits.x;
                }           
                // value01 -= increment01;
                //Debug.Log("Left: Allow fine = " + allowFine);
            }
            if (GUI.Button(rectButtonR, "", WingProceduralManager.uiStyleButton))
            {
                //if (Input.GetMouseButtonUp(1))
                //{
                if (needDelta)
                    delta += 1;
                    
                //}
               // else if (Input.GetMouseButtonUp(1) && allowFine)
               //     value01 += increment01;
                //Debug.Log("Right: Allow fine = " + allowFine);
            }

            if (rectLast.Contains(Event.current.mousePosition) && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) // right click drag doesn't work properly without the event check
                    && Event.current.type != EventType.MouseUp) // drag event covers this, but don't want it to
            {
                value01 = GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, WingProceduralManager.uiStyleSlider, WingProceduralManager.uiStyleSliderThumb);

                if (valueOld != value01)
                {
                    if (Input.GetMouseButton(0) || !allowFine) // normal control
                    {
                        double excess = value01 / increment01;
                        value01 -= (excess - Math.Round(excess)) * increment01;
                        //Debug.Log("Normal: value01 => " + value01);
                    }
                    else if (Input.GetMouseButton(1) && allowFine) // fine control
                    {
                        double excess = valueOld / increment01;
                        value01 = (valueOld - (excess - Math.Round(excess)) * increment01) + Math.Min(value01 - 0.5, 0.4999) * increment01;
                        //Debug.Log("Fine: value01 => " + value01);
                    }
                }
            }
            else
                GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, WingProceduralManager.uiStyleSlider, WingProceduralManager.uiStyleSliderThumb);


            value = Mathf.Clamp((float)(value01 * range + range * delta) , limits.x, limits.y); // lower limit is halved so the fine control can reduce it further but the normal tweak still snaps. Min makes -ve values work
            //value = (float)(value01 * range + limits.x);  //releases clamp
            changed = valueOld != value ? true : false;

            GUI.DrawTexture (rectSliderValue, backgroundColor.GetTexture2D ());
            GUI.Label (rectSlider, "  " + name, WingProceduralManager.uiStyleLabelHint);
            GUI.Label (rectLabelValue, GetValueTranslation (value, valueType), WingProceduralManager.uiStyleLabelHint);

            GUILayout.EndHorizontal ();
            //Vector2 value1 = new Vector2(value, delta);
            //Debug.Log("B9PW: Value changed to " + value + ", delta = " + delta);  //log it
            //return value1;
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

        public static Rect SetToScreenCenterAlways(this Rect r)
        {
            r.x = Screen.width / 2f - r.width / 2f;
            r.y = Screen.height / 2f - r.height / 2f;
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
        public static bool CheckBox (string desc, string choice1, string choice2, bool value, out bool changed)
        {
            float buttonWidth = 50;
            //float spaceWidth = 3;
            GUILayout.BeginHorizontal();
            GUILayout.Label("", WingProceduralManager.uiStyleLabelHint);
            Rect rectLast = GUILayoutUtility.GetLastRect();
            Rect rectButton = new Rect(rectLast.x + rectLast.width - 53, rectLast.y, buttonWidth, rectLast.height);
            //Rect rectChoice = new Rect(rectButton.xMin, rectButton.yMin, rectButton.width, rectButton.height); 
            Rect rectDesc = new Rect(rectLast.x, rectLast.y, rectLast.width  - 53, rectLast.height);
            string choice;
            changed = false;
            choice = getChoice(choice1, choice2, value);
            if (GUI.Button(rectButton, choice, WingProceduralManager.uiStyleButton))
            {
                value = !value;
                changed = true;
            }
            GUI.Label(rectDesc, "  " + desc, WingProceduralManager.uiStyleLabelHint);
            

            GUILayout.EndHorizontal();
            return value;
        }
        public static string getChoice(string choice1, string choice2, bool state)
        {
            string choice;
            if (!state)
                choice = choice1;
            else
                choice = choice2;
            return choice;
        }
    }
}
