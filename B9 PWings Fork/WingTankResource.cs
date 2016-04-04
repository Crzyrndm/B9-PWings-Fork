using System.Linq;

namespace WingProcedural
{
    public class WingTankResource : IConfigNode
    {
        public PartResourceDefinition resource;
        public float unitsPerVolume = 200; // resource units per 1m^3 of wing, default to 5L per unit

        public WingTankResource(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            int resourceID = node.GetValue("name").GetHashCode();
            if (PartResourceLibrary.Instance.resourceDefinitions.Any(rd => rd.id == resourceID))
            {
                resource = PartResourceLibrary.Instance.resourceDefinitions[resourceID];
                float.TryParse(node.GetValue("unitsPerVolume"), out unitsPerVolume);
            }
        }

        public void Save(ConfigNode node) { }
    }
}
