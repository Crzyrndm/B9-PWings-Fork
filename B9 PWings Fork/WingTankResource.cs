

using System.Collections.Generic;
using System.Linq;
using System.Text;




namespace WingProcedural
{
    public class WingTankResource : IConfigNode
    {
        public PartResourceDefinition resource;
<<<<<<< HEAD

        public float unitsPerVolume; // resource units per 1m^3 of wing

        //public float unitsPerVolume = 200; // resource units per 1m^3 of wing, default to 5L per unit

=======
        public float unitsPerVolume;
        public float ratio = 1.0f; // fuel type fraction = ratio/sum(ratio)
>>>>>>> refs/remotes/Crzyrndm/master

        public WingTankResource(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {

            resource = PartResourceLibrary.Instance.resourceDefinitions[node.GetValue("name").GetHashCode()];
            float.TryParse(node.GetValue("unitsPerVolume"), out unitsPerVolume);

            int resourceID = node.GetValue("name").GetHashCode();
            if (PartResourceLibrary.Instance.resourceDefinitions.Any(rd => rd.id == resourceID))
            {
                resource = PartResourceLibrary.Instance.resourceDefinitions[resourceID];
                float.TryParse(node.GetValue("ratio"), out ratio);
            }

        }

        public void Save(ConfigNode node) { }

        public void SetUnitsPerVolume(float ratioTotal)
        {
            if (resource.volume == 0)
                unitsPerVolume = ratio;
            else
                unitsPerVolume = ratio * 1000.0f / (resource.volume * ratioTotal);
        }
    }
}
