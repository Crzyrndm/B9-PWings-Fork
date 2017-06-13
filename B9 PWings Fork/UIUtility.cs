using System;
using UnityEngine;

namespace WingProcedural
{
    public static class UIUtility
    {
        public static Rect uiRectWindowEditor = new Rect();

        public static GUIStyle uiStyleWindow = new GUIStyle();
        public static GUIStyle uiStyleLabelMedium = new GUIStyle();
        public static GUIStyle uiStyleLabelHint = new GUIStyle();
        public static GUIStyle uiStyleButton = new GUIStyle();
        public static GUIStyle uiStyleSlider = new GUIStyle();
        public static GUIStyle uiStyleSliderThumb = new GUIStyle();
        public static GUIStyle uiStyleToggle = new GUIStyle();
        public static bool uiStyleConfigured = false;

        public static Font uiFont = null;
        private static float alphaNormal = 0.5f;
        private static float alphaHover = 0.35f;
        private static float alphaActive = 0.75f;

        public static void ConfigureStyles()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            }
            if (uiFont != null)
            {
                uiStyleWindow = new GUIStyle(HighLogic.Skin.window) {
                    fixedWidth = 300f,
                    wordWrap = true
                };
                uiStyleWindow.normal.textColor = Color.white;
                uiStyleWindow.font = uiFont;
                uiStyleWindow.fontStyle = FontStyle.Normal;
                uiStyleWindow.fontSize = 13;
                uiStyleWindow.alignment = TextAnchor.UpperLeft;

                uiStyleLabelMedium = new GUIStyle(HighLogic.Skin.label) {
                    stretchWidth = true,
                    font = uiFont,
                    fontStyle = FontStyle.Normal,
                    fontSize = 13
                };
                uiStyleLabelMedium.normal.textColor = Color.white;

                uiStyleLabelHint = new GUIStyle(HighLogic.Skin.label) {
                    stretchWidth = true,
                    font = uiFont,
                    fontStyle = FontStyle.Normal,
                    fontSize = 11
                };
                uiStyleLabelHint.normal.textColor = Color.white;

                uiStyleButton = new GUIStyle(HighLogic.Skin.button);
                AssignTexturesToStyle(uiStyleButton);
                uiStyleButton.padding = new RectOffset(0, 0, 0, 0);
                uiStyleButton.overflow = new RectOffset(0, 0, 0, 0);
                uiStyleButton.font = uiFont;
                uiStyleButton.fontStyle = FontStyle.Normal;
                uiStyleButton.fontSize = 11;
                uiStyleButton.fixedHeight = 16;

                uiStyleSlider = new GUIStyle(HighLogic.Skin.horizontalSlider);
                AssignTexturesToStyle(uiStyleSlider);
                uiStyleSlider.border = new RectOffset(0, 0, 0, 0);
                uiStyleSlider.margin = new RectOffset(4, 4, 4, 4);
                uiStyleSlider.padding = new RectOffset(0, 0, 0, 0);
                uiStyleSlider.overflow = new RectOffset(0, 0, 0, 0);
                uiStyleSlider.fixedHeight = 16;

                uiStyleSliderThumb = new GUIStyle(HighLogic.Skin.horizontalSliderThumb);
                AssignTexturesToStyle(uiStyleSlider);
                uiStyleSliderThumb.border = new RectOffset(0, 0, 0, 0);
                uiStyleSliderThumb.margin = new RectOffset(4, 4, 4, 4);
                uiStyleSliderThumb.padding = new RectOffset(0, 0, 0, 0);
                uiStyleSliderThumb.overflow = new RectOffset(0, 0, 0, 0);
                uiStyleSliderThumb.normal.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.hover.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.active.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.onNormal.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.onHover.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.onActive.background = Color.black.WithAlpha(0).GetTexture2D();
                uiStyleSliderThumb.fixedWidth = 0f;
                uiStyleSliderThumb.fixedHeight = 16;

                uiStyleToggle = new GUIStyle(HighLogic.Skin.toggle) {
                    font = uiFont,
                    fontStyle = FontStyle.Normal,
                    fontSize = 11
                };
                uiStyleToggle.normal.textColor = Color.white;
                uiStyleToggle.padding = new RectOffset(4, 4, 4, 4);
                uiStyleToggle.margin = new RectOffset(4, 4, 4, 4);

                uiStyleConfigured = true;
            }
        }

        private static void AssignTexturesToStyle(GUIStyle s)
        {
            s.normal.textColor = s.onNormal.textColor = Color.white;
            s.hover.textColor = s.onHover.textColor = Color.white;
            s.active.textColor = s.onActive.textColor = Color.white;

            s.normal.background = Color.black.WithAlpha(alphaNormal).GetTexture2D();
            s.hover.background = Color.black.WithAlpha(alphaHover).GetTexture2D();
            s.active.background = Color.black.WithAlpha(alphaActive).GetTexture2D();
            s.onNormal.background = Color.black.WithAlpha(alphaNormal).GetTexture2D();
            s.onHover.background = Color.black.WithAlpha(alphaHover).GetTexture2D();
            s.onActive.background = Color.black.WithAlpha(alphaActive).GetTexture2D();
            uiStyleButton.border = new RectOffset(0, 0, 0, 0);
        }

        public static float FieldSlider(float value, float increment, float incrementLarge, Vector2 limits, string name, out bool changed, Color backgroundColor, int valueType, bool allowFine = true)
        {
            if (!UIUtility.uiStyleConfigured)
            {
                UIUtility.ConfigureStyles();
            }

            GUILayout.BeginHorizontal();
            double range = limits.y - limits.x;
            double value01 = (value - limits.x) / range; // rescaling value to be <0-1> of range for convenience
            double increment01 = increment / range;
            double valueOld = value01;
            const float buttonWidth = 12, spaceWidth = 3;

            GUILayout.Label(string.Empty, UIUtility.uiStyleLabelHint);
            Rect rectLast = GUILayoutUtility.GetLastRect();
            var rectSlider = new Rect(rectLast.xMin + buttonWidth + spaceWidth, rectLast.yMin, rectLast.width - 2 * (buttonWidth + spaceWidth), rectLast.height);
            var rectSliderValue = new Rect(rectSlider.xMin, rectSlider.yMin, rectSlider.width * (float)value01, rectSlider.height - 3f);
            var rectButtonL = new Rect(rectLast.xMin, rectLast.yMin, buttonWidth, rectLast.height);
            var rectButtonR = new Rect(rectLast.xMin + rectLast.width - buttonWidth, rectLast.yMin, buttonWidth, rectLast.height);
            var rectLabelValue = new Rect(rectSlider.xMin + rectSlider.width * 0.75f, rectSlider.yMin, rectSlider.width * 0.25f, rectSlider.height);

            if (GUI.Button(rectButtonL, string.Empty, UIUtility.uiStyleButton))
            {
                if (Input.GetMouseButtonUp(0) || !allowFine)
                {
                    value01 -= incrementLarge / range;
                }
                else if (Input.GetMouseButtonUp(1) && allowFine)
                {
                    value01 -= increment01;
                }
            }
            if (GUI.Button(rectButtonR, string.Empty, UIUtility.uiStyleButton))
            {
                if (Input.GetMouseButtonUp(0) || !allowFine)
                {
                    value01 += incrementLarge / range;
                }
                else if (Input.GetMouseButtonUp(1) && allowFine)
                {
                    value01 += increment01;
                }
            }

            if (rectLast.Contains(Event.current.mousePosition) && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) // right click drag doesn't work properly without the event check
                    && Event.current.type != EventType.MouseUp) // drag event covers this, but don't want it to
            {
                value01 = GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, UIUtility.uiStyleSlider, UIUtility.uiStyleSliderThumb);

                if (valueOld != value01)
                {
                    if (Input.GetMouseButton(0) || !allowFine) // normal control
                    {
                        double excess = value01 / increment01;
                        value01 -= (excess - Math.Round(excess)) * increment01;
                    }
                    else if (Input.GetMouseButton(1) && allowFine) // fine control
                    {
                        double excess = valueOld / increment01;
                        value01 = (valueOld - (excess - Math.Round(excess)) * increment01) + Math.Min(value01 - 0.5, 0.4999) * increment01;
                    }
                }
            }
            else
            {
                GUI.HorizontalSlider(rectSlider, (float)value01, 0f, 1f, UIUtility.uiStyleSlider, UIUtility.uiStyleSliderThumb);
            }

            value = Mathf.Clamp((float)(value01 * range + limits.x), Mathf.Min((float)(limits.x * 0.5), limits.x), limits.y); // lower limit is halved so the fine control can reduce it further but the normal tweak still snaps. Min makes -ve values work
            changed = valueOld != value;

            GUI.DrawTexture(rectSliderValue, backgroundColor.GetTexture2D());
            GUI.Label(rectSlider, $"  {name}", UIUtility.uiStyleLabelHint);
            GUI.Label(rectLabelValue, GetValueTranslation(value, valueType), UIUtility.uiStyleLabelHint);

            GUILayout.EndHorizontal();
            return value;
        }

        public static Rect ClampToScreen(Rect window)
        {
            window.x = Mathf.Clamp(window.x, -window.width + 20, Screen.width - 20);
            window.y = Mathf.Clamp(window.y, -window.height + 20, Screen.height - 20);

            return window;
        }

        public static Rect SetToScreenCenter(this Rect r)
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

        public static double TextEntryForDouble(string label, int labelWidth, double prevValue)
        {
            string valString = prevValue.ToString();
            TextEntryField(label, labelWidth, ref valString);

            if (!double.TryParse(valString, out double temp))
            {
                return prevValue;
            }

            return temp;
        }

        public static void TextEntryField(string label, int labelWidth, ref string inputOutput)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            inputOutput = GUILayout.TextField(inputOutput);
            GUILayout.EndHorizontal();
        }

        private static Vector3 mousePos = Vector3.zero;

        public static Vector3 GetMousePos()
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

        private static readonly string[][] stringIDs = new string[][] { new string[] { "Uniform", "Standard", "Reinforced", "LRSI", "HRSI" },
                                                               new string[] { "", "No Edge", "Rounded", "Biconvex", "Triangular" },
                                                               new string[] { "", "No Edge", "Rounded", "Biconvex", "Triangular" }}; // yup, I'm feeling lazy here...

        public static string GetValueTranslation(float value, int type)
        {
            if (type < 1 || type > 3)
            {
                return value.ToString("F3");
            }
            else
            {
                return stringIDs[type - 1][(int)value]; // dont check for range errors, any range exceptions need to be visible in testing
            }
        }
    }
}