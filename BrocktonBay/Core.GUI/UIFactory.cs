﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace BrocktonBay {

	public static class UIFactory {

		public static readonly Type[] constructorSignature = {
			typeof(PropertyInfo),
			typeof(object),
			typeof(Context),
			typeof(DisplayableAttribute)
		};

		public static VBox GenerateVertical (IGUIComplete obj)
			=> GenerateVertical(new Context(obj, Game.player, true, false), obj);
		public static VBox GenerateHorizontal (IGUIComplete obj)
			=> GenerateHorizontal(new Context(obj, Game.player, false, false), obj);

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
			List<Tuple<PropertyInfo, DisplayableAttribute>> pairs = properties.ConvertAll(
				(property) => new Tuple<PropertyInfo, DisplayableAttribute>(property, (DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute))));
			pairs.RemoveAll((pair) => pair.Item2 == null || !pair.Item2.generate || pair.Item2.horizontalOnly || !pair.Item2.ViewAuthorized(obj));
			pairs.Sort((x, y) => x.Item2.order.CompareTo(y.Item2.order));

			//Create boxes
			VBox mainBox = new VBox(false, 0) { BorderWidth = 5 };
			VBox emphasisBox = null;

			//Draw each property
			foreach (Tuple<PropertyInfo, DisplayableAttribute> pair in pairs) {

				PropertyInfo property = pair.Item1;
				DisplayableAttribute attribute = pair.Item2;

				//Construct the widget
				ConstructorInfo constructor = attribute.widget.GetConstructor(constructorSignature);
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					pair.Item1,
					obj,
					attribute.forceHorizontal ? context.butHorizontal : context,
					attribute
				});

				//Manage padding
				if (attribute.topPadding != 0 || attribute.bottomPadding != 0 || attribute.leftPadding != 0 || attribute.rightPadding != 0) {
					newWidget = new Gtk.Alignment(0, 0, 1, 1) {
						Child = newWidget,
						TopPadding = attribute.topPadding,
						BottomPadding = attribute.bottomPadding,
						LeftPadding = attribute.leftPadding,
						RightPadding = attribute.rightPadding
					};
				}

				if (!attribute.fillSides) newWidget = Align(newWidget, 0.5f, 0, 0, 1);

				//Manage emphasis
				if (attribute.emphasized || attribute.emphasizedIfVertical) {
					if (emphasisBox == null)                                     // If no emphasisBox at the moment,
						emphasisBox = new VBox(false, 5);                        // make one.
					emphasisBox.PackStart(new HSeparator(), false, false, 0);    // Install a delimiter
					emphasisBox.PackStart(newWidget, attribute.expand, attribute.expand, 0);           // Pack the widget into emphasisBox
				} else { // and non-emphasis
					if (emphasisBox != null) {
						emphasisBox.PackStart(new HSeparator(), false, false, 0); // Finish off the emphasis box
						mainBox.PackStart(emphasisBox, false, false, 6);          // And pack emphasisBox into mainBox
						emphasisBox = null;                                       // Null it so we know to create a new one next time
					}
					mainBox.PackStart(newWidget, attribute.expand, attribute.expand, 2);                // Now actually pack the current widget
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
			List<Tuple<PropertyInfo, DisplayableAttribute>> pairs = properties.ConvertAll(
				(property) => new Tuple<PropertyInfo, DisplayableAttribute>(property, (DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute))));
			pairs.RemoveAll((pair) => pair.Item2 == null || !pair.Item2.generate || pair.Item2.verticalOnly || !pair.Item2.ViewAuthorized(obj));
			pairs.Sort((x, y) => x.Item2.order.CompareTo(y.Item2.order));

			//Initialize boxes
			VBox mainBox = new VBox(false, 10) { BorderWidth = 5 };
			HBox regularBox = new HBox(false, 0);
			VBox emphasisBox = new VBox(false, 2);

			foreach (Tuple<PropertyInfo, DisplayableAttribute> pair in pairs) {

				PropertyInfo property = pair.Item1;
				DisplayableAttribute attribute = pair.Item2;

				//Obtain the correct constructor
				ConstructorInfo constructor = (attribute.altWidget ?? attribute.widget).GetConstructor(constructorSignature);

				//Construct the widget
				Widget newWidget = (Widget)constructor.Invoke(new object[] {
					property,
					obj,
					attribute.forceVertical?context.butVertical:context,
					attribute
				});

				if (!attribute.fillSides) newWidget = Align(newWidget, 0, 0.5f, 1, 0);

				//Pack into the correcumentt box (emphasisBox/regularBox)
				if (attribute.emphasized || attribute.emphasizedIfHorizontal) {
					emphasisBox.PackStart(newWidget, attribute.expand, attribute.expand, 0);
				} else {
					if (regularBox.Children.Length > 0) regularBox.PackStart(new VSeparator(), false, false, 5);
					regularBox.PackStart(newWidget, attribute.expand, attribute.expand, 0);
				}

			}

			//Pack emphasisBox and regularBox into mainBox
			mainBox.PackStart(new Gtk.Alignment(0, 0, 1, 1) { Child = regularBox, RightPadding = 5 }, false, false, 0);
			mainBox.PackStart(emphasisBox, false, false, 0);

			return mainBox;

		}

		public static Widget Fabricate (object obj, string propertyName, Context context) {
			PropertyInfo property = obj.GetType().GetProperty(propertyName);
			DisplayableAttribute attribute = (DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute));
			return (Widget)attribute.widget.GetConstructor(constructorSignature)
									.Invoke(new object[] { property, obj, context, attribute });
		}

		public static Gtk.Alignment Align (Widget widget, float xalign, float yalign, float xscale, float yscale)
			=> new Gtk.Alignment(xalign, yalign, xscale, yscale) { Child = widget };

		public static Label Align (Label label, float xalign, float yalign) {
			label.SetAlignment(xalign, yalign);
			return label;
		}

		public static bool HasAttribute (PropertyInfo property, Type attribute)
			=> property.GetCustomAttribute(attribute) != null;

		public static bool EditAuthorized (object obj, string property)
			=> EditAuthorized(obj.GetType().GetProperty(property), obj);

		public static bool EditAuthorized (PropertyInfo property, object obj)
			=> ((DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute))).EditAuthorized(obj);

		public static bool ViewAuthorized (object obj, string property)
			=> ViewAuthorized(obj.GetType().GetProperty(property), obj);

		public static bool ViewAuthorized (PropertyInfo property, object obj)
			=> ((DisplayableAttribute)property.GetCustomAttribute(typeof(DisplayableAttribute))).ViewAuthorized(obj);

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

	public static class MenuFactory {

		public static MenuItem CreateInspectButton (IGUIComplete obj, Widget referenceWidget) {
			MenuItem inspectButton = new MenuItem("Inspect");
			inspectButton.Activated += (o, a) => Inspector.InspectInNearestInspector(obj, referenceWidget);
			return inspectButton;
		}

		public static MenuItem CreateInspectInNewWindowButton (IGUIComplete obj) {
			MenuItem inspectInWindowButton = new MenuItem("Inspect in New Window");
			inspectInWindowButton.Activated += (o, a) => Inspector.InspectInNewWindow(obj);
			return inspectInWindowButton;
		}

		public static MenuItem CreateDeleteButton (IDependable obj) {
			MenuItem deleteButton = new MenuItem("Delete");
			deleteButton.Activated += delegate {
				DependencyManager.Destroy(obj);
				DependencyManager.TriggerAllFlags();
			};
			return deleteButton;
		}

		public static MenuItem CreateMoveButton (IGUIComplete child) {
			MenuItem moveButton = new MenuItem("Move");
			moveButton.Activated += (o, a)
				=> new SelectorDialog("Select new parent for " + child.name,
									  (tested) => tested.Accepts(child),
									  delegate (GameObject returned) {
										  returned.Add(child);
				DependencyManager.TriggerAllFlags();
									  });
			return moveButton;
		}

		public static MenuItem CreateRemoveButton (IContainer container, object child) {
			MenuItem removeButton = new MenuItem("Remove");
			removeButton.Activated += delegate {
				container.Remove(child);
				DependencyManager.TriggerAllFlags();
			};
			return removeButton;
		}

		public static MenuItem CreateActionButton (GameAction action, Context context) {
			MenuItem actionButton = new MenuItem(action.name);
			actionButton.Activated += (o, a) => action.action(context);
			actionButton.Sensitive = action.condition(context);
			return actionButton;
		}

	}

}