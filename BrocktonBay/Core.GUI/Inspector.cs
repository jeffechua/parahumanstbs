using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public class Listing : Frame, IDependable {

		public int order { get { return obj == null ? 0 : obj.order + 1; } }
		public bool destroyed { get; set; }

		public GUIComplete obj;
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Listing (GUIComplete obj) {
			this.obj = obj;
			DependencyManager.Connect(obj, this);
			LabelXalign = 1;
			Reload();
		}

		public void Reload () {
			if (Child != null) Child.Destroy();
			if (LabelWidget != null) LabelWidget.Destroy();
			LabelWidget = obj.GetHeader(new Context(obj, 0, false, true));
			Add(UIFactory.GenerateHorizontal(obj));
			ShowAll();
		}

	}

	// In fact, the Reload() requirement of IDependable is already fulfilled in Cell. There, it is not triggered by
	// DependencyManager when flagged, but instead called once in the constructor to initialize the Cell.
	public class SmartCell : Cell, IDependable {

		public int order { get { return obj == null ? 0 : obj.order + 1; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public SmartCell (Context context, GUIComplete obj) : base(context, obj) {
			DependencyManager.Connect(obj, this);
			Destroyed += (o, a) => DependencyManager.DisconnectAll(this);
		}

	}

	public class Cell : ClickableEventBox {

		public Frame frame;
		public GUIComplete obj;
		public Context context;

		public Cell (Context context, GUIComplete obj) {

			//Basic setup
			this.obj = obj;
			frame = new Frame();
			Child = frame;

			//Graphical tweak
			prelight = false;

			//Set up drag and drop
			Drag.SourceSet(this, Gdk.ModifierType.Button1Mask,
						   new TargetEntry[] { new TargetEntry(obj.GetType().ToString(), TargetFlags.App, 0) },
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

		public void Reload () {
			if (frame.Child != null) frame.Child.Destroy();
			if (frame.LabelWidget != null) frame.LabelWidget.Destroy();
			frame.LabelWidget = obj.GetHeader(context.butCompact);
			frame.Add(new Gtk.Alignment(0, 0, 1, 0) { Child = obj.GetCell(context) });
			ShowAll();
		}

	}

	public class Inspector : ScrolledWindow, IDependable {

		public int order { get { return 10; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public GUIComplete obj;
		public ScrolledWindow scrollbin;

		public Inspector () : this(null) { }
		public Inspector (GUIComplete obj) {
			HscrollbarPolicy = PolicyType.Never;
			Inspect(obj);
			Shown += delegate {
				if (this.obj == null)
					Hide();
			};
		}

		public void Inspect (GUIComplete obj) {

			// Clean up prior attachments
			if (Child != null) Child.Destroy();
			DependencyManager.DisconnectAll(this);
			ShowAll();

			// Handle inspection request
			this.obj = obj;
			if (obj == null) {
				Hide();
			} else {
				DependencyManager.Connect(obj, this);
				if (Child != null) Child.Destroy();
				VBox mainbox = new VBox(false, 0);
				mainbox.PackStart(obj.GetHeader(new Context(obj, 0, true, false)), false, false, 10);
				mainbox.PackStart(new HSeparator(), false, false, 0);
				mainbox.PackStart(UIFactory.GenerateVertical(obj), false, false, 5);
				AddWithViewport(mainbox);
				ShowAll();
			}
		}

		public void Reload () {
			if (obj.destroyed) {
				Inspect(null);
			} else {
				Inspect(obj);
			}
		}

		public static Window InspectInNewWindow (GUIComplete newObj, Window transientFor) {
			DefocusableWindow win = new DefocusableWindow();
			win.SetPosition(WindowPosition.Center);
			win.Title = "Inspector";
			win.TransientFor = transientFor;
			win.TypeHint = Gdk.WindowTypeHint.Utility;
			Inspector inspector = new Inspector(newObj) { BorderWidth = 2 };
			inspector.Hidden += delegate { if (inspector.obj == null) win.Destroy(); };
			win.Add(inspector);
			win.DeleteEvent += (o, a) => DependencyManager.DisconnectAll(inspector);
			//Gtk complains if GC hasn't gotten around to us, and obj tries to reload this.
			win.FocusInEvent += (o, a) => win.TransientFor = transientFor;
			inspector.Realize();
			win.DefaultHeight = inspector.Child.Requisition.Height + 10;
			win.ShowAll();
			return win;
		}

		public static void InspectInNearestInspector (GUIComplete obj, Widget referenceWidget) {
			Inspector nearestInspector = FindNearestInspector(referenceWidget);
			if (nearestInspector == null) {
				InspectInNewWindow(obj, (Window)referenceWidget.Toplevel);
			} else {
				nearestInspector.Inspect(obj);
			}
		}

		private static Inspector FindNearestInspector (Widget referenceWidget) {
			Widget container = referenceWidget;
			while (!container.IsTopLevel && !(container is Inspector)) container = container.Parent;
			if (container is Inspector)
				return (Inspector)container;
			if (container is DefocusableWindowWithInspector)
				if (((DefocusableWindowWithInspector)container).inspectorEnabled)
					return ((DefocusableWindowWithInspector)container).inspector;
			return null;    // Null -> invoker should InspectInNewWindow. I could just return main, but this is clearer.
		}

	}

}