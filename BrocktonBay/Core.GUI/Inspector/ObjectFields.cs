using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System;
using Gtk;

namespace Parahumans.Core {

	public class ObjectField : Gtk.Alignment, IDependable {

		public int order { get { return obj.order + 1; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public PropertyInfo property;
		public IGUIComplete obj;
		public Context context;

		public ObjectField (PropertyInfo property, object obj, Context context, object arg) : base(0, 0, 1, 1) {

			this.property = property;
			this.obj = (IGUIComplete)property.GetValue(obj);
			this.context = context;

			DependencyManager.Connect(this.obj, this);
			Destroyed += (o, a) => DependencyManager.DisconnectAll(this);

			Reload();

		}

		public void Reload () {
			if (Child != null) Child.Destroy();
			if (context.vertical) {
				Expander expander = new Expander(TextTools.ToReadable(property.Name) + ": " + obj.name) { Expanded = true };
				expander.Add(UIFactory.GenerateVertical(obj));
				Add(expander);
			} else {
				HBox headerBox = new HBox(false, 0);
				headerBox.PackStart(new Label(TextTools.ToReadable(property.Name) + ": "), false, false, 0);
				headerBox.PackStart(obj.GetHeader(context.butCompact), false, false, 0);
				Add(headerBox);
			}
			ShowAll();
		}

	}

	// the absolute value of (int)arg = number of columns in table if used
	// arg>0 => expander starts expanded, <0 => starts collapsed
	public abstract class ObjectListField<T> : EventBox where T : IGUIComplete {

		protected IContainer parent;
		protected Context context;

		public ObjectListField (PropertyInfo property, object obj, Context context, object arg) { //obj must be an IContainer.

			parent = (IContainer)obj;
			this.context = context;

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
				((IContainer)obj).Add(newObj);
				if (newObj is GameObject) MainClass.city.Add((GameObject)newObj);
				DependencyManager.TriggerAllFlags();
			};
			rightclickMenu.Append(addNewButton);

			// "Add existing" button, but only if it's a list of GameObjects which can be searched from SelectorDialog.
			if (typeof(T).IsSubclassOf(typeof(GameObject))) {
				MenuItem addExistingButton = new MenuItem("Add Existing");
				rightclickMenu.Append(addExistingButton);
				addExistingButton.Activated += (o, a) => new SelectorDialog(
					(Window)Toplevel, "Select new addition to " + TextTools.ToReadable(property.Name),
					(tested) => ((IContainer)obj).Accepts(tested) && tested is T,
					delegate (GameObject returned) {
						((IContainer)obj).Add(returned);
						DependencyManager.TriggerAllFlags();
					});
			}

			// Load tooltip (if exists)
			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			if (context.vertical) {

				Table table = new Table(0, 0, true) {
					BorderWidth = (uint)(context.compact?0:10),
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

				if (context.compact) {
					
					EventBox eventBox = new EventBox { Child = table };
					alignment.Add(eventBox);

					//Set up right-click menu
					eventBox.ButtonPressEvent += delegate (object widget, ButtonPressEventArgs args) {
						if (args.Event.Button == 3) {
							rightclickMenu.Popup();
							rightclickMenu.ShowAll();
						}
					};

					//Set up drag support
					Drag.DestSet(eventBox, DestDefaults.All,
								 new TargetEntry[] { new TargetEntry(typeof(T).ToString(), TargetFlags.App, 0) },
								 Gdk.DragAction.Move);
					eventBox.DragDataReceived += AttemptDrag;

				} else {
					
					Expander expander = new Expander(TextTools.ToReadable(property.Name));
					expander.Expanded = (int)arg > 0;
					expander.Add(table);
					alignment.Add(expander);

					//Set up right-click menu
					expander.ButtonPressEvent += delegate (object widget, ButtonPressEventArgs args) {
						if (args.Event.Button == 3) {
							rightclickMenu.Popup();
							rightclickMenu.ShowAll();
						}
					};

					//Set up drag support
					Drag.DestSet(expander, DestDefaults.All,
								 new TargetEntry[] { new TargetEntry(typeof(T).ToString(), TargetFlags.App, 0) },
								 Gdk.DragAction.Move);
					expander.DragDataReceived += AttemptDrag;
				}


			} else {

				HBox box = new HBox(false, 5);
				Label label = new Label(TextTools.ToReadable(property.Name)) { Angle = 90 };
				ClickableEventBox labelEventBox = new ClickableEventBox { Child = label };
				labelEventBox.RightClicked += delegate {
					rightclickMenu.Popup();
					rightclickMenu.ShowAll();
				};
				box.PackStart(labelEventBox, false, false, 2);
				for (int i = 0; i < list.Count; i++)
					box.PackStart(GetListElementWidget(list[i]), false, false, 0);
				alignment.Add(box);

				//Set up drag support
				Drag.DestSet(this, DestDefaults.All,
							 new TargetEntry[] { new TargetEntry(typeof(T).ToString(), TargetFlags.App, 0) },
							 Gdk.DragAction.Move);
				DragDataReceived += AttemptDrag;

			}

		}

		void AttemptDrag (object obj, DragDataReceivedArgs args) {
			if (parent.Accepts(DragTmpVars.currentDragged)) {
				parent.Add(DragTmpVars.currentDragged);
				DependencyManager.TriggerAllFlags();
			}
		}

		protected abstract Widget GetListElementWidget (T obj);

	}

	public class CellObjectListField<T> : ObjectListField<T> where T : IGUIComplete {

		public CellObjectListField (PropertyInfo property, object obj, Context context, object arg)
			: base(property, obj, context, arg) { }

		protected override Widget GetListElementWidget (T obj) {

			// Set up the actual widget.

			Cell cell;
			cell = new Cell(context, obj);
			InspectableBox cellLabel = (InspectableBox)cell.frame.LabelWidget;

			//Set up menu

			MenuItem moveButton = new MenuItem("Move");
			moveButton.Activated += (o, a)
				=> new SelectorDialog((Window)Toplevel, "Select new parent for " + obj.name,
									  (tested) => tested.Accepts(obj),
									  delegate (GameObject returned) {
										  returned.Add(obj);
										  DependencyManager.TriggerAllFlags();
									  });

			MenuItem removeButton = new MenuItem("Remove");
			removeButton.Activated += delegate {
				parent.Remove(obj);
				DependencyManager.TriggerAllFlags();
			};

			cellLabel.rightclickMenu.Append(new SeparatorMenuItem());
			cellLabel.rightclickMenu.Append(moveButton);
			cellLabel.rightclickMenu.Append(removeButton);

			//Set up drag/drop

			cellLabel.DragEnd += delegate {
				Console.WriteLine("DragEnd");
				parent.Remove(obj);
				DependencyManager.TriggerAllFlags();
			};

			cell.DragEnd += delegate {
				Console.WriteLine("DragEnd");
				parent.Remove(obj);
				DependencyManager.TriggerAllFlags();
			};

			// Rationale for removing only if drag had no target
			// - If cellObject is dragged from an aggregative list to another aggregative list,
			//   the Add() function on the second automatically removes it from the first, so calling Remove() is unnecessary.
			// - If cellObject is dragged from an associative list to an aggregative list or vice versa,
			//   We reasonably assume that user doesn't want it removed from the first list since the concept of "moving" doesn't apply in this context.
			// - Only if the user has dragged cellObject from any list to *nothing* can it be assumed that they need it manually removed by us.

			return cell;

		}

	}
}