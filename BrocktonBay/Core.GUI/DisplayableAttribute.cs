using System;

namespace BrocktonBay {

	[Flags]
	public enum Locks { Turn = 1 << 0, Affiliation = 1 << 1, None = 0, All = 1 << 0 | 1 << 1 }

	// "DisplayableAttribute" is used to tag a property to indicate that they can be displayed by the UI and to supply the relevant metadata for displaying them.
	public class DisplayableAttribute : Attribute {

		public int order;   //The position in which the property is displayed, with lower numbers rendered first and higher numbers shown last.
		public Type widget; //The widget used to display this attribute.
		public object arg; //An optional argument that is passed to the initialization of the display widget.

		public Type altWidget = null; //The widget used for horizontal display if a different one is required
		public bool generate = true;

		public bool verticalOnly = false;
		public bool horizontalOnly = false;
		public bool emphasized = false;
		public bool emphasizedIfVertical = false;
		public bool emphasizedIfHorizontal = false;
		public bool expand = false;
		public bool forceVertical = false;
		public bool forceHorizontal = false;

		public Phase editablePhases = Phase.None;
		public Phase visiblePhases = Phase.All;
		public Locks editLocks = Locks.All;
		public Locks viewLocks = Locks.None;

		bool turnEditLocked { get => (editLocks & Locks.Turn) != 0; }
		bool affiliationEditLocked { get => (editLocks & Locks.Affiliation) != 0; }
		bool turnViewLocked { get => (viewLocks & Locks.Turn) != 0; }
		bool affiliationViewLocked { get => (viewLocks & Locks.Affiliation) != 0; }

		public uint topPadding = 0;
		public uint bottomPadding = 0;
		public uint leftPadding = 0;
		public uint rightPadding = 0;
		public bool fillSides = true;

		public string tooltipText = null;
		public string overrideLabel = null;

		public DisplayableAttribute (int order, Type widget, params object[] arg) {
			this.order = order;
			this.widget = widget;
			switch (arg.Length) {
				case 0:
					this.arg = null;
					break;
				case 1:
					this.arg = arg[0];
					break;
				default:
					this.arg = arg;
					break;
			}
		}

		public bool EditAuthorized (object obj) {
			if (Game.omnipotent) return true;
			if (GameObject.TryCast(obj, out IAffiliated affiliated)) {
				if (affiliationEditLocked && affiliated.affiliation != Game.player) return false;
				if (turnEditLocked && Game.turnOrder[Game.turn] != Game.player) return false;
			}
			return (editablePhases & Game.phase) == Game.phase;
		}

		public bool ViewAuthorized (object obj) {
			if (Game.omniscient) return true;
			if (GameObject.TryCast(obj, out IAffiliated affiliated)) {
				if (affiliationViewLocked && affiliated.affiliation != Game.player) return false;
				if (turnViewLocked && Game.turnOrder[Game.turn] != Game.player) return false;
			}
			return (visiblePhases & Game.phase) == Game.phase;
		}

	}

	public class ChildDisplayableAttribute : DisplayableAttribute {
		public string name;
		public ChildDisplayableAttribute (string name, Type widget, params object[] arg) : base(0, widget, arg) {
			this.name = this.overrideLabel = name;
			generate = false;
		}
	}

}
