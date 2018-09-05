using System;
using Gtk;

namespace BrocktonBay {

	public sealed class StructureMarker : InspectableMapMarker {

		public const int markerSize = 25;
		public override int order { get { return 2; } }

		Structure structure;
		Gdk.Color shownColor;
		StructureType shownType;
		IntVector2 shownPosition = new IntVector2(-1,-1); //So Repin() triggers on first Reload();

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
			if (!Graphics.GetColor(structure.affiliation).Equal(shownColor) || structure.type != shownType) {
				shownColor = Graphics.GetColor(structure.affiliation);
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

	public sealed class StructureRebuildMarker : NonInteractiveMapMarker {

		public const int markerSize = 25;
		public override int order { get { return 2; } }

		Structure structure;
		int? rebuild_time;
		IntVector2 shownPosition = new IntVector2(-1, -1); //So Repin() triggers on first Reload();

		HBox hBox;

		public override int layer { get => 2; }
		protected override Vector2 position { get => shownPosition; }
		protected override Vector2 offset { get => new Vector2(-markerSize / 2, -markerSize / 2); }
		public override bool magnifRedraw { get => false; }

		public StructureRebuildMarker (Structure structure, Map map) : base(map) {
			this.structure = structure;
			hBox = new HBox(false, 0);
			Add(hBox);
			DependencyManager.Connect(structure, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public override void Redraw () {
			while (hBox.Children.Length > 0) hBox.Children[0].Destroy();
			if (rebuild_time > 0) {
				hBox.PackStart(Graphics.GetIcon(IconTemplate.X, new Gdk.Color(255, 0, 0), markerSize, true), false, false, 0);
				Label label = new Label {
					UseMarkup = true,
					Markup = "<b><span font_desc =\"" + markerSize * 4 / 5 + "\">" + rebuild_time + "</span></b>"
				};
				label.SetSizeRequest(markerSize, markerSize);
				label.SetAlignment(0.5f, 0.5f);
				hBox.PackStart(label, false, false, 0);
				ShowAll();
			}
		}

		public override void Reload () {
			if (rebuild_time != structure.rebuild_time) {
				rebuild_time = structure.rebuild_time;
				Redraw();
			}
			if (structure.position != shownPosition) {
				shownPosition = structure.position;
				Repin();
			}
		}

	}

}
