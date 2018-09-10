using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public enum EffectTrigger {
		None,
		GetRightClickMenu,
		GetRatings,
		ActionPhase,
		ResponsePhase,
		ResolutionPhase,
		MastermindPhase,
		EventPhase
	}

	public sealed class TraitData {
		public string name = "New Mechanic";
		public string type = "";
		public int secrecy = 0;
		public string description = "";
		public string effect = "";
		public TraitData () { }
		public TraitData (Trait mechanic) {
			name = mechanic.name;
			type = mechanic.type;
			secrecy = mechanic.secrecy;
			description = mechanic.description;
			effect = mechanic.effect;
		}
		public TraitData (string type)
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
		public abstract List<EffectTrigger> trigger { get; }

		public bool Known (Context context) {
			return (context.requester == Game.player && Game.omniscient) || context.requester.knowledge[parent] >= secrecy;
		}

		public virtual GameObject parent { get; set; }

		public static Trait Create () {
			Trait newMechanic = null;
			Dialog dialog = new Dialog("Choose type of mechanic to create", MainWindow.main, DialogFlags.DestroyWithParent, "Cancel", ResponseType.Cancel, "Ok", ResponseType.Ok);
			ComboBox comboBox = new ComboBox(new string[] { "Weakness", "TrueForm", "Prison" });
			dialog.VBox.PackStart(comboBox, true, true, 0);
			dialog.ShowAll();
			dialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Ok) {
					newMechanic = Load(new TraitData(comboBox.ActiveText));
				}
			};
			dialog.Run();
			dialog.Destroy();
			return newMechanic;
		}

		public static Trait Load (TraitData data) {
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

		public Trait (TraitData data) {
			name = data.name;
			type = data.type;
			secrecy = data.secrecy;
			description = data.description;
			//set effect in derived class' constructor
		}

		public virtual object Invoke (EffectTrigger trigger, Context context = new Context(), object obj = null) => null;
		public virtual void ManageCellContents (Widget widget) { }

		//IGUIComplete stuff
		public virtual Widget GetHeader (Context context) {
			if (context.compact) {
				return new InspectableBox(new Label(name), this, context);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this, context);
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

		public virtual Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = new Menu();
			rightClickMenu.Append(MenuFactory.CreateInspectButton(this, rightClickedWidget));
			rightClickMenu.Append(MenuFactory.CreateInspectInNewWindowButton(this));
			if (Game.omnipotent) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateDeleteButton(this));
			}
			return rightClickMenu;
		}

		public void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) { }

	}
}
