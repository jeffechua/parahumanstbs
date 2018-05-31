﻿using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core.GUI {

	public class ClickableEventBox : EventBox {

		private static Widget currentMouseOver;
		private bool clickValid;
		public EventHandler<ButtonReleaseEventArgs> Clicked = delegate { };
		public EventHandler<ButtonPressEventArgs> RightClicked = delegate { };
		public EventHandler<ButtonPressEventArgs> DoubleClicked = delegate { };
		public bool prelight = true;
		public bool depress = true;

		public ClickableEventBox() {
			CanFocus = true;
			EnterNotifyEvent += delegate (object obj, EnterNotifyEventArgs args) {
				SetCurrentMouseOver(this);
			};
			LeaveNotifyEvent += delegate (object obj, LeaveNotifyEventArgs args) {
				if (currentMouseOver == this) SetCurrentMouseOver(null);
				clickValid = false;
			};
			ButtonPressEvent += delegate (object obj, ButtonPressEventArgs args) {
				if (depress) State = StateType.Active;
				if (args.Event.Button == 3) RightClicked(this, args);
				if (args.Event.Type == Gdk.EventType.TwoButtonPress) DoubleClicked(this, args);
				clickValid = true;
				GrabFocus();
				args.RetVal = true;
			};
			ButtonReleaseEvent += delegate (object obj, ButtonReleaseEventArgs args) {
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

		public GUIComplete inspected;
		public Menu rightclickMenu;

		public InspectableBox(Widget child, GUIComplete inspectedObject) {

			Child = child;
			inspected = inspectedObject;

			Clicked += delegate (object obj, ButtonReleaseEventArgs args) {
				if (args.Event.Button == 2 || (args.Event.Type == Gdk.EventType.TwoButtonPress && args.Event.Button == 1)) {
					Inspector.InspectInNewWindow(inspected);
				} else if (args.Event.Button == 1) {
					Inspector.GetNearestInspector(this).Inspect(inspected);
				}
			};

			RightClicked += delegate {
				rightclickMenu.Popup();
				rightclickMenu.ShowAll();
			};

			rightclickMenu = new Menu();
			MenuItem inspectButton = new MenuItem("Inspect");
			inspectButton.Activated += (o, args) => Inspector.GetNearestInspector(this).Inspect(inspected);
			MenuItem inspectInWindowButton = new MenuItem("Inspect in New Window");
			inspectInWindowButton.Activated += (o, args) => Inspector.InspectInNewWindow(inspected);
			MenuItem deleteButton = new MenuItem("Delete");
			deleteButton.Activated += (o, args) => DependencyManager.Delete(inspected);

			rightclickMenu.Append(inspectButton);
			rightclickMenu.Append(inspectInWindowButton);
			rightclickMenu.Append(new SeparatorMenuItem());
			rightclickMenu.Append(deleteButton);

			//Set up drag support
			Drag.SourceSet(this, Gdk.ModifierType.Button1Mask,
						   new TargetEntry[] { new TargetEntry(inspected.GetType().ToString(), TargetFlags.App, 0) },
						   Gdk.DragAction.Move);
			DragDataGet += (o, a) => DragTmpVars.currentDragged = inspected;

		}

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

		public ToggleButton attachedToggle;

		public ToggleMenu(ToggleButton toggle) : base(WindowType.Popup) {
			Gravity = Gdk.Gravity.NorthWest;
			attachedToggle = toggle;
			attachedToggle.Toggled += Toggled;
			FocusOutEvent += (object x, FocusOutEventArgs y)
				=> attachedToggle.Active = false;
		}

		public void Toggled(object obj, EventArgs args) {
			if (TransientFor == null) TransientFor = (Window)attachedToggle.Toplevel;
			if (attachedToggle.Active) {
				((Window)attachedToggle.Toplevel).GdkWindow.GetOrigin(out int x, out int y);
				Move(attachedToggle.Allocation.Left + x, attachedToggle.Allocation.Bottom + y);
				ShowAll();
				GrabFocus();
			} else {
				HideAll();
			}
		}

	}

}