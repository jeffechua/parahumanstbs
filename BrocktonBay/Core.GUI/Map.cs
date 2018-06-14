using System;
using System.Collections.Generic;
using Gtk;
using Gdk;


namespace Parahumans.Core {

	public class MapMarker {
		public Gtk.Image markerImage;
		public Faction affiliation;
		public IntVector2 location; //We round this to prevent floating point precision errors
		public Vector2 scaledPosition;
		public MapMarker (Territory territory) {
			markerImage = Map.DrawTerritoryMarker(territory);
			affiliation = territory.affiliation;
			location = territory.location;
			scaledPosition = location;
		}
	}

	public class Map : EventBox, IDependable {

		public int order { get { return 10; } }
		public List<IDependable> dependencies { get; set; } = new List<IDependable>();
		public List<IDependable> dependents { get; set; } = new List<IDependable>();

		public City city;

		Fixed stage;
		Fixed positioner;
		Gtk.Image mapImage;
		VScale zoomScale;
		Gdk.Pixbuf baseMap;

		Dictionary<Territory, MapMarker> territoryRegister;

		Vector2 currentDrag;
		Vector2 currentPan;
		double currentMagnif;
		bool dragging;

		const double maxZoom = 5;
		const double zoomFactor = 1.25; //Factor of magnification per step
		double maxMagnif = Math.Pow(zoomFactor, maxZoom);

		public Map () { }

		public Map (City city) {

			this.city = city;

			//Constructing the map itself

			DependencyManager.Connect(city, this);

			Profiler.Log();

			stage = new Fixed();
			positioner = new Fixed();
			positioner.Put(stage, 0, 0);
			Add(positioner);

			baseMap = new Pixbuf(city.mapPngSource);
			baseMap = baseMap.ScaleSimple(
				(int)(city.mapDefaultWidth * maxMagnif),
				(int)(city.mapDefaultWidth * baseMap.Height * maxMagnif / baseMap.Width),
				Gdk.InterpType.Hyper);
			mapImage = new Gtk.Image();
			stage.Put(mapImage, 0, 0);

			Profiler.Log(ref Profiler.mapBackgroundCreateTime);

			//Placing markers

			List<GameObject> territories = city.gameObjects.FindAll((obj) => obj is Territory);
			territoryRegister = new Dictionary<Territory, MapMarker>();
			foreach (GameObject obj in territories) {
				Territory territory = (Territory)obj;
				MapMarker marker = new MapMarker(territory);
				territoryRegister.Add(territory, marker);
				stage.Put(marker.markerImage, marker.location.x, marker.location.y);
			}

			Profiler.Log(ref Profiler.mapTerritoriesPlaceTime);
			Profiler.Log(ref Profiler.mapStructuresPlaceTime);

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
			SizeAllocatedHandler InitialZoom = null;
			InitialZoom = delegate {
				SizeAllocated -= InitialZoom;
				Zoom();
			};
			SizeAllocated += InitialZoom;

			Profiler.Log(ref Profiler.mapBehaviourAssignTime);
		}

		public void Zoom () {

			// Zoom the background by a combination of scaling the image and translating the "stage" GtkFixed.
			double newMagnif = Math.Pow(zoomFactor, zoomScale.Value);
			mapImage.Pixbuf = baseMap.ScaleSimple((int)(baseMap.Width / maxMagnif * newMagnif), (int)(baseMap.Height / maxMagnif * newMagnif), InterpType.Nearest);
			Vector2 allocationSize = new Vector2(Allocation.Width, Allocation.Height);
			currentPan -= (allocationSize / 2 - currentPan) * (newMagnif - currentMagnif) / currentMagnif;  //Math!
			positioner.Move(stage, (int)currentPan.x, (int)currentPan.y);
			currentMagnif = newMagnif;

			//Repin the markers
			foreach (KeyValuePair<Territory, MapMarker> pair in territoryRegister)
				PinMarker(pair.Key, pair.Value);

			ShowAll();
		}

		public void Reload () {
			foreach (KeyValuePair<Territory, MapMarker> pair in territoryRegister) {
				Territory territory = pair.Key;
				MapMarker marker = pair.Value;
				if (territory.affiliation != marker.affiliation) {
					stage.Remove(marker.markerImage);
					marker.markerImage = DrawTerritoryMarker(territory);
					PinMarker(territory, marker);
					marker.affiliation = territory.affiliation;
					//Update position once this has realized
					SizeAllocatedHandler handler = null;
					handler = delegate {
						marker.markerImage.SizeAllocated -= handler;
						PinMarker(territory, marker);
					};
					marker.markerImage.SizeAllocated += handler;
				}
				if (territory.location != marker.location) {
					PinMarker(territory, marker);
					marker.location = territory.location;
				}
			}
			List<GameObject> unregistered = city.gameObjects.FindAll((obj) => obj is Territory);
			unregistered.RemoveAll((territory) => territoryRegister.ContainsKey((Territory)territory));
			foreach (GameObject obj in unregistered) {
				Territory territory = (Territory)obj;
				MapMarker marker = new MapMarker(territory);
				territoryRegister.Add(territory, marker);
				PinMarker(territory, marker);
			}
			ShowAll();
		}

		// Pinning the markers. As they are inside "stage", their position is relative to the map's upper-left
		// corner and thus easy to manipulate. I just scale their position to the magnification.
		public void PinMarker (Territory territory, MapMarker marker) {
			marker.scaledPosition = territory.location * currentMagnif;
			marker.scaledPosition = territory.location * currentMagnif;
			marker.scaledPosition.x -= marker.markerImage.Allocation.Width / 2; // Center the tip instead 
			marker.scaledPosition.y -= marker.markerImage.Allocation.Height;    // of the upper-left corner.
			if (marker.markerImage.Parent == stage) {
				stage.Move(marker.markerImage, (int)marker.scaledPosition.x, (int)marker.scaledPosition.y);
			} else {
				stage.Put(marker.markerImage, (int)marker.scaledPosition.x, (int)marker.scaledPosition.y);
			}
		}

		public static Gtk.Image DrawTerritoryMarker (Territory territory) {

			double pixelWidth = 40;
			double pixelHeight = 60;

			double width = pixelWidth * 10;
			double height = pixelHeight * 10;

			Pixmap color = new Pixmap(MainClass.mainWindow.GdkWindow, (int)width, (int)height);
			Pixmap mask = new Pixmap(MainClass.mainWindow.GdkWindow, (int)width, (int)height);

			Gdk.GC markerShape = new Gdk.GC(color) { RgbFgColor = territory.affiliation == null ? Graphics.Unaffiliated : territory.affiliation.color };
			Gdk.GC markerCore = new Gdk.GC(color) { RgbFgColor = new Color(100, 100, 100) };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(255, 255, 255) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };

			color.DrawRectangle(markerShape, true, new Rectangle(0, 0, (int)width, (int)height));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)width, (int)height));

			mask.DrawArc(visible, true, 0, 0, (int)width, (int)width, 0, 23040);

			// The "triangle" here refers to the triangle formed by the bottom vertex, a tangent point and the bottom of the image.
			//    ______
			//  /        \
			// |          |
			// |          |
			//  \        /.
			//   \      / .
			//    \    /  .  <-- this triangle.
			//     \  /   .
			//      \/.....

			double triangleHypotenuse = Math.Sqrt(height * height - height * width);
			double triangleWidth = width / (2 * height - width) * triangleHypotenuse;
			double triangleHeight = Math.Sqrt(triangleHypotenuse * triangleHypotenuse - triangleWidth * triangleWidth);

			Vector2 bottomVertex = new Vector2(width / 2, height);
			Vector2 leftVertex = bottomVertex + new Vector2(-triangleWidth, -triangleHeight);
			Vector2 rightVertex = bottomVertex + new Vector2(triangleWidth, -triangleHeight);

			mask.DrawPolygon(visible, true, new Point[] { bottomVertex.ToPoint(), leftVertex.ToPoint(), rightVertex.ToPoint() });

			double coreRadius = width / 5;
			double coreCenter = width / 2;

			color.DrawArc(markerCore, true,
						  (int)(coreCenter - coreRadius), (int)(coreCenter - coreRadius),
						  (int)(coreRadius * 2), (int)(coreRadius * 2),
						  0, 23040);

			return new Gtk.Image(Graphics.Scale(color, width, height, 0.1), Graphics.Scale(mask, width, height, 0.1));

		}

	}

}
