using System;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace BrocktonBay {

	public enum AlertIconType {
		Important = 0,
		Unopposed = 1,
		Opposed = 2,
	}

	public partial class Map {

		static List<Map> maps = new List<Map>();
		static List<MapMarked> markeds = new List<MapMarked>();

		public static void Register (MapMarked marked) {
			foreach (Map map in maps)
				map.markers.Add(marked, marked.GetMarkers(map));
			markeds.Add(marked);
		}

		public static void Deregister (MapMarked marked) {
			foreach (Map map in maps)
				map.markers.Remove(marked);
			markeds.Remove(marked);
		}

	}

	public partial class Map : EventBox {

		public Fixed stage;
		Fixed positioner;
		Gtk.Image mapImage;
		VScale zoomScale;
		Gdk.Pixbuf baseMap;

		Dictionary<MapMarked, IMapMarker[]> markers;

		public Vector2 currentDrag;
		public Vector2 currentPan;
		public double currentMagnif;
		bool dragging;

		const double maxZoom = 5;
		const double zoomFactor = 1.25; //Factor of magnification per step
		double maxMagnif = Math.Pow(zoomFactor, maxZoom);

		public Map () {

			maps.Add(this);
			Destroyed += (o, a) => maps.Remove(this);

			Profiler.Log();

			stage = new Fixed();
			positioner = new Fixed();
			positioner.Put(stage, 0, 0);
			Add(positioner);

			//Drawing the map background
			baseMap = new Pixbuf(Game.city.mapPngSource);
			baseMap = baseMap.ScaleSimple(
				(int)(Game.city.mapDefaultWidth * maxMagnif),
				(int)(Game.city.mapDefaultWidth * baseMap.Height * maxMagnif / baseMap.Width),
				Gdk.InterpType.Hyper);
			mapImage = new Gtk.Image();
			stage.Put(mapImage, 0, 0);

			Profiler.Log(ref Profiler.mapBackgroundCreateTime);

			//Panning and zooming functionality

			zoomScale = new VScale(0, maxZoom, 1);
			zoomScale.Value = 0;
			currentMagnif = 1;
			zoomScale.HeightRequest = 100;
			positioner.Put(zoomScale, 0, 0);
			zoomScale.ValueChanged += (o, a) => Zoom();

			ButtonPressEvent += delegate (object obj, ButtonPressEventArgs args) {
				if (args.Event.Button == 1) {
					dragging = true;
					currentDrag = new Vector2(args.Event.XRoot, args.Event.YRoot);
				}
			};
			ButtonReleaseEvent += delegate (object obj, ButtonReleaseEventArgs args) {
				if (args.Event.Button == 1) {
					dragging = false;
				}
			};
			MotionNotifyEvent += delegate (object obj, MotionNotifyEventArgs args) {
				if (dragging) {
					currentPan += new Vector2(args.Event.XRoot, args.Event.YRoot) - currentDrag;
					currentDrag = new Vector2(args.Event.XRoot, args.Event.YRoot);
					positioner.Move(stage, (int)currentPan.x, (int)currentPan.y);
				}
			};

			SetSizeRequest(0, 0);
			Graphics.SetAllocTrigger(this, delegate { Zoom(); });

			Profiler.Log(ref Profiler.mapBehaviourAssignTime);

			//Register territories
			markers = new Dictionary<MapMarked, IMapMarker[]>();
			foreach (MapMarked marked in markeds)
				markers.Add(marked, marked.GetMarkers(this));

			Profiler.Log(ref Profiler.mapMarkersPlaceTime);

		}

		public void Zoom () {

			// Zoom the background by a combination of scaling the image and translating the "stage" GtkFixed.
			double newMagnif = Math.Pow(zoomFactor, zoomScale.Value);
			mapImage.Pixbuf = baseMap.ScaleSimple((int)(baseMap.Width / maxMagnif * newMagnif), (int)(baseMap.Height / maxMagnif * newMagnif), InterpType.Nearest);
			Vector2 allocationSize = new Vector2(Allocation.Width, Allocation.Height);
			currentPan -= (allocationSize / 2 - currentPan) * (newMagnif - currentMagnif) / currentMagnif;  //Math!
			positioner.Move(stage, (int)currentPan.x, (int)currentPan.y);
			currentMagnif = newMagnif;

			//Repin (and Redraw if necessary) the markers

			Profiler.Log();

			List<IMapMarker> sortedMarkers = new List<IMapMarker>();
			foreach (KeyValuePair<MapMarked, IMapMarker[]> markerSet in markers)
				foreach (IMapMarker marker in markerSet.Value)
					sortedMarkers.Add(marker);
			sortedMarkers.Sort((x, y) => x.layer.CompareTo(y.layer));
			foreach (IMapMarker marker in sortedMarkers) {
				marker.Repin();
				if (marker.magnifRedraw)
					marker.Redraw();
			}

			ShowAll();

			Profiler.WriteLog();

		}

	}

}
