using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public static class UIFactory {

		public static readonly Type[] constructorSignature = {
			typeof(PropertyInfo),
			typeof(object),
			typeof(Context),
			typeof(object)
		};

		public static VBox GenerateVertical (object obj)
			=> GenerateVertical(new Context(Game.player, obj, true, false), obj);
		public static VBox GenerateHorizontal (object obj)
			=> GenerateHorizontal(new Context(Game.player, obj, false, false), obj);

		public static VBox Generate (Context context, object obj) {
			if (context.vertical) {
				return GenerateVertical(context, obj);
			} else {
				return GenerateHorizontal(context, obj);
			}
		}

		public static VBox GenerateVertical (Context context, object obj) {

			context = context.butVertical;

			//Load up all properties
			List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType().GetProperties());

			for (int i = 0; i < properties.Count; i++) {
				if (!HasAttribute(properties[i], typeof(DisplayableAttribute)) ||
					HasAttribute(properties[i], typeof(ChildAttribute)) ||
					properties[i].GetValue(obj) == null) {
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
			foreach (PropertyInfo property in properties) {

				if (!Game.omniscient && HasAttribute(property, typeof(PlayerInvisibleAttribute))) continue;

				//Load up attributes
				DisplayableAttribute attr = (DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emphAttribute = (EmphasizedAttribute)property.GetCustomAttribute(typeof(EmphasizedAttribute));
				PaddedAttribute padded = (PaddedAttribute)property.GetCustomAttribute(typeof(PaddedAttribute));
				bool expand = HasAttribute(property, typeof(ExpandAttribute));
				bool forceHorizontal = HasAttribute(property, typeof(ForceHorizontalAttribute));

				//Construct the widget
				ConstructorInfo constructor = attr.widget.GetConstructor(constructorSignature);
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					property,
					obj,
					forceHorizontal ? context.butHorizontal : context,
					attr.arg
				});

				//Manage padumentding
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
				if (!HasAttribute(properties[i], typeof(DisplayableAttribute)) ||
					HasAttribute(properties[i], typeof(ChildAttribute)) ||
					properties[i].GetValue(obj) == null) {
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

			foreach (PropertyInfo property in properties) {

				if (!Game.omniscient && HasAttribute(property, typeof(PlayerInvisibleAttribute))) continue;
				if (property.GetCustomAttribute(typeof(VerticalOnlyAttribute)) != null) continue;

				//Load attributes
				DisplayableAttribute attr = (DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute));
				EmphasizedAttribute emph = (EmphasizedAttribute)property.GetCustomAttribute(typeof(EmphasizedAttribute));
				bool expand = HasAttribute(property, typeof(ExpandAttribute));
				bool forceVertical = HasAttribute(property, typeof(ForceVerticalAttribute));

				//Obtain the correct constructor
				ConstructorInfo constructor;
				if (attr is BimorphicDisplayableAttribute) {
					constructor = ((BimorphicDisplayableAttribute)attr).widget2.GetConstructor(constructorSignature);
				} else {
					constructor = attr.widget.GetConstructor(constructorSignature);
				}

				//Construct the widget
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					property,
					obj,
					forceVertical?context.butVertical:context,
					attr.arg
				});

				//Pack into the correcumentt box (emphasisBox/regularBox)
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

		public static Gtk.Alignment Align (Widget widget, float xalign, float yalign, float xscale, float yscale)
			=> new Gtk.Alignment(xalign, yalign, xscale, yscale) { Child = widget };

		public static bool HasAttribute (PropertyInfo property, Type attribute)
			=> property.GetCustomAttribute(attribute) != null;

		public static bool CurrentlyEditable (PropertyInfo property, object obj) {
			if (Game.omnipotent) return true;
			if (obj is IAffiliated && ((IAffiliated)obj).affiliation != Game.player) return false;
			PlayerEditableAttribute editableAttribute = (PlayerEditableAttribute)property.GetCustomAttribute(typeof(PlayerEditableAttribute));
			if (editableAttribute == null) return false;
			return editableAttribute.currentlyEditable;
		}

		public static string ToReadable (string str) {
			string newStr = str[0].ToString().ToUpper();
			for (int i = 1; i < str.Length; i++) {
				if (str[i] == '_') {
					newStr += " ";
					newStr += str[i + 1].ToString().ToUpper();
					i++;
				} else {
					newStr += str[i];
				}
			}
			return newStr;
		}

	}
}