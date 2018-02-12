using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	public class ModuleDecouplerShroud : PartModule {

		const int SIDES = 24;

		[KSPField(guiName = "DecouplerShroud", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool shroudEnabled = false;

		[KSPField(guiName = "Automatic Shroud Size", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool autoDetectSize = true;

		[KSPField(guiName = "Top Radius", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float topRad = 1.25f;
		[KSPField(guiName = "Bottom Radius", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float botRad = 1.25f;
		[KSPField(guiName = "Shroud Thickness", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = (1 / 64f), maxValue = 1f, stepIncrement = (1 / 64f))]
		public float width = .1f;
		[KSPField(guiName = "Shroud Height", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float height = 1.25f;
		[KSPField(guiName = "Vertical Offset", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.125f)]
		public float bottomStart = 0.0f;

		[KSPField(guiName = "Shroud Texture", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "None" }, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
		public int textureIndex;

		ModuleJettison engineShroud;
		GameObject shroudGO;
		Material shroudMat;
		ShroudShaper shroudCylinders;

		static List<SurfaceTexture> surfaceTextures;

		public void setup() {

			getTextures();

			//Set up events
			part.OnEditorAttach += partReattached;
			part.OnEditorDetach += partDetached;

			Fields[nameof(shroudEnabled)].OnValueModified += activeToggled;
			Fields[nameof(autoDetectSize)].OnValueModified += setButtonActive;
			Fields[nameof(autoDetectSize)].OnValueModified += detectSize;

			Fields[nameof(topRad)].OnValueModified += updateShroud;
			Fields[nameof(botRad)].OnValueModified += updateShroud;
			Fields[nameof(height)].OnValueModified += updateShroud;
			Fields[nameof(width)].OnValueModified += updateShroud;
			Fields[nameof(bottomStart)].OnValueModified += updateShroud;
			Fields[nameof(textureIndex)].OnValueModified += updateTexture;

			setButtonActive();

			if (HighLogic.LoadedSceneIsFlight) {
				createShroudGO();
			} else {
				createShroudGO();
			}
		}


		public void Start() {
			setup();
		}

		void getTextures() {
			if (surfaceTextures == null) {
				List<GameDatabase.TextureInfo> textures = GameDatabase.Instance.GetAllTexturesInFolder("DecouplerShroud/Textures/");
				surfaceTextures = new List<SurfaceTexture>();
				foreach (GameDatabase.TextureInfo inf in textures) {
					string name = inf.name.Replace("DecouplerShroud/Textures/", "");
					if (inf.name.EndsWith("_Normals")) {
						continue;
					}
					Texture tex = inf.texture;
					Texture nor = GameDatabase.Instance.GetTexture(inf.name + "_Normals", false);
					surfaceTextures.Add(new SurfaceTexture(name, tex, nor));
					if (nor == null) {
						Debug.Log("No Normal map found: " + inf.name + "_Normals");
					}
				}
			}

			string[] options = new string[surfaceTextures.Count];
			for (int i = 0; i < options.Length; i++) {
				options[i] = surfaceTextures[i].name;
			}

			BaseField textureField = Fields["textureIndex"];
			UI_ChooseOption textureOptions = (UI_ChooseOption)textureField.uiControlEditor;
			textureOptions.options = options;
		}

		//Executes when shroud is enabled/disabled
		void activeToggled(object arg) {
			Part topPart;
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode.owner == (part)) {
				topPart = topNode.attachedPart;
			} else {
				topPart = topNode.owner;
			}
			if (topPart != null) {
				engineShroud = topPart.GetComponent<ModuleJettison>();
				if (engineShroud != null) {
					engineShroud.shroudHideOverride = shroudEnabled;
				}
			}
			setButtonActive();
			updateShroud();
		}

		//Enables or disables KSPFields based on values
		void setButtonActive(object arg) { setButtonActive(); }
		void setButtonActive() {

			if (shroudEnabled) {
				Fields[nameof(autoDetectSize)].guiActiveEditor = true;
				Fields[nameof(textureIndex)].guiActiveEditor = true;

			} else {
				Fields[nameof(autoDetectSize)].guiActiveEditor = false;
				Fields[nameof(textureIndex)].guiActiveEditor = false;

			}
			if (shroudEnabled && !autoDetectSize) {
				Fields[nameof(topRad)].guiActiveEditor = true;
				Fields[nameof(botRad)].guiActiveEditor = true;
				Fields[nameof(height)].guiActiveEditor = true;
				Fields[nameof(bottomStart)].guiActiveEditor = true;
				Fields[nameof(width)].guiActiveEditor = true;
			} else {
				Fields[nameof(topRad)].guiActiveEditor = false;
				Fields[nameof(botRad)].guiActiveEditor = false;
				Fields[nameof(height)].guiActiveEditor = false;
				Fields[nameof(bottomStart)].guiActiveEditor = false;
				Fields[nameof(width)].guiActiveEditor = false;
			}

		}

		void updateTexture(object arg) { updateTexture(); }
		void updateTexture() {

			Debug.Log("Setting Texture to " + textureIndex);
			Debug.Log("eq: " + (shroudMat == shroudGO.GetComponent<Renderer>().sharedMaterial));
			if (shroudMat != shroudGO.GetComponent<Renderer>().sharedMaterial) {
				shroudMat = shroudGO.GetComponent<Renderer>().sharedMaterial;
			}
			shroudMat.SetTexture("_MainTex", surfaceTextures[textureIndex].texture);
			shroudMat.SetTexture("_BumpMap", surfaceTextures[textureIndex].normalMap);
		}

		void partReattached() {
			detectSize();
			if (shroudGO == null)
				createShroudGO();
		}

		//Automatically sets size of shrouds
		void detectSize(object arg) { detectSize(); }
		void detectSize() {

			if (!autoDetectSize || !HighLogic.LoadedSceneIsEditor) {
				return;
			}
			Part topPart;
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode.owner == (part)) {
				topPart = topNode.attachedPart;
			} else {
				topPart = topNode.owner;
			}

			//Not attached to anything
			if (topPart != null) {

				AttachNode tippytopNode = topPart.FindAttachNode("top");
				AttachNode tippybotNode = topPart.FindAttachNode("bottom");

				if (tippybotNode != null && tippytopNode != null)
					height = Mathf.Abs(tippytopNode.position.y - tippybotNode.position.y);

				Part tippytopPart = tippytopNode.owner;
				if (tippytopPart = topPart) {
					tippytopPart = tippytopNode.attachedPart;
				}

				if (tippytopPart != null) {
					if (tippytopPart.collider != null) {
						topRad = tippytopPart.collider.bounds.size.x / 2f;
					}
				}
				if (part.collider != null) {
					botRad = part.collider.bounds.size.x / 2f;
				}
			}
			width = 0.1f;

			if (shroudGO != null) {
				updateShroud();
			}
		}

		void partDetached() {
			destroyShroud();
		}

		void destroyShroud() {
			Destroy(shroudGO);
			Destroy(shroudMat);
		}

		//updates the shroud mesh when values changed
		void updateShroud(object arg) { updateShroud(); }
		void updateShroud() {
			if (!shroudEnabled) {
				destroyShroud();
			}
			if (shroudGO == null || shroudCylinders == null) {
				createShroudGO();
			}
			shroudCylinders.update(bottomStart, height, botRad, topRad, width);
		}


		//Create the gameObject with the meshrenderer
		void createShroudGO() {
			if (!shroudEnabled) {
				return;
			}

			AttachNode topNode = part.FindAttachNode("top");

			shroudCylinders = new ShroudShaper(24);
			shroudCylinders.generate(bottomStart, height, botRad, topRad, width);

			if (shroudGO != null) {
				Destroy(shroudGO);
			}

			shroudGO = new GameObject("DecouplerShroud");
			shroudGO.transform.parent = transform;
			shroudGO.transform.localPosition = topNode.position;
			shroudGO.transform.localRotation = Quaternion.identity;
			shroudGO.AddComponent<MeshFilter>().sharedMesh = shroudCylinders.multiCylinder.mesh;

			if (shroudMat != null) {
				Destroy(shroudMat);
			}
			shroudMat = CreateMat();
			shroudGO.AddComponent<MeshRenderer>();
			shroudGO.GetComponent<Renderer>().material = shroudMat;
		}

		//Creates the material for the mesh
		Material CreateMat() {
			Material mat = new Material(Shader.Find("KSP/Bumped Specular"));

			mat.SetTexture("_MainTex", surfaceTextures[textureIndex].texture);
			mat.SetTexture("_BumpMap", surfaceTextures[textureIndex].normalMap);

			mat.SetFloat("_Shininess", .07f);
			mat.SetColor("_SpecColor", Color.white * (95 / 255f));
			return mat;
		}

	}
}
