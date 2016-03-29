using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WingProcedural
{
    public class WingTankResource : IConfigNode
    {
        public PartResourceDefinition resource;
        public float unitsPerVolume; // resource units per 1m^3 of wing

        public WingTankResource(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            resource = PartResourceLibrary.Instance.resourceDefinitions[node.GetValue("name").GetHashCode()];
            float.TryParse(node.GetValue("unitsPerVolume"), out unitsPerVolume);
        }

        public void Save(ConfigNode node) { }
    }
}
