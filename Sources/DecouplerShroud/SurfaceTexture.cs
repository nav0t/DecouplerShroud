using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class SurfaceTexture {
		public static Dictionary<string, Shader> loadedShaders = new Dictionary<string, Shader>();


		public string shader = "KSP/Bumped Specular";

		public Material mat;

		List<Tuple<string, Texture>> textures;
		List<Tuple<string, float>> floats;
		List<Tuple<string, Color>> colors;

		public Vector2 scale = new Vector2(1, 1);

		public bool autoScale = false;
		public bool autoWidthDivide = false;
		public bool autoHeightDivide = false;
		public float autoWidthStep = 1;
		public float autoHeightStep = 1;
		public float autoMinUFactor = 1;
		public float autoMinVFactor = 1;

		public SurfaceTexture(ConfigNode node, int version) {
			textures = new List<Tuple<string, Texture>>();
			floats = new List<Tuple<string, float>>();
			colors = new List<Tuple<string, Color>>();
			
			if (version == 1) {
				ParseNodeV1(node);
			} else if (version == 2) {
				ParseNodeV2(node);
			} else {
				Debug.LogError("DecouplerShroud: Unknown Texture config version: "+ version);
			}
			createMaterial();

		}

		void createMaterial() {
			mat = new Material(getShader(shader));

			
			foreach (Tuple<string, Texture> t in textures) {
				mat.SetTexture(t.Item1, t.Item2);
			}
			foreach (Tuple<string, float> t in floats) {
				mat.SetFloat(t.Item1, t.Item2);
			}
			foreach (Tuple<string, Color> t in colors) {
				mat.SetColor(t.Item1, t.Item2);
			}
		}

		ConfigNode selectMaterialVariant(ConfigNode node) {
			if (node.HasNode("MaterialVariant")) {
				foreach (ConfigNode m in node.GetNodes("MaterialVariant")) {
					if (m.HasValue("shader")) {
						if (getShader(m.GetValue("shader")) == null) {
							//Debug.Log("==========\ndidn't find shader: " + m.GetValue("shader") + "\n================");
							continue;
						}
					}
					return m;
				}
			}
			return null;
		}

		Shader getShader(string name) {
			if (loadedShaders.ContainsKey(name)) {
				return loadedShaders[name];
			}
			Shader s = GameDatabase.Instance.databaseShaders.Find(m => m.name == name);
			if (s != null) {
				loadedShaders.Add(name, s);
				return s;
			}
			s = Shader.Find(name);
			loadedShaders.Add(name, s);
			
			return s;
		}

		void ParseNodeV2(ConfigNode node) {

			getPropertiesFromNode(node);

			ConfigNode mVar = selectMaterialVariant(node);

			if (mVar != null) {
				getPropertiesFromNode(mVar);
			}
		}

		void getPropertiesFromNode(ConfigNode node) {

			foreach (string s in node.GetValues("texture")) {
				string[] split = s.Split(',');
				//RemovePropertyIfExists(textures, split[0]);
				textures.Add(new Tuple<string, Texture>(split[0], GameDatabase.Instance.GetTexture(split[1], false)));
			}
			foreach (string s in node.GetValues("float")) {
				string[] split = s.Split(',');
				//RemovePropertyIfExists(floats, split[0]);
				floats.Add(new Tuple<string, float>(split[0], float.Parse(split[1])));
			}
			foreach (string s in node.GetValues("color")) {
				string[] split = s.Split(',');
				//RemovePropertyIfExists(colors, split[0]);
				colors.Add(new Tuple<string, Color>(split[0], ConfigNode.ParseColor(s.Substring(split[0].Length))));
			}

			ParseScalingOptions(node);
		}

		/*void RemovePropertyIfExists<T>(List<Tuple<string, T>> list, string name) {
			foreach (Tuple<string, T> t in list) {
				if (t.Item1 == name) {
					list.Remove(t);
					return;
				}
			}
		}*/

		void ParseNodeV1(ConfigNode node) {
			GameDatabase gdb = GameDatabase.Instance;

			float shininess = 0.7f;
			Color specularColor = Color.white * .2f;
			Texture texture = null;
			Texture normalMap = null;

			if (node.HasValue("shader"))
				shader = node.GetValue("shader");

			if (node.HasValue("texture"))
				texture = gdb.GetTexture(node.GetValue("texture"), false);
			if (node.HasValue("normals"))
				normalMap = gdb.GetTexture(node.GetValue("normals"), false);

			if (node.HasValue("shininess"))
				float.TryParse(node.GetValue("shininess"), out shininess);
			if (node.HasValue("specularColor"))
				specularColor = ConfigNode.ParseColor(node.GetValue("specularColor"));

			
			textures.Add(new Tuple<string, Texture>("_MainTex", texture));
			textures.Add(new Tuple<string, Texture>("_BumpMap", normalMap));

			floats.Add(new Tuple<string, float>("_Shininess", shininess));
			colors.Add(new Tuple<string, Color>("_SpecColor", specularColor));

			ParseScalingOptions(node);
		}

		void ParseScalingOptions(ConfigNode node) {
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

			if (node.HasValue("autoMinUFactor"))
				float.TryParse(node.GetValue("autoMinUFactor"), out autoMinUFactor);
			if (node.HasValue("autoMinVFactor"))
				float.TryParse(node.GetValue("autoMinVFactor"), out autoMinVFactor);
		}

		public void SetTextureScale(Material m, Vector2 size) {
			Vector2 uvScale = scale;

			if (autoWidthDivide) {
				size /= size.x;
			}
			if (autoHeightDivide) {
				size /= size.y;
			}
			if (autoScale) {
				size.x = roundScaleToStep(size.x, autoWidthStep, autoMinUFactor);
				size.y = roundScaleToStep(size.y, autoHeightStep, autoMinVFactor);
				uvScale.Scale(size);
			}

			foreach (Tuple<string, Texture> t in textures) {
				m.SetTextureScale(t.Item1, uvScale);
			}
		
		}

		float roundScaleToStep(float s, float step, float minScale) {
			if (step > 0) {
				
				s -= 1;
				s /= step;
				s = Mathf.Round(s);
				s *= step;
				s += 1;
				
			}
			if (s < minScale) {
				return minScale;
			}
			return s;
		}

	}
}
