using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System;
using Gtk;
using Gdk;

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
	public abstract class TabularListField<T> : EventBox where T : IGUIComplete {

		protected IContainer parent;
		protected Context context;

		Menu rightclickMenu;

		public TabularListField (PropertyInfo property, object obj, Context context, object arg) { //obj must be an IContainer.

			parent = (IContainer)obj;
			this.context = context;

			// Local convenience variable
			List<T> list = (List<T>)property.GetValue(obj);

			// To align contents properly.
			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 1) { TopPadding = 3, BottomPadding = 3 };
			Add(alignment);

			// Creates rightclick menu for listwide management
			rightclickMenu = new Menu();

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
					(Gtk.Window)Toplevel, "Select new addition to " + TextTools.ToReadable(property.Name),
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

				int columns = (int)arg;
				if (columns < 0) columns *= -1;
				DynamicTable table = new DynamicTable(list.ConvertAll((element) => GetElementWidget(element)), (uint)columns);

				if (context.compact) {
					EventBox eventBox = new EventBox { Child = table };
					alignment.Add(eventBox);
					eventBox.ButtonPressEvent += ListPressed; //Set up right-click menu
					MyDragDrop.DestSet(eventBox, typeof(T).ToString()); //Set up
					MyDragDrop.DestSetDropAction(eventBox, AttemptDrag);//drag support
				} else {
					Expander expander = new Expander(TextTools.ToReadable(property.Name));
					expander.Expanded = (int)arg > 0;
					expander.Add(table);
					alignment.Add(expander);
					expander.ButtonPressEvent += ListPressed; //Set up right-click menu
					MyDragDrop.DestSet(expander, typeof(T).ToString()); //Set up
					MyDragDrop.DestSetDropAction(expander, AttemptDrag);//drag support
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
					box.PackStart(GetElementWidget(list[i]), false, false, 0);
				alignment.Add(box);

				//Set up drag support
				MyDragDrop.DestSet(this, typeof(T).ToString());
				MyDragDrop.DestSetDropAction(this, AttemptDrag);

			}

		}

		void ListPressed (object widget, ButtonPressEventArgs args) {
			if (args.Event.Button == 3) {
				rightclickMenu.Popup();
				rightclickMenu.ShowAll();
			}
		}

		void AttemptDrag (object data) {
			if (parent.Accepts(data)) {
				parent.Add(data);
				DependencyManager.TriggerAllFlags();
			}
		}

		protected abstract Widget GetElementWidget (T obj);

	}

	public class DynamicTable : Gtk.Alignment {

		protected List<Widget> cells;
		Table table;

		int cellWidth;
		int columns;
		int minColumns;

		const int SPACING = 10;

		public DynamicTable (List<Widget> cells, uint minColumns) : base(0, 0, 0, 0) {
			table = new Table(1, minColumns, true) {
				RowSpacing = SPACING,
				ColumnSpacing = SPACING,
				BorderWidth = SPACING / 2
			};
			Add(table);
			this.minColumns = (int)minColumns;
			columns = (int)minColumns;
			this.cells = cells;
			Arrange();
		}

		protected override void OnSizeRequested (ref Requisition requisition) {
			if (cellWidth == 0) {
				foreach (Widget cell in cells) {
					Requisition cellRequisition = cell.SizeRequest();
					if (cellRequisition.Width > cellWidth) cellWidth = cellRequisition.Width;
				}
				cellWidth += SPACING;
			}
			base.OnSizeRequested(ref requisition);
			requisition.Width = minColumns * cellWidth;
		}

		protected override void OnSizeAllocated (Rectangle allocation) {
			if (cells.Count == 0) return;
			int currentColumns = allocation.Width / cellWidth;
			if (currentColumns < minColumns) currentColumns = 2;
			if (columns != currentColumns) {
				columns = currentColumns;
				Arrange();
				table.NColumns = (uint)columns;
				table.NRows = (uint)(cells.Count - 1) / table.NColumns + 1;
				Toplevel.ShowAll();
			} else {
				base.OnSizeAllocated(allocation);
			}
		}

		void Arrange () {
			for (int i = 0; i < cells.Count; i++) {
				if (cells[i].Parent == table) table.Remove(cells[i]);
				uint x = (uint)(i % columns);
				uint y = (uint)(i / columns);
				table.Attach(cells[i], x, x + 1, y, y + 1);
			}
		}

	}

	public class CellTabularListField<T> : TabularListField<T> where T : IGUIComplete {

		public CellTabularListField (PropertyInfo property, object obj, Context context, object arg)
			: base(property, obj, context, arg) { }

		protected override Widget GetElementWidget (T obj) {

			// Set up the actual widget.

			Cell cell;
			cell = new Cell(context, obj);
			InspectableBox cellLabel = (InspectableBox)cell.frame.LabelWidget;

			//Set up menu

			MenuItem moveButton = new MenuItem("Move");
			moveButton.Activated += (o, a)
				=> new SelectorDialog((Gtk.Window)Toplevel, "Select new parent for " + obj.name,
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

			MyDragDrop.SetFailAction(cellLabel, delegate {
				parent.Remove(obj);
				DependencyManager.TriggerAllFlags();
			});

			MyDragDrop.SetFailAction(cell, delegate {
				parent.Remove(obj);
				DependencyManager.TriggerAllFlags();
			});

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