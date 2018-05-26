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

		[KSPField(guiName = "Shroud Texture", isPersistant = false, guiActiveEditor = true, guiActive = false)]
		[UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "None" }, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
		public int textureIndex;

		[KSPField(isPersistant = true)]
		public string textureName;

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
		[KSPField(isPersistant = false)]
		public int outerEdgeLoops = 13;
		[KSPField(isPersistant = false)]
		public int topEdgeLoops = 7;

		bool setupFinished = false;
		ModuleJettison[] engineShrouds;
		GameObject shroudGO;
		Material[] shroudMats;
		ShroudShaper shroudCylinders;

		[KSPField]
		bool turnedOffEngineShroud;

		//true when decoupler has no grandparent in the editor
		public bool invisibleShroud;

		//Variables for detecting wheter automatic size needs to be recalculated
		Vector3 lastPos;
		Vector3 lastScale;
		Vector3 lastBounds;
		Vector3 lastShroudAttachedPos;
		Vector3 lastShroudAttachedScale;
		Vector3 lastShroudAttachedBounds;
		Part lastShroudAttachedPart;
		Part lastShroudedPart = null;

		DragCubeList starDragCubes;

		public void setup() {

			//Get rid of decoupler shroud module if no top node found
			if (destroyShroudIfNoTopNode()) {
				return;
			}

			starDragCubes = part.DragCubes;
			getTextureNames();
			//Remove copied decoupler shroud when copied
			Transform copiedDecouplerShroud = transform.FindChild("DecouplerShroud");
			if (copiedDecouplerShroud != null) {
				Destroy(copiedDecouplerShroud.gameObject);
				shroudCylinders = null;
			}

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
			setupFinished = true;
		}

		public void Start() {
			setup();
		}

		void Update() {
			if (!setupFinished) {
				return;
			}

			if (HighLogic.LoadedSceneIsEditor) {
				if (part.isAttached && shroudEnabled) {
					detectRequiredRecalculation();
				}
			}

			if (lastShroudedPart != GetShroudedPart()) {
				onShroudedPartChanged();
				lastShroudedPart = GetShroudedPart();
			}

		}

		void onShroudedPartChanged() {
			if (HighLogic.LoadedSceneIsFlight) {
				if (lastShroudedPart != null) {
					//Debug.Log("shrouded Part Changed! was " + lastShroudedPart + " is " + GetShroudedPart());
					lastShroudedPart.RemoveShield(this);
				}
			}
			if (HighLogic.LoadedSceneIsEditor) {
				// Toggle engineShroud if new engine is placed on decoupler
				setEngineShroudActivity();
			}
		}

		void detectRequiredRecalculation() {
			bool requiredRecalc = false;

			//Check if collider bounds changed (for example procedural parts can cause this)
			if (part.collider != null) {
				if (lastBounds != part.collider.bounds.size) {
					lastBounds = part.collider.bounds.size;
					requiredRecalc = true;
				}
			}
			
			//Checking if part position/scale changed
			if (transform.position != lastPos || transform.localScale != lastScale) {
				lastPos = transform.position;
				lastScale = transform.localScale;
				requiredRecalc = true;
			}
			//If there is a new attached part
			if (GetShroudAttachedPart() != lastShroudAttachedPart) {
				lastShroudAttachedPart = GetShroudAttachedPart();
				requiredRecalc = true;
			}
			//Check if attached part changed
			if (lastShroudAttachedPart != null) {
				if (lastShroudAttachedPos != lastShroudAttachedPart.transform.position
				|| lastShroudAttachedScale != lastShroudAttachedPart.transform.localScale) {
					lastShroudAttachedPos = lastShroudAttachedPart.transform.position;
					lastShroudAttachedScale = lastShroudAttachedPart.transform.localScale;
					requiredRecalc = true;
				}
				//Check if collider bounds of attatched part changed (for example procedural parts can cause this)
				if (lastShroudAttachedPart.collider != null) {
					if (lastShroudAttachedBounds != lastShroudAttachedPart.collider.bounds.size) {
						lastShroudAttachedBounds = lastShroudAttachedPart.collider.bounds.size;
						requiredRecalc = true;
					}
				}
			}

			if (requiredRecalc) {
				detectSize();
			}

		}

		//Gets textures from Textures folder and loads them into surfaceTextures list + set Field options
		void getTextureNames() {
			if (ShroudTexture.shroudTextures == null) {
				ShroudTexture.LoadTextures();
			}

			if (textureIndex >= ShroudTexture.shroudTextures.Count) {
				textureIndex = 0;
			}

			string[] options = new string[ShroudTexture.shroudTextures.Count];
			for (int i = 0; i < options.Length; i++) {
				options[i] = ShroudTexture.shroudTextures[i].name;

				//Sets textureindex to the saved texture
				if (options[i].Equals(textureName)) {
					textureIndex = i;
				}
			}

			BaseField textureField = Fields[nameof(textureIndex)];
			UI_ChooseOption textureOptions = (UI_ChooseOption)textureField.uiControlEditor;
			textureOptions.options = options;
		}

		//Executes when shroud is enabled/disabled
		void activeToggled(object arg) {
			setEngineShroudActivity();
			setButtonActive();
			detectSize();
			updateShroud();
		}

		//Disables and reenables stock engine shrouds
		void setEngineShroudActivity() {
			Part topPart = GetShroudedPart();

			if (topPart != null) {
				engineShrouds = topPart.GetComponents<ModuleJettison>();
				if (engineShrouds.Length > 0) {
					if (shroudEnabled) {
						turnedOffEngineShroud = engineShrouds[0].shroudHideOverride;
						foreach (ModuleJettison engineShroud in engineShrouds) {
							engineShroud.shroudHideOverride = true;
						}
					} else {
						foreach (ModuleJettison engineShroud in engineShrouds) {
							engineShroud.shroudHideOverride = turnedOffEngineShroud;
						}
					}
				}
			}
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
			
			ShroudTexture shroudTex = ShroudTexture.shroudTextures[textureIndex];

			//save current textures name
			textureName = shroudTex.name;

			Vector2 sideSize = new Vector2(Mathf.Max(botWidth,topWidth), new Vector2(height,topWidth-botWidth).magnitude);
			Vector2 topSize = new Vector2(topWidth, topWidth * thickness);

			shroudTex.textures[0].SetMaterialProperties(shroudMats[0], sideSize);
			shroudTex.textures[1].SetMaterialProperties(shroudMats[1], topSize);
			shroudTex.textures[2].SetMaterialProperties(shroudMats[2], sideSize);

			shroudGO.GetComponent<Renderer>().materials = shroudMats;
		}

		void partReattached() {
			detectSize();
			if (shroudGO == null)
				createShroudGO();
			
		}

		//Automatically sets size of shrouds
		void detectSize(object arg) { detectSize(); }
		void detectSize() {


			invisibleShroud = false;
			

			//Check if the size has to be reset
			if (!autoDetectSize || !HighLogic.LoadedSceneIsEditor || !part.isAttached || !shroudEnabled) {
				return;
			}


			thickness = defaultThickness;
			vertOffset = defaultVertOffset;
			//Debug.Log("Defaults: " + defaultBotWidth + ", " + defaultVertOffset);

			if (defaultBotWidth != 0) {
				botWidth = defaultBotWidth;
			} else {
				if (part.collider != null) {
					//botWidth = part.collider.bounds.size.x * part.transform.localScale.x;
					//botWidth = TrySnapToSize(botWidth, radialSnapMargin);
					MeshCollider mc = null;
					if (part.collider is MeshCollider) {
						mc = (MeshCollider)part.collider;
					} else {
						Debug.Log("part collider is " + part.collider.GetType().ToString());
					}

					if (mc != null) {
						//mc.sharedMesh.RecalculateBounds();
						botWidth = mc.sharedMesh.bounds.size.x * part.transform.localScale.x;

						//Scale width with scale of parent transforms
						Transform parentTransform = mc.transform;
						while (parentTransform != part.transform && parentTransform != null) {
							botWidth *= parentTransform.localScale.x;
							parentTransform = parentTransform.parent;
						}


						botWidth = getPartRadAtPos(part, GetDecouperShroudedNodeWorldPos(), part.transform.up, .2f) * 2;

						botWidth = TrySnapToSize(botWidth, radialSnapMargin);
						//Debug.Log("MeshSize: " + mc.sharedMesh.bounds.size.x + ", Scale: " + shroudAttatchedPart.transform.localScale.x+ ", MeshGO Scale" + mc.transform.localScale.x);
					} else {
						botWidth = part.collider.bounds.size.x;
						botWidth = TrySnapToSize(botWidth, radialSnapMargin);
						//Debug.Log("Size: " + shroudAttatchedPart.collider.bounds.size.x + ", " + shroudAttatchedPart.transform.localScale.x);
					}

				}
			}

			//Get part the shroud is attached to
			Part shroudAttatchedPart = GetShroudAttachedPart();

			if (shroudAttatchedPart != null) {
				//Calculate top Width
				if (shroudAttatchedPart.collider != null) {

					//Check if meshCollider
					MeshCollider mc = null;
					if (shroudAttatchedPart.collider is MeshCollider) {
						mc = (MeshCollider)shroudAttatchedPart.collider;
					} else {
						Debug.Log("attached collider is "+ shroudAttatchedPart.collider.GetType().ToString());
					}

					if (mc != null) {
						//mc.sharedMesh.RecalculateBounds();
						/*
						topWidth = mc.sharedMesh.bounds.size.x * shroudAttatchedPart.transform.localScale.x;

						//Scale width with scale of parent transforms
						Transform parentTransform = mc.transform;
						while (parentTransform != shroudAttatchedPart.transform && parentTransform != null) {
							topWidth *= parentTransform.localScale.x;
							parentTransform = parentTransform.parent;
						}*/
						topWidth = getPartRadAtPos(shroudAttatchedPart, GetShroudattachShroudedNodeWorldPos(), part.transform.up, .2f) * 2;

						topWidth = TrySnapToSize(topWidth, radialSnapMargin);
						//Debug.Log("MeshSize: " + mc.sharedMesh.bounds.size.x + ", Scale: " + shroudAttatchedPart.transform.localScale.x+ ", MeshGO Scale" + mc.transform.localScale.x);
					} else {
						topWidth = shroudAttatchedPart.collider.bounds.size.x;
						topWidth = TrySnapToSize(topWidth, radialSnapMargin);
						//Debug.Log("Size: " + shroudAttatchedPart.collider.bounds.size.x + ", " + shroudAttatchedPart.transform.localScale.x);
					}
					
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
				invisibleShroud = true;
				topWidth = botWidth;
			}
			
			//Update shroud mesh
			if (shroudGO != null) {
				updateShroud();
			}
		}

		public float getPartRadAtPos(Part p, Vector3 pos, Vector3 nor, float maxDist) {
			MeshCollider mc = p.collider as MeshCollider;
			Vector3 locPos = mc.transform.InverseTransformPoint(pos);
			Vector3 locNor = mc.transform.InverseTransformVector(nor.normalized);
			maxDist *= locNor.magnitude;
			locNor.Normalize();
			//Debug.Log("getPartSizeAtPos: " + p.name + ", " + locPos + ", "+locNor+", " + nor+", "+mc.sharedMesh.vertices.Length);

			Vector3 minV = Vector3.one * 10000;
			bool inRange = false;
			Vector3 maxRad = Vector3.zero;
			foreach (Vector3 v in mc.sharedMesh.vertices) {
				Vector3 d = v - locPos;
				float dist = Vector3.Dot(d, locNor);
				if (Mathf.Abs(dist) < maxDist) {
					inRange = true;
					Vector3 rad = (d - dist * locNor);
					if (rad.magnitude > maxRad.magnitude) {
						maxRad = rad;
					}
				} else {
					if (Mathf.Abs(dist) < Vector3.Dot(minV, locNor)) {
						minV = d;
					}
				}
			}
			if (!inRange) {
				//Debug.Log("None in range "+minV);
			}
			//Debug.Log(maxRad+" _ "+mc.transform.TransformVector(maxRad)+"\n");
			return mc.transform.TransformVector(maxRad).magnitude;
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
			if (engineShrouds != null) {
				if (engineShrouds.Length > 0) {
					foreach (ModuleJettison engineShroud in engineShrouds) {
						engineShroud.shroudHideOverride = turnedOffEngineShroud;
					}
				}
			}
		}

		public void OnDestroy() {
			destroyShroud();
		}

		void destroyShroud() {
			if (shroudGO != null) {
				Destroy(shroudGO);
			}
			if (shroudMats != null) {
				foreach (Material mat in shroudMats) {
					if (mat != null) {
						Destroy(mat);
					}
				}
			}
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
			updateTexture();
			shroudCylinders.update();
			shroudGO.GetComponent<MeshRenderer>().enabled = !invisibleShroud;
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

			if (shroudMats != null) {
				foreach(Material mat in shroudMats) {
					Destroy(mat);
				}
			}
			shroudGO.AddComponent<MeshRenderer>();

			//Setup materials
			CreateMaterials();
			updateTexture();

			generateDragCube();
		}

		//Creates the material for the mesh
		void CreateMaterials() {
			shroudMats = new Material[3];
			for (int i = 0; i < shroudMats.Length; i++) {
				shroudMats[i] = new Material(Shader.Find("KSP/Bumped Specular"));
			}

		}

		bool destroyShroudIfNoTopNode() {
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode == null) {
				Debug.LogError("Decoupler is missing top node!");
				Debug.LogError("Removing Decouplershroud from part: "+part.name);
				part.RemoveModule(this);
				Destroy(this);
				return true;
			}
			return false;
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

		Part GetShroudAttachedPart() {
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

		Vector3 GetDecouperShroudedNodeWorldPos() {
			return part.transform.TransformPoint(part.FindAttachNode("top").position);
		}
		Vector3 GetShroudattachShroudedNodeWorldPos() {
			Part attached = GetShroudAttachedPart();
			AttachNode an = attached.FindAttachNodeByPart(GetShroudedPart());
			if (an != null) {
				return attached.transform.TransformPoint(an.position);
			}
			return Vector3.zero;
		}

		public bool ClosedAndLocked() {
			return shroudEnabled && GetShroudedPart() != null;
		}

		public Vessel GetVessel() {
			return vessel;
		}

		public Part GetPart() {
			return part;
		}
	}
}
