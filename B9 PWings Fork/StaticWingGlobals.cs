using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            Debug.Log("[B9PW] start bundle load process");
            StartCoroutine(LoadBundleAssets());
        }

        public IEnumerator LoadBundleAssets()
        {
            Debug.Log("[B9PW] Aquiring bundle data");
            AssetBundle shaderBundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData" + Path.DirectorySeparatorChar + "B9_Aerospace_ProceduralWings" + Path.DirectorySeparatorChar + "wingshader");
            if (shaderBundle != null)
            {
                Shader[] objects = shaderBundle.LoadAllAssets<Shader>();
                for (int i = 0; i < objects.Length; ++i)
                {
                    if (objects[i].name == "KSP/Specular Layered")
                    {
                        wingShader = objects[i];
                        Debug.Log($"[B9 PWings] Wing shader \"{wingShader.name}\" loaded. Shader supported? {wingShader.isSupported}");
                    }
                }

                yield return null;
                yield return null; // unknown how neccesary this is

                Debug.Log("[B9PW] unloading bundle");
                shaderBundle.Unload(false); // unload the raw asset bundle
            }
            else
            {
                Debug.Log("[B9PW] Error: Found no asset bundle to load");
            }
        }
    }
}