using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System;
using Gtk;

namespace Parahumans.Core.GUI {

	public class ObjectField : Gtk.Alignment {

		public PropertyInfo property;
		public GUIComplete displayed;
		public bool vertical;

		public ObjectField(PropertyInfo p, object o, bool vert, object arg) : base(0, 0, 1, 1) {
			property = p;
			displayed = (GUIComplete)property.GetValue(o);
			vertical = vert;

			if (vertical) {
				Expander expander = new Expander(TextTools.ToReadable(property.Name) + ": " + displayed.name) { Expanded = true };
				expander.Add(UIFactory.GenerateVertical(displayed));
				Add(expander);
			} else {
				HBox headerBox = new HBox(false, 0);
				headerBox.PackStart(new Label(TextTools.ToReadable(property.Name) + ": "), false, false, 0);
				headerBox.PackStart(displayed.GetHeader(true), false, false, 0);
				Add(headerBox);
			}

			ShowAll();
		}

	}

	public abstract class ObjectListField<T> : ClickableEventBox where T : GUIComplete {

		protected IContainer parent;

		public ObjectListField(PropertyInfo property, object obj, bool vertical, object arg) { //obj must be an IContainer.

			parent = (IContainer)obj;

			// Local convenience variable
			List<T> list = (List<T>)property.GetValue(obj);

			// To align contents properly.
			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 1) { TopPadding = 3, BottomPadding = 3 };
			Add(alignment);

			// Creates rightclick menu for listwide management
			Menu rightclickMenu = new Menu();

			// "Clear" button
			MenuItem clearButton = new MenuItem("Clear"); //Clears list
			clearButton.Activated += delegate {
				((IContainer)obj).RemoveRange(new List<T>(list));
				DependencyManager.TriggerAllFlags();
			};
			rightclickMenu.Append(clearButton);

			// "Add new" button
			MenuItem addNewButton = new MenuItem("Add New");
			addNewButton.Activated += delegate {
				object newObj = typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
				((IContainer)obj).AddRange(new List<object> { newObj });
				if (newObj is GameObject) City.city.Add((GameObject)newObj);
				DependencyManager.TriggerAllFlags();
			};
			rightclickMenu.Append(addNewButton);

			// "Add existing" button, but only if it's a list of GameObjects which can be searched from SelectorDialog.
			if (typeof(T).IsSubclassOf(typeof(GameObject))) {
				MenuItem addExistingButton = new MenuItem("Add Existing");
				rightclickMenu.Append(addExistingButton);
				addExistingButton.Activated += (o, a) => new SelectorDialog(
					"Select new addition to " + TextTools.ToReadable(property.Name),
					delegate (GameObject returned) {
						((IContainer)obj).AddRange(new List<object> { returned });
						DependencyManager.TriggerAllFlags();
					},
					(tested) => ((IContainer)obj).Accepts(tested) && tested is T);
			}

			// Connect rightclick menu to signal
			RightClicked += delegate {
				rightclickMenu.Popup();
				rightclickMenu.ShowAll();
			};

			// Load tooltip
			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			if (vertical) {

				Expander expander = new Expander(TextTools.ToReadable(property.Name));
				expander.Expanded = (int)arg > 0;
				Table table = new Table(0, 0, true) {
					BorderWidth = 10,
					ColumnSpacing = 10,
					RowSpacing = 10
				};

				for (int i = 0; i < list.Count; i++) {
					uint xpos = (uint)i % (uint)Math.Abs((int)arg); // The x position is given by cell index % row length
					uint ypos = (uint)i / (uint)Math.Abs((int)arg); // The y position is given by floor(index / row length)
					table.Attach(GetListElementWidget(list[i]),
								 xpos, xpos + 1, ypos, ypos + 1,
								 AttachOptions.Fill, AttachOptions.Fill, 0, 0);
				}

				expander.Add(table);
				alignment.Add(expander);

			} else {
				HBox box = new HBox(false, 5);
				Label label = new Label(TextTools.ToReadable(property.Name)) { Angle = 90 };
				box.PackStart(label, false, false, 2);
				for (int i = 0; i < list.Count; i++)
					box.PackStart(GetListElementWidget(list[i]), false, false, 0);
				alignment.Add(box);
			}

			//Set up drag support
			Drag.DestSet(this, DestDefaults.All,
						 new TargetEntry[] { new TargetEntry(typeof(T).ToString(), TargetFlags.App, 0) },
						 Gdk.DragAction.Move);
			DragDataReceived += AttemptDrag;
		}

		void AttemptDrag(object obj, DragDataReceivedArgs args) {
			if (parent.Accepts(DragTmpVars.currentDragged)) {
				parent.AddRange(new List<object> { DragTmpVars.currentDragged });
				DependencyManager.TriggerAllFlags();
			}
		}

		protected abstract Widget GetListElementWidget(T widget);

	}

	public class CellObjectListField<T> : ObjectListField<T> where T : GUIComplete {

		public CellObjectListField(PropertyInfo property, object obj, bool vertical, object arg)
			: base(property, obj, vertical, arg) { }

		protected override Widget GetListElementWidget(T cellObject) {

			Cell cellWidget = new Cell(cellObject);
			InspectableBox cellLabel = (InspectableBox)cellWidget.LabelWidget;

			MenuItem moveButton = new MenuItem("Move");
			moveButton.Activated += (o, a)
				=> new SelectorDialog("Select new parent for " + cellObject.name,
									  delegate (GameObject returned) {
										  returned.Add(cellObject);
										  DependencyManager.TriggerAllFlags();
									  },
									  (tested) => tested.Accepts(cellObject));

			MenuItem removeButton = new MenuItem("Remove");
			removeButton.Activated += delegate {
				parent.RemoveRange(new List<object> { cellObject });
				DependencyManager.TriggerAllFlags();
			};

			cellLabel.rightclickMenu.Append(new SeparatorMenuItem());
			cellLabel.rightclickMenu.Append(moveButton);
			cellLabel.rightclickMenu.Append(removeButton);
			cellLabel.DragEnd += delegate {
				parent.RemoveRange(new List<T> { cellObject });
				DependencyManager.TriggerAllFlags();
			};

			ClickableEventBox clickableEventBox = new ClickableEventBox {
				Child = cellWidget,
				prelight = false
			};

			Drag.SourceSet(clickableEventBox, Gdk.ModifierType.Button1Mask,
						   new TargetEntry[] { new TargetEntry(typeof(T).ToString(), TargetFlags.App, 0) },
						   Gdk.DragAction.Move);
			clickableEventBox.DragDataGet += (o, a) => DragTmpVars.currentDragged = cellObject;
			clickableEventBox.DragEnd += delegate {
				parent.RemoveRange(new List<T> { cellObject });
				DependencyManager.TriggerAllFlags();
			};
			// Rationale for removing only if drag had no target
			// - If cellObject is dragged from an aggregative list to another aggregative list,
			//   the Add() function on the second automatically removes it from the first, so calling Remove() is unnecessary.
			// - If cellObject is dragged from an associative list to an aggregative list or vice versa,
			//   We reasonably assume that user doesn't want it removed from the first list since the concept of "moving" doesn't apply in this context.
			// - Only if the user has dragged cellObject from any list to *nothing* can it be assumed that they need it manually removed by us.
			return clickableEventBox;
		}

	}
}