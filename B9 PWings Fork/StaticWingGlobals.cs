using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WingProcedural
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class StaticWingGlobals : MonoBehaviour
    {
        public static List<WingTankConfiguration> wingTankConfigurations = new List<WingTankConfiguration>();

        public static Shader wingShader;

        public void Start()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("ProceduralWingFuelSetups"))
            {
                ConfigNode[] fuelNodes = node.GetNodes("FuelSet");
                for (int i = 0; i < fuelNodes.Length; ++i)
                    wingTankConfigurations.Add(new WingTankConfiguration(fuelNodes[i]));
            }

            StartCoroutine(LoadBundleAssets());
        }

        public IEnumerator LoadBundleAssets()
        {
            while (!Caching.ready)
                yield return null;
            using (WWW www = WWW.LoadFromCacheOrDownload("file://" + (Assembly.GetExecutingAssembly().Location).Replace("Plugins\\B9_Aerospace_WingStuff.dll", "wingshader.ksp"), 1))
            {
                yield return www;

                AssetBundle shaderBundle = www.assetBundle;
                Shader[] objects = shaderBundle.LoadAllAssets<Shader>();
                for (int i = 0; i < objects.Length; ++i)
                {
                    if (objects[i].name == "KSP/Specular Layered")
                    {
                        wingShader = objects[i] as Shader;
                        Debug.Log($"[B9 PWings] Wing shader \"{objects[i].name}\" loaded");
                    }
                }

                yield return new WaitForSeconds(10.0f); // unknown how neccesary this is
                shaderBundle.Unload(false); // unload the raw asset bundle
            }
        }
    }
}