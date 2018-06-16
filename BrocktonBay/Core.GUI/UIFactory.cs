using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public static class DragTmpVars {
		public static object currentDragged;
	}

	public static class UIFactory {

		static readonly Type[] constructorSignature = {
			typeof(PropertyInfo),
			typeof(object),
			typeof(Context),
			typeof(object)
		};

		public static VBox GenerateVertical (object obj) {

			Context context = new Context(obj, 0, true, false);

			//Load up all properties
			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());
			for (int i = 0; i < properties.Count; i++) {
				if (properties[i].GetValue(obj) == null || properties[i].GetCustomAttribute(typeof(DisplayableAttribute)) == null) {
					properties.RemoveAt(i);
					i--;
				}
			}
			properties.Sort((x, y) => ((DisplayableAttribute)x.GetCustomAttribute(typeof(DisplayableAttribute))).order.CompareTo(
				((DisplayableAttribute)y.GetCustomAttribute(typeof(DisplayableAttribute))).order));

			//Create boxes
			VBox mainBox = new VBox(false, 0) { BorderWidth = 5 };
			VBox emphasisBox = null;

			//Draw each property
			for (int i = 0; i < properties.Count; i++) {

				//Load up attributes
				DisplayableAttribute attr = (DisplayableAttribute)properties[i].GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emphAttribute = (EmphasizedAttribute)properties[i].GetCustomAttribute(typeof(EmphasizedAttribute));
				PaddedAttribute padded = (PaddedAttribute)properties[i].GetCustomAttribute(typeof(PaddedAttribute));
				bool forceHorizontal = HasAttribute(properties[i], typeof(ForceHorizontalAttribute));

				//Construct the widget
				ConstructorInfo constructor = attr.widget.GetConstructor(constructorSignature);
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					properties[i],
					obj,
					forceHorizontal ? context.butHorizontal : context,
					attr.argument
				});

				//Manage padding
				if (padded != null) {
					newWidget = new Gtk.Alignment(0, 0, 1, 1) {
						Child = newWidget,
						TopPadding = padded.topPadding,
						BottomPadding = padded.bottomPadding,
						LeftPadding = padded.leftPadding,
						RightPadding = padded.rightPadding
					};
				}

				//Manage emphasis
				if (emphAttribute is EmphasizedIfHorizontalAttribute) emphAttribute = null;
				if (emphAttribute != null) {
					if (emphasisBox == null)                                     // If no emphasisBox at the moment,
						emphasisBox = new VBox(false, 5);                        // make one.
					emphasisBox.PackStart(new HSeparator(), false, false, 0);    // Install a delimiter
					emphasisBox.PackStart(newWidget, false, false, 0);           // Pack the widget into emphasisBox
				} else { // and non-emphasis
					if (emphasisBox != null) {
						emphasisBox.PackStart(new HSeparator(), false, false, 0); // Finish off the emphasis box
						mainBox.PackStart(emphasisBox, false, false, 6);          // And pack emphasisBox into mainBox
						emphasisBox = null;                                       // Null it so we know to create a new one next time
					}
					mainBox.PackStart(newWidget, false, false, 2);                // Now actually pack the current widget
				}

			}

			//Pack the emphasisBox into mainBox if it's not already.
			if (emphasisBox != null) {
				emphasisBox.PackStart(new HSeparator(), false, false, 0);
				mainBox.PackStart(emphasisBox, false, false, 5);
				emphasisBox = null;
			}

			return mainBox;

		}

		public static VBox GenerateHorizontal (object obj) {

			Context context = new Context(obj, 0, false, false);

			//Load up all properties
			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());
			for (int i = 0; i < properties.Count; i++) {
				if (properties[i].GetValue(obj) == null || properties[i].GetCustomAttribute(typeof(DisplayableAttribute)) == null) {
					properties.RemoveAt(i);
					i--;
				}
			}
			properties.Sort((x, y) => ((DisplayableAttribute)x.GetCustomAttribute(typeof(DisplayableAttribute))).order.CompareTo(
				((DisplayableAttribute)y.GetCustomAttribute(typeof(DisplayableAttribute))).order));

			//Initialize boxes
			VBox mainBox = new VBox(false, 10) { BorderWidth = 5 };
			HBox regularBox = new HBox(false, 0);
			VBox emphasisBox = new VBox(false, 2);

			for (int i = 0; i < properties.Count; i++) {

				if (properties[i].GetCustomAttribute(typeof(VerticalOnlyAttribute)) != null) continue;

				//Load attributes
				DisplayableAttribute attr = (DisplayableAttribute)properties[i].GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emph = (EmphasizedAttribute)properties[i].GetCustomAttribute(typeof(EmphasizedAttribute));
				bool forceVertical = HasAttribute(properties[i], typeof(ForceVerticalAttribute));

				//Obtain the correct constructor
				ConstructorInfo constructor;
				if (attr is BimorphicDisplayableAttribute) {
					constructor = ((BimorphicDisplayableAttribute)attr).widget2.GetConstructor(constructorSignature);
				} else {
					constructor = attr.widget.GetConstructor(constructorSignature);
				}

				//Construct the widget
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					properties[i],
					obj,
					forceVertical?context.butVertical:context,
					attr.argument
				});

				//Pack into the correct box (emphasisBox/regularBox)
				if (emph is EmphasizedIfVerticalAttribute) emph = null;
				if (emph == null) {
					if (regularBox.Children.Length > 0) regularBox.PackStart(new VSeparator(), false, false, 5);
					regularBox.PackStart(newWidget, false, false, 0);
				} else {
					emphasisBox.PackStart(newWidget, false, false, 0);
				}

			}

			//Pack emphasisBox and regularBox into mainBox
			mainBox.PackStart(new Gtk.Alignment(0, 0, 1, 1) { Child = regularBox, RightPadding = 5 }, false, false, 0);
			mainBox.PackStart(emphasisBox, false, false, 0);

			return mainBox;

		}

		public static bool HasAttribute (PropertyInfo property, Type attribute)
			=> property.GetCustomAttribute(attribute) != null;

	}


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
	public class EmphasizedAttribute : Attribute { }
	public class EmphasizedIfVerticalAttribute : EmphasizedAttribute { }
	public class EmphasizedIfHorizontalAttribute : EmphasizedAttribute { }
	public class VerticalOnlyAttribute : Attribute { } //Show only if rendered in vertical mode.
	public class ForceVerticalAttribute : Attribute { } //Force "vertical" to always be passed to the generated field.
	public class ForceHorizontalAttribute : Attribute { } //Force "horizontal" to always be passed to the generated field.
}
