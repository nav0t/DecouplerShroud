using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class SurfaceTexture {
		public Texture texture;
		public Texture normalMap;

		public float shininess = 0.7f;
		public Color specularColor = Color.white * .4f;
		
		public SurfaceTexture(ConfigNode node) {
			GameDatabase gdb = GameDatabase.Instance;

			if (node.HasValue("texture"))
				texture = gdb.GetTexture(node.GetValue("texture"), false);
			if (node.HasValue("normals"))
				normalMap = gdb.GetTexture(node.GetValue("normals"), false);
			if (node.HasValue("shininess"))
				float.TryParse(node.GetValue("shininess"), out shininess);
			if (node.HasValue("specularColor"))
				specularColor = ConfigNode.ParseColor(node.GetValue("specularColor"));
		}

	}
}
