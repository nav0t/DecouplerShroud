using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	public class ModuleDecouplerShroud : PartModule, IAirstreamShield {

		[KSPField(isPersistant = true)]
		public int nSides = 24;

		[KSPField(guiName = "DecouplerShroud", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool shroudEnabled = false;

		[KSPField(guiName = "Automatic Shroud Size", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool autoDetectSize = true;

		[KSPField(guiName = "Top", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 10f, incrementLarge = .625f, incrementSlide =  0.01f, incrementSmall = 0.05f, unit = "m", sigFigs = 2, useSI = false)]
		public float topWidth = 1.25f;

		[KSPField(guiName = "Bottom", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 10f, incrementLarge = .625f, incrementSlide = 0.01f, incrementSmall = 0.05f, unit = "m", sigFigs = 2, useSI = false)]
		public float botWidth = 1.25f;

		[KSPField(guiName = "Thickness", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 1f, incrementLarge = .1f, incrementSlide = 0.01f, incrementSmall = 0.01f, sigFigs = 2, useSI = false)]
		public float thickness = .1f;

		[KSPField(guiName = "Height", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 10f, incrementLarge = 0.25f, incrementSlide = 0.01f, incrementSmall = 0.02f, unit = "m", sigFigs = 2, useSI = false)]
		public float height = 1.25f;

		[KSPField(guiName = "Vertical Offset", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = -2f, maxValue = 2f, incrementLarge = .1f, incrementSlide = 0.01f, incrementSmall = 0.01f, unit = "m", sigFigs = 2, useSI = false)]
		public float vertOffset = 0.0f;

		[KSPField(guiName = "Shroud Texture", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "None" }, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
		public int textureIndex;

		[KSPField(isPersistant = true)]
		public float defaultBotWidth = 0;
		[KSPField(isPersistant = true)]
		public float defaultVertOffset = 0;
		[KSPField(isPersistant = true)]
		public float defaultThickness = 0.1f;


		ModuleJettison engineShroud;
		GameObject shroudGO;
		Material shroudMat;
		ShroudShaper shroudCylinders;

		static List<SurfaceTexture> surfaceTextures;
		DragCubeList starDragCubes;

		public void setup() {
			starDragCubes = part.DragCubes;
			getTextures();

			//Set up events
			part.OnEditorAttach += partReattached;
			part.OnEditorDetach += partDetached;

			Fields[nameof(shroudEnabled)].OnValueModified += activeToggled;
			Fields[nameof(autoDetectSize)].OnValueModified += setButtonActive;
			Fields[nameof(autoDetectSize)].OnValueModified += detectSize;

			Fields[nameof(topWidth)].OnValueModified += updateShroud;
			Fields[nameof(botWidth)].OnValueModified += updateShroud;
			Fields[nameof(height)].OnValueModified += updateShroud;
			Fields[nameof(thickness)].OnValueModified += updateShroud;
			Fields[nameof(vertOffset)].OnValueModified += updateShroud;
			Fields[nameof(textureIndex)].OnValueModified += updateTexture;

			setButtonActive();

			if (HighLogic.LoadedSceneIsFlight) {
				createShroudGO();
				if (getShroudedPart() != null && shroudEnabled) {
					getShroudedPart().AddShield(this);
				}
			} else {
				if (part.isAttached) {
					createShroudGO();
				}
			}
			detectSize();
		}

		public void Start() {
			setup();
		}

		//Gets textures from Textures folder and loads them into surfaceTextures list + set Field options
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

			if (textureIndex >= surfaceTextures.Count) {
				textureIndex = 0;
			}

			string[] options = new string[surfaceTextures.Count];
			for (int i = 0; i < options.Length; i++) {
				options[i] = surfaceTextures[i].name;
			}

			BaseField textureField = Fields[nameof(textureIndex)];
			UI_ChooseOption textureOptions = (UI_ChooseOption)textureField.uiControlEditor;
			textureOptions.options = options;
		}

		//Executes when shroud is enabled/disabled
		void activeToggled(object arg) {
			Part topPart = getShroudedPart();
			
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
				Fields[nameof(topWidth)].guiActiveEditor = true;
				Fields[nameof(botWidth)].guiActiveEditor = true;
				Fields[nameof(height)].guiActiveEditor = true;
				Fields[nameof(vertOffset)].guiActiveEditor = true;
				Fields[nameof(thickness)].guiActiveEditor = true;
			} else {
				Fields[nameof(topWidth)].guiActiveEditor = false;
				Fields[nameof(botWidth)].guiActiveEditor = false;
				Fields[nameof(height)].guiActiveEditor = false;
				Fields[nameof(vertOffset)].guiActiveEditor = false;
				Fields[nameof(thickness)].guiActiveEditor = false;
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
			Part topPart = getShroudedPart();
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
						topWidth = tippytopPart.collider.bounds.size.x;
					}
				}
				if (part.collider != null) {
					botWidth = part.collider.bounds.size.x;
				}
			}
			thickness = defaultThickness;

			if (defaultBotWidth != 0) {
				botWidth = defaultBotWidth;
			}
			vertOffset = defaultVertOffset;
			Debug.Log("defaults: "+defaultBotWidth+", "+defaultVertOffset);

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

		//Generates the shroud for the first time
		void generateShroud() {
			shroudCylinders = new ShroudShaper(nSides);
			shroudCylinders.generate(vertOffset, height, botWidth, topWidth, thickness);
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
			shroudCylinders.update(vertOffset, height, botWidth, topWidth, thickness);
		}

		//Recalculates the drag cubes for the model
		void generateDragCube() {

			if (shroudEnabled && HighLogic.LoadedSceneIsFlight) {
				//Calculate dragcube for the cone manually
				DragCube dc = DragCubeSystem.Instance.RenderProceduralDragCube(part);
				part.DragCubes.ClearCubes();
				part.DragCubes.Cubes.Add(dc);
				part.DragCubes.ResetCubeWeights();

			}

		}

		//Create the gameObject with the meshrenderer
		void createShroudGO() {
			if (!shroudEnabled) {
				return;
			}

			AttachNode topNode = part.FindAttachNode("top");

			generateShroud();

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
			generateDragCube();
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

		Part getShroudedPart() {
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode.owner == (part)) {
				return topNode.attachedPart;
			} else {
				return topNode.owner;
			}
		}

		public bool ClosedAndLocked() {
			return shroudEnabled;
		}

		public Vessel GetVessel() {
			return vessel;
		}

		public Part GetPart() {
			return part;
		}
	}
}
