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
		public Color specularColor = Color.white * .2f;

		public Vector2 scale = new Vector2(1, 1);

		public bool autoScale = false;
		public bool autoWidthDivide = false;
		public bool autoHeightDivide = false;
		public float autoWidthStep = 1;
		public float autoHeightStep = 1;

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

			if (node.HasValue("uScale"))
				float.TryParse(node.GetValue("uScale"), out scale.x);
			if (node.HasValue("vScale"))
				float.TryParse(node.GetValue("vScale"), out scale.y);

			if (node.HasValue("autoScale"))
				bool.TryParse(node.GetValue("autoScale"), out autoScale);
			if (node.HasValue("autoWidthDivide"))
				bool.TryParse(node.GetValue("autoWidthDivide"), out autoWidthDivide);
			if (node.HasValue("autoHeightDivide"))
				bool.TryParse(node.GetValue("autoHeightDivide"), out autoHeightDivide);
			if (node.HasValue("autoWidthStep"))
				float.TryParse(node.GetValue("autoWidthStep"), out autoWidthStep);
			if (node.HasValue("autoHeightStep"))
				float.TryParse(node.GetValue("autoHeightStep"), out autoHeightStep);

		}

		public void SetMaterialProperties(Material m, Vector2 size) {
			Vector2 uvScale = scale;

			if (autoWidthDivide) {
				size /= size.x;
			}
			if (autoHeightDivide) {
				size /= size.y;
			}
			if (autoScale) {
				size.x = roundScaleToStep(size.x, autoWidthStep);
				size.y = roundScaleToStep(size.y, autoHeightStep);
				uvScale.Scale(size);
			}

			m.SetTexture("_MainTex", texture);
			m.SetTextureScale("_MainTex", uvScale);

			m.SetTexture("_BumpMap", normalMap);
			m.SetTextureScale("_BumpMap", uvScale);

			m.SetFloat("_Shininess", shininess);
			m.SetColor("_SpecColor", specularColor);

		}

		float roundScaleToStep(float s, float step) {
			if (step > 0) {
				s -= 1;
				s /= step;
				s = Mathf.Round(s);
				s *= step;
				s += 1;
			}
			return s;
		}

	}
}
