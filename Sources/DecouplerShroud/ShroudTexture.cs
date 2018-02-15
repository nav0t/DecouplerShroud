using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class ShroudTexture {
		public string name = "ERR";
		public Texture texture;
		public Texture normalMap;

		public static List<ShroudTexture> surfaceTextures;

		
		public ShroudTexture() {

		}

		public ShroudTexture(string name, Texture texture, Texture normalMap) {
			this.name = name;
			this.texture = texture;
			this.normalMap = normalMap;
		}

		public static void LoadTextures() {
			if (surfaceTextures == null) {
				List<GameDatabase.TextureInfo> textures = GameDatabase.Instance.GetAllTexturesInFolder("DecouplerShroud/Textures/");
				surfaceTextures = new List<ShroudTexture>();

				foreach(ConfigNode texconf in GameDatabase.Instance.GetConfigNodes("ShroudTexture")) {
					ShroudTexture tex = new ShroudTexture();
					Debug.Log(texconf.GetValue("name"));
					Debug.Log(texconf.GetValue("texture"));
					Debug.Log(texconf.GetValue("normals"));

					tex.name = texconf.GetValue("name");
					tex.texture = GameDatabase.Instance.GetTexture(texconf.GetValue("texture"), false);
					tex.normalMap = GameDatabase.Instance.GetTexture(texconf.GetValue("normals"), false);

					surfaceTextures.Add(tex);
				}
			}
		}
	}
}
