using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public class StructureMarker : InspectableBox, IDependable {

		public int order { get { return 2; } }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;

		public static readonly int markerSize = 25;
		public Image markerImage;
		public Widget line;
		public Window popup;
		public Vector2 scaledPosition;

		public Structure structure;
		public IntVector2 location; //We round this to prevent floating point precision errors
		public Faction affiliation;
		public StructureType type;

		public StructureMarker (Structure structure, Map map) : base(structure) {

			this.map = map;

			this.structure = structure;
			location = structure.location;
			affiliation = structure.affiliation;
			type = structure.type;

			map.stage.Put(this, 0, 0);
			Redraw();
			Repin();
			VisibleWindow = false;

			line = new HSeparator();
			line.SetSizeRequest(markerSize * 2, 4);

			EnterNotifyEvent += delegate {
				if (popup != null) popup.Destroy();
				popup = new StructurePopup(this);
				map.stage.Put(line, (int)scaledPosition.x, (int)scaledPosition.y-2);
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

			DependencyManager.Connect(structure, this);
			if (structure.parent != null) DependencyManager.Connect(structure.parent, this);

		}

		public void Redraw () {
			markerImage = Graphics.GetIcon(structure.type, Graphics.GetColor(affiliation), markerSize);
			if (Child != null) Remove(Child);
			Add(markerImage);
		}

		public void Repin () {
			scaledPosition = location * map.currentMagnif;
			Vector2 stagePosition = scaledPosition - new Vector2(markerSize / 2, markerSize / 2);
			map.stage.Move(this, (int)stagePosition.x, (int)stagePosition.y);
		}

		public void Reload () {
			if (affiliation != structure.affiliation || type != structure.type) {
				affiliation = structure.affiliation;
				type = structure.type;
				Redraw();
			}
			if (location != structure.location) {
				location = structure.location;
				Repin();
			}
		}

	}

	public class StructurePopup : Window {

		Structure structure;

		public StructurePopup (StructureMarker marker) : base(WindowType.Popup) {
			structure = marker.structure;
			Gravity = Gdk.Gravity.West;
			TransientFor = (Window)marker.map.Toplevel;

			Context context = new Context(structure, 0, true, true);

			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(new Gtk.Alignment(0.5f, 0, 0, 1) { Child = structure.GetHeader(context) }, false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (structure.affiliation == null) {
				affiliationBox.PackStart(new Label("None"));
			} else {
				affiliationBox.PackStart(structure.affiliation.GetHeader(context));
			}
			mainBox.PackStart(new Gtk.Alignment(0, 0, 0, 1) { Child = affiliationBox });
			mainBox.PackStart(new Gtk.Alignment(0, 0, 0, 1) { Child = new Label("Type: " + structure.type) });
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(new TabularLabeledValuesField<int>(structure.GetType().GetProperty("buffs"), structure, context, 2));
			Add(mainBox);

			((Window)marker.Toplevel).GdkWindow.GetOrigin(out int x, out int y);
			Move(x + marker.Allocation.Right + StructureMarker.markerSize * 3 / 2, y + marker.Allocation.Top + StructureMarker.markerSize / 2);

			ShowAll();
		}
	}

}