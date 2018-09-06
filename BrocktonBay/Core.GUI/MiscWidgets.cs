using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public class ClickableEventBox : EventBox {

		private static Widget currentMouseOver;
		private bool clickValid;
		public EventHandler<ButtonReleaseEventArgs> Clicked = delegate { };
		public EventHandler<ButtonReleaseEventArgs> MiddleClicked = delegate { };
		public EventHandler<ButtonPressEventArgs> RightClicked = delegate { };
		public EventHandler<ButtonPressEventArgs> DoubleClicked = delegate { };
		public bool active = true;
		public bool prelight = true;
		public bool depress = true;

		public ClickableEventBox () {
			CanFocus = true;
			EnterNotifyEvent += delegate (object obj, EnterNotifyEventArgs args) {
				if (!active) return;
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
				} else {
					State = StateType.Normal;
				}
				if (clickValid && args.Event.Button == 1) Clicked(this, args);
				if (clickValid && args.Event.Button == 2) MiddleClicked(this, args);
				clickValid = false;
				args.RetVal = true;
			};
		}

		public static void SetCurrentMouseOver (ClickableEventBox widget) {
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
		public bool destructible;
		public bool draggable;

		SeparatorMenuItem deleteSeparator;
		MenuItem deleteButton;
		protected bool deletable;

		public InspectableBox (Widget child, IGUIComplete inspected, bool destructible = true, bool draggable = true) : this(inspected, destructible, draggable)
			=> Child = child;

		public InspectableBox (IGUIComplete inspected, bool destructible = true, bool draggable = true) {

			this.inspected = inspected;
			this.destructible = destructible;
			this.draggable = draggable;


			Clicked += OnClicked;
			MiddleClicked += OnMiddleClicked;
			RightClicked += OnDoubleClicked;
			RightClicked += OpenRightClickMenu;

			rightclickMenu = new Menu();

			MenuItem inspectButton = new MenuItem("Inspect");
			inspectButton.Activated += Inspect;
			rightclickMenu.Append(inspectButton);
			MenuItem inspectInWindowButton = new MenuItem("Inspect in New Window");
			inspectInWindowButton.Activated += InspectInNewWindow;
			rightclickMenu.Append(inspectInWindowButton);

			deleteSeparator = new SeparatorMenuItem();
			deleteButton = new MenuItem("Delete");
			deleteButton.Activated += Delete;
			if (Game.omnipotent && destructible)
				EnableDelete();

			//Set up drag support
			if (draggable)
				MyDragDrop.SourceSet(this, inspected);

		}

		protected void EnableDelete () {
			rightclickMenu.Append(deleteSeparator);
			rightclickMenu.Append(deleteButton);
			deletable = true;
		}

		protected void DisableDelete () {
			rightclickMenu.Remove(deleteSeparator);
			rightclickMenu.Remove(deleteButton);
			deletable = false;
		}

		public void OpenRightClickMenu (object obj, ButtonPressEventArgs args) {
			rightclickMenu.Popup();
			rightclickMenu.ShowAll();
		}

		public virtual void OnClicked (object obj, ButtonReleaseEventArgs args) {
			if (inspected == null) return;
			Inspector.InspectInNearestInspector(inspected, this);
		}

		public virtual void OnMiddleClicked (object obj, ButtonReleaseEventArgs args) {
			if (inspected == null) return;
			Inspector.InspectInNewWindow(inspected);
		}

		public virtual void OnDoubleClicked (object obj, ButtonPressEventArgs args) {
			if (inspected == null) return;
			Inspector.InspectInNewWindow(inspected);
		}

		public void Inspect (object obj, EventArgs args) {
			if (inspected == null) return;
			Inspector.InspectInNearestInspector(inspected, this);
		}
		public void InspectInNewWindow (object obj, EventArgs args) {
			if (inspected == null) return;
			Inspector.InspectInNewWindow(inspected);
		}
		public void Delete (object obj, EventArgs args) {
			if (inspected == null) return;
			DependencyManager.Delete(inspected);
		}

	}

	public class Checklist : VBox {
		public object[] metadata;
		public List<CheckButton> elements;
		public Checklist (bool def, string[] names, object[] data) {
			metadata = data;
			for (int i = 0; i < names.Length; i++) PackStart(new CheckButton(names[i]) { Active = def }, false, false, 0);
			elements = new List<Widget>(Children).ConvertAll((input) => (CheckButton)input);
		}
	}

	public class ToggleMenu : Window {

		public ToggleButton toggle;

		public ToggleMenu (ToggleButton toggle) : base(WindowType.Popup) {
			Gravity = Gdk.Gravity.NorthWest;
			this.toggle = toggle;
			toggle.Toggled += Toggled;
			FocusOutEvent += (object x, FocusOutEventArgs y)
				=> toggle.Active = false;
		}

		public void Toggled (object obj, EventArgs args) {
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