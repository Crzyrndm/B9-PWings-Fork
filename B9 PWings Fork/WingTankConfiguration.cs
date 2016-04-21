<<<<<<< HEAD


using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;



=======
﻿using System.Collections.Generic;
using System.Linq;
>>>>>>> refs/remotes/Crzyrndm/master

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
<<<<<<< HEAD

            Debug.Log(node);
=======
            float ratioTotal = 0;

>>>>>>> refs/remotes/Crzyrndm/master
            GUIName = node.GetValue("name");
            Debug.Log("name is: " + GUIName);
            ConfigNode[] nodes = node.GetNodes("Resource");
            Debug.Log(nodes.Length);
            for (int i = 0; i < nodes.Length; ++i)
            {
                WingTankResource res = new WingTankResource(nodes[i]);
<<<<<<< HEAD
                resources.Add(res.resource.name, res);
            }
            GUIName = node.GetValue("name");


=======
                if (res.resource != null)
                {
                    resources.Add(res.resource.name, res);
                    ratioTotal += res.ratio;
                }
            }
            foreach (KeyValuePair<string, WingTankResource> kvp in resources)
                kvp.Value.SetUnitsPerVolume(ratioTotal);
>>>>>>> refs/remotes/Crzyrndm/master
        }

        public void Save(ConfigNode node) { }
    }
}
