using System;

namespace Parahumans.Core {

	// "DisplayableAttribute" is used to tag a property to indicate that they can be displayed by the UI and to supply the relevant metadata for displaying them.
	public class DisplayableAttribute : Attribute {
		public int order;   //The position in which the property is displayed, with lower numbers rendered first and higher numbers shown last.
		public Type widget; //The widget used to display this attribute.
		public object argument; //An optional argument that is passed to the initialization of the display widget.

		public DisplayableAttribute (int o, Type wgt, object arg = null) {
			order = o;
			widget = wgt;
			argument = arg;
		}

	}

	// A bimorphically displayable property uses different display widgets when presented vertically and horizontally.
	// "widget" is the vertical display and "widget2" is for the horizontal
	public class BimorphicDisplayableAttribute : DisplayableAttribute {
		public Type widget2;
		public BimorphicDisplayableAttribute (int o, Type wgt, Type wgt2, object arg = null) : base(o, wgt, arg)
			=> widget2 = wgt2;
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

	// "EmphasizedAttribute" 'emphasizes' a property in the UI, separating the from other attributes with horizontal separators and adding some padding in cases.
	// Emphasis does not change the actual contents displayed, only the spacing and placing of the display widget relative to the surroundings.
	public class ExpandAttribute : Attribute { } //Only really works in a vertical generation, with either no emphasisBoxes or the property being in the last emphasisBox.
	public class EmphasizedAttribute : Attribute { }
	public class EmphasizedIfVerticalAttribute : EmphasizedAttribute { }
	public class EmphasizedIfHorizontalAttribute : EmphasizedAttribute { }
	public class VerticalOnlyAttribute : Attribute { } //Show only if rendered in vertical mode.
	public class ForceVerticalAttribute : Attribute { } //Force "vertical" to always be passed to the generated field.
	public class ForceHorizontalAttribute : Attribute { } //Force "horizontal" to always be passed to the generated field.

}
