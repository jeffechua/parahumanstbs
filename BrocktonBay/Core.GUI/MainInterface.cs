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
		AssetsBottomBar assetsBar;

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

			HBox mainBox = new HBox();
			PackStart(mainBox, true, true, 0);

			//Sets up main layout and inspector
			Notebook notebook = new Notebook();
			Inspector inspector = new Inspector { BorderWidth = 10 };
			mainBox.PackStart(notebook, true, true, 0);
			mainBox.PackStart(inspector, false, false, 0);

			//Map tab
			map = new Map(); //Profiler called inside Map constructor
			Label mapTabLabel = new Label("Map");
			notebook.AppendPage(map, mapTabLabel);

			Profiler.Log();

			//Search tab
			Label searchLabel = new Label("Search");
			Search search = new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, this));
			notebook.AppendPage(search, searchLabel);

			//My domain
			Label domainLabel = new Label("My domain");
			Search domain = new Search((obj) => (obj is Territory || obj is Structure) && ((IAffiliated)obj).affiliation == Game.player,
									   (obj) => Inspector.InspectInNearestInspector(obj, this));
			domain.typesButton.State = StateType.Insensitive;
			domain.toplevelOnlyButton.State = StateType.Insensitive;
			notebook.AppendPage(domain, domainLabel);

			//Agents bottom bar
			assetsBar = new AssetsBottomBar { BorderWidth = 10 };
			PackStart(assetsBar, false, false, 0);

			Profiler.Log(ref Profiler.searchCreateTime);

			MainWindow.main.inspector = inspector;

			Destroyed += (o, a) => DependencyManager.Destroy(this);

			Reload();

			notebook.CurrentPage = 0;

		}

		public void Reload () {

			uint spacing = (uint)(Graphics.textSize / 5);
			Gdk.Color black = new Gdk.Color(0, 0, 0);

			while (textBar.Children.Length > 0) textBar.Children[0].Destroy();
			while (numbersBar.Children.Length > 0) numbersBar.Children[0].Destroy();

			textBar.PackStart(new Label("Playing as: "), false, false, spacing);
			InspectableBox player = (InspectableBox)Game.player.GetHeader(new Context(Game.player, this, false, true));
			if (Game.omnipotent) {
				MyDragDrop.DestSet(player, "Active IAgent");
				MyDragDrop.DestSetDropAction(player, delegate (object obj) {
					Game.player = (IAgent)obj;
					Game.RefreshUIAndTriggerAllFlags();
				});
			}
			textBar.PackStart(player, false, false, 0);
			Image nextPhaseArrow = Graphics.GetIcon(IconTemplate.RightArrow, black, (int)(Graphics.textSize * 0.75));
			ClickableEventBox nextPhaseButton = new ClickableEventBox {
				Child = nextPhaseArrow,
				BorderWidth = (uint)(Graphics.textSize * 0.25),
				Sensitive = Game.CanNext()
			};
			nextPhaseButton.Clicked += (o, a) => Game.Next();
			textBar.PackEnd(nextPhaseButton, false, false, spacing);
			textBar.PackEnd(new Label(Game.phase + " Phase"), false, false, spacing);

			if (GameObject.TryCast(Game.player, out Faction faction)) {
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Economic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.resources.ToString()), false, false, spacing);
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Aesthetic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.reputation.ToString()), false, false, spacing);
			}

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
			InspectableBox inspectable = new InspectableBox(icon, agent) {
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
			context = new Context(Game.player, this);
			while (mainBox.Children.Length > 0) mainBox.Children[0].Destroy();
			if (GameObject.TryCast(Game.player, out Faction faction)) {
				mainBox.PackStart(new VSeparator(), false, false, 0);
				AppendCategory("Unsettled prisoners", faction.unassignedCaptures);
				AppendCategory("Teams", faction.teams);
				AppendCategory("Direct members", faction.roster);
				List<IGUIComplete> teamed = new List<IGUIComplete>();
				foreach (Team team in faction.teams) foreach (Parahuman parahuman in team.roster) teamed.Add(parahuman);
				AppendCategory("Members of teams", teamed);
				AppendCategory("Prisoners", faction.assignedCaptures);
			} else if (GameObject.TryCast(Game.player, out Team team)) {
				mainBox.PackStart(new VSeparator(), false, false, 0);
				AppendCategory("The team", new List<Team> { team });
				AppendCategory("Team members", team.roster);
			} else if (GameObject.TryCast(Game.player, out Parahuman parahuman)) {
				AppendCategory("Yourself", new List<Parahuman> { parahuman });
			}
			ShowAll();
		}

		void AppendCategory<T> (string title, List<T> elements) where T : IGUIComplete {
			if (elements.Count > 0) {
				VBox vBox = new VBox(false, 3) { BorderWidth = 3 };
				mainBox.PackStart(vBox, false, false, 0);
				vBox.PackStart(new Label(title) { Sensitive = false }, true, true, 0);
				HBox hBox = new HBox(false, 10);
				vBox.PackStart(hBox, true, true, 0);
				foreach (T element in elements)
					hBox.PackStart(new Cell(context, element), false, false, 0);
				mainBox.PackStart(new VSeparator(), false, false, 0);
			}
		}

	}


}