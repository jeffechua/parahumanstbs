using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public sealed class BattleAlertMarker : InspectableMapMarker {

		int size;
		public override int order { get { return 6; } }

		IBattleground battleground;
		IAgent attackingAgent;
		IAgent defendingAgent;
		Phase phase;
		IntVector2 shownPosition;
		bool shownRelevance;

		Vector2 _offset;
		public override int layer { get => 2; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => _offset; }
		protected override Vector2 lineOffset { get => _offset + new Vector2(size / 2, size / 2); }
		public override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => size * 2; }

		public BattleAlertMarker (IBattleground battleground, Map map) : base(battleground, map, false) {
			this.battleground = battleground;
			if (battleground is Structure) {
				size = StructureMarker.markerSize * 6 / 5;
				_offset = new Vector2(-size / 2, -StructureMarker.markerSize * 2);
			} else {
				size = TerritoryMarker.markerHeight;
				_offset = new Vector2(-size / 2, -TerritoryMarker.markerHeight - size - TerritoryMarker.markerWidth / 3);
			}
			MyDragDrop.DestSet(this, typeof(Parahuman).Name, typeof(Team).Name);
			MyDragDrop.DestSetDropAction(this, AttemptDrag);
			DependencyManager.Connect(battleground, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		void AttemptDrag (object obj) {
			if (Deployment.StaticAccepts(obj) && (obj as GameObject).affiliation == Game.player) {
				if (Game.phase == Phase.Action) {
					if (battleground.attackers == null) battleground.attack.action(new Context(battleground, Game.player));
					battleground.attackers.Add(obj);
					DependencyManager.TriggerAllFlags();
				} else if (Game.phase == Phase.Response) {
					if (battleground.defenders == null) battleground.defend.action(new Context(battleground, Game.player));
					battleground.defenders.Add(obj);
					DependencyManager.TriggerAllFlags();
				}
			}
		}

		public override void Redraw () {

			if (Child != null) Child.Destroy();

			if (battleground.attackers == null)
				return;

			Add(Graphics.GetAlert(battleground, size));

			if (battleground.battle != null) {
				inspected = null;
			} else if (battleground.defenders != null) {
				inspected = battleground.defenders;
			} else if (battleground.attackers != null) {
				inspected = battleground.attackers;
			}

			ShowAll();

		}


		protected override void OnClicked (object obj, ButtonReleaseEventArgs args) {
			if (battleground.battle == null) { //This is a battle
				base.OnClicked(obj, args);
			} else {
				battleground.battle.GenerateInterface();
			}
		}

		protected override void OnMiddleClicked (object obj, ButtonReleaseEventArgs args) {
			if (battleground.battle == null) { //This is a battle
				base.OnMiddleClicked(obj, args);
			} else {
				battleground.battle.GenerateInterface();
			}
		}

		protected override void OnDoubleClicked (object obj, ButtonPressEventArgs args) {
			if (battleground.battle == null) { //This is a battle
				base.OnDoubleClicked(obj, args);
			} else {
				battleground.battle.GenerateInterface();
			}
		}

		public override void Reload () {
			IAgent newAttackingAgent = battleground.attackers == null ? null : battleground.attackers.affiliation;
			IAgent newDefendingAgent = battleground.defenders == null ? null : battleground.defenders.affiliation;
			if (attackingAgent != newAttackingAgent || defendingAgent != newDefendingAgent ||
				Game.phase != phase || Battle.Relevant(battleground, Game.player) != shownRelevance) {
				attackingAgent = newAttackingAgent;
				defendingAgent = newDefendingAgent;
				shownRelevance = Battle.Relevant(battleground, Game.player);
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
			if (attackingAgent == null) return null;
			switch (Game.phase) {
				case Phase.Action:
					return null;
				case Phase.Response:
					return defendingAgent == null ? null : GenerateDeploymentPopup(battleground.attackers);
				default:
					return null;
			}
		}

		protected override Window GenerateRightPopup () {
			if (attackingAgent == null) return null;
			switch (Game.phase) {
				case Phase.Action:
					return GenerateDeploymentPopup(battleground.attackers);
				case Phase.Response:
					return GenerateDeploymentPopup(defendingAgent == null ?
												   (Deployment)battleground.attackers :
												   (Deployment)battleground.defenders);
				default:
					return GenerateBattlePopup(battleground.battle);
			}
		}

		Window GenerateDeploymentPopup (Deployment deployment) {

			Window popup = new Window(WindowType.Popup) { TransientFor = (Window)map.Toplevel };
			Context depContext = new Context(deployment);
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
				affiliationBox.PackStart(deployment.affiliation.GetHeader(depContext.butCompact));
			}
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Threat: " + deployment.threat), 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Force Employed: " + deployment.force_employed), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);

			List<Widget> cells = new List<Widget>();
			cells.AddRange(deployment.teams.ConvertAll((team) => new Cell(depContext, team)));
			cells.AddRange(deployment.independents.ConvertAll((team) => new Cell(depContext, team)));
			mainBox.PackStart(new DynamicTable(cells, 2));

			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox stats = new HBox(false, 2);
			stats.PackStart(new Label {
				UseMarkup = true,
				Markup = "<b>Strength</b>" + Graphics.Shrink("\n\n", 6) + deployment.base_stats[0].formattedResult,
				Justify = Justification.Right
			}, true, true, 0);
			stats.PackStart(new VSeparator(), false, false, 0);
			stats.PackStart(new Label {
				UseMarkup = true,
				Markup = "<b>Stealth</b>" + Graphics.Shrink("\n\n", 6) + deployment.base_stats[1].formattedResult,
				Justify = Justification.Center
			}, true, true, 0);
			stats.PackStart(new VSeparator(), false, false, 0);
			stats.PackStart(new Label {
				UseMarkup = true,
				Markup = "<b>Insight</b>" + Graphics.Shrink("\n\n", 6) + deployment.base_stats[2].formattedResult,
				Justify = Justification.Left
			}, true, true, 0);
			mainBox.PackStart(stats, false, false, 0);
			mainBox.PackStart(new HSeparator(), false, false, 5);

			mainBox.PackStart(UIFactory.Fabricate(deployment, "ratings", new Context(deployment, Game.player)));

			popup.Add(mainBox);
			return popup;

		}

		Window GenerateBattlePopup (Battle battle) {

			Window popup = new Window(WindowType.Popup) {
				Gravity = Gdk.Gravity.West,
				TransientFor = (Window)map.Toplevel
			};
			Context battleContext = new Context(battle);
			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };

			Label name = new Label(battle.name);
			name.SetAlignment(0.5f, 1);
			mainBox.PackStart(name, false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(battle, "victor_display", battleContext));
			mainBox.PackStart(new HSeparator(), false, false, 7);

			Context compactContext = battleContext.butCompact;

			if (battle.defenders == null) {

				VBox attackersBox = new VBox();
				mainBox.PackStart(attackersBox);
				for (int i = 0; i < battle.attackers.combined_roster.Count; i++) {
					Parahuman parahuman = battle.attackers.combined_roster[i];
					HBox parahumanBox = new HBox(false, 2);
					parahumanBox.PackStart(parahuman.GetHeader(compactContext), true, true, 0);
					int delta = battle.pDeltaR[0][i];
					parahumanBox.PackStart(new Label(delta >= 0 ? ("+" + delta) : delta.ToString()), false, false, 0);
					attackersBox.PackStart(parahumanBox);
				}

			} else {

				HBox hBox = new HBox();
				VBox attackersBox = new VBox();
				VBox defendersBox = new VBox();
				hBox.PackStart(attackersBox, true, true, 0);
				hBox.PackStart(new VSeparator(), false, false, 4);
				hBox.PackStart(defendersBox, true, true, 0);
				mainBox.PackStart(hBox);

				System.Console.WriteLine(battle.attackers.combined_roster.Count);
				for (int i = 0; i < battle.attackers.combined_roster.Count; i++) {
					Parahuman parahuman = battle.attackers.combined_roster[i];
					HBox parahumanBox = new HBox(false, 2);
					parahumanBox.PackStart(parahuman.GetHeader(compactContext), true, true, 0);
					int delta = battle.pDeltaR[0][i];
					parahumanBox.PackStart(new Label(delta >= 0 ? ("+" + delta) : delta.ToString()), false, false, 0);
					attackersBox.PackStart(parahumanBox);
				}

				for (int i = 0; i < battle.defenders.combined_roster.Count; i++) {
					Parahuman parahuman = battle.defenders.combined_roster[i];
					HBox parahumanBox = new HBox(false, 2);
					parahumanBox.PackStart(parahuman.GetHeader(compactContext), true, true, 0);
					int delta = battle.pDeltaR[1][i];
					parahumanBox.PackStart(new Label(delta >= 0 ? ("+" + delta) : delta.ToString()), false, false, 0);
					defendersBox.PackStart(parahumanBox);
				}

			}

			mainBox.PackStart(new Label { HeightRequest = 3 });

			HBox territoryBox = new HBox();
			GameObject disreputedTerritory = GameObject.TryCast(battleground, out Structure structure) ? structure.parent : (Territory)battleground;
			territoryBox.PackStart(disreputedTerritory.GetHeader(compactContext));
			string deltaText = battle.tDeltaR >= 0 ? ("+" + battle.tDeltaR) : battle.tDeltaR.ToString();
			territoryBox.PackStart(new Label(" " + deltaText + " reputation."));

			mainBox.PackStart(territoryBox);

			popup.Add(mainBox);
			return popup;

		}

	}
}
