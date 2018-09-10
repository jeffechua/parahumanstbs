using System;
using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public interface MapMarked {
		IMapMarker[] GetMarkers (Map map);
	}

	public interface IMapMarker {
		int layer { get; }
		bool magnifRedraw { get; }
		void Repin ();
		void Redraw ();
	}

	public abstract class NonInteractiveMapMarker : Gtk.Alignment, IMapMarker, IDependable {

		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;
		protected Vector2 scaledPosition;

		public abstract int layer { get; }
		protected abstract Vector2 position { get; }
		protected abstract Vector2 offset { get; }
		public abstract bool magnifRedraw { get; }

		public NonInteractiveMapMarker (Map map) : base(0, 0, 1, 1) {
			this.map = map;
		}

		public abstract void Reload ();
		public abstract void OnTriggerDestroyed (IDependable trigger);
		public abstract void OnListenerDestroyed (IDependable listener);
		public abstract void Redraw ();
		public void Repin () {
			if (Parent == map.stage) map.stage.Remove(this);
			scaledPosition = position * map.currentMagnif;
			Vector2 markerCoords = scaledPosition + offset;
			map.stage.Put(this, (int)markerCoords.x, (int)markerCoords.y);
		}

	}

	public abstract class InspectableMapMarker : InspectableBox, IMapMarker, IDependable {

		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;
		protected Vector2 scaledPosition;
		Window leftPopup;
		Window rightPopup;
		HSeparator rline;
		HSeparator lline;
		Image node;

		public abstract int layer { get; }
		protected abstract Vector2 position { get; }
		protected abstract Vector2 offset { get; }
		protected abstract Vector2 lineOffset { get; }
		public abstract bool magnifRedraw { get; }
		protected abstract int popupDistance { get; }

		public InspectableMapMarker (IGUIComplete obj, Map map, bool draggable = true) : base(obj, new Context(obj), draggable) {
			VisibleWindow = false;
			this.map = map;
			node = Graphics.GetIcon(Threat.C, new Gdk.Color(255, 255, 255), 8);
			EnterNotifyEvent += MouseEnter;
			LeaveNotifyEvent += MouseLeave;
		}

		void MouseEnter (object obj, EnterNotifyEventArgs args) {

			if (leftPopup != null) leftPopup.Destroy();
			if (lline != null) lline.Destroy();
			if (rightPopup != null) rightPopup.Destroy();
			if (rline != null) rline.Destroy();
			if (node.Parent == map.stage) map.stage.Remove(node);

			GdkWindow.GetOrigin(out int x, out int y);
			Vector2 nodeFixedPosition = scaledPosition + lineOffset;
			Vector2 nodeScreenPosition = new Vector2(x + Allocation.Left, y + Allocation.Top) + lineOffset - offset;
			//This is the screen coordinates of the point defined by lineOffset — where the line(s) begin and the node sits.

			leftPopup = GenerateLeftPopup();
			if (leftPopup != null) {
				lline = new HSeparator();
				lline.SetSizeRequest(popupDistance, 4);
				map.stage.Put(lline, (int)nodeFixedPosition.x - popupDistance, (int)nodeFixedPosition.y - 2);
				lline.ShowAll();
				Graphics.SetAllocTrigger(leftPopup, delegate {
					leftPopup.GetSize(out int width, out int height);
					Vector2 truePosition = nodeScreenPosition + new Vector2(-popupDistance - width, -height / 4);
					leftPopup.Move((int)truePosition.x, (int)truePosition.y);
				});
				leftPopup.ShowAll();
			}

			rightPopup = GenerateRightPopup();
			if (rightPopup != null) {
				rline = new HSeparator();
				rline.SetSizeRequest(popupDistance, 4);
				map.stage.Put(rline, (int)nodeFixedPosition.x, (int)nodeFixedPosition.y - 2);
				rline.ShowAll();
				Graphics.SetAllocTrigger(rightPopup, delegate {
					rightPopup.GetSize(out int width, out int height);
					Vector2 truePosition = nodeScreenPosition + new Vector2(popupDistance, -height / 4);
					rightPopup.Move((int)truePosition.x, (int)truePosition.y);
				});
				rightPopup.ShowAll();
			}

			if (rightPopup != null && leftPopup != null) {
				map.stage.Put(node, (int)nodeFixedPosition.x - 4, (int)nodeFixedPosition.y - 4);
				node.ShowAll();
			}

		}

		void MouseLeave (object obj, LeaveNotifyEventArgs args) {
			if (leftPopup != null) {
				leftPopup.Destroy();
				leftPopup = null;
			}
			if (rightPopup != null) {
				rightPopup.Destroy();
				rightPopup = null;
			}
			if (rline != null) {
				rline.Destroy();
				rline = null;
			}
			if (lline != null) {
				lline.Destroy();
				lline = null;
			}
			if (node.Parent == map.stage) map.stage.Remove(node);
		}

		public void Repin () {
			if (Parent == map.stage) map.stage.Remove(this);
			scaledPosition = position * map.currentMagnif;
			Vector2 markerCoords = scaledPosition + offset;
			map.stage.Put(this, (int)markerCoords.x, (int)markerCoords.y);
			ShowAll();
		}

		public abstract void Redraw ();

		protected virtual Window GenerateLeftPopup () => null;
		protected virtual Window GenerateRightPopup () => null;
		public abstract void Reload ();
		public abstract void OnTriggerDestroyed (IDependable trigger);
		public abstract void OnListenerDestroyed (IDependable listener);

	}

}