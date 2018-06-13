﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public static class DragTmpVars {
		public static object currentDragged;
	}

	public static class UIFactory {

		public static VBox GenerateVertical (object obj) {

			VBox mainBox = new VBox(false, 0) { BorderWidth = 5 };

			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());
			for (int i = 0; i < properties.Count; i++) {
				if (properties[i].GetValue(obj) == null || properties[i].GetCustomAttribute(typeof(DisplayableAttribute)) == null) {
					properties.RemoveAt(i);
					i--;
				}
			}
			properties.Sort((x, y) => ((DisplayableAttribute)x.GetCustomAttribute(typeof(DisplayableAttribute))).order.CompareTo(
				((DisplayableAttribute)y.GetCustomAttribute(typeof(DisplayableAttribute))).order));

			VBox emphasisBox = null;

			for (int i = 0; i < properties.Count; i++) {

				DisplayableAttribute attr = (DisplayableAttribute)properties[i].GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emphAttribute = (EmphasizedAttribute)properties[i].GetCustomAttribute(typeof(EmphasizedAttribute));
				PaddedAttribute padded = (PaddedAttribute)properties[i].GetCustomAttribute(typeof(PaddedAttribute));
				ConstructorInfo constructor = attr.widget.GetConstructor(new Type[] { typeof(PropertyInfo), typeof(object), typeof(bool), typeof(object) });
				Widget newWidget = (Widget)constructor.Invoke(new object[] { properties[i], obj, !HasAttribute(properties[i], typeof(ForceHorizontalAttribute)), attr.argument });

				if (padded != null) {
					newWidget = new Gtk.Alignment(0, 0, 1, 1) {
						Child = newWidget,
						TopPadding = padded.topPadding,
						BottomPadding = padded.bottomPadding,
						LeftPadding = padded.leftPadding,
						RightPadding = padded.rightPadding
					};
				}

				if (emphAttribute is EmphasizedIfHorizontalAttribute) emphAttribute = null;

				if (emphAttribute != null) {
					if (emphasisBox == null)
						emphasisBox = new VBox(false, 5);
					emphasisBox.PackStart(new HSeparator(), false, false, 0);
					emphasisBox.PackStart(newWidget, false, false, 0);
				} else {
					if (emphasisBox != null) {
						emphasisBox.PackStart(new HSeparator(), false, false, 0);
						mainBox.PackStart(emphasisBox, false, false, 6);
						emphasisBox = null;
					}
					mainBox.PackStart(newWidget, false, false, 2);
				}

			}

			if (emphasisBox != null) {
				emphasisBox.PackStart(new HSeparator(), false, false, 0);
				mainBox.PackStart(emphasisBox, false, false, 5);
				emphasisBox = null;
			}

			return mainBox;

		}

		public static VBox GenerateHorizontal (object obj) {

			VBox mainBox = new VBox(false, 10) { BorderWidth = 5 };

			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());
			for (int i = 0; i < properties.Count; i++) {
				if (properties[i].GetValue(obj) == null || properties[i].GetCustomAttribute(typeof(DisplayableAttribute)) == null) {
					properties.RemoveAt(i);
					i--;
				}
			}
			properties.Sort((x, y) => ((DisplayableAttribute)x.GetCustomAttribute(typeof(DisplayableAttribute))).order.CompareTo(
				((DisplayableAttribute)y.GetCustomAttribute(typeof(DisplayableAttribute))).order));

			HBox regularBox = new HBox(false, 0);
			VBox emphasisBox = new VBox(false, 2);
			for (int i = 0; i < properties.Count; i++) {

				if (properties[i].GetCustomAttribute(typeof(VerticalOnlyAttribute)) != null) continue;

				//Load the core attributes
				DisplayableAttribute attr = (DisplayableAttribute)properties[i].GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emph = (EmphasizedAttribute)properties[i].GetCustomAttribute(typeof(EmphasizedAttribute));

				//Obtain the correct constructor
				ConstructorInfo constructor;
				if(attr is BimorphicDisplayableAttribute) {
					constructor = ((BimorphicDisplayableAttribute)attr).widget2.GetConstructor(new Type[] { typeof(PropertyInfo), typeof(object), typeof(bool), typeof(object) });
				}else {
					constructor = attr.widget.GetConstructor(new Type[] { typeof(PropertyInfo), typeof(object), typeof(bool), typeof(object) });
				}

				//Create the widget
				Widget newWidget = (Widget)constructor.Invoke(new object[] { properties[i], obj, HasAttribute(properties[i], typeof(ForceVerticalAttribute)), attr.argument });

				//Handle emphasis
				if (emph is EmphasizedIfVerticalAttribute) emph = null;
				if (emph == null) {
					if (regularBox.Children.Length > 0) regularBox.PackStart(new VSeparator(), false, false, 5);
					regularBox.PackStart(newWidget, false, false, 0);
				} else {
					emphasisBox.PackStart(newWidget, false, false, 0);
				}

			}

			mainBox.PackStart(new Gtk.Alignment(0, 0, 1, 1) { Child = regularBox, RightPadding = 5 }, false, false, 0);
			mainBox.PackStart(emphasisBox, false, false, 0);

			return mainBox;

		}

		public static bool HasAttribute (PropertyInfo property, Type attribute) {
			return property.GetCustomAttribute(attribute) != null;
		}

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
	public class ForceVerticalAttribute : Attribute {} //Force "vertical" to always be passed to the generated field.
	public class ForceHorizontalAttribute : Attribute { } //Force "horizontal" to always be passed to the generated field.
}
