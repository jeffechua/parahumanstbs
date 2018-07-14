using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public enum MechanicType {
		None = 0,
		RatingsModifier = 1
	}

	public enum InvocationTrigger {
		None = 0,
		GetRatings = 1
	}

	public sealed class MechanicData {
		public string name = "";
		public MechanicType type = MechanicType.None;
		public InvocationTrigger trigger = InvocationTrigger.None;
		public string description = "";
	}

	public abstract class Mechanic : IGUIComplete {

		//IDependable stuff
		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public abstract void Reload ();

		
		[Displayable(0, typeof(StringField))]
		public string name { get; set; }

		[Displayable(1, typeof(EnumField<MechanicType>))]
		public MechanicType type { get; set; }

		[Displayable(1, typeof(EnumField<MechanicType>)), PlayerInvisible]
		public MechanicType trigger { get; set; }

		[Displayable(1, typeof(StringField))]
		public abstract string effect { get; }

		[Displayable(2, typeof(StringField))]
		public string description;

		public GameObject parent;

		public Mechanic () {
		}

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
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = Graphics.GetSmartHeader(context.butCompact, parent) });
				return headerBox;
			}
		}
		public virtual Widget GetCell (Context context) {
			return new Label(effect);
		}

	}
}
