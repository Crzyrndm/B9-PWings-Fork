using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WingProcedural
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class StaticWingGlobals : MonoBehaviour
    {
        public static List<WingTankConfiguration> wingTankConfigurations = new List<WingTankConfiguration>();

        public void Start()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("ProceduralWingFuelSetups").FirstOrDefault().GetNodes("FuelSet");
            for (int i = 0; i < nodes.Length; ++i )
                wingTankConfigurations.Add(new WingTankConfiguration(nodes[i]));
        }
    }
}
