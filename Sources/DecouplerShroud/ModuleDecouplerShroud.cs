using System;
using UnityEngine;

namespace DecouplerShroud
{
    public class ModuleDecouplerShroud : PartModule, IAirstreamShield {

		float[] snapSizes = new float[] { .63f , 1.25f, 2.5f, 3.75f, 5f, 7.5f};
		int[] segmentCountLUT =   new int[] { 1, 2, 3, 4, 6 };
		int[] collPerSegmentLUT = new int[] { 12, 6, 4, 3, 2};

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

		[KSPField(guiName = "Jettison Mode", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "Stay", "2 Shells", "3 Shells", "4 Shells", "6 Shells" }, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
		int segmentIndex;

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
		[KSPField(isPersistant = false)]
		public float jettisonVelocity = 1;
		[KSPField(isPersistant = false)]
		public bool collisionEnabled;
		[KSPField(isPersistant = false)]
		public float editorMinAlpha = .2f;

		[KSPField(isPersistant = true)]
		public int segments = 1;
		[KSPField(isPersistant = true)]
		public int collPerSegment = 1;

		bool setupFinished = false;
		ModuleJettison[] engineShrouds;
		GameObject shroudGO;
		Material[] shroudMats;
		ShroudShaper shroudShaper;

		[KSPField(isPersistant = true)]
		public bool jettisoned = false;
		[KSPField]
		bool turnedOffEngineShroud;

		//Needed to call updateTexture a few times after changing segment count
		//otherwise the transparency of the outside shroud is constant for some reason
		int Fix_SegmentChangedCallUpdateTexture = 0;

		//true when decoupler has no grandparent in the editor and automatic size detection is active
		[KSPField(isPersistant = true)]
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

		DragCubeList stockDragCubes;

		public void setup() {
			//Debug.Log("[Decoupler Shroud] jet: " + jettisoned + ", attached: " + part.isAttached + ", inv: "+invisibleShroud);
			
			//Get rid of decoupler shroud module if no top node found
			if (destroyShroudIfNoTopNode()) {
				return;
			}

			segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCountLUT.Length - 1);
			segments = segmentCountLUT[segmentIndex];
			collPerSegment = collPerSegmentLUT[segmentIndex];
			//Debug.Log("!!set segment count to: " + segments + ", Index: " + segmentIndex);

			stockDragCubes = part.DragCubes;
			getTextureNames();
			//Remove copied decoupler shroud when copied
			Transform copiedDecouplerShroud = transform.Find("DecouplerShroud");
			if (copiedDecouplerShroud != null) {
				Destroy(copiedDecouplerShroud.gameObject);
				shroudShaper = null;
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
			Fields[nameof(textureIndex)].OnValueModified += changeMaterial;

			Fields[nameof(segmentIndex)].OnValueModified += segmentUpdate;


			setButtonActive();

			if (HighLogic.LoadedSceneIsFlight) {
				if (GetShroudedPart() != null && shroudEnabled) {
					GetShroudedPart().AddShield(this);
				}
			}
			createNewShroudGO();
			//detectSize();
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
					UpdateMaterialsOpacity();

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
					if (!jettisoned && part.GetComponent<ModuleDecouple>().isDecoupled) {
						Jettison();
					}
				}
			}
			if (HighLogic.LoadedSceneIsEditor) {
				// Toggle engineShroud if new engine is placed on decoupler
				detectSize();
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

		//Jettisons the shroud
		[KSPEvent(guiName = "Jettison", guiActive = true, guiActiveEditor = false)]
		public void Jettison() {
			//Debug.Log("Jettison called on DecouplerShroud of "+part.name);

			if (segments < 2 || !HighLogic.LoadedSceneIsFlight) {
				return;
			}
			Events[nameof(Jettison)].guiActive = false;

			jettisoned = true;

			for (int i = 0; i < shroudGO.transform.childCount; i++) {
				GameObject c = shroudGO.transform.GetChild(i).gameObject;
				//c.layer = 19;
				physicalObject ph = c.AddComponent<physicalObject>();
				ph.rb = c.AddComponent<Rigidbody>();

				float ang = 2 * Mathf.PI * (i+.5f) / (float)segments;
				ph.rb.AddRelativeForce(new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang)) * jettisonVelocity, ForceMode.VelocityChange);
			}
			shroudGO.transform.DetachChildren();
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
				options[i] = ShroudTexture.shroudTextures[i].displayName;

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

		//Executes when amount of segments is changed
		private void segmentUpdate(object arg1) {
			segments = segmentCountLUT[segmentIndex];
			collPerSegment = collPerSegmentLUT[segmentIndex];
			//Trigger rebuilding of shrouds
			createNewShroudGO();

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

            if (!HighLogic.LoadedSceneIsEditor)
                return;

			if (shroudEnabled) {
				Fields[nameof(autoDetectSize)].guiActiveEditor = true;
				Fields[nameof(segmentIndex)].guiActiveEditor = true;
				Fields[nameof(textureIndex)].guiActiveEditor = true;

			} else {
				Fields[nameof(autoDetectSize)].guiActiveEditor = false;
				Fields[nameof(segmentIndex)].guiActiveEditor = false;
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

			Events[nameof(Jettison)].guiActive = !jettisoned && shroudEnabled && (segments > 1);
			//Debug.Log("set jettison gui to: "+ (!jettisoned && shroudEnabled && (segments > 1)) +", "+jettisoned+", "+shroudEnabled+", "+(segments>1)+", "+segments);
		}

		void changeMaterial(object arg) { changeMaterial(); }
		void changeMaterial() {
			ShroudTexture shroudTex = ShroudTexture.shroudTextures[textureIndex];

			//save current textures name
			textureName = shroudTex.name;
			CreateMaterials(shroudTex);

			updateTextureScale();
		}

		void updateTextureScale() {
			if (shroudMats == null) {
				changeMaterial();
				return;
			}
			if (shroudMats[0] == null) {
				Debug.LogWarning("called updateTExtureScale while shroudMats[0] == null");
				changeMaterial();
				return;
			}

			ShroudTexture shroudTex = ShroudTexture.shroudTextures[textureIndex];

			Vector2 sideSize = new Vector2(Mathf.Max(botWidth,topWidth), new Vector2(height,topWidth-botWidth).magnitude);
			Vector2 topSize = new Vector2(topWidth, topWidth * thickness);

			shroudTex.textures[0].SetTextureScale(shroudMats[0], sideSize);
			shroudTex.textures[1].SetTextureScale(shroudMats[1], topSize);
			shroudTex.textures[2].SetTextureScale(shroudMats[2], sideSize);

			if (shroudGO == null) {
				return;
			}
			foreach (Renderer r in shroudGO.GetComponentsInChildren<Renderer>()) {
				if (r.materials != shroudMats) {
					foreach (Material mat in r.materials) {
						if (mat != null)
							Destroy(mat);
					}
					r.materials = shroudMats;
				}
			}
		}

		//Creates the material for the mesh
		void CreateMaterials(ShroudTexture shroudTex) {
			//Clean up old materials
			if (shroudMats != null) {
				foreach (Material mat in shroudMats) {
					if (mat != null) {
						Destroy(mat);
					}
				}
			}
			shroudMats = new Material[3];

			for (int i = 0; i < shroudMats.Length; i++) {

				SurfaceTexture surf = shroudTex.textures[i];

				shroudMats[i] = Instantiate(surf.mat);
				shroudMats[i].name = "shroudMat: " + i + ", " + segments + " segments";

				if (HighLogic.LoadedSceneIsEditor) {
					// Enables transparency in editor
					shroudMats[i].renderQueue = 3000;
				}
			}

		}

		void partReattached() {
			detectSize();
			if (shroudGO == null)
				createNewShroudGO();
			
		}

		//Automatically sets size of shrouds
		void detectSize(object arg) { detectSize(); }
		void detectSize() {

			if(!HighLogic.LoadedSceneIsEditor){
				return;
			}

			invisibleShroud = false;
			
			//Check if the size has to be reset
			if (!autoDetectSize || !part.isAttached || !shroudEnabled) {
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
						Debug.LogWarning("DecouplerShroud: part collider is " + part.collider.GetType().ToString());
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

			//World Pos of top attach node, or biggest part of meshcollider
			Vector3 topCenterWorld = Vector3.zero;

			if (shroudAttatchedPart != null) {
				//Calculate top Width
				if (shroudAttatchedPart.collider != null) {

					//Check if meshCollider
					MeshCollider mc = null;
					if (shroudAttatchedPart.collider is MeshCollider) {
						mc = (MeshCollider)shroudAttatchedPart.collider;
					} else {
						//Debug.LogWarning("[DecouplerShroud] attached collider is "+ shroudAttatchedPart.collider.GetType().ToString());
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
						topWidth = getPartRadAtPos(shroudAttatchedPart, GetShroudattachShroudedNodeWorldPos(), part.transform.up, .2f, out topCenterWorld) * 2;

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

				//If we got the top from mesh collider
				if (topCenterWorld != Vector3.zero) {
					nodeWorldPos = topCenterWorld;
				}

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
			Vector3 a = Vector3.zero;
			return getPartRadAtPos(p,pos,nor,maxDist, out a);
		}
		public float getPartRadAtPos(Part p, Vector3 pos, Vector3 nor, float maxDist, out Vector3 worldPos) {

			MeshCollider mc = p.collider as MeshCollider;
			if (mc == null) {
				worldPos = Vector3.zero;
				return 0;
			}
			Vector3 locPos = mc.transform.InverseTransformPoint(pos);
			Vector3 locNor = mc.transform.InverseTransformVector(nor.normalized);
			maxDist *= locNor.magnitude;
			locNor.Normalize();
			//Debug.Log("getPartSizeAtPos: " + p.name + ", " + locPos + ", "+locNor+", " + nor+", "+mc.sharedMesh.vertices.Length);

			Vector3 minV = Vector3.one * 10000;
			bool inRange = false;
			Vector3 maxRad = Vector3.zero;
			worldPos = Vector3.zero;
			foreach (Vector3 v in mc.sharedMesh.vertices) {
				Vector3 d = v - locPos;
				float dist = Vector3.Dot(d, locNor);
				if (Mathf.Abs(dist) < maxDist) {
					inRange = true;
					Vector3 rad = (d - dist * locNor);
					if (rad.magnitude > maxRad.magnitude) {
						maxRad = rad;
						worldPos = v - rad;
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
			worldPos = mc.transform.TransformPoint(worldPos);
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

		//If decoupler is detached from engine, remove shroud and reenable stock shrouds
		void partDetached() {
			if (GetShroudedPart() == null) {
				destroyShroud();
				if (engineShrouds != null) {
					if (engineShrouds.Length > 0) {
						foreach (ModuleJettison engineShroud in engineShrouds) {
							engineShroud.shroudHideOverride = turnedOffEngineShroud;
						}
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
				for (int i = 0; i < shroudMats.Length; i++) {
					if (shroudMats[i] != null) {
						Destroy(shroudMats[i]);
					}
				}
			}
			shroudGO = null;
			shroudMats = null;
		}

		//Generates the shroud for the first time
		void generateShroud() {
			shroudShaper = new ShroudShaper(this, nSides);
			shroudShaper.generate();
		}

		//updates the shroud mesh when values changed
		void updateShroud(object arg) { updateShroud(); }
		void updateShroud() {
			if (!shroudEnabled) {
				destroyShroud();
			}
			if (shroudGO == null || shroudShaper == null) {
				createNewShroudGO();
			}

			updateTextureScale();

			shroudShaper.update();
			if (shroudGO != null) {
				shroudGO.SetActive(!invisibleShroud);
			}

            if (isFarInstalled())
            {
                part.SendMessage("GeometryPartModuleRebuildMeshData");
            }
        }

		//Recalculates the drag cubes for the model
		void generateDragCube() {


			if (isFarInstalled()) {
				Debug.Log("[Debug] Updating FAR Voxels");
				part.SendMessage("GeometryPartModuleRebuildMeshData");
			}

			if (shroudEnabled && HighLogic.LoadedSceneIsFlight) {
				//Calculate dragcube for the cone manually
				DragCube dc = DragCubeSystem.Instance.RenderProceduralDragCube(part);
				part.DragCubes.ClearCubes();
				part.DragCubes.Cubes.Add(dc);
				part.DragCubes.ResetCubeWeights();
				part.DragCubes.ForceUpdate(true,true,false);

			}
		}

		//Create the gameObject with the meshrenderer
		void createNewShroudGO() {

            //Debug.Log("[Debug] createNewShroudGO called, shroudEnabled: "+shroudEnabled+", jettisoned: "+jettisoned);

			if (!shroudEnabled || jettisoned) {
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

			if (shroudMats != null) {
				foreach(Material mat in shroudMats) {
					Destroy(mat);
				}
			}
			shroudMats = null;

			//Create Segment GameObjects
			for (int i = 0; i < segments; i++) {
				GameObject segment = new GameObject("ShroudSegment: "+i);
				segment.transform.parent = shroudGO.transform;
				segment.transform.localPosition = Vector3.zero;
				segment.transform.localRotation = Quaternion.identity;
				segment.AddComponent<MeshFilter>().mesh = shroudShaper.multiCylinder.meshes[i];
				segment.AddComponent<MeshRenderer>();

				//Create Gameobjects with meshColliders if collisionEnabled
				if (collisionEnabled) {
					for (int j = 0; j < collPerSegment; j++) {
						GameObject segColl = new GameObject("SegColl");
						segColl.transform.parent = segment.transform;
						segColl.transform.localPosition = Vector3.zero;
						segColl.transform.localRotation = Quaternion.identity;
						segColl.AddComponent<MeshCollider>();
						segColl.GetComponent<MeshCollider>().sharedMesh = shroudShaper.collCylinder.meshes[i * collPerSegment + j];
						segColl.GetComponent<MeshCollider>().convex = true;

						//segColl.AddComponent<MeshFilter>().sharedMesh = shroudCylinders.collCylinder.meshes[i * collPerSegment + j];
						//segColl.AddComponent<MeshRenderer>();
					}
				}
			}
			//Setup materials
			changeMaterial();

			Fix_SegmentChangedCallUpdateTexture = 5;

			generateDragCube();

			shroudGO.SetActive(!invisibleShroud);
		}


		void UpdateMaterialsOpacity() {
			if (!HighLogic.LoadedSceneIsEditor || shroudMats == null) {
				return;
			}

			//Ugly Fix
			if (--Fix_SegmentChangedCallUpdateTexture > 0) {
				updateTextureScale();
			}

			float alpha = distPointRay(transform.TransformPoint((vertOffset + height) / 2f * Vector3.up),Camera.main.ScreenPointToRay(Input.mousePosition)) / (botWidth + topWidth + height) * 2;
			alpha = Mathf.Clamp(alpha, editorMinAlpha, 1);

			foreach (Material m in shroudMats) {
				if (m == null) {
					Debug.LogWarning("DecouplerShroud: Material in shroudMats is null");
					continue;
				}
				m.SetFloat("_Opacity", alpha);
				
			}
		}

		float distPointRay(Vector3 p, Ray r) {
			return Vector3.Cross(r.direction, p - r.origin).magnitude / r.direction.magnitude;
		}

		bool destroyShroudIfNoTopNode() {
			AttachNode topNode = part.FindAttachNode("top");
			if (topNode == null) {
				Debug.LogError("DecouplerShroud: Decoupler is missing top node!");
				Debug.LogError("DecouplerShroud: Removing Decouplershroud from part: "+part.name);
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

		public static bool FARinstalled, FARchecked;
		public static bool isFarInstalled() {
            
            // FAR support doesn't work yet, so let's just pretend it is not installed
            return false; 
            
			if (!FARchecked) {
				var asmlist = AssemblyLoader.loadedAssemblies;

				if (asmlist != null) {
					for (int i = 0; i < asmlist.Count; i++) {
						if (asmlist[i].name == "FerramAerospaceResearch") {
							FARinstalled = true;

							break;
						}
					}
				}
				Debug.Log("[Debug] Far installed: "+FARinstalled);
				FARchecked = true;
			}

			return FARinstalled;
		}

	}
}
