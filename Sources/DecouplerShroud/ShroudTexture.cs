using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class ShroudTexture {
		public string name = "ERR";
		public List<SurfaceTexture> textures;

		public static List<ShroudTexture> shroudTextures;

		
		public ShroudTexture() {
			textures = new List<SurfaceTexture>();
		}

		public static void LoadTextures() {
			if (shroudTextures == null) {
				List<GameDatabase.TextureInfo> textures = GameDatabase.Instance.GetAllTexturesInFolder("DecouplerShroud/Textures/");
				shroudTextures = new List<ShroudTexture>();

				foreach(ConfigNode texconf in GameDatabase.Instance.GetConfigNodes("ShroudTexture")) {

					if (!texconf.HasNode("outside") || !texconf.HasNode("top") || !texconf.HasNode("inside")) {
						Debug.LogError("Decoupler Shroud texture config missing node: "+ texconf);
						continue;
					}

					ShroudTexture tex = new ShroudTexture();
					if (texconf.HasValue("name"))
						tex.name = texconf.GetValue("name");

					tex.textures.Add(new SurfaceTexture(texconf.GetNode("outside")));
					tex.textures.Add(new SurfaceTexture(texconf.GetNode("top")));
					tex.textures.Add(new SurfaceTexture(texconf.GetNode("inside")));

					shroudTextures.Add(tex);
				}
			}
		}
	}
}
