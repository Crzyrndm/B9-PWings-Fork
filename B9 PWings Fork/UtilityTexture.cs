using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WingProcedural
{
    public static class UtilityTexture
    {
        static Dictionary<int, Texture2D> textures = new Dictionary<int,Texture2D>();
        static public Texture2D ToTexture2D (this string base64, string id = null)
        {
            Texture2D tex = new Texture2D (16, 16);
            tex.LoadImage (Convert.FromBase64String (base64));

            int val = 0;
            if (!int.TryParse(id, out val))
                return tex;
            if (!textures.ContainsKey (val) || textures[val] == null)
                textures.Add (val, tex);
            else
            {
                Debug.Log ("vlbTexture.ToTexture2D() Error :: id <" + id + "> already exist and will be replaced");
                textures[val] = tex;
            }

            return tex;
        }
        
        static public Texture2D GetTexture2D (this Color c)
        {
            int colorKey = c.ToInt();
            if (textures == null)
                textures = new Dictionary<int, Texture2D> ();
            if (textures.ContainsKey (colorKey) && textures[colorKey] != null)
                return textures[colorKey];
            Texture2D tex = new Texture2D (1, 1, TextureFormat.ARGB32, false);
            tex.SetPixel (0, 0, c);
            tex.Apply ();
            textures.Add (colorKey, tex);
            return tex;
        }

        static public Rect GetRect (this Texture2D tex)
        {
            return new Rect (0, 0, tex.width, tex.height);
        }

        public static int ToInt (this Color c)
        {
            Color32 c32 = c;
            return (c32.a << 24) | (c32.r << 16) | (c32.g << 8) | c32.b;
        }

        public static Color WithAlpha (this Color c, float a)
        {
            return new Color (c.r, c.g, c.b, a);
        }

        public static int[] doubleSideTris(int[] toinvert)
        {
            List<int> invertTris = toinvert.ToList();
            for (int i = 0; i < toinvert.Length; i += 3)
            {
                invertTris.Add(toinvert[i + 2]);
                invertTris.Add(toinvert[i + 1]);
                invertTris.Add(toinvert[i]);
            }
            return invertTris.ToArray();
        }
    }
}
