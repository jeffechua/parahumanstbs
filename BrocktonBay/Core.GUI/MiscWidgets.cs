using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public enum WindowVisibility { Always, Never, WhenInteracting }

	public class ClickableEventBox : EventBox {

		private static ClickableEventBox currentMouseOver;
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
		public Context context; // So derived classes can use the more convenient "context" variable name

		public InspectableBox (Widget child, IGUIComplete inspected, Context context, bool draggable = true) : this(inspected, context, draggable)
			=> Child = child;

		public InspectableBox (IGUIComplete inspected, Context context, bool draggable = true) {
			this.inspected = inspected;
			this.context = context.butCompact;
			Clicked += OnClicked;
			MiddleClicked += OnMiddleClicked;
			DoubleClicked += OnDoubleClicked;
			RightClicked += OnRightClicked;
			if (draggable)
				MyDragDrop.SourceSet(this, inspected);
		}

		protected void OnRightClicked (object obj, ButtonPressEventArgs args) {
			Menu rightClickMenu = inspected.GetRightClickMenu(context, this);
			if (context.UIContext != null) context.UIContext.ContributeMemberRightClickMenu(inspected, rightClickMenu, context, this);
			rightClickMenu.Popup();
			rightClickMenu.ShowAll();
		}

		protected virtual void OnClicked (object obj, ButtonReleaseEventArgs args) => Inspector.InspectInNearestInspector(inspected, this);
		protected virtual void OnMiddleClicked (object obj, ButtonReleaseEventArgs args) => Inspector.InspectInNewWindow(inspected);
		protected virtual void OnDoubleClicked (object obj, ButtonPressEventArgs args) => Inspector.InspectInNewWindow(inspected);

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

	public class TogglePopup : Window {

		public ToggleButton toggle;

		public TogglePopup (ToggleButton toggle) : base(WindowType.Popup) {
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