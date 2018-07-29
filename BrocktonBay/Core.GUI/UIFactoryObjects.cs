using System;

namespace Parahumans.Core {

	// "DisplayableAttribute" is used to tag a property to indicate that they can be displayed by the UI and to supply the relevant metadata for displaying them.
	public class DisplayableAttribute : Attribute {
		public int order;   //The position in which the property is displayed, with lower numbers rendered first and higher numbers shown last.
		public Type widget; //The widget used to display this attribute.
		public object arg; //An optional argument that is passed to the initialization of the display widget.

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

	// A bimorphically displayable property uses different display widgets when presented vertically and horizontally.
	// "widget" is the vertical display and "widget2" is for the horizontal
	public class BimorphicDisplayableAttribute : DisplayableAttribute {
		public Type widget2;
		public BimorphicDisplayableAttribute (int o, Type wgt, Type wgt2, params object[] arg) : base(o, wgt, arg)
			=> widget2 = wgt2;
	}

	// ChildAttribute indicates a Displayable should not be rendered in UIFactory.Generate(), but instead independently
	// by ContainerFields when those are rendered.
	public class ChildAttribute : Attribute {
		public string name;
		public ChildAttribute (string name) => this.name = name;
	}

	// "PaddedAttribute" is used with "DisplayableAttribute" when we want to tell the UI system to give a property padding.
	public class PaddedAttribute : Attribute {
		public uint topPadding;
		public uint bottomPadding;
		public uint leftPadding;
		public PaddedAttribute (uint top, uint bottom) {
			topPadding = top;
			bottomPadding = bottom;
			leftPadding = 0;
			rightPadding = 0;
		}
		public uint rightPadding;
		public PaddedAttribute (uint top, uint bottom, uint left, uint right) {
			topPadding = top;
			bottomPadding = bottom;
			leftPadding = left;
			rightPadding = right;
		}
	}

	public class TooltipTextAttribute : Attribute {
		public string text;
		public TooltipTextAttribute (string tooltip) {
			text = tooltip;
		}
	}

	public class PlayerEditableAttribute : Attribute {
		public Phase editablePhases;
		public bool currentlyEditable { get => editablePhases == Phase.All || editablePhases == Game.phase; }
		public PlayerEditableAttribute (Phase phases) => editablePhases = phases;
	}
	public class LimitVisibilityAttribute : Attribute {
		public Phase visiblePhases;
		public bool currentlyVisible { get => visiblePhases == Phase.All || visiblePhases == Game.phase; }
		public LimitVisibilityAttribute (Phase phases) => visiblePhases = phases;
	}

	// "EmphasizedAttribute" 'emphasizes' a property in the UI, separating the from other attributes with horizontal separators and adding some padding in cases.
	// Emphasis does not change the actual contents displayed, only the spacing and placing of the display widget relative to the surroundings.
	public class ExpandAttribute : Attribute { } //Only really works in a vertical generation, with either no emphasisBoxes or the property being in the last emphasisBox.
	public class EmphasizedAttribute : Attribute { }
	public class EmphasizedIfVerticalAttribute : EmphasizedAttribute { }
	public class EmphasizedIfHorizontalAttribute : EmphasizedAttribute { }
	public class VerticalOnlyAttribute : Attribute { } //Show only if rendered in vertical mode.
	public class ForceVerticalAttribute : Attribute { } //Force "vertical" to always be passed to the generated field.
	public class ForceHorizontalAttribute : Attribute { } //Force "horizontal" to always be passed to the generated field.

	public interface LabelOverridable {
		void OverrideLabel (string newLabel);
	}

}
