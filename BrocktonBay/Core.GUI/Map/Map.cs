using System;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace Parahumans.Core {

	public enum AlertIconType {
		Important = 0,
		Unopposed = 1,
		Opposed = 2,
	}

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
			DependencyManager.Connect(Game.UIKey, this);

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
			List<GameObject> unregisteredStructures = city.gameObjects.FindAll((obj) => obj is Structure);
			unregisteredStructures.RemoveAll((structure) => structureRegister.ContainsKey((Structure)structure));
			foreach (GameObject obj in unregisteredStructures) {
				Structure structure = (Structure)obj;
				StructureMarker marker = new StructureMarker(structure, this);
				structureRegister.Add(structure, marker);
			}
			List<GameObject> unregisteredTerritories = city.gameObjects.FindAll((obj) => obj is Territory);
			unregisteredTerritories.RemoveAll((territory) => territoryRegister.ContainsKey((Territory)territory));
			foreach (GameObject obj in unregisteredTerritories) {
				Territory territory = (Territory)obj;
				TerritoryMarker marker = new TerritoryMarker(territory, this);
				territoryRegister.Add(territory, marker);
			}
			ShowAll();
		}

		public static ClickableEventBox NewAlert (IBattleground battleground) {
			Gtk.Image icon;
			AlertIconType alertType;
			if (battleground.attacker != null && battleground.defender != null) {
				alertType = AlertIconType.Opposed;
			} else {
				alertType = AlertIconType.Unopposed;
			}
			if (battleground is Structure) {
				icon = Graphics.GetIcon(alertType, new Color(230, 120, 0), StructureMarker.markerSize * 6 / 5);
			} else { //battleground is Territory
				icon = Graphics.GetIcon(alertType, new Color(230, 0, 0), TerritoryMarker.markerHeight);
			}
			ClickableEventBox alert;
			if (Game.phase == Phase.Action || (Game.phase == Phase.Response && battleground.defender == null)) {
				if (battleground.attacker.affiliation == Game.player) {
					alert = new InspectableBox(icon, battleground.attacker);
				} else {
					alert = new ClickableEventBox { Child = icon, active = false };
				}
			} else if (Game.phase == Phase.Response) {
				if (battleground.defender.affiliation == Game.player) {
					alert = new InspectableBox(icon, battleground.defender);
				} else {
					alert = new ClickableEventBox { Child = icon, active = false };
				}
			} else if (Game.phase == Phase.Mastermind) {
				if (battleground.battle == null) battleground.battle = new Battle(battleground, battleground.attacker, battleground.defender);
				alert = new ClickableEventBox { Child = icon };
				alert.Clicked += delegate {
					SecondaryWindow eventWindow = new SecondaryWindow("Battle at " + battleground.name);
					eventWindow.SetMainWidget(new BattleInterface(battleground.battle));
					eventWindow.ShowAll();
				};
			}else {
				throw new Exception("Invalid game phase");
			}
			alert.VisibleWindow = false;
			return alert;
		}

	}

}
