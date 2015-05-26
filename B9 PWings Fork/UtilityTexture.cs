using KSP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WingProcedural
{
    public static class UtilityTexture
    {
        static Dictionary<string, Texture2D> textures;
        static public Texture2D ToTexture2D (this string base64, string id = null)
        {
            var tex = new Texture2D (16, 16);
            tex.LoadImage (Convert.FromBase64String (base64));

            if (string.IsNullOrEmpty (id)) return tex;
            if (textures == null) textures = new Dictionary<string, Texture2D> ();
            if (!textures.ContainsKey (id) || textures[id] == null)
            {
                textures.Add (id, tex);
            }
            else
            {
                Debug.Log ("vlbTexture.ToTexture2D() Error :: id <" + id + "> already exist and will be replaced");
                textures[id] = tex;
            }

            return tex;
        }

        static public bool HasTextureId (string id)
        {
            return textures != null && textures.ContainsKey (id) && textures[id] != null;
        }
        static public Texture2D GetTextureFromId (this string id)
        {
            if (string.IsNullOrEmpty (id))
            {
                Debug.LogWarning ("vlbTexture.GetTextureFromId() Error :: id should not be null or empty");
                return null;
            }
            if (textures == null || !textures.ContainsKey (id))
            {
                Debug.LogWarning ("vlbTexture.GetTextureFromId() Error :: id <" + id + "> not found, consider adding it first by calling base64Source.ToTexture2D(" + id + ")");
                return null;
            }

            if (textures[id] != null) return textures[id];

            Debug.LogWarning ("vlbTexture.GetTextureFromId() Error : texture with id <" + id + "> is destroyed, consider adding it again");
            return null;
        }
        static public Texture2D GetTexture2D (this Color c)
        {
            var colorKey = c.ToInt ().ToString ();
            if (textures == null) textures = new Dictionary<string, Texture2D> ();
            if (textures.ContainsKey (colorKey) && textures[colorKey] != null) return textures[colorKey];
            var tex = new Texture2D (1, 1, TextureFormat.ARGB32, false);
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
