using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

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
			map = new Map(Game.city); //Profiler called inside Map constructor
			Label mapTabLabel = new Label("Map");
			notebook.AppendPage(map, mapTabLabel);

			Profiler.Log();

			//Search tab
			Search search = new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, this));
			Label searchLabel = new Label("Search");
			notebook.AppendPage(search, searchLabel);

			Profiler.Log(ref Profiler.searchCreateTime);

			Game.mainWindow.inspector = inspector;

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
				BorderWidth = (uint)(Graphics.textSize * 0.25)
			};
			nextPhaseButton.Clicked += delegate {
				if (Game.phase == Phase.Mastermind) {
					Game.phase = Phase.Action;
				} else {
					Game.phase += 1;
				}
				Game.RefreshUI();
			};
			textBar.PackEnd(nextPhaseButton, false, false, spacing);
			textBar.PackEnd(new Label(Game.phase + " Phase"), false, false, spacing);

			if (GameObject.TryCast(Game.player, out Faction faction)) {
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Economic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.resources.ToString()), false, false, spacing);
				numbersBar.PackStart(Graphics.GetIcon(StructureType.Aesthetic, black, Graphics.textSize), false, false, spacing);
				numbersBar.PackStart(new Label(faction.reputation.ToString()), false, false, spacing);
				foreach (Parahuman parahuman in faction.roster)
					numbersBar.PackEnd(GetParahumanIcon(parahuman), false, false, spacing);
				foreach (Team team in faction.teams)
					foreach (Parahuman parahuman in team.roster)
						numbersBar.PackEnd(GetParahumanIcon(parahuman), false, false, spacing);
			} else if (GameObject.TryCast(Game.player, out Team team)) {
				foreach (Parahuman parahuman in team.roster)
					numbersBar.PackEnd(GetParahumanIcon(parahuman), false, false, spacing);
			} else if (GameObject.TryCast(Game.player, out Parahuman parahuman)) {
				numbersBar.PackEnd(GetParahumanIcon(parahuman), false, false, spacing);
			}
			ShowAll();

		}

		Image GetParahumanIcon (Parahuman parahuman) {
			Image icon = Graphics.GetIcon(parahuman.threat, Graphics.GetColor(parahuman.health), Graphics.textSize);
			icon.HasTooltip = true;
			icon.TooltipText = parahuman.name;
			return icon;
		}

	}
}