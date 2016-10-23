using System.Collections.Generic;

namespace WingProcedural
{
    public class WingTankConfiguration : IConfigNode
    {
        public string GUIName;
        public Dictionary<string, WingTankResource> resources = new Dictionary<string, WingTankResource>();

        public WingTankConfiguration(ConfigNode node)
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            float ratioTotal = 0;

            GUIName = node.GetValue("name");
            ConfigNode[] nodes = node.GetNodes("Resource");
            for (int i = 0; i < nodes.Length; ++i)
            {
                WingTankResource res = new WingTankResource(nodes[i]);
                if (res.resource != null)
                {
                    resources.Add(res.resource.name, res);
                    ratioTotal += res.ratio;
                }
            }
            foreach (KeyValuePair<string, WingTankResource> kvp in resources)
                kvp.Value.SetUnitsPerVolume(ratioTotal);
        }

        public void Save(ConfigNode node)
        {
        }
    }
}