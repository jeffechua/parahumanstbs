using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public enum InvocationTrigger {
		None = 0,
		GetRatings = 1
	}

	public sealed class MechanicData {
		public string name = "New Mechanic";
		public string type = "";
		public int secrecy = 0;
		public string description = "";
		public string effect = "";
		public MechanicData () {}
		public MechanicData (Mechanic mechanic) {
			name = mechanic.name;
			type = mechanic.type;
			secrecy = mechanic.secrecy;
			description = mechanic.description;
			effect = mechanic.effect;
		}
		public MechanicData (string type)
			=> this.type = type;
	}

	public abstract class Mechanic : IGUIComplete, IKnowable {

		//IDependable stuff
		public int order { get { return 0; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public virtual void Reload () { }


		[Displayable(0, typeof(StringField))]
		public string name { get; set; }

		public string type;

		[Displayable(1, typeof(IntField))]
		public int secrecy { get; set; }

		[Displayable(2, typeof(StringField))]
		public string description { get; set; }

		//Managed by derivative classes
		[Displayable(3, typeof(DialogTextEditableField)), Emphasized]
		public abstract string effect { get; set; }
		public abstract InvocationTrigger trigger { get; }

		public bool Known (Context context) {
			return context.requester.knowledge[parent] >= secrecy;
		}

		public GameObject parent;

		public static Mechanic Create () {
			Mechanic newMechanic = null;
			Dialog dialog = new Dialog("Choose type of mechanic to create", Game.mainWindow, DialogFlags.DestroyWithParent, "Cancel", ResponseType.Cancel, "Ok", ResponseType.Ok);
			ComboBox comboBox = new ComboBox(new string[] { "Weakness" });
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

		public static Mechanic Load (MechanicData data) {
			switch (data.type) {
				case "Weakness":
					return new WeaknessMechanic(data);
				default:
					return null;
			}
		}

		public Mechanic (MechanicData data) {
			name = data.name;
			type = data.type;
			secrecy = data.secrecy;
			description = data.description;
			//set effect in derived class' constructor
		}

		public abstract object Invoke (Context context, object obj);

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
			Table table = new Table(1, 1, true);
			Label number = new Label { UseMarkup = true, Markup = "<big><b>" + secrecy + "</b></big>" };
			Image image = new Image(Stock.DialogAuthentication, IconSize.Dnd);
			table.Attach(UIFactory.Align(number, 0.5f, 0.5f, 0, 0), 0, 1, 0, 1);
			table.Attach(UIFactory.Align(image, 0.5f, 0.5f, 0, 0), 0, 1, 0, 1);
			table.BorderWidth = 10;
			return table;
		}

	}

}
