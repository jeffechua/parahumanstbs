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
		public ClickableEventBox eventIndicator;
		public Vector2 scaledPosition;

		public Structure structure;
		public IntVector2 location; //We round this to prevent floating point precision errors
		public IAgent affiliation;
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
				map.stage.Put(line, (int)scaledPosition.x, (int)scaledPosition.y - 2);
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
			markerImage = Graphics.GetIcon(structure.type, Graphics.GetColor(affiliation), markerSize, true);
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
			if (structure.ongoing_event == null && eventIndicator != null) {
				map.stage.Remove(eventIndicator);
				eventIndicator.Destroy();
				eventIndicator = null;
			} else if (structure.ongoing_event != null && eventIndicator == null) {
				int iconSize = markerSize * 6 / 5;
				Image icon = Graphics.GetIcon(structure.ongoing_event.type, new Gdk.Color(230, 120, 0), iconSize);
				eventIndicator = new ClickableEventBox { Child = icon };
				eventIndicator.Clicked += delegate {
					SecondaryWindow eventWindow = new SecondaryWindow("Event at " + structure.name);
					eventWindow.SetMainWidget(new EventInterface(structure.ongoing_event));
					eventWindow.ShowAll();
				};
				eventIndicator.VisibleWindow = false;
				Vector2 pos = scaledPosition - new Vector2(iconSize / 2, markerSize * 2);
				map.stage.Put(eventIndicator, (int)pos.x, (int)pos.y);
			}
		}

	}

	public class StructurePopup : Window {

		Structure structure;

		public StructurePopup (StructureMarker marker) : base(WindowType.Popup) {
			structure = marker.structure;
			TransientFor = (Window)marker.map.Toplevel;

			Context context = new Context(MainClass.playerAgent, structure, true, false);

			VBox mainBox = new VBox(false, 2) { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(structure.GetHeader(context), 0.5f, 0, 0, 1), false, false, 3);
			mainBox.PackStart(new HSeparator(), false, false, 5);
			HBox affiliationBox = new HBox();
			affiliationBox.PackStart(new Label("Affiliation: "));
			if (structure.affiliation == null) {
				affiliationBox.PackStart(new Label("None"));
			} else {
				affiliationBox.PackStart(structure.affiliation.GetHeader(context.butCompact));
			}
			mainBox.PackStart(UIFactory.Align(affiliationBox, 0, 0, 0, 1));
			mainBox.PackStart(UIFactory.Align(Child = new Label("Type: " + structure.type), 0, 0, 0, 1));
			mainBox.PackStart(new HSeparator(), false, false, 5);
			mainBox.PackStart(new TabularContainerField(structure.GetType().GetProperty("buffs"), structure, context,
														new object[] { "strength_buff", "resource_buff", "reputation_buff" }));
			Add(mainBox);

			marker.GdkWindow.GetOrigin(out int x, out int y);
			Graphics.SetAllocationTrigger(this, delegate {
				Move(x + marker.Allocation.Right + StructureMarker.markerSize * 3 / 2,
					 y + marker.Allocation.Top + StructureMarker.markerSize / 2 - Allocation.Height / 4);
			});

			ShowAll();
		}
	}

}