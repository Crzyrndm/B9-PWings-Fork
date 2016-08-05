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
            while (!Caching.ready)
                yield return null;
            Debug.Log("[B9PW] Aquiring bundle data");
            using (WWW www = WWW.LoadFromCacheOrDownload("file://" + KSPUtil.ApplicationRootPath + Path.DirectorySeparatorChar + "GameData"
                                                                            + Path.DirectorySeparatorChar + "B9_Aerospace_ProceduralWings" + Path.DirectorySeparatorChar + "wingshader.ksp", 1))
            {
                yield return www;

                AssetBundle shaderBundle = www.assetBundle;
                Shader[] objects = shaderBundle.LoadAllAssets<Shader>();
                for (int i = 0; i < objects.Length; ++i)
                {
                    if (objects[i].name == "KSP/Specular Layered")
                    {
                        wingShader = objects[i];
                        Debug.Log($"[B9 PWings] Wing shader \"{wingShader.name}\" loaded. Shadeer supported? {wingShader.isSupported}");
                    }
                }

                yield return null; // unknown how neccesary this is
                yield return null; // unknown how neccesary this is

                Debug.Log("[B9PW] unloading bundle");
                shaderBundle.Unload(false); // unload the raw asset bundle
            }
        }
    }
}