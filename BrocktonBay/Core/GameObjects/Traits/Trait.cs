using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public enum EffectTrigger {
		None = 0,
		GetRatings = 1,
		ActionPhase = 2,
		ResponsePhase = 3,
		ResolutionPhase = 4,
		MastermindPhase = 5,
		EventPhase = 6
	}

	public sealed class MechanicData {
		public string name = "New Mechanic";
		public string type = "";
		public int secrecy = 0;
		public string description = "";
		public string effect = "";
		public MechanicData () { }
		public MechanicData (Trait mechanic) {
			name = mechanic.name;
			type = mechanic.type;
			secrecy = mechanic.secrecy;
			description = mechanic.description;
			effect = mechanic.effect;
		}
		public MechanicData (string type)
			=> this.type = type;
	}

	public abstract class Trait : IGUIComplete {

		//IDependable stuff
		public int order { get { return 0; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public virtual void Reload () { }
		public virtual void OnTriggerDestroyed (IDependable trigger) { }
		public virtual void OnListenerDestroyed (IDependable listener) { if (listener == parent) DependencyManager.Destroy(this); }


		[Displayable(0, typeof(StringField), overrideLabel = "Trait")]
		public string name { get; set; }

		public string type;

		[Displayable(1, typeof(IntField))]
		public int secrecy { get; set; }

		[Displayable(2, typeof(DialogTextEditableField))]
		public string description { get; set; }

		//Managed by derivative classes
		public abstract string effect { get; set; }
		public abstract EffectTrigger trigger { get; }

		public bool Known (Context context) {
			return (context.agent == Game.player && Game.omniscient) || context.agent.knowledge[parent] >= secrecy;
		}

		public GameObject parent;

		public static Trait Create () {
			Trait newMechanic = null;
			Dialog dialog = new Dialog("Choose type of mechanic to create", MainWindow.main, DialogFlags.DestroyWithParent, "Cancel", ResponseType.Cancel, "Ok", ResponseType.Ok);
			ComboBox comboBox = new ComboBox(new string[] { "Weakness", "TrueForm", "Prison" });
			dialog.VBox.PackStart(comboBox, true, true, 0);
			dialog.ShowAll();
			dialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Ok) {
					newMechanic = Load(new MechanicData(comboBox.ActiveText));
				}
			};
			dialog.Run();
			dialog.Destroy();
			return newMechanic;
		}

		public static Trait Load (MechanicData data) {
			switch (data.type) {
				case "Weakness":
					return new WeaknessTrait(data);
				case "TrueForm":
					return new TrueFormTrait(data);
				case "Prison":
					return new PrisonTrait(data);
				case "Prisoner":
					return new PrisonerTrait(data);
				default:
					return null;
			}
		}

		public Trait (MechanicData data) {
			name = data.name;
			type = data.type;
			secrecy = data.secrecy;
			description = data.description;
			//set effect in derived class' constructor
		}

		public virtual object Invoke (Context context = new Context(), object obj = null) => null;
		public virtual void ManageCellContents (Widget widget) { }

		//IGUIComplete stuff
		public virtual Widget GetHeader (Context context) {
			if (context.compact) {
				return new InspectableBox(new Label(name), this);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox, WidthRequest = 200 };
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(UIFactory.Align(Graphics.GetSmartHeader(context.butCompact, parent), 0.5f, 0.5f, 0, 0));
				return headerBox;
			}
		}

		public virtual Widget GetCellContents (Context context) {
			DialogTextEditableField field = (DialogTextEditableField)UIFactory.Fabricate(this, "effect", context.butCompact);
			field.BorderWidth = 5;
			return field;
		}

	}

}
