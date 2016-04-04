using System;
using System.Reflection;
using UnityEngine;

namespace WingProcedural
{
    // Credit goes to xEvilReeperx

    public static class ResourceExtractor
    {
        public static Material GetEmbeddedMaterial (string name)
        {
            string str = string.Empty;
            return GetEmbeddedContents (name, Assembly.GetExecutingAssembly (), out str) ? new Material (str) : null;
        }

        public static bool GetEmbeddedContents (string resource, Assembly assembly, out string contents)
        {
            if (WPDebug.logUpdateMaterials)
                Debug.Log ("ResourceExtractor | GetEmbeddedContents | Resource: " + resource);
            contents = string.Empty;
            try
            {
                var stream = assembly.GetManifestResourceStream (resource);
                if (stream != null)
                {
                    var reader = new System.IO.StreamReader (stream);
                    contents = reader.ReadToEnd ();
                    return contents.Length > 0;
                }
                else
                {
                    Debug.Log ("ResourceExtractor | Stream is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogException (e);
            }
            return false;
        }
    }
}
