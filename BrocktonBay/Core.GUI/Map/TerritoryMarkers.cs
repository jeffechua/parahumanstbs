using Gtk;

namespace BrocktonBay {

	public sealed class TerritoryMarker : InspectableMapMarker {

		public const int markerWidth = 30;
		public const int markerHeight = 50;
		public override int order { get { return 3; } }

		Territory territory;
		Gdk.Color shownColor;
		IntVector2 shownPosition = new IntVector2(-1, -1); //So Repin() triggers on first Reload();

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
			if (!Graphics.GetColor(territory.affiliation).Equal(shownColor)) {
				shownColor = Graphics.GetColor(territory.affiliation);
				Redraw();
			}
			if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
		}

		public override void OnTriggerDestroyed (IDependable trigger) {
			if (trigger == territory) {
				Destroy();
				DependencyManager.DisconnectAll(this);
			}
		}
		public override void OnListenerDestroyed (IDependable listener) { }

		protected override Window GenerateRightPopup () {
			Window popup = new Window(WindowType.Popup) { TransientFor = (Window)map.Toplevel };
			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(territory.GetHeader(context), 0.5f, 0, 0, 1), false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (territory.affiliation == null)
				affiliationBox.PackStart(new Label("None"));
			else
				affiliationBox.PackStart(territory.affiliation.GetHeader(context));
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Size: " + territory.size), 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(new Label("Reputation: " + territory.reputation), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(territory, "combat_buffs", context.butNotCompact));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(UIFactory.Fabricate(territory, "incomes", context.butNotCompact));
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
		IntVector2 shownPosition = new IntVector2(-1, -1); //So Repin() triggers on first Reload();

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
			if (!Graphics.GetColor(territory.affiliation).Equal(shownColor) || territory.size != shownSize) {
				shownColor = territory.affiliation.color;
				shownSize = territory.size;
				Redraw();
			}
			if (territory.position != shownPosition) {
				shownPosition = territory.position;
				Repin();
			}
		}

		public override void OnTriggerDestroyed (IDependable trigger) {
			if (trigger == territory) {
				Destroy();
				DependencyManager.DisconnectAll(this);
			}
		}
		public override void OnListenerDestroyed (IDependable listener) { }

	}

}
