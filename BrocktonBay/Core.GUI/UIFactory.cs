using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public static class UIFactory {

		static readonly Type[] constructorSignature = {
			typeof(PropertyInfo),
			typeof(object),
			typeof(Context),
			typeof(object)
		};

		public static VBox GenerateVertical (object obj)
			=> GenerateVertical(new Context(MainClass.playerAgent, obj, true, false), obj);
		public static VBox GenerateHorizontal (object obj)
			=> GenerateHorizontal(new Context(MainClass.playerAgent, obj, false, false), obj);

		public static VBox GenerateVertical (Context context, object obj) {

			context = context.butVertical;

			//Load up all properties
			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());
			for (int i = 0; i < properties.Count; i++) {
				if (properties[i].GetCustomAttribute(typeof(DisplayableAttribute)) == null || properties[i].GetValue(obj) == null) {
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
				bool expand = HasAttribute(properties[i], typeof(ExpandAttribute));
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
					emphasisBox.PackStart(newWidget, expand, expand, 0);           // Pack the widget into emphasisBox
				} else { // and non-emphasis
					if (emphasisBox != null) {
						emphasisBox.PackStart(new HSeparator(), false, false, 0); // Finish off the emphasis box
						mainBox.PackStart(emphasisBox, false, false, 6);          // And pack emphasisBox into mainBox
						emphasisBox = null;                                       // Null it so we know to create a new one next time
					}
					mainBox.PackStart(newWidget, expand, expand, 2);                // Now actually pack the current widget
				}

			}

			//Pack the emphasisBox into mainBox if it's not already.
			if (emphasisBox != null) {
				emphasisBox.PackStart(new HSeparator(), false, false, 0);
				mainBox.PackStart(emphasisBox, true, true, 5);
				emphasisBox = null;
			}

			return mainBox;

		}

		public static VBox GenerateHorizontal (Context context, object obj) {

			context = context.butHorizontal;

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
				bool expand = HasAttribute(properties[i], typeof(ExpandAttribute));
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
					regularBox.PackStart(newWidget, expand, expand, 0);
				} else {
					emphasisBox.PackStart(newWidget, expand, expand, 0);
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

}
