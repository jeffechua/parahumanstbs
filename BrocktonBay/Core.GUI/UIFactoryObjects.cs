using System;

namespace BrocktonBay {

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
		public bool isEditPhase { get => editablePhases == Phase.All || editablePhases == Game.phase; }
		public bool isVisiblePhase { get => visiblePhases == Phase.All || visiblePhases == Game.phase; }

		public uint topPadding = 0;
		public uint bottomPadding = 0;
		public uint leftPadding = 0;
		public uint rightPadding = 0;

		public string tooltipText = "";
		public string overrideLabel = "";

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

	}

	public class ChildDisplayableAttribute : DisplayableAttribute {
		public string name;
		public ChildDisplayableAttribute (string name, Type widget, params object[] arg) : base(0, widget, arg) {
			this.name = this.overrideLabel = name;
			generate = false;
		}
	}

}
