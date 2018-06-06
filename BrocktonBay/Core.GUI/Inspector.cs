﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public class Listing : Frame, IDependable {

		public int order { get { return listed == null ? 0 : listed.order + 1; } }

		public GUIComplete listed;
		public List<IDependable> dependencies { get; set; } = new List<IDependable>();
		public List<IDependable> dependents { get; set; } = new List<IDependable>();

		public Listing(GUIComplete obj) {
			listed = obj;
			DependencyManager.Connect(obj, this);
			LabelXalign = 1;
			Reload();
		}

		public void Reload() {
			if (Child != null) Child.Destroy();
			if (LabelWidget != null) LabelWidget.Destroy();
			LabelWidget = listed.GetHeader(true);
			Add(UIFactory.GenerateHorizontal(listed));
			ShowAll();
		}

	}

	public class Cell : ClickableEventBox, IDependable {

		public int order { get { return displayed == null ? 0 : displayed.order + 1; } }

		public Frame frame;
		public GUIComplete displayed;
		public List<IDependable> dependencies { get; set; } = new List<IDependable>();
		public List<IDependable> dependents { get; set; } = new List<IDependable>();

		public Cell(GUIComplete obj) {

			//Basic setup
			displayed = obj;
			frame = new Frame();
			Child = frame;
			DependencyManager.Connect(obj, this);

			//Graphical tweak
			prelight = false;

			//Set up drag and drop
			Drag.SourceSet(this, Gdk.ModifierType.Button1Mask,
			               new TargetEntry[] { new TargetEntry(displayed.GetType().ToString(), TargetFlags.App, 0) },
						   Gdk.DragAction.Move);
			DragDataGet += (o, a) => DragTmpVars.currentDragged = obj;

			// "Removing by dragging away to nothing" functionality should be implemented manually when the Cell is created.
			// It should be implemented via DragEnd +=
			// The object should generally be removed from the parent list ONLY in this case.
			// Rationale for removing only if drag had no target:
			// - If cellObject is dragged from an aggregative list to another aggregative list,
			//   the Add() function on the second automatically removes it from the first, so calling Remove() is unnecessary.
			// - If cellObject is dragged from an associative list to an aggregative list or vice versa,
			//   We reasonably assume that user doesn't want it removed from the first list since the concept of "moving" doesn't apply in this context.
			// - Only if the user has dragged cellObject from any list to *nothing* can it be assumed that they need it manually removed by us.

			Reload();

		}

		public void Reload() {
			if (frame.Child != null) frame.Child.Destroy();
			if (frame.LabelWidget != null) frame.LabelWidget.Destroy();
			frame.LabelWidget = displayed.GetHeader(true);
			frame.Add(new Gtk.Alignment(0, 0, 1, 0) { Child = displayed.GetCell() });
			ShowAll();
		}

	}

	public class Inspector : ScrolledWindow, IDependable {

		public int order { get { return inspected == null ? 0 : inspected.order + 1; } }

		public static Inspector main;
		public GUIComplete inspected;
		public ScrolledWindow scrollbin;

		public List<IDependable> dependencies { get; set; } = new List<IDependable>();
		public List<IDependable> dependents { get; set; } = new List<IDependable>();

		public Inspector() => HscrollbarPolicy = PolicyType.Never;
		public Inspector(GUIComplete obj) : this() => Inspect(obj);

		public void Inspect(GUIComplete obj) {
			inspected = obj;
			DependencyManager.DisconnectAll(this);
			DependencyManager.Connect(inspected, this);
			if (inspected is GameObject) {
				if (((GameObject)inspected).parent != null) {
					DependencyManager.Connect(((GameObject)inspected).parent, this);
					if (((GameObject)inspected).parent.parent != null) {
						DependencyManager.Connect(((GameObject)inspected).parent.parent, this);
					}
				}
			}
			if (Child != null) Child.Destroy();
			VBox mainbox = new VBox(false, 0);
			mainbox.PackStart(obj.GetHeader(false), false, false, 10);
			mainbox.PackStart(new HSeparator(), false, false, 0);
			mainbox.PackStart(UIFactory.GenerateVertical(obj), false, false, 5);
			AddWithViewport(mainbox);
			ShowAll();
		}

		public void Reload() => Inspect(inspected);

		public static Window InspectInNewWindow(GUIComplete obj) {
			DefocusableWindow win = new DefocusableWindow();
			win.SetPosition(WindowPosition.Center);
			win.Title = "Inspector";
			win.TransientFor = (Window)main.Toplevel;
			win.TypeHint = Gdk.WindowTypeHint.Dialog;
			Inspector inspector = new Inspector(obj) { BorderWidth = 2 };
			win.Add(inspector);
			win.DeleteEvent += (o, a) => DependencyManager.DisconnectAll(inspector);
			//Gtk complains if GC hasn't gotten around to us, and obj tries to reload this.
			inspector.Realize();
			win.DefaultHeight = inspector.Child.Requisition.Height + 10;
			win.ShowAll();
			return win;
		}

		public static Inspector GetNearestInspector(Widget widget) {
			Widget container = widget.Parent;
			while (container != null && !container.IsTopLevel && !(container is Inspector)) container = container.Parent;
			if (container is Inspector) return (Inspector)container;
			return main;
		}

	}

}