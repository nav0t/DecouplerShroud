using System.Collections.Generic;
using UnityEngine;

namespace DecouplerShroud {
	class SurfaceTexture {

		public static Dictionary<string, Shader> loadedShaders = new Dictionary<string, Shader>();

		public string shader = "KSP/Bumped Specular";
		public string texBaseShader = "";

		public Material mat;

		Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		Dictionary<string, float> floats = new Dictionary<string, float>();
		Dictionary<string, Color> colors = new Dictionary<string, Color>();

		public Vector2 scale = new Vector2(1, 1);

		public bool autoScale = false;
		public bool autoWidthDivide = false;
		public bool autoHeightDivide = false;
		public bool autoCenterHeightAroundMiddle = false;
		public float autoWidthStep = 1;
		public float autoHeightStep = 1;

		public float autoMinU = 1;
		public float autoMinV = 1;
		public float autoMaxU = 1000;
		public float autoMaxV = 1000;

		public SurfaceTexture(ConfigNode node, int version) {
			
			if (version == 1) {
				ParseNodeV1(node);
			} else if (version == 2) {
				ParseNodeV2(node);
			} else {
				Debug.LogError("[DecouplerShroud] Unknown Texture config version: "+ version);
			}
			createMaterial();

		}

		public SurfaceTexture(ConfigNode node, SurfaceTexture texBase, int version) {

			if (version == 1) {
				Debug.LogWarning("[DecouplerShroud] Base Textures are designed for Texture Config Version 2+ (v = 2 in config)");
			} else {
				textures = new Dictionary<string, Texture>(texBase.textures);
				floats = new Dictionary<string, float>(texBase.floats);
				colors = new Dictionary<string, Color>(texBase.colors);

				texBaseShader = texBase.shader;
				shader = texBase.shader;
				scale = texBase.scale;
				autoScale = texBase.autoScale;
				autoWidthDivide = texBase.autoWidthDivide;
				autoHeightDivide = texBase.autoHeightDivide;
				autoWidthStep = texBase.autoWidthStep;
				autoHeightStep = texBase.autoHeightStep;

				autoMinU = texBase.autoMinU;
				autoMinV = texBase.autoMinV;
				autoMaxU = texBase.autoMaxU;
				autoMaxV = texBase.autoMaxV;
				autoCenterHeightAroundMiddle = texBase.autoCenterHeightAroundMiddle;
			}

			if (node != null) {
				if (version == 1) {
					ParseNodeV1(node);
				} else if (version == 2) {
					ParseNodeV2(node);
				} else {
					Debug.LogError("[DecouplerShroud] Unknown Texture config version: " + version);
				}
			}
			
			createMaterial();

		}

		void createMaterial() {
			mat = new Material(getShader(shader));

			
			foreach (KeyValuePair<string, Texture> t in textures) {
				if (mat.HasProperty(t.Key)) {
					mat.SetTexture(t.Key, t.Value);
				} else {
					Debug.LogWarning("[DecouplerShroud] Material doesn't have property: "+t.Key+" ("+mat.shader.name+")");
				}
			}
			foreach (KeyValuePair<string, float> t in floats) {
				if (mat.HasProperty(t.Key)) {
					mat.SetFloat(t.Key, t.Value);
				} else {
					Debug.LogWarning("[DecouplerShroud] Material doesn't have property: " + t.Key + " (" + mat.shader.name + ")");
				}
			}
			foreach (KeyValuePair<string, Color> t in colors) {
				if (mat.HasProperty(t.Key)) {
					mat.SetColor(t.Key, t.Value);
				} else {
					Debug.LogWarning("[DecouplerShroud] Material doesn't have property: " + t.Key + " (" + mat.shader.name + ")");
				}
			}
		}

		ConfigNode selectMaterialVariant(ConfigNode node) {
			if (node.HasNode("MaterialVariant")) {
				foreach (ConfigNode m in node.GetNodes("MaterialVariant")) {
					if (m.HasValue("shader")) {
						if (getShader(m.GetValue("shader")) == null) {
							//Debug.Log("[DecouplerShroud] Didn't find shader: " + m.GetValue("shader")+", switching to fallback");
							continue;
						}
						if (texBaseShader != "" && texBaseShader != m.GetValue("shader")) {
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

			if (node.HasValue("shader"))
				shader = node.GetValue("shader");

			foreach (string s in node.GetValues("texture")) {
				string[] split = s.Split(',');
				if (split.Length < 2) {
					Debug.LogWarning("[DecouplerShroud] texture value only has one parameter: "+ s);
					continue;
				}
				RemovePropertyIfExists(textures, split[0]);
				textures.Add(split[0], GameDatabase.Instance.GetTexture(split[1].Trim(), false));
			}
			foreach (string s in node.GetValues("float")) {
				string[] split = s.Split(',');
				if (split.Length < 2) {
					Debug.LogWarning("[DecouplerShroud] float value only has one parameter: " + s);
					continue;
				}
				RemovePropertyIfExists(floats, split[0]);
				floats.Add(split[0], float.Parse(split[1].Trim()));
			}
			foreach (string s in node.GetValues("color")) {
				string[] split = s.Split(',');
				RemovePropertyIfExists(colors, split[0]);
				colors.Add(split[0], ConfigNode.ParseColor(s.Substring(split[0].Length).Trim()));
			}

			ParseScalingOptions(node);
		}

		void RemovePropertyIfExists<T>(Dictionary<string, T> dict, string name) {
			if (dict.ContainsKey(name)) {
				dict.Remove(name);
			}
		}

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

			
			textures.Add("_MainTex", texture);
			textures.Add("_BumpMap", normalMap);

			floats.Add("_Shininess", shininess);
			colors.Add("_SpecColor", specularColor);

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
			if (node.HasValue("autoCenterHeightAroundMiddle"))
				bool.TryParse(node.GetValue("autoCenterHeightAroundMiddle"), out autoCenterHeightAroundMiddle);
			if (node.HasValue("autoWidthStep"))
				float.TryParse(node.GetValue("autoWidthStep"), out autoWidthStep);
			if (node.HasValue("autoHeightStep"))
				float.TryParse(node.GetValue("autoHeightStep"), out autoHeightStep);

			//This is to not have the factor variables deprecated
			if (node.HasValue("autoMinUFactor")) {
				float.TryParse(node.GetValue("autoMinUFactor"), out autoMinU);
				autoMinU *= scale.x;
			}
			if (node.HasValue("autoMinVFactor")) {
				float.TryParse(node.GetValue("autoMinVFactor"), out autoMinV);
				autoMinV *= scale.y;
			}
			if (node.HasValue("autoMaxUFactor")) {
				float.TryParse(node.GetValue("autoMaxUFactor"), out autoMaxU);
				autoMaxU *= scale.x;
			}
			if (node.HasValue("autoMaxVFactor")) {
				float.TryParse(node.GetValue("autoMaxVFactor"), out autoMaxV);
				autoMaxV *= scale.y;
			}

			if (node.HasValue("autoMinU"))
				float.TryParse(node.GetValue("autoMinU"), out autoMinU);
			if (node.HasValue("autoMinV"))
				float.TryParse(node.GetValue("autoMinV"), out autoMinV);

			if (node.HasValue("autoMaxU"))
				float.TryParse(node.GetValue("autoMaxU"), out autoMaxU);
			if (node.HasValue("autoMaxV"))
				float.TryParse(node.GetValue("autoMaxV"), out autoMaxV);
		}

		public void SetTextureScale(Material m, Vector2 size) {
			Vector2 uvScale = scale;
			
			if (autoScale) {
				if (autoWidthDivide) {
					size /= size.x;
				}
				if (autoHeightDivide) {
					size /= size.y;
				}
				size.x = roundScaleToStep(size.x, autoWidthStep);
				size.y = roundScaleToStep(size.y, autoHeightStep);
				uvScale.x = Mathf.Clamp(size.x * uvScale.x, autoMinU, autoMaxU);
				uvScale.y = Mathf.Clamp(size.y * uvScale.y, autoMinV, autoMaxV);

			}

			foreach (KeyValuePair<string, Texture> t in textures) {
				if (m.HasProperty(t.Key)) {
					m.SetTextureScale(t.Key, uvScale);
					
					if (autoCenterHeightAroundMiddle) {
						m.SetTextureOffset(t.Key, new Vector2(0, 0.5f - uvScale.y / 2));
					}
				}
			}
		}

		float roundScaleToStep(float scale, float step) {
			if (step > 0) {
				
				scale -= 1;
				scale /= step;
				scale = Mathf.Round(scale);
				scale *= step;
				scale += 1;
				
			}
			
			return scale;
		}

	}
}
