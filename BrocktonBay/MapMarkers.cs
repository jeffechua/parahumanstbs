using System;
using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public interface MapMarked {
		List<MapMarker> GetMarkers (Map map);
	}

	public abstract class MapMarker : InspectableBox, IDependable {

		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;
		protected Vector2 scaledPosition;
		Window leftPopup;
		Window rightPopup;

		protected abstract Vector2 position { get; }
		protected abstract Vector2 offset { get; }
		protected abstract bool magnifRedraw { get; }
		protected abstract int popupDistance { get; }

		public MapMarker (IGUIComplete obj, Map map) : base(obj) {

			this.map = map;
			VisibleWindow = false;

			HSeparator line1 = new HSeparator();
			line1.SetSizeRequest(popupDistance, 4);
			HSeparator line2 = new HSeparator();
			line2.SetSizeRequest(popupDistance, 4);

			EnterNotifyEvent += delegate {

				map.stage.Put(line1, (int)scaledPosition.x, (int)scaledPosition.y - 2);
				line1.GdkWindow.Raise();
				line1.ShowAll();
				map.stage.Put(line2, (int)scaledPosition.x - popupDistance, (int)scaledPosition.y - 2);
				line2.GdkWindow.Raise();
				line2.ShowAll();
				GdkWindow.Raise();

				if (leftPopup != null) leftPopup.Destroy();
				leftPopup = GenerateLeftPopup();
				GdkWindow.GetOrigin(out int x, out int y);
				Graphics.SetAllocationTrigger(leftPopup, () =>
					leftPopup.Move((int)(x + scaledPosition.x - offset.x - popupDistance - leftPopup.Allocation.Width),
								   (int)(y + scaledPosition.y - offset.y - leftPopup.Allocation.Height / 4)));
				leftPopup.ShowAll();

				if (rightPopup != null) rightPopup.Destroy();
				rightPopup = GenerateRightPopup();
				Graphics.SetAllocationTrigger(rightPopup, () =>
					rightPopup.Move((int)(x + scaledPosition.x - offset.x + popupDistance),
									(int)(y + scaledPosition.y - offset.y - rightPopup.Allocation.Height / 4)));
				rightPopup.ShowAll();

			};

			LeaveNotifyEvent += delegate {
				if (leftPopup != null) {
					leftPopup.Destroy();
					leftPopup = null;
				}
				if (rightPopup != null) {
					rightPopup.Destroy();
					rightPopup = null;
				}
				map.stage.Remove(line1);
				map.stage.Remove(line2);
			};

		}

		public virtual void Repin () {
			scaledPosition = position * map.currentMagnif;
			Vector2 markerCoords = scaledPosition + offset;
			map.stage.Move(this, (int)markerCoords.x, (int)markerCoords.y);
		}

		public abstract void Redraw ();

		protected virtual Window GenerateLeftPopup () => null;
		protected virtual Window GenerateRightPopup () => null;
		public abstract void Reload ();

	}

	public sealed class StructureMarker : MapMarker {

		public const int markerSize = 25;
		public override int order { get { return 2; } }

		Structure structure;
		IAgent shownAffiliation;
		StructureType shownType;
		IntVector2 shownPosition;

		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-markerSize / 2, -markerSize / 2); }
		protected override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => markerSize * 2; }

		public StructureMarker (Structure structure, Map map) : base(structure, map) {
			this.structure = structure;
			DependencyManager.Connect(structure, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetIcon(structure.type, Graphics.GetColor(structure.affiliation), markerSize, true));
			ShowAll();
		}

		public override void Reload () {
			if (structure.affiliation != shownAffiliation || structure.type != shownType) {
				shownAffiliation = structure.affiliation;
				shownType = structure.type;
				Redraw();
			}
			if (structure.position != shownPosition) {
				shownPosition = structure.position;
				Repin();
			}
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

	public sealed class TerritoryMarker : MapMarker {

		public const int markerWidth = 30;
		public const int markerHeight = 50;
		public override int order { get { return 3; } }

		Territory territory;
		IAgent shownAffiliation;
		IntVector2 shownPosition;

		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-markerWidth / 2, -markerHeight / 2); }
		protected override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => markerWidth * 2; }

		public TerritoryMarker (Territory territory, Map map) : base(territory, map) {
			this.territory = territory;
			DependencyManager.Connect(territory, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetLocationPin(Graphics.GetColor(territory.affiliation), markerWidth, markerHeight));
			ShowAll();
		}

		public override void Reload () {
			if (territory.affiliation != shownAffiliation) {
				shownAffiliation = territory.affiliation;
				Redraw();
			}
			if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
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
			Add(mainBox);
			return popup;
		}

	}

	public sealed class TerritoryZoneMarker : MapMarker {

		public override int order { get { return 3; } }

		Territory territory;
		IAgent shownAffiliation;
		int shownSize;
		IntVector2 shownPosition;

		int radius { get => (int)(territory.size * Game.city.territorySizeScale * map.currentMagnif); }

		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-radius, -radius); }
		protected override bool magnifRedraw { get => true; }
		protected override int popupDistance { get => 0; }

		public TerritoryZoneMarker (Territory territory, Map map) : base(territory, map) {
			this.territory = territory;
			active = false;
			DependencyManager.Connect(territory, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			if (Child != null) Remove(Child);
			Add(Graphics.GetCircle(Graphics.GetColor(shownAffiliation), 50, radius));
			ShowAll();
		}

		public override void Reload () {
			if (territory.affiliation != shownAffiliation || territory.size != shownSize) {
				shownAffiliation = territory.affiliation;
				shownSize = territory.size;
				Redraw();
			}
			if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
		}
	}


	public sealed class BattleAlertMarker : MapMarker {

		int size;
		public override int order { get { return 3; } }

		IBattleground battleground;
		bool attacked;
		bool defended;
		IntVector2 shownPosition;

		Vector2 _offset;
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => _offset; }
		protected override bool magnifRedraw { get => false; }
		protected override int popupDistance { get => size * 2; }

		public BattleAlertMarker (IBattleground battleground, Map map) : base(battleground, map) {
			destructible = false;
			this.battleground = battleground;
			if (battleground is Structure) {
				size = StructureMarker.markerSize * 6 / 5;
				_offset = new Vector2(size / 2, StructureMarker.markerSize * 2);
			} else {
				size = TerritoryMarker.markerHeight;
				_offset = new Vector2(size / 2, TerritoryMarker.markerHeight + size + TerritoryMarker.markerWidth / 3);
			}
			DependencyManager.Connect(battleground, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {

			if (Child != null) Child.Destroy();

			if (battleground.attacker == null)
				return;

			AlertIconType alertType = battleground.defender == null ? AlertIconType.Unopposed : AlertIconType.Opposed;
			Gdk.Color primaryColor = battleground.attacker.affiliation.color;
			Gdk.Color secondaryColor = battleground.defender == null ? primaryColor : battleground.defender.affiliation.color;
			Gdk.Color trim;
			if (Relevant(Game.player)) {
				trim = new Gdk.Color(0, 0, 0);
			} else {
				trim = new Gdk.Color(50, 50, 50);
				primaryColor.Red = (ushort)((primaryColor.Red + 150) / 2);
				primaryColor.Green = (ushort)((primaryColor.Green + 150) / 2);
				primaryColor.Blue = (ushort)((primaryColor.Blue + 150) / 2);
				secondaryColor.Red = (ushort)((secondaryColor.Red + 150) / 2);
				secondaryColor.Green = (ushort)((secondaryColor.Green + 150) / 2);
				secondaryColor.Blue = (ushort)((secondaryColor.Blue + 150) / 2);
			}
			Add(Graphics.GetAlert(alertType, size, primaryColor, secondaryColor, trim));

			if (!Relevant(Game.player)) {
				active = false;
				return;
			}

			if (Game.phase == Phase.Action || (Game.phase == Phase.Response && battleground.defender == null)) {
				inspected = battleground.attacker;
				return;
			} else if (Game.phase == Phase.Response) {
				inspected = battleground.defender;
				return;
			} else if (Game.phase == Phase.Mastermind) {
				inspected = null;
				if (battleground.battle == null) battleground.battle = new Battle(battleground, battleground.attacker, battleground.defender);
				Clicked += delegate {
					SecondaryWindow eventWindow = new SecondaryWindow("Battle at " + battleground.name);
					eventWindow.SetMainWidget(new BattleInterface(battleground.battle));
					eventWindow.ShowAll();
				};
				return;
			}

			ShowAll();
		}

		public bool Relevant (IAgent agent)
			=> battleground.affiliation == agent ||
					   (battleground.attacker != null && battleground.attacker.affiliation == agent) ||
					   (battleground.defender != null && battleground.defender.affiliation == agent);

		public override void Reload () {
			if ((battleground.attacker == null) != attacked) {
				attacked = battleground.attacker == null;
				Redraw();
			}
			if ((battleground.defender == null) != defended) {
				defended = battleground.defender == null;
				Redraw();
			}
			if (battleground.position != shownPosition) {
				shownPosition = battleground.position;
				Repin();
			}
		}

		protected override Window GenerateRightPopup () {
		}

	}

}
