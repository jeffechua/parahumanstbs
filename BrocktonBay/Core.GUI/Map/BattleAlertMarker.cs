using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public sealed class BattleAlertMarker : InspectableMapMarker {

		int size;
		public override int order { get { return 3; } }

		IBattleground battleground;
		bool attacked;
		bool defended;
		Phase phase;
		IntVector2 shownPosition;

		Vector2 _offset;
		public override int layer { get => 2; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => _offset; }
		protected override Vector2 lineOffset { get => _offset + new Vector2(size / 2, size / 2); }
		public override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => size * 2; }

		public BattleAlertMarker (IBattleground battleground, Map map) : base(battleground, map, false, false) {
			this.battleground = battleground;
			if (battleground is Structure) {
				size = StructureMarker.markerSize * 6 / 5;
				_offset = new Vector2(-size / 2, -StructureMarker.markerSize * 2);
			} else {
				size = TerritoryMarker.markerHeight;
				_offset = new Vector2(-size / 2, -TerritoryMarker.markerHeight - size - TerritoryMarker.markerWidth / 3);
			}
			DependencyManager.Connect(battleground, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {

			active = false;

			if (Child != null) Child.Destroy();

			if (battleground.attacker == null)
				return;

			Add(Graphics.GetAlert(battleground, size));

			if (Battle.Relevant(battleground, Game.player)) {
				active = true;
				if (Game.phase == Phase.Action || (Game.phase == Phase.Response && battleground.defender == null)) {
					inspected = battleground.attacker;
				} else if (Game.phase == Phase.Response) {
					inspected = battleground.defender;
				} else {
					inspected = null;
				}
			}

			ShowAll();

		}

		public override void OnClicked (object obj, ButtonReleaseEventArgs args) {
			if (inspected != null) { //This is a battle
				base.OnClicked(obj, args);
			} else {
				SecondaryWindow eventWindow = new SecondaryWindow("Battle at " + battleground.name);
				eventWindow.SetMainWidget(new BattleInterface(battleground.battle));
				eventWindow.ShowAll();
			}
		}

		public override void Reload () {
			if ((battleground.attacker != null) != attacked || (battleground.defender != null) != defended || Game.phase != phase) {
				attacked = battleground.attacker != null;
				defended = battleground.defender != null;
				phase = Game.phase;
				Redraw();
			}
			if (battleground.position != shownPosition) {
				shownPosition = battleground.position;
				Repin();
			}
		}
		public override void OnTriggerDestroyed (IDependable trigger) {
			if (trigger == battleground) {
				Destroy();
				DependencyManager.DisconnectAll(this);
			}
		}
		public override void OnListenerDestroyed (IDependable listener) { }

		protected override Window GenerateLeftPopup () {
			if (!attacked) return null;
			switch (Game.phase) {
				case Phase.Action:
					return null;
				case Phase.Response:
					return defended ? GenerateDeploymentPopup(battleground.attacker) : null;
				default:
					return null;
			}
		}

		protected override Window GenerateRightPopup () {
			if (!attacked) return null;
			switch (Game.phase) {
				case Phase.Action:
					return GenerateDeploymentPopup(battleground.attacker);
				case Phase.Response:
					return GenerateDeploymentPopup(defended ?
												   (Deployment)battleground.defender :
												   (Deployment)battleground.attacker);
				default:
					return GenerateBattlePopup(battleground.battle);
			}
		}

		Window GenerateDeploymentPopup (Deployment deployment) {

			Window popup = new Window(WindowType.Popup) { TransientFor = (Window)map.Toplevel };
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

			popup.Add(mainBox);
			return popup;

		}

		Window GenerateBattlePopup (Battle battle) {

			Window popup = new Window(WindowType.Popup) {
				Gravity = Gdk.Gravity.West,
				TransientFor = (Window)map.Toplevel
			};
			Context context = new Context(Game.player, battle, true, false);
			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };

			Label name = new Label(battle.name);
			name.SetAlignment(0.5f, 1);
			mainBox.PackStart(name, false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(battle, "victor_display", context));
			mainBox.PackStart(new HSeparator(), false, false, 7);

			if (battle.defenders == null) {

			} else {
				Table casualties = new Table(9, 3, false) { ColumnSpacing = 5, RowSpacing = 5 };
				casualties.Attach(new VSeparator(), 1, 2, 0, 9);
				casualties.Attach(new HSeparator(), 0, 3, 1, 2);
				casualties.Attach(new HSeparator(), 0, 3, 3, 4);
				casualties.Attach(new HSeparator(), 0, 3, 5, 6);
				casualties.Attach(new HSeparator(), 0, 3, 7, 8);
				casualties.Attach(new Label("Attackers"), 0, 1, 0, 1);
				casualties.Attach(new Label("Defenders"), 2, 3, 0, 1);

				VBox aInjuries = new VBox();
				VBox dInjuries = new VBox();
				VBox aDowned = new VBox();
				VBox dDowned = new VBox();
				VBox aDeaths = new VBox();
				VBox dDeaths = new VBox();
				VBox aCaptures = new VBox();
				VBox dCaptures = new VBox();

				foreach (Parahuman parahuman in battle.attackers.combined_roster.FindAll((p) => p.health == Health.Injured))
					aInjuries.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.attackers.combined_roster.FindAll((p) => p.health == Health.Down))
					aDowned.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.attackers.combined_roster.FindAll((p) => p.health == Health.Deceased))
					aDeaths.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.attackers.combined_roster.FindAll((p) => p.health == Health.Captured))
					aCaptures.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.defenders.combined_roster.FindAll((p) => p.health == Health.Injured))
					dInjuries.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.defenders.combined_roster.FindAll((p) => p.health == Health.Down))
					dDowned.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.defenders.combined_roster.FindAll((p) => p.health == Health.Deceased))
					dDeaths.PackStart(parahuman.GetHeader(context.butCompact));
				foreach (Parahuman parahuman in battle.defenders.combined_roster.FindAll((p) => p.health == Health.Captured))
					dCaptures.PackStart(parahuman.GetHeader(context.butCompact));

				casualties.Attach(aInjuries, 0, 1, 2, 3);
				casualties.Attach(dInjuries, 2, 3, 2, 3);
				casualties.Attach(aDowned, 0, 1, 4, 5);
				casualties.Attach(dDowned, 2, 3, 4, 5);
				casualties.Attach(aDeaths, 0, 1, 6, 7);
				casualties.Attach(dDeaths, 2, 3, 6, 7);
				casualties.Attach(aCaptures, 0, 1, 8, 9);
				casualties.Attach(dCaptures, 2, 3, 8, 9);

				mainBox.PackStart(casualties);
				popup.Add(mainBox);

			}

			return popup;

		}

	}
}
