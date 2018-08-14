using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public class MainInterface : VBox, IDependable {

		public int order { get { return 0; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Map map;
		HBox textBar;
		HBox numbersBar;

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

			//My agents
			Label agentsLabel = new Label("My agents");
			Search agents = new Search((obj) => (obj is Team || obj is Parahuman) && ((IAffiliated)obj).affiliation == Game.player,
									   (obj) => Inspector.InspectInNearestInspector(obj, this));
			agents.typesButton.State = StateType.Insensitive;
			agents.toplevelOnlyButton.State = StateType.Insensitive;
			notebook.AppendPage(agents, agentsLabel);

			//My domain
			Label domainLabel = new Label("My domain");
			Search domain = new Search((obj) => (obj is Territory || obj is Structure) && ((IAffiliated)obj).affiliation == Game.player,
									   (obj) => Inspector.InspectInNearestInspector(obj, this));
			domain.typesButton.State = StateType.Insensitive;
			domain.toplevelOnlyButton.State = StateType.Insensitive;
			notebook.AppendPage(domain, domainLabel);

			//Search tab
			Label searchLabel = new Label("Search");
			Search search = new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, this));
			notebook.AppendPage(search, searchLabel);

			Profiler.Log(ref Profiler.searchCreateTime);

			MainWindow.main.inspector = inspector;

			DestroyEvent += (o, a) => DependencyManager.Delete(this);

			Reload();

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
					Game.RefreshUI();
				});
			}
			textBar.PackStart(player, false, false, 0);
			Image nextPhaseArrow = Graphics.GetIcon(DirectionType.Right, black, (int)(Graphics.textSize * 0.75));
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


}