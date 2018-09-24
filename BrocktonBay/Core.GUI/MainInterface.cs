using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public class MainInterface : VBox, IDependable {

		public int order { get { return 5; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public void OnListenerDestroyed (IDependable listener) { }
		public void OnTriggerDestroyed (IDependable trigger) { }

		public Map map;
		HBox textBar;
		HBox numbersBar;
		public AssetsBottomBar assetsBar;
		public VBox eventLogsDisplay;
		Label eventLogLabel;
		int unreadEventLogsCounter;

		public MainInterface () {

			DependencyManager.Connect(Game.city, this);
			DependencyManager.Connect(Game.UIKey, this);

			textBar = new HBox();
			numbersBar = new HBox();
			VBox topBars = new VBox(false, 2) { BorderWidth = 5 };
			topBars.PackStart(textBar);
			topBars.PackStart(numbersBar);
			PackStart(new HSeparator(), false, false, 0);
			PackStart(topBars, false, false, 0);
			PackStart(new HSeparator(), false, false, 0);

			HBox mainHBox = new HBox();
			PackStart(mainHBox, true, true, 0);

			//Set up the left and right sides
			VBox leftVBox = new VBox();
			Notebook mainNotebook = new Notebook();
			leftVBox.PackStart(mainNotebook, true, true, 0);
			mainHBox.PackStart(leftVBox, true, true, 0);
			VPaned rightPaned = new VPaned { BorderWidth = 10 };
			mainHBox.PackStart(rightPaned, false, false, 0);

			////Left side
			/// 
			//Map tab
			map = new Map(); //Profiler called inside Map constructor
			mainNotebook.AppendPage(map, new Label("Map"));
			Profiler.Log();
			//Search tab
			Search search = new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, this));
			mainNotebook.AppendPage(search, new Label("Search"));
			//My domain
			Search domain = new Search((obj) => (obj is Territory || obj is Structure) && ((IAffiliated)obj).affiliation == Game.player,
									   (obj) => Inspector.InspectInNearestInspector(obj, this));
			domain.typesButton.State = StateType.Insensitive;
			domain.toplevelOnlyButton.State = StateType.Insensitive;
			mainNotebook.AppendPage(domain, new Label("Domain"));
			//Agents bottom bar
			assetsBar = new AssetsBottomBar { BorderWidth = 10 };
			leftVBox.PackStart(assetsBar, false, false, 0);

			////Right side
			/// 
			//Inspector tab
			Inspector inspector = new Inspector();
			inspector.Unhidden += (o, a) => rightPaned.Position = rightPaned.Allocation.Height * 2 / 3;
			rightPaned.Add1(inspector);
			//Event log tab
			eventLogLabel = new Label("Logs");
			ScrolledWindow eventLogsScroller = new ScrolledWindow();
			eventLogsScroller.SetSizeRequest(200, -1);
			eventLogsDisplay = new VBox { BorderWidth = 10 };
			eventLogsScroller.AddWithViewport(eventLogsDisplay);
			rightPaned.Add2(eventLogsScroller);

			Profiler.Log(ref Profiler.searchCreateTime);

			MainWindow.main.inspector = inspector;

			Destroyed += (o, a) => DependencyManager.Destroy(this);

			Reload();

			mainNotebook.CurrentPage = 0;

		}


		public void Reload () {

			uint spacing = (uint)(Graphics.textSize / 5);
			Gdk.Color black = new Gdk.Color(0, 0, 0);

			//Destroy previous displays for top bar
			while (textBar.Children.Length > 0) textBar.Children[0].Destroy();
			while (numbersBar.Children.Length > 0) numbersBar.Children[0].Destroy();

			//Create display for active agent (player)
			textBar.PackStart(new Label("Playing as: "), false, false, spacing);
			InspectableBox player = (InspectableBox)Game.player.GetHeader(new Context(null, Game.player, false, true));
			if (Game.omnipotent) {
				MyDragDrop.DestSet(player, "Active IAgent");
				MyDragDrop.DestSetDropAction(player, delegate (object obj) {
					if (GameObject.TryCast(obj, out IAgent agent))
						Game.SetPlayer(agent);
				});
			}
			textBar.PackStart(player, false, false, 0);

			//Create phase indicator and "next" arrow
			Image nextPhaseArrow = Graphics.GetIcon(IconTemplate.RightArrow, black, (int)(Graphics.textSize * 0.75));
			ClickableEventBox nextPhaseButton = new ClickableEventBox {
				Child = nextPhaseArrow,
				BorderWidth = (uint)(Graphics.textSize * 0.25),
				Sensitive = Game.CanNext()
			};
			nextPhaseButton.Clicked += (o, a) => Game.Next();
			textBar.PackEnd(nextPhaseButton, false, false, spacing);
			textBar.PackEnd(new Label(Game.phase + " Phase"), false, false, spacing);

			//Update resource and reputation displays
			if (GameObject.TryCast(Game.player, out Faction faction)) {
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Economic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.resources.ToString()), false, false, spacing);
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Aesthetic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.reputation.ToString()), false, false, spacing);
			} else if (GameObject.TryCast(Game.player, out Team team)) {
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Aesthetic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(team.reputation.ToString()), false, false, spacing);
			} else if (GameObject.TryCast(Game.player, out Parahuman parahuman)) {
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Aesthetic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(parahuman.reputation.ToString()), false, false, spacing);
			}

			//Create the icons for each agent and frame the current turn-taker
			if ((Game.phase & (Phase.Resolution | Phase.Event)) == Phase.None) {
				for (int i = Game.turnOrder.Count - 1; i >= 0; i--) {
					InspectableBox icon = GetAgentIcon(Game.turnOrder[i]);
					if (i == Game.turn) {
						numbersBar.PackEnd(new Frame { Child = icon }, false, false, 0);
					} else {
						numbersBar.PackEnd(icon, false, false, 0);
					}
				}
			}

			ShowAll();

		}

		InspectableBox GetAgentIcon (IAgent agent) {
			Image icon = Graphics.GetIcon(agent.threat, Graphics.GetColor(agent), Graphics.textSize);
			InspectableBox inspectable = new InspectableBox(icon, agent, new Context(null)) {
				HasTooltip = true,
				TooltipText = agent.name,
				VisibleWindow = false
			};
			return inspectable;
		}

	}

	public sealed class AssetsBottomBar : Expander, IDependable {

		public int order { get { return 5; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public void OnListenerDestroyed (IDependable listener) { }
		public void OnTriggerDestroyed (IDependable trigger) { }

		Context context;
		HBox mainBox;

		//This dictionary allows fast updating of the bar to reflect engagement of agents.
		List<Tuple<GameObject, Container>> table = new List<Tuple<GameObject, Container>>();

		bool redrawQueued;

		public AssetsBottomBar () : base("Parahuman resources") {
			Spacing = 5;
			ScrolledWindow scroller = new ScrolledWindow { VscrollbarPolicy = PolicyType.Never };
			mainBox = new HBox(false, 10);
			scroller.AddWithViewport(mainBox);
			Add(scroller);
			DependencyManager.Connect(Game.city, this);
			DependencyManager.Connect(Game.UIKey, this);
			Activated += OnActivated;
			Reload();
		}

		public void Reload () {
			if (Expanded) {
				Redraw();
			} else if (!redrawQueued) {
				redrawQueued = true;
			}
		}

		public void OnActivated (object obj, EventArgs args) {
			if (redrawQueued) {
				Redraw();
				redrawQueued = false;
			}
		}

		public void Redraw () {
			table.Clear();
			context = new Context(null);
			while (mainBox.Children.Length > 0) mainBox.Children[0].Destroy();
			if (GameObject.TryCast(Game.player, out Faction faction)) {
				mainBox.PackStart(new VSeparator(), false, false, 0);
				AppendParahumans("Unsettled prisoners", faction.unassignedCaptures);
				AppendTeams("Teams", faction.teams);
				AppendParahumans("Direct members", faction.roster);
				List<Parahuman> teamed = new List<Parahuman>();
				foreach (Team team in faction.teams) foreach (Parahuman parahuman in team.roster) teamed.Add(parahuman);
				AppendParahumans("Members of teams", teamed);
				AppendParahumans("Prisoners", faction.assignedCaptures);
			} else if (GameObject.TryCast(Game.player, out Team team)) {
				mainBox.PackStart(new VSeparator(), false, false, 0);
				AppendTeams("The team", new List<Team> { team });
				AppendParahumans("Team members", team.roster);
			} else if (GameObject.TryCast(Game.player, out Parahuman parahuman)) {
				AppendParahumans("Yourself", new List<Parahuman> { parahuman });
			}
			UpdateEngagement();
			ShowAll();
		}

		public void UpdateEngagement () {
			foreach (Tuple<GameObject, Container> tuple in table) {
				if (tuple.Item1.isEngaged) {
					tuple.Item2.Children[0].Sensitive = false;
				} else {
					tuple.Item2.Children[0].Sensitive = true;
				}
			}
		}

		void AppendTeams (string title, List<Team> elements) {
			if (elements.Count > 0) {
				VBox vBox = new VBox(false, 3) { BorderWidth = 3 };
				mainBox.PackStart(vBox, false, false, 0);
				vBox.PackStart(new Label(title) { Sensitive = false }, true, true, 0);
				HBox hBox = new HBox(false, 10);
				vBox.PackStart(hBox, true, true, 0);
				foreach (Team element in elements) {            //Here we see spaghetti code to get the parahuman headers embedded in Team.GetCellContents()
					Cell teamCell = new Cell(context, element); //All the casting and child getting looks horrible but there's really no major performance loss
					teamCell.prelight = false; teamCell.depress = false; //Why? Because its statetype changes messes up the Sensitive settings of its children.
					table.Add(new Tuple<GameObject, Container>(element, (Container)teamCell.frame.LabelWidget));
					Container current = teamCell;
					while (current is Frame || current is Gtk.Alignment || current is EventBox) //This reaches rosterBox in Team.GetCellContents() but no further
						current = (Container)current.Children[0];
					Widget[] parahumanBoxes = current.Children;
					foreach (Widget parahumanBox in parahumanBoxes)
						table.Add(new Tuple<GameObject, Container>(
							(GameObject)((InspectableBox)parahumanBox).inspected, (Container)parahumanBox));
					hBox.PackStart(teamCell, false, false, 0);
				}
				mainBox.PackStart(new VSeparator(), false, false, 0);
			}
		}

		void AppendParahumans (string title, List<Parahuman> elements) {
			if (elements.Count > 0) {
				VBox vBox = new VBox(false, 3) { BorderWidth = 3 };
				mainBox.PackStart(vBox, false, false, 0);
				vBox.PackStart(new Label(title) { Sensitive = false }, true, true, 0);
				HBox hBox = new HBox(false, 10);
				vBox.PackStart(hBox, true, true, 0);
				foreach (Parahuman element in elements) {
					Cell parahumanCell = new Cell(context, element);
					table.Add(new Tuple<GameObject, Container>(element, parahumanCell));
					hBox.PackStart(parahumanCell, false, false, 0);
				}
				mainBox.PackStart(new VSeparator(), false, false, 0);
			}
		}

	}


}