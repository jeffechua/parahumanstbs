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

		public static bool Relevant (IBattleground battleground, IAgent agent)
			=> battleground.affiliation == agent ||
					   (battleground.attacker != null && battleground.attacker.affiliation == agent) ||
					   (battleground.defender != null && battleground.defender.affiliation == agent);

		public Widget NewAlert (IBattleground battleground) {

			AlertIconType alertType = battleground.defender == null ? AlertIconType.Unopposed : AlertIconType.Opposed;
			int alertSize = battleground is Structure ? StructureMarker.markerSize * 6 / 5 : TerritoryMarker.markerHeight;
			Color primaryColor = battleground.attacker.affiliation.color;
			Color secondaryColor = battleground.defender == null ? primaryColor : battleground.defender.affiliation.color;
			Color trim;
			if (Relevant(battleground, Game.player)) {
				trim = new Color(0, 0, 0);
			} else {
				trim = new Color(50, 50, 50);
				primaryColor.Red = (ushort)((primaryColor.Red + 150) / 2);
				primaryColor.Green = (ushort)((primaryColor.Green + 150) / 2);
				primaryColor.Blue = (ushort)((primaryColor.Blue + 150) / 2);
				secondaryColor.Red = (ushort)((secondaryColor.Red + 150) / 2);
				secondaryColor.Green = (ushort)((secondaryColor.Green + 150) / 2);
				secondaryColor.Blue = (ushort)((secondaryColor.Blue + 150) / 2);
			}

			Gtk.Image icon = Graphics.GetAlert(alertType, alertSize, primaryColor, secondaryColor, trim);

			if (!Relevant(battleground, Game.player))
				return icon;

			if (Game.phase == Phase.Action || (Game.phase == Phase.Response && battleground.defender == null)) {
				InspectableBox alert = new InspectableBox(icon, battleground.attacker) { VisibleWindow = false };
				alert.EnterNotifyEvent += delegate {
					alert.GdkWindow.GetOrigin(out int x, out int y);
					Gtk.Window popup = GetDeploymentPopup(battleground.attacker);
					Graphics.SetAllocationTrigger(popup, delegate {
						popup.Move(x + alert.Allocation.Right + alertSize * 3 / 2,
								   y + alert.Allocation.Top + alertSize / 2 - popup.Allocation.Height / 4);
					});
					alert.ShowAll();
				};
				LeaveNotifyEvent += delegate {
					if (popup != null) {
						popup.Destroy();
						popup = null;
					}
					map.stage.Remove(line);
				};
			} else if (Game.phase == Phase.Response) {
				InspectableBox alert = new InspectableBox(icon, battleground.defender) { VisibleWindow = false };
				alert.EnterNotifyEvent += delegate {
					alert.GdkWindow.GetOrigin(out int x, out int y);
					Gtk.Window popup = GetDeploymentPopup(battleground.attacker);
					Graphics.SetAllocationTrigger(popup, delegate {
						popup.Move(x + alert.Allocation.Right + alertSize * 3 / 2,
								   y + alert.Allocation.Top + alertSize / 2 - popup.Allocation.Height / 4);
					});
					alert.ShowAll();
				};
				alert.EnterNotifyEvent += delegate {
					alert.GdkWindow.GetOrigin(out int x, out int y);
					Gtk.Window popup = GetDeploymentPopup(battleground.attacker);
					Graphics.SetAllocationTrigger(popup, delegate {
						popup.Move(x + alert.Allocation.Right + alertSize * 3 / 2,
								   y + alert.Allocation.Top + alertSize / 2 - popup.Allocation.Height / 4);
					});
					alert.ShowAll();
				};
			} else if (Game.phase == Phase.Mastermind) {
				if (battleground.battle == null) battleground.battle = new Battle(battleground, battleground.attacker, battleground.defender);
				ClickableEventBox alert = new ClickableEventBox { Child = icon, VisibleWindow = false };
				alert.Clicked += delegate {
					SecondaryWindow eventWindow = new SecondaryWindow("Battle at " + battleground.name);
					eventWindow.SetMainWidget(new BattleInterface(battleground.battle));
					eventWindow.ShowAll();
				};
				return alert;
			}

			throw new Exception("Invalid game phase");

		}

		public Gtk.Window GetDeploymentPopup (Widget alert, Deployment deployment) {

			Gtk.Window window = new Gtk.Window(Gtk.WindowType.Popup) {
				Gravity = Gravity.West,
				TransientFor = (Gtk.Window)Toplevel
			}

			Context context = new Context(Game.player, deployment, true, false);

			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };

			Label name = new Label(deployment.name);
			name.SetAlignment(0.5f, 1);
			mainBox.PackStart(name, false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (deployment.affiliation == null) {
				affiliationBox.PackStart(new Label("None"));
			} else {
				affiliationBox.PackStart(deployment.affiliation.GetHeader(context.butCompact));
			}
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Threat: " + deployment.threat), 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Force Employed: " + deployment.force_employed), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);

			List<Widget> cells = new List<Widget>();
			cells.AddRange(deployment.teams.ConvertAll((team) => new Cell(context, team)));
			cells.AddRange(deployment.independents.ConvertAll((team) => new Cell(context, team)));
			mainBox.PackStart(new DynamicTable(cells, 2));

			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(deployment, "strength", context));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(deployment, "stealth", context));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(deployment, "insight", context));

			window.Add(mainBox);

			Graphics.SetAllocationTrigger(window, delegate {
				window.Move(x + marker.Allocation.Right + StructureMarker.markerSize * 3 / 2,
				            y + marker.Allocation.Top + StructureMarker.markerSize / 2 - window.Allocation.Height / 4);
			});

			return window

		}

	}

}
