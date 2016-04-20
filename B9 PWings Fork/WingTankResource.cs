<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
=======
﻿using System.Linq;
>>>>>>> refs/remotes/Crzyrndm/master

namespace WingProcedural
{
    public class WingTankResource : IConfigNode
    {
        public PartResourceDefinition resource;
<<<<<<< HEAD
        public float unitsPerVolume; // resource units per 1m^3 of wing
=======
        public float unitsPerVolume = 200; // resource units per 1m^3 of wing, default to 5L per unit
>>>>>>> refs/remotes/Crzyrndm/master

        public WingTankResource(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
<<<<<<< HEAD
            resource = PartResourceLibrary.Instance.resourceDefinitions[node.GetValue("name").GetHashCode()];
            float.TryParse(node.GetValue("unitsPerVolume"), out unitsPerVolume);
=======
            int resourceID = node.GetValue("name").GetHashCode();
            if (PartResourceLibrary.Instance.resourceDefinitions.Any(rd => rd.id == resourceID))
            {
                resource = PartResourceLibrary.Instance.resourceDefinitions[resourceID];
                float.TryParse(node.GetValue("unitsPerVolume"), out unitsPerVolume);
            }
>>>>>>> refs/remotes/Crzyrndm/master
        }

        public void Save(ConfigNode node) { }
    }
}
