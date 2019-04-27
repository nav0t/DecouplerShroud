using System.Collections.Generic;
using UnityEngine;

namespace DecouplerShroud {
	class ShroudTexture {
		public static List<ShroudTexture> shroudTextures;
		static Dictionary<string, ShroudTexture> shroudTextureDictionary;


		public string name = "ERR";
        public string displayName = "ERR";
		public bool showInVAB = true;
		public List<SurfaceTexture> textures;

		public ShroudTexture() {
			textures = new List<SurfaceTexture>();
		}

		public static void LoadTextures() {

			if (shroudTextures == null) {
				Debug.Log("[DecouplerShroud] Loading ShroudTextures from configs");

				List<GameDatabase.TextureInfo> textures = GameDatabase.Instance.GetAllTexturesInFolder("DecouplerShroud/Textures/");
				shroudTextures = new List<ShroudTexture>();
				shroudTextureDictionary = new Dictionary<string, ShroudTexture>();

				Dictionary<string, List<ConfigNode>> waiting = new Dictionary<string, List<ConfigNode>>();
				foreach(ConfigNode node in GameDatabase.Instance.GetConfigNodes("ShroudTexture")) {

					if (node.HasValue("base")) {
						string texBaseName = node.GetValue("base").Trim();
						if (getShroudTextureByName(texBaseName) == null) {
							// If the base has not been parsed, add texture to waiting list. When base is done parsing, it will be called
							List<ConfigNode> texBaseWaitList;
							if (waiting.TryGetValue(texBaseName, out texBaseWaitList)){
								texBaseWaitList.Add(node);
							} else {
								texBaseWaitList = new List<ConfigNode>();
								texBaseWaitList.Add(node);
								waiting.Add(texBaseName, texBaseWaitList);
							}
							continue;
						}
						ParseShroudTextureWithTexBase(node, getShroudTextureByName(texBaseName), waiting);
					} else {
						ParseShroudTexture(node, waiting);
					}
				}

				// Take all visible shroudTextures from the dictionary and put them in the public list
				foreach (KeyValuePair<string, ShroudTexture> p in shroudTextureDictionary) {
					if (p.Value.showInVAB) {
						shroudTextures.Add(p.Value);
					}
				}

				// The waiting queue should be empty if all went well
				if (waiting.Count == 0) {
					Debug.Log("[DecouplerShroud] Done loading all ShroudTextures from configs");
				} else {
					Debug.LogError("[DecouplerShroud] Couldn't load all ShroudTextures from config (probably either due to a circular base issue, or base doesn't exist)");
					// Printing all Textures which didn't load to help finding errors
					foreach (KeyValuePair<string, List<ConfigNode>> p in waiting) {
						foreach (ConfigNode c in p.Value) {
							Debug.LogError("[DecouplerShroud] ShroudTexture "+ c.GetValue("name").Trim() + " is still waiting for it's base " + p.Key + " which wasn't loaded");
						}
					}
				}
			}
		}

		static ShroudTexture getShroudTextureByName(string name) {
			
			if (shroudTextureDictionary.ContainsKey(name)) {
				ShroudTexture s;
				if (shroudTextureDictionary.TryGetValue(name, out s))
					return s;
			}

			return null;
		}

		static void ParseShroudTexture(ConfigNode node, Dictionary<string, List<ConfigNode>> waiting) {
			int v = 1;
			if (node.HasValue("v"))
				int.TryParse(node.GetValue("v"), out v);
			ShroudTexture tex = new ShroudTexture();
			if (node.HasValue("name")) {
				tex.name = node.GetValue("name");
                tex.displayName = tex.name;
                if (getShroudTextureByName(tex.name) != null) {
                    Debug.LogError("[DecouplerShroud] Another ShroudTexture already exists with name "+ tex.name + ". Make sure all textures have unique names");
					return;
                }

			} else {
				Debug.LogError("[DecouplerShroud] ShroudTexture needs name! " + node);
				return;
			}

            if (node.HasValue("displayName")) {
                tex.displayName = node.GetValue("displayName");
			}

			if (node.HasValue("showInVAB")) {
				bool.TryParse(node.GetValue("showInVAB"), out tex.showInVAB);
			}

			if (!node.HasNode("outside") || !node.HasNode("top") || !node.HasNode("inside")) {
				Debug.LogError("[DecouplerShroud] texture config needs outside, top, inside nodes if no base is given. Texutre Name: " + tex.name);
				return;
			}

            tex.textures.Add(new SurfaceTexture(node.GetNode("outside"), v));
			tex.textures.Add(new SurfaceTexture(node.GetNode("top"), v));
			tex.textures.Add(new SurfaceTexture(node.GetNode("inside"), v));
			shroudTextureDictionary.Add(tex.name, tex);
			Debug.Log("[DecouplerShroud] Loaded ShroudedTexture: " + tex.name);

			// Check if this is the base of a texture in the waiting list, if so parse all textures waiting for this texture
			List<ConfigNode> waitingForMe;
			if (waiting.TryGetValue(tex.name, out waitingForMe)) {
				waiting.Remove(tex.name);
				//Debug.Log("[DecouplerShroud] Found " + waitingForMe.Count + " nodes waiting for " + tex.name);
				foreach (ConfigNode waitingNode in waitingForMe) {
					ParseShroudTextureWithTexBase(waitingNode, tex, waiting);
				}
			}
		}

		static void ParseShroudTextureWithTexBase(ConfigNode node, ShroudTexture texBase, Dictionary<string, List<ConfigNode>> waiting) {
			int v = 1;
			if (node.HasValue("v"))
				int.TryParse(node.GetValue("v"), out v);

			ShroudTexture tex = new ShroudTexture();
			if (node.HasValue("name")) {
				tex.name = node.GetValue("name");
				tex.displayName = tex.name;
				if (getShroudTextureByName(tex.name) != null) {
					Debug.LogError("[DecouplerShroud] Another ShroudTexture already exists with name " + tex.name + ". Make sure all textures have unique names");
					return;
				}
			} else {
				Debug.LogError("[DecouplerShroud] ShroudTexture needs name! " + node);
				return;
			}
			if (node.HasValue("displayName")) {
                tex.displayName = node.GetValue("displayName");
            }
			if (node.HasValue("showInVAB")) {
				bool.TryParse(node.GetValue("showInVAB"), out tex.showInVAB);
			}

			tex.textures.Add(new SurfaceTexture(node.GetNode("outside"), texBase.textures[0], v));
			tex.textures.Add(new SurfaceTexture(node.GetNode("top"), texBase.textures[1], v));
			tex.textures.Add(new SurfaceTexture(node.GetNode("inside"), texBase.textures[2], v));
			shroudTextureDictionary.Add(tex.name, tex);
			Debug.Log("[DecouplerShroud] Loaded ShroudedTexture: "+tex.name + " with base: "+texBase.name);

			// Check if this is the base of a texture in the waiting list, if so parse all textures waiting for this texture
			List<ConfigNode> waitingForMe;
			if (waiting.TryGetValue(tex.name, out waitingForMe)) {
				waiting.Remove(tex.name);
				//Debug.Log("[DecouplerShroud] Found " + waitingForMe.Count + " nodes waiting for " + tex.name);
				foreach (ConfigNode waitingNode in waitingForMe) {
					ParseShroudTextureWithTexBase(waitingNode, tex, waiting);
				}
			}
		}

	}
}
