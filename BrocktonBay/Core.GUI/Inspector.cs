using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace BrocktonBay {

	public class Listing : Frame, IDependable {

		public int order { get { return obj == null ? 0 : obj.order + 1; } }
		public bool destroyed { get; set; }

		public IGUIComplete obj;
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		bool lazy;
		bool redrawQueued;

		public Listing (IGUIComplete obj, bool lazy) {
			this.obj = obj;
			this.lazy = lazy;
			DependencyManager.Connect(obj, this);
			DependencyManager.Connect(Game.UIKey, this);
			Destroyed += (o, a) => DependencyManager.DisconnectAll(this);
			SetSizeRequest(1, 300);
			LabelXalign = 1;
			Reload();
		}

		public void Reload () {
			if (obj.destroyed) {
				Destroy();
			} else {
				HideAll();
				if (lazy) {
					if (!redrawQueued) {
						redrawQueued = true;
						Graphics.SetExposeTrigger(this, Redraw);
					}
				} else {
					Redraw();
				}
			}
		}

		public void Redraw () {
			SetSizeRequest(-1, -1);
			if (Child != null) Child.Destroy();
			if (LabelWidget != null) LabelWidget.Destroy();
			Add(UIFactory.GenerateHorizontal(obj));
			LabelWidget = obj.GetHeader(new Context(Game.player, obj, false, true));
			ShowAll();
			redrawQueued = false;
		}

	}

	// In fact, the Reload() requirement of IDependable is already fulfilled in Cell. There, it is not triggered by
	// DependencyManager when flagged, but instead called once in the constructor to initialize the Cell.
	public class SmartCell : InspectableBox, IDependable {

		public int order { get { return obj == null ? 0 : obj.order + 1; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Frame frame;
		public IGUIComplete obj;
		public Context context;

		bool lazy;
		bool redrawQueued;

		public SmartCell (Context context, IGUIComplete obj, bool lazy) : base(obj) {
			//Basic setup
			this.obj = obj;
			this.lazy = lazy;
			this.context = context;
			frame = new Frame();
			Child = frame;
			prelight = false;
			MyDragDrop.SourceSet(this, obj);
			// "Removing by dragging away to nothing" functionality should be... [see Cell comment]
			DependencyManager.Connect(obj, this);
			DependencyManager.Connect(Game.UIKey, this);
			Destroyed += (o, a) => DependencyManager.DisconnectAll(this);
			Reload();
		}

		public void Reload () {
			if (obj.destroyed) {
				Destroy();
			} else {
				HideAll();
				if (lazy) {
					if (!redrawQueued) {
						redrawQueued = true;
						Graphics.SetExposeTrigger(this, Redraw);
					}
				} else {
					Redraw();
				}
			}
		}

		public void Redraw () {
			if (frame.Child != null) frame.Child.Destroy();
			if (frame.LabelWidget != null) frame.LabelWidget.Destroy();
			frame.Add(obj.GetCellContents(context));
			frame.LabelWidget = obj.GetHeader(context.butCompact);
			InspectableBox inspectableBox = frame.LabelWidget as InspectableBox;
			if (inspectableBox != null) {
				inspectableBox.rightclickMenu.Destroy();
				inspectableBox.rightclickMenu = rightclickMenu;
			}
			ShowAll();
			redrawQueued = false;
		}

	}

	public class Cell : InspectableBox {

		public Frame frame;
		public IGUIComplete obj;
		public Context context;

		public Cell (Context context, IGUIComplete obj) : base(obj) {
			//Basic setup
			this.obj = obj;
			this.context = context;
			frame = new Frame();
			Child = frame;
			prelight = false;
			MyDragDrop.SourceSet(this, obj);
			// "Removing by dragging away to nothing" functionality should be implemented manually when the Cell is created.
			// It should be implemented via MyDragDrop.SourceSetFailAction
			// The object should generally be removed from the parent list ONLY in this case.
			// Rationale for removing only if drag had no target:
			// - If cellObject is dragged from an aggregative list to another aggregative list,
			//   the Add() function on the second automatically removes it from the first, so calling Remove() is unnecessary.
			// - If cellObject is dragged from an associative list to an aggregative list or vice versa,
			//   We reasonably assume that user doesn't want it removed from the first list since the concept of "moving" doesn't apply in this context.
			// - Only if the user has dragged cellObject from any list to *nothing* can it be assumed that they need it manually removed by us.
			frame.Add(obj.GetCellContents(context));
			frame.LabelWidget = obj.GetHeader(context.butCompact);
			InspectableBox inspectableBox = frame.LabelWidget as InspectableBox;
			if (inspectableBox != null) {
				inspectableBox.rightclickMenu.Destroy();
				inspectableBox.rightclickMenu = rightclickMenu;
			}
			ShowAll();
		}

	}

	public class Inspector : ScrolledWindow, IDependable {

		public int order { get { return 10; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public IGUIComplete obj;
		public ScrolledWindow scrollbin;

		public Inspector () : this(null) { }
		public Inspector (IGUIComplete obj) {
			HscrollbarPolicy = PolicyType.Never;
			Inspect(obj);
			Shown += delegate {
				if (this.obj == null)
					Hide();
			};
		}

		public void Inspect (IGUIComplete obj) {

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
				DependencyManager.Connect(Game.UIKey, this);
				if (Child != null) Child.Destroy();
				VBox mainbox = new VBox(false, 0);
				mainbox.PackStart(obj.GetHeader(new Context(Game.player, obj, true, false)), false, false, 10);
				mainbox.PackStart(new HSeparator(), false, false, 0);
				mainbox.PackStart(UIFactory.GenerateVertical(obj), true, true, 5);
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

		public static Window InspectInNewWindow (IGUIComplete newObj) {
			DefocusableWindow win = new DefocusableWindow();
			win.SetPosition(WindowPosition.Center);
			win.Title = "Inspector";
			win.TransientFor = MainWindow.main;
			win.TypeHint = Gdk.WindowTypeHint.Utility;
			Inspector inspector = new Inspector(newObj) { BorderWidth = 2 };
			inspector.Hidden += delegate { if (inspector.obj == null) win.Destroy(); };
			win.Add(inspector);
			win.DeleteEvent += (o, a) => DependencyManager.DisconnectAll(inspector);
			//Gtk complains if GC hasn't gotten around to us, and obj tries to reload this.
			win.FocusInEvent += (o, a) => win.TransientFor = MainWindow.main;
			win.DefaultHeight = inspector.Child.SizeRequest().Height + 10;
			win.ShowAll();
			return win;
		}

		public static void InspectInNearestInspector (IGUIComplete obj, Widget referenceWidget) {
			Inspector nearestInspector = FindNearestInspector(referenceWidget);
			if (nearestInspector == null) {
				InspectInNewWindow(obj);
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
			return null;    // null -> invoker should InspectInNewWindow.
		}

	}

}