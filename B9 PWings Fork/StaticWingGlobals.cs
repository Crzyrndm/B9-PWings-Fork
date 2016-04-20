<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
=======
﻿using System.Collections.Generic;
>>>>>>> refs/remotes/Crzyrndm/master
using UnityEngine;

namespace WingProcedural
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class StaticWingGlobals : MonoBehaviour
    {
        public static List<WingTankConfiguration> wingTankConfigurations = new List<WingTankConfiguration>();

        public void Start()
        {
<<<<<<< HEAD
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("ProceduralWingFuelSetups").FirstOrDefault().GetNodes("FuelSet");
            for (int i = 0; i < nodes.Length; ++i )
                wingTankConfigurations.Add(new WingTankConfiguration(nodes[i]));
        }
    }
}
=======
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("ProceduralWingFuelSetups"))
            {
                ConfigNode[] fuelNodes = node.GetNodes("FuelSet");
                for (int i = 0; i < fuelNodes.Length; ++i)
                    wingTankConfigurations.Add(new WingTankConfiguration(fuelNodes[i]));
            }
        }
    }
}
>>>>>>> refs/remotes/Crzyrndm/master
