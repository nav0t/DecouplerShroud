using UnityEngine;

namespace DecouplerShroud {
	class ModuleDecouplerScaler : PartModule, IPartMassModifier, IPartCostModifier {

		[KSPField(guiName = "Diameter", isPersistant = true, guiActiveEditor = true, guiActive = false)]
		[UI_FloatEdit(scene = UI_Scene.Editor, minValue = .15f, maxValue = 20f, incrementLarge = .625f, incrementSlide = 0.01f, incrementSmall = 0.05f, unit = "m", sigFigs = 2, useSI = false)]
		public float diameter = 1.25f;

		[KSPField(guiName = "Mass", guiActiveEditor = true, guiActive = false)]
		public float partMass;

		[KSPField(guiName = "Cost", guiActiveEditor = true, guiActive = false)]
		public float partCost;

		[KSPField]
		public float modelSize = 1.25f;

		[KSPField]
		public float defaultMass = 0;

		[KSPField]
		public float massPerMeterSquared = 0.0256f;

		[KSPField]
		public float defaultCost = 0;

		[KSPField]
		public float baseCost = 200;
		[KSPField]
		public float costPerMeter = 100;

		[KSPField]
		public float ejectionForcePerMeter = 80;

		public void Start() {
			Fields[nameof(diameter)].OnValueModified += setScale;
			setScale();
		}

		public void setScale(object arg) { setScale(); }
		public void setScale() {
			part.transform.FindChild("model").localScale = Vector3.one * diameter / modelSize;
			partMass = Mathf.Round(1000*diameter * diameter * massPerMeterSquared)/1000;
			partCost = Mathf.Round(costPerMeter * diameter + baseCost);
			if (GetComponent<ModuleDecouple>() != null) {
				GetComponent<ModuleDecouple>().ejectionForce = diameter * ejectionForcePerMeter;

			}
		}


		public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) {
			return  partMass - defaultMass;
		}

		public ModifierChangeWhen GetModuleMassChangeWhen() {
			return ModifierChangeWhen.CONSTANTLY;
		}

		public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) {
			return partCost - defaultCost;
		}

		public ModifierChangeWhen GetModuleCostChangeWhen() {
			return ModifierChangeWhen.CONSTANTLY;
		}
	}
}
