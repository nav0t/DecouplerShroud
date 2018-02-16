using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	public class ModuleDecouplerShroud : PartModule, IAirstreamShield {

		float[] snapSizes = new float[] { .63f , 1.25f, 2.5f, 3.75f, 5f, 7.5f};

		[KSPField(isPersistant = true)]
		public int nSides = 24;

		[KSPField(guiName = "DecouplerShroud", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool shroudEnabled = false;

		[KSPField(guiName = "Automatic Shroud Size", isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool autoDetectSize = true;

		[KSPField(guiName = "Top", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 20f, incrementLarge = .625f, incrementSlide =  0.01f, incrementSmall = 0.05f, unit = "m", sigFigs = 2, useSI = false)]
		public float topWidth = 1.25f;

		[KSPField(guiName = "Bottom", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 20f, incrementLarge = .625f, incrementSlide = 0.01f, incrementSmall = 0.05f, unit = "m", sigFigs = 2, useSI = false)]
		public float botWidth = 1.25f;

		[KSPField(guiName = "Thickness", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 1f, incrementLarge = .1f, incrementSlide = 0.01f, incrementSmall = 0.01f, sigFigs = 2, useSI = false)]
		public float thickness = .1f;

		[KSPField(guiName = "Height", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .01f, maxValue = 20f, incrementLarge = 0.25f, incrementSlide = 0.01f, incrementSmall = 0.02f, unit = "m", sigFigs = 2, useSI = false)]
		public float height = 1.25f;

		[KSPField(guiName = "Vertical Offset", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = -2f, maxValue = 2f, incrementLarge = .1f, incrementSlide = 0.01f, incrementSmall = 0.01f, unit = "m", sigFigs = 2, useSI = false)]
		public float vertOffset = 0.0f;

		[KSPField(guiName = "Shroud Texture", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "None" }, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
		public int textureIndex;

		[KSPField(isPersistant = false)]
		public float defaultBotWidth = 0;
		[KSPField(isPersistant = false)]
		public float defaultVertOffset = 0;
		[KSPField(isPersistant = false)]
		public float defaultThickness = 0.1f;
		[KSPField(isPersistant = false)]
		public float radialSnapMargin = .15f;
		[KSPField(isPersistant = false)]
		public float bottomEdgeSize = .1f;
		[KSPField(isPersistant = false)]
		public float topBevelSize = .05f;
		[KSPField(isPersistant = false)]
		public float antiZFightSizeIncrease = .001f;

		ModuleJettison engineShroud;
		GameObject shroudGO;
		Material shroudMat;
		ShroudShaper shroudCylinders;
		Vector3 lastPos;

		DragCubeList starDragCubes;

		public void setup() {
			starDragCubes = part.DragCubes;
			getTextureNames();

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
				if (GetShroudedPart() != null && shroudEnabled) {
					GetShroudedPart().AddShield(this);
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

		void Update() {
			if (HighLogic.LoadedSceneIsEditor) {
				if (transform.position != lastPos && part.isAttached && shroudEnabled) {
					lastPos = transform.position;
					detectSize();
				}
			}

			//DEBUG!!
			//if (Input.GetKeyDown(KeyCode.P)) {
			//	printPartPresetsToConsole();
			//}

		}

		void printPartPresetsToConsole() {
			Debug.Log("@PART["+part.name+"]{");
			Debug.Log("\t@MODULE[ModuleDecouplerShroud]{");
			Debug.Log("\t\t%defaultBotWidth = " + botWidth);
			Debug.Log("\t\t%defaultVertOffset = " + vertOffset);
			Debug.Log("\t}");
			Debug.Log("}");
		}

		//Gets textures from Textures folder and loads them into surfaceTextures list + set Field options
		void getTextureNames() {
			if (ShroudTexture.surfaceTextures == null) {
				ShroudTexture.LoadTextures();
			}

			if (textureIndex >= ShroudTexture.surfaceTextures.Count) {
				textureIndex = 0;
			}

			string[] options = new string[ShroudTexture.surfaceTextures.Count];
			for (int i = 0; i < options.Length; i++) {
				options[i] = ShroudTexture.surfaceTextures[i].name;
			}

			BaseField textureField = Fields[nameof(textureIndex)];
			UI_ChooseOption textureOptions = (UI_ChooseOption)textureField.uiControlEditor;
			textureOptions.options = options;
		}

		//Executes when shroud is enabled/disabled
		void activeToggled(object arg) {
			Part topPart = GetShroudedPart();
			
			if (topPart != null) {
				engineShroud = topPart.GetComponent<ModuleJettison>();
				if (engineShroud != null) {
					engineShroud.shroudHideOverride = shroudEnabled;
				}
			}
			setButtonActive();
			detectSize();
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
			shroudMat.SetTexture("_MainTex", ShroudTexture.surfaceTextures[textureIndex].texture);
			shroudMat.SetTexture("_BumpMap", ShroudTexture.surfaceTextures[textureIndex].normalMap);
		}

		void partReattached() {
			detectSize();
			if (shroudGO == null)
				createShroudGO();
			
		}

		//Automatically sets size of shrouds
		void detectSize(object arg) { detectSize(); }
		void detectSize() {

			//Check if the size has to be reset
			if (!autoDetectSize || !HighLogic.LoadedSceneIsEditor || !part.isAttached) {
				return;
			}


			thickness = defaultThickness;
			vertOffset = defaultVertOffset;
			//Debug.Log("Defaults: " + defaultBotWidth + ", " + defaultVertOffset);

			if (defaultBotWidth != 0) {
				botWidth = defaultBotWidth;
			} else {
				if (part.collider != null) {
					botWidth = part.collider.bounds.size.x * part.transform.localScale.x;
					botWidth = TrySnapToSize(botWidth, radialSnapMargin);
				}
			}

			//Get part the shroud is attached to
			Part shroudAttatchedPart = GetShroudAttachPart();


			if (shroudAttatchedPart != null) {
				//Calculate top Width
				if (shroudAttatchedPart.collider != null) {
					topWidth = shroudAttatchedPart.collider.bounds.size.x * shroudAttatchedPart.transform.localScale.x;
					topWidth = TrySnapToSize(topWidth, radialSnapMargin);
				}

				//============================
				//==== Calculate Height ======
				//============================

				//Get the world position of the node we want to attach to
				AttachNode targetNode = shroudAttatchedPart.FindAttachNodeByPart(GetShroudedPart());

				//Bring the node position to world space
				Vector3 nodeWorldPos = shroudAttatchedPart.transform.TransformPoint(targetNode.position);

				//Get local position of nodeWorldPos
				Vector3 nodeRelativePos = transform.InverseTransformPoint(nodeWorldPos);

				//Calculate position of decoupler side
				Vector3 bottomAttachPos = part.FindAttachNode("top").position + Vector3.up * defaultVertOffset;

				Vector3 differenceVector = nodeRelativePos - bottomAttachPos;
				//Debug.Log("Difference Vector: "+ differenceVector);

				//Set height of shroud to vertical difference between top and bottom node
				height = differenceVector.y;
			} else {
				//Debug.LogError("Decoupler has no grandparent");
				height = 0;
				topWidth = botWidth;
			}

			//Update shroud mesh
			if (shroudGO != null) {
				updateShroud();
			}
		}

		public float TrySnapToSize(float size, float margin) {
			
			foreach (float snap in snapSizes) {
				if (Math.Abs(snap - size) < margin * size) {
					return snap;
				}
			}

			return size;
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
			shroudCylinders = new ShroudShaper(this, nSides);
			shroudCylinders.generate();
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
			shroudCylinders.update();
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

			mat.SetTexture("_MainTex", ShroudTexture.surfaceTextures[textureIndex].texture);
			mat.SetTexture("_BumpMap", ShroudTexture.surfaceTextures[textureIndex].normalMap);

			mat.SetFloat("_Shininess", .07f);
			mat.SetColor("_SpecColor", Color.white * (95 / 255f));
			return mat;
		}

		Part GetShroudedPart() {
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode == null) {
				Debug.LogError("Decoupler is missing top node!");
				return null;
			}
			if (topNode.owner == (part)) {
				return topNode.attachedPart;
			} else {
				return topNode.owner;
			}
		}

		Part GetShroudAttachPart() {
			Part shroudedPart = GetShroudedPart();
			if (shroudedPart == null) {
				return null;
			}

			AttachNode shroudedTopNode = shroudedPart.FindAttachNode("top");
			AttachNode shroudedBotNode = shroudedPart.FindAttachNode("bottom");

			Part shroudAttatchPart = null;
			if (shroudedTopNode != null) {
				if (shroudedTopNode.owner == shroudedPart) {
					shroudAttatchPart = shroudedTopNode.attachedPart;
				} else {
					shroudAttatchPart = shroudedTopNode.owner;
				}
			}
			if (shroudedBotNode != null) {
				if (shroudAttatchPart == part || shroudAttatchPart == null) {
					shroudAttatchPart = shroudedBotNode.owner;
					if (shroudAttatchPart == shroudedPart) {
						shroudAttatchPart = shroudedBotNode.attachedPart;
					}
				}
				if (shroudAttatchPart == part) {
					shroudAttatchPart = null;
				}
			}

			
			return shroudAttatchPart;

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
