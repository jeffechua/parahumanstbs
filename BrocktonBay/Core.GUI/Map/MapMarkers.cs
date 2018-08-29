﻿using System;
using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public interface MapMarked {
		IMapMarker[] GetMarkers (Map map);
	}

	public interface IMapMarker {
		int layer { get; }
		bool magnifRedraw { get; }
		void Repin ();
		void Redraw ();
	}

	public abstract class NonInteractiveMapMarker : Gtk.Alignment, IMapMarker, IDependable {

		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;
		protected Vector2 scaledPosition;

		public abstract int layer { get; }
		protected abstract Vector2 position { get; }
		protected abstract Vector2 offset { get; }
		public abstract bool magnifRedraw { get; }

		public NonInteractiveMapMarker (Map map) : base(0, 0, 1, 1) {
			this.map = map;
		}

		public abstract void Reload ();
		public abstract void Redraw ();
		public void Repin () {
			if (Parent == map.stage) map.stage.Remove(this);
			scaledPosition = position * map.currentMagnif;
			Vector2 markerCoords = scaledPosition + offset;
			map.stage.Put(this, (int)markerCoords.x, (int)markerCoords.y);
		}

	}

	public abstract class InspectableMapMarker : InspectableBox, IMapMarker, IDependable {

		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;
		protected Vector2 scaledPosition;
		Window leftPopup;
		Window rightPopup;
		HSeparator rline;
		HSeparator lline;
		Image node;

		public abstract int layer { get; }
		protected abstract Vector2 position { get; }
		protected abstract Vector2 offset { get; }
		protected abstract Vector2 lineOffset { get; }
		public abstract bool magnifRedraw { get; }
		protected abstract int popupDistance { get; }

		public InspectableMapMarker (IGUIComplete obj, Map map, bool destructible = true, bool draggable = true) : base(obj, destructible, draggable) {
			VisibleWindow = false;
			this.map = map;
			node = Graphics.GetIcon(Threat.C, new Gdk.Color(255, 255, 255), 8);
			EnterNotifyEvent += MouseEnter;
			LeaveNotifyEvent += MouseLeave;
		}

		void MouseEnter (object obj, EnterNotifyEventArgs args) {

			if (leftPopup != null) leftPopup.Destroy();
			if (lline != null) lline.Destroy();
			if (rightPopup != null) rightPopup.Destroy();
			if (rline != null) rline.Destroy();
			if (node.Parent == map.stage) map.stage.Remove(node);

			GdkWindow.GetOrigin(out int x, out int y);
			Vector2 nodeFixedPosition = scaledPosition + lineOffset;
			Vector2 nodeScreenPosition = new Vector2(x + Allocation.Left, y + Allocation.Top) + lineOffset - offset;
			//This is the screen coordinates of the point defined by lineOffset — where the line(s) begin and the node sits.

			leftPopup = GenerateLeftPopup();
			if (leftPopup != null) {
				lline = new HSeparator();
				lline.SetSizeRequest(popupDistance, 4);
				map.stage.Put(lline, (int)nodeFixedPosition.x - popupDistance, (int)nodeFixedPosition.y - 2);
				lline.ShowAll();
				Graphics.SetAllocTrigger(leftPopup, delegate {
					leftPopup.GetSize(out int width, out int height);
					Vector2 truePosition = nodeScreenPosition + new Vector2(-popupDistance - width, -height / 4);
					leftPopup.Move((int)truePosition.x, (int)truePosition.y);
				});
				leftPopup.ShowAll();
			}

			rightPopup = GenerateRightPopup();
			if (rightPopup != null) {
				rline = new HSeparator();
				rline.SetSizeRequest(popupDistance, 4);
				map.stage.Put(rline, (int)nodeFixedPosition.x, (int)nodeFixedPosition.y - 2);
				rline.ShowAll();
				Graphics.SetAllocTrigger(rightPopup, delegate {
					rightPopup.GetSize(out int width, out int height);
					Vector2 truePosition = nodeScreenPosition + new Vector2(popupDistance, -height / 4);
					rightPopup.Move((int)truePosition.x, (int)truePosition.y);
				});
				rightPopup.ShowAll();
			}

			if (rightPopup != null && leftPopup != null) {
				map.stage.Put(node, (int)nodeFixedPosition.x - 4, (int)nodeFixedPosition.y - 4);
				node.ShowAll();
			}

		}

		void MouseLeave (object obj, LeaveNotifyEventArgs args) {
			if (leftPopup != null) {
				leftPopup.Destroy();
				leftPopup = null;
			}
			if (rightPopup != null) {
				rightPopup.Destroy();
				rightPopup = null;
			}
			if (rline != null) {
				rline.Destroy();
				rline = null;
			}
			if (lline != null) {
				lline.Destroy();
				lline = null;
			}
			if (node.Parent == map.stage) map.stage.Remove(node);
		}

		public void Repin () {
			if (Parent == map.stage) map.stage.Remove(this);
			scaledPosition = position * map.currentMagnif;
			Vector2 markerCoords = scaledPosition + offset;
			map.stage.Put(this, (int)markerCoords.x, (int)markerCoords.y);
		}

		public abstract void Redraw ();

		protected virtual Window GenerateLeftPopup () => null;
		protected virtual Window GenerateRightPopup () => null;
		public abstract void Reload ();

	}

	public sealed class StructureMarker : InspectableMapMarker {

		public const int markerSize = 25;
		public override int order { get { return 2; } }

		Structure structure;
		Gdk.Color shownColor;
		StructureType shownType;
		IntVector2 shownPosition;

		public override int layer { get => 2; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-markerSize / 2, -markerSize / 2); }
		protected override Vector2 lineOffset { get => new Vector2(); }
		public override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => markerSize * 2; }

		public StructureMarker (Structure structure, Map map) : base(structure, map) {
			this.structure = structure;
			DependencyManager.Connect(structure, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetIcon(structure.type, shownColor, markerSize, true));
			ShowAll();
		}

		public override void Reload () {
			if (!structure.affiliation.color.Equal(shownColor) || structure.type != shownType) {
				shownColor = structure.affiliation.color;
				shownType = structure.type;
				Redraw();
			}
			if (structure.position != shownPosition) {
				shownPosition = structure.position;
				Repin();
			}
			if (!deletable && Game.omnipotent) EnableDelete();
			if (deletable && !Game.omnipotent) DisableDelete();
		}

		protected override Window GenerateRightPopup () {
			Window popup = new Window(WindowType.Popup) { TransientFor = (Window)map.Toplevel };
			Context context = new Context(Game.player, structure, true, false);
			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(structure.GetHeader(context), 0.5f, 0, 0, 1), false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (structure.affiliation == null)
				affiliationBox.PackStart(new Label("None"));
			else
				affiliationBox.PackStart(structure.affiliation.GetHeader(context.butCompact));
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Type: " + structure.type), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(structure, "combat_buffs", context));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(structure, "incomes", context));
			popup.Add(mainBox);
			return popup;
		}

	}

	public sealed class TerritoryMarker : InspectableMapMarker {

		public const int markerWidth = 30;
		public const int markerHeight = 50;
		public override int order { get { return 3; } }

		Territory territory;
		Gdk.Color shownColor;
		IntVector2 shownPosition;

		public override int layer { get => 2; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-markerWidth / 2, -markerHeight); }
		protected override Vector2 lineOffset { get => new Vector2(0, -markerHeight + markerWidth / 2); }
		public override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => markerWidth * 2; }

		public TerritoryMarker (Territory territory, Map map) : base(territory, map) {
			this.territory = territory;
			DependencyManager.Connect(territory, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetLocationPin(shownColor, markerWidth, markerHeight));
			ShowAll();
		}

		public override void Reload () {
			if (!territory.affiliation.color.Equal(shownColor)) {
				shownColor = territory.affiliation.color;
				Redraw();
			}
			if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
			if (!deletable && Game.omnipotent) EnableDelete();
			if (deletable && !Game.omnipotent) DisableDelete();
		}

		protected override Window GenerateRightPopup () {
			Window popup = new Window(WindowType.Popup) { TransientFor = (Window)map.Toplevel };
			Context context = new Context(Game.player, territory, true, false);
			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(territory.GetHeader(context.butCompact), 0.5f, 0, 0, 1), false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (territory.affiliation == null)
				affiliationBox.PackStart(new Label("None"));
			else
				affiliationBox.PackStart(territory.affiliation.GetHeader(context.butCompact));
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Size: " + territory.size), 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Reputation: " + territory.reputation), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(territory, "combat_buffs", context));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(territory, "incomes", context));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(territory, "structures", context));
			popup.Add(mainBox);
			return popup;
		}

	}

	public sealed class TerritoryZoneMarker : NonInteractiveMapMarker {

		public override int order { get { return 3; } }

		Territory territory;
		Gdk.Color shownColor;
		int shownSize;
		IntVector2 shownPosition;

		int radius { get => (int)(territory.size * Game.city.territorySizeScale * map.currentMagnif); }

		public override int layer { get => 1; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-radius, -radius); }
		public override bool magnifRedraw { get => true; }

		public TerritoryZoneMarker (Territory territory, Map map) : base(map) {
			this.territory = territory;
			DependencyManager.Connect(territory, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetCircle(shownColor, 30, radius));
			ShowAll();
		}

		public override void Reload () {
			if (!territory.affiliation.color.Equal(shownColor) || territory.size != shownSize) {
				shownColor = territory.affiliation.color;
				shownSize = territory.size;
				shownPosition = territory.position;
				Redraw();
				Repin();
			} else if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
		}
	}


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