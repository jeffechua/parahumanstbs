using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public class TerritoryMarker : InspectableBox, IDependable {

		public int order { get { return 3; } }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;

		public static readonly int markerWidth = 30;
		public static readonly int markerHeight = 50;
		public Image markerImage;
		public Image zone;
		public Widget line;
		public Window popup;
		public ClickableEventBox alert;
		public Vector2 scaledPosition;

		public Territory territory;
		public IAgent affiliation;
		public IntVector2 location; //We round this to prevent floating point precision errors
		public int size;

		public TerritoryMarker (Territory territory, Map map) : base(territory) {

			this.map = map;

			this.territory = territory;
			affiliation = territory.affiliation;
			location = territory.location;
			size = territory.size;

			map.stage.Put(this, 0, 0);
			Redraw();
			Rezone();
			Repin();
			VisibleWindow = false;

			line = new HSeparator();
			line.SetSizeRequest(markerWidth * 2, 4);

			EnterNotifyEvent += delegate {
				if (popup != null) popup.Destroy();
				popup = new TerritoryPopup(this);
				map.stage.Put(line, (int)scaledPosition.x, (int)scaledPosition.y - markerHeight + markerWidth / 2 - 2);
				line.GdkWindow.Raise();
				line.ShowAll();
			};
			LeaveNotifyEvent += delegate {
				if (popup != null) {
					popup.Destroy();
					popup = null;
				}
				map.stage.Remove(line);
			};

			DependencyManager.Connect(territory, this);
			DependencyManager.Connect(Game.UIKey, this);

		}

		public void Redraw () {
			markerImage = Graphics.GetLocationPin(Graphics.GetColor(affiliation), markerWidth, markerHeight);
			if (Child != null) Remove(Child);
			Add(markerImage);
		}

		public void Rezone () {
			if (zone != null) map.stage.Remove(zone);
			int radius = (int)(size * Game.city.territorySizeScale * map.currentMagnif);
			zone = Graphics.GetCircle(Graphics.GetColor(affiliation), 50, radius);
			Vector2 zonePosition = scaledPosition - new Vector2(radius, radius);
			map.stage.Put(zone, (int)zonePosition.x, (int)zonePosition.y);
		}

		public void Repin () {
			scaledPosition = location * map.currentMagnif;
			Vector2 stagePosition = scaledPosition - new Vector2(markerWidth / 2, markerHeight);
			Vector2 zonePosition = scaledPosition - new Vector2(1, 1) * size * Game.city.territorySizeScale;
			map.stage.Move(this, (int)stagePosition.x, (int)stagePosition.y);
			map.stage.Move(zone, (int)zonePosition.x, (int)zonePosition.y);
		}

		public void Reload () {
			if (affiliation != territory.affiliation) {
				affiliation = territory.affiliation;
				Redraw();
				Rezone();
			}
			if (size != territory.size) {
				size = territory.size;
				Rezone();
			}
			if (location != territory.location) {
				location = territory.location;
				Repin();
			}
			if (territory.attacker != null) {
				if (alert != null) alert.Destroy();
				alert = Core.Map.NewAlert(territory);
				Vector2 pos = scaledPosition - new Vector2(markerHeight / 2, markerHeight * 2 + markerWidth * 1 / 3);
				map.stage.Put(alert, (int)pos.x, (int)pos.y);
			} else if (alert != null) {
				map.stage.Remove(alert);
				alert.Destroy();
				alert = null;
			}
		}

	}

	public class TerritoryPopup : Window {

		Territory territory;

		public TerritoryPopup (TerritoryMarker marker) : base(WindowType.Popup) {
			territory = marker.territory;
			Gravity = Gdk.Gravity.West;
			TransientFor = (Window)marker.map.Toplevel;

			Context context = new Context(Game.player, territory, true, false);

			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(territory.GetHeader(context.butCompact), 0.5f, 0, 0, 1), false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (territory.affiliation == null) {
				affiliationBox.PackStart(new Label("None"));
			} else {
				affiliationBox.PackStart(territory.affiliation.GetHeader(context));
			}
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Size: " + territory.size), 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Reputation: " + territory.reputation), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(new TabularContainerField(territory.GetType().GetProperty("combat_buffs"), territory, context,
			                                            new object[] { "strength_buff", "stealth_buff", "insight_buff" }));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(new TabularContainerField(territory.GetType().GetProperty("incomes"), territory, context,
			                                            new object[] { "resource_income", "reputation_income" }));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(new CellTabularListField<Structure>(territory.GetType().GetProperty("structures"), territory, context, 2));
			Add(mainBox);

			marker.GdkWindow.GetOrigin(out int x, out int y);
			Graphics.SetAllocationTrigger(this, delegate {
				Move(x + marker.Allocation.Right + TerritoryMarker.markerWidth * 3 / 2,
					 y + marker.Allocation.Top + TerritoryMarker.markerHeight / 2 - Allocation.Height / 4);
			});

			ShowAll();
		}
	}

}