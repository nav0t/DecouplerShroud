using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class SurfaceTexture {
		public string name = "ERR";
		public Texture texture;
		public Texture normalMap;

		public SurfaceTexture() {

		}

		public SurfaceTexture(string name, Texture texture, Texture normalMap) {
			this.name = name;
			this.texture = texture;
			this.normalMap = normalMap;
		}

	}
}
