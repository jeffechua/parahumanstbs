using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	// Why don't I use drag-failed? At the time of writing this, gtk-sharp for some reason does not emit the drag-failed
	// signal when it should. Or maybe I'm just misunderstanding everything. Anyway, there's no other signal that tells
	// me when a drag fails (no target selected). The DragResult of a drag action, which *could* tell me if a drag
	// fails, is only accessible through - guess what - DragFailedArgs from the drag-failed signal, which doesn't emit.

	public static class MyDragDrop {

		public static object currentDragged;

		public static void SourceSet (Widget widget, object data, params string[] targets) {
			Drag.SourceSet(widget, Gdk.ModifierType.Button1Mask, new List<string>(targets).ConvertAll(
				(target) => new TargetEntry(target, TargetFlags.App, 0)
			).ToArray(), Gdk.DragAction.Move);
			widget.DragBegin += (o, a) => currentDragged = data;
		}

		public static void DestSet (Widget widget, params string[] targets) {
			Drag.DestSet(widget, DestDefaults.All, new List<string>(targets).ConvertAll(
				(target) => new TargetEntry(target, TargetFlags.App, 0)
			).ToArray(), Gdk.DragAction.Move);
		}

		public static void DestSetDropAction (Widget widget, Action<object> action) {
			widget.DragDataReceived += delegate {
				action?.Invoke(currentDragged);
				currentDragged = null;
			};
		}

		public static void SourceSetSuccessAction (Widget widget, System.Action action) {
			widget.DragEnd += delegate {
				if (currentDragged == null) {
					action();
				}
			};
		}

		public static void SetFailAction (Widget widget, System.Action action) {
			widget.DragEnd += delegate {
				if (currentDragged != null) {
					action();
					currentDragged = null;
				}
			};
		}

	}
}
