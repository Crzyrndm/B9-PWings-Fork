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
            Shader shader;
            return GetEmbeddedContents (name, Assembly.GetExecutingAssembly (), out shader) ? new Material (shader) : null;
        }

        public static bool GetEmbeddedContents (string resource, Assembly assembly, out Shader shader )//string contents
        {
            if (WPDebug.logUpdateMaterials)
                Debug.Log ("ResourceExtractor | GetEmbeddedContents | Resource: " + resource);
            //contents = string.Empty;
            shader = null;
            try
            {
                var stream = assembly.GetManifestResourceStream (resource);
                if (stream != null)
                {
                    var reader = new System.IO.StreamReader (stream);
                    string contents = reader.ReadToEnd ();
                    shader = Shader.Find(contents);
                    //return contents.Length > 0;
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
