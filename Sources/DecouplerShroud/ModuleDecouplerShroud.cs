using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud
{
	public class ModuleDecouplerShroud : PartModule {

		const int SIDES = 24;

		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool shroudEnabled = false;

		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_Toggle(invertButton = true)]
		public bool autoDetectSize = true;

		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float topRad = 1.25f;
		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float botRad = 1.25f;
		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = (1/64f), maxValue = 1f, stepIncrement = (1/64f))]
		public float width = .1f;
		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = .125f, maxValue = 7f, stepIncrement = 0.125f)]
		public float height = 1.25f;
		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false), UI_FloatRange(minValue = -1f, maxValue = 1f, stepIncrement = 0.125f)]
		public float bottomStart = 0.0f;

		ModuleJettison engineShroud;
		GameObject shroudGO;
		Material shroudMat;
		ShroudShaper shroudCylinders;

		public void setup() {

			//Set up events
			part.OnEditorAttach += partReattached;
			part.OnEditorDetach += partDetached;

			Fields["shroudEnabled"].OnValueModified += activeToggled;
			Fields["autoDetectSize"].OnValueModified += setButtonActive;
			Fields["autoDetectSize"].OnValueModified += detectSize;

			Fields["topRad"].OnValueModified += updateShroud;
			Fields["botRad"].OnValueModified += updateShroud;
			Fields["height"].OnValueModified += updateShroud;
			Fields["width"].OnValueModified += updateShroud;
			Fields["bottomStart"].OnValueModified += updateShroud;

			setButtonActive();

			if (HighLogic.LoadedSceneIsFlight) {
				createShroudGO();
			} else {
				//Check if part is loaded in VAB with parent
				if (part.parent != null) {
					createShroudGO();
				}
			}
		}


		public void Start() {
			setup();

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
				Fields["autoDetectSize"].guiActiveEditor = true;
			} else {
				Fields["autoDetectSize"].guiActiveEditor = false;
			}
			if (shroudEnabled && !autoDetectSize) {
				Fields["topRad"].guiActiveEditor = true;
				Fields["botRad"].guiActiveEditor = true;
				Fields["height"].guiActiveEditor = true;
				Fields["bottomStart"].guiActiveEditor = true;
				Fields["width"].guiActiveEditor = true;
			} else {
				Fields["topRad"].guiActiveEditor = false;
				Fields["botRad"].guiActiveEditor = false;
				Fields["height"].guiActiveEditor = false;
				Fields["bottomStart"].guiActiveEditor = false;
				Fields["width"].guiActiveEditor = false;
			}

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

			shroudGO = new GameObject("DecouplerShroud");
			shroudGO.transform.parent = transform;
			shroudGO.transform.localPosition = topNode.position;
			shroudGO.transform.localRotation = Quaternion.identity;
			shroudGO.AddComponent<MeshFilter>().sharedMesh = shroudCylinders.multiCylinder.mesh;

			shroudMat = CreateMat();
			shroudGO.AddComponent<MeshRenderer>().material = shroudMat;
		}
		
		//Creates the material for the mesh
		Material CreateMat() {
			Material mat = new Material(Shader.Find("KSP/Bumped Specular"));
			Texture tex = GameDatabase.Instance.GetTexture("DecouplerShroud/Textures/DecouplerShroud", false);
			Texture nor = GameDatabase.Instance.GetTexture("DecouplerShroud/Textures/DecouplerShroudNormals", false);
			
			mat.SetTexture("_MainTex", tex);
			mat.SetTexture("_BumpMap", nor);

			mat.SetFloat("_Shininess", .07f);
			mat.SetColor("_SpecColor", Color.white * (95 / 255f));
			return mat;
		}

	}
}
