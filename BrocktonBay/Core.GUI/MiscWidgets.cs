using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public class ClickableEventBox : EventBox {

		private static Widget currentMouseOver;
		private bool clickValid;
		public EventHandler<ButtonReleaseEventArgs> Clicked = delegate { };
		public EventHandler<ButtonPressEventArgs> RightClicked = delegate { };
		public EventHandler<ButtonPressEventArgs> DoubleClicked = delegate { };
		public bool active = true;
		public bool prelight = true;
		public bool depress = true;

		public ClickableEventBox() {
			CanFocus = true;
			EnterNotifyEvent += delegate (object obj, EnterNotifyEventArgs args) {
				if(!active)return;
				SetCurrentMouseOver(this);
			};
			LeaveNotifyEvent += delegate (object obj, LeaveNotifyEventArgs args) {
				if (!active) return;
				if (currentMouseOver == this) SetCurrentMouseOver(null);
				clickValid = false;
			};
			ButtonPressEvent += delegate (object obj, ButtonPressEventArgs args) {
				if (!active) return;
				if (depress) State = StateType.Active;
				if (args.Event.Button == 3) RightClicked(this, args);
				if (args.Event.Type == Gdk.EventType.TwoButtonPress) DoubleClicked(this, args);
				clickValid = true;
				GrabFocus();
				args.RetVal = true;
			};
			ButtonReleaseEvent += delegate (object obj, ButtonReleaseEventArgs args) {
				if (!active) return;
				if (prelight) {
					State = StateType.Prelight;
				}else {
					State = StateType.Normal;
				}
				if (clickValid) Clicked(this, args);
				clickValid = false;
				args.RetVal = true;
			};
		}

		public static void SetCurrentMouseOver(ClickableEventBox widget) {
			if (currentMouseOver != null)
				currentMouseOver.State = StateType.Normal;
			currentMouseOver = widget;
			if (currentMouseOver != null && widget.prelight)
				currentMouseOver.State = StateType.Prelight;
		}

	}

	public class InspectableBox : ClickableEventBox {

		public IGUIComplete inspected;
		public Menu rightclickMenu;

		public InspectableBox(IGUIComplete inspected) {
			
			this.inspected = inspected;
			rightclickMenu = new Menu();

			Clicked += delegate (object obj, ButtonReleaseEventArgs args) {
				if (args.Event.Button == 2 || (args.Event.Type == Gdk.EventType.TwoButtonPress && args.Event.Button == 1)) {
					Inspector.InspectInNewWindow(inspected, (Window)Toplevel);
				} else if (args.Event.Button == 1) {
					Inspector.InspectInNearestInspector(inspected, this);
				}
			};

			RightClicked += delegate {
				rightclickMenu.Popup();
				rightclickMenu.ShowAll();
			};

			MenuItem inspectButton = new MenuItem("Inspect");
			inspectButton.Activated += (o, args) => Inspector.InspectInNearestInspector(inspected, this);
			MenuItem inspectInWindowButton = new MenuItem("Inspect in New Window");
			inspectInWindowButton.Activated += (o, args) => Inspector.InspectInNewWindow(inspected, (Window)Toplevel);

			rightclickMenu.Append(inspectButton);
			rightclickMenu.Append(inspectInWindowButton);

			if (Game.omnipotent) {
				MenuItem deleteButton = new MenuItem("Delete");
				deleteButton.Activated += (o, args) => DependencyManager.Delete(inspected);
				rightclickMenu.Append(new SeparatorMenuItem());
				rightclickMenu.Append(deleteButton);
			}

			//Set up drag support
			MyDragDrop.SourceSet(this, inspected, inspected.GetType().ToString());

		}

		public InspectableBox(Widget child, IGUIComplete inspected) : this(inspected)
			=> Child = child;

	}

	public class Checklist : VBox {
		public object[] metadata;
		public List<CheckButton> elements;
		public Checklist(bool def, string[] names, object[] data) {
			metadata = data;
			for (int i = 0; i < names.Length; i++) PackStart(new CheckButton(names[i]) { Active = def }, false, false, 0);
			elements = new List<Widget>(Children).ConvertAll((input) => (CheckButton)input);
		}
	}

	public class ToggleMenu : Window {

		public ToggleButton toggle;

		public ToggleMenu(ToggleButton toggle) : base(WindowType.Popup) {
			Gravity = Gdk.Gravity.NorthWest;
			this.toggle = toggle;
			toggle.Toggled += Toggled;
			FocusOutEvent += (object x, FocusOutEventArgs y)
				=> toggle.Active = false;
		}

		public void Toggled(object obj, EventArgs args) {
			if (TransientFor == null) TransientFor = (Window)toggle.Toplevel;
			if (toggle.Active) {
				((Window)toggle.Toplevel).GdkWindow.GetOrigin(out int x, out int y);
				Move(toggle.Allocation.Left + x, toggle.Allocation.Bottom + y);
				ShowAll();
				GrabFocus();
			} else {
				HideAll();
			}
		}

	}

}