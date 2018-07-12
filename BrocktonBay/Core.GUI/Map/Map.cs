using System;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace Parahumans.Core {

	public class Map : EventBox, IDependable {

		public int order { get { return 10; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public City city;

		public Fixed stage;
		Fixed positioner;
		Gtk.Image mapImage;
		VScale zoomScale;
		Gdk.Pixbuf baseMap;

		Dictionary<Territory, TerritoryMarker> territoryRegister;
		Dictionary<Structure, StructureMarker> structureRegister;

		public Vector2 currentDrag;
		public Vector2 currentPan;
		public double currentMagnif;
		bool dragging;

		const double maxZoom = 5;
		const double zoomFactor = 1.25; //Factor of magnification per step
		double maxMagnif = Math.Pow(zoomFactor, maxZoom);

		public Map () { }

		public Map (City city) {

			this.city = city;
			DependencyManager.Connect(city, this);
			DependencyManager.Connect(MainClass.UIKey, this);

			Profiler.Log();

			stage = new Fixed();
			positioner = new Fixed();
			positioner.Put(stage, 0, 0);
			Add(positioner);

			//Drawing the map background
			baseMap = new Pixbuf(city.mapPngSource);
			baseMap = baseMap.ScaleSimple(
				(int)(city.mapDefaultWidth * maxMagnif),
				(int)(city.mapDefaultWidth * baseMap.Height * maxMagnif / baseMap.Width),
				Gdk.InterpType.Hyper);
			mapImage = new Gtk.Image();
			stage.Put(mapImage, 0, 0);

			Profiler.Log(ref Profiler.mapBackgroundCreateTime);

			//Register territories
			territoryRegister = new Dictionary<Territory, TerritoryMarker>();
			structureRegister = new Dictionary<Structure, StructureMarker>();

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
			Graphics.SetAllocationTrigger(this, delegate { Zoom(); Reload(); });

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
			foreach (KeyValuePair<Territory, TerritoryMarker> pair in territoryRegister) {
				pair.Value.Repin();
				pair.Value.Rezone();
			}
			foreach (KeyValuePair<Structure, StructureMarker> pair in structureRegister)
				pair.Value.Repin();

			ShowAll();

		}

		public void Reload () {
			List<GameObject> unregisteredTerritories = city.gameObjects.FindAll((obj) => obj is Territory);
			unregisteredTerritories.RemoveAll((territory) => territoryRegister.ContainsKey((Territory)territory));
			foreach (GameObject obj in unregisteredTerritories) {
				Territory territory = (Territory)obj;
				TerritoryMarker marker = new TerritoryMarker(territory, this);
				territoryRegister.Add(territory, marker);
			}
			List<GameObject> unregisteredStructures = city.gameObjects.FindAll((obj) => obj is Structure);
			unregisteredStructures.RemoveAll((structure) => structureRegister.ContainsKey((Structure)structure));
			foreach (GameObject obj in unregisteredStructures) {
				Structure structure = (Structure)obj;
				StructureMarker marker = new StructureMarker(structure, this);
				structureRegister.Add(structure, marker);
			}
			ShowAll();
		}

	}

}
