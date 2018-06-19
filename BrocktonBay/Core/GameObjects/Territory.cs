using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public class TerritoryData {

		public string name = "New Territory";
		public int ID = 0;
		public IntVector2 location = new IntVector2(0, 0);
		public int size = 0;
		public int reputation = 0;
		public List<int> structures = new List<int>();

		public TerritoryData () { }

		public TerritoryData (Territory territory) {
			name = territory.name;
			ID = territory.ID;
			location = territory.location;
			size = territory.size;
			reputation = territory.reputation;
			structures = territory.structures.ConvertAll((structure) => structure.ID);
		}

	}

	public class Territory : GameObject, IContainer, EventLocation, Affiliated {

		public override int order { get { return 2; } }

		[Displayable(2, typeof(IntVector2Field))]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public Agent affiliation { get { return (Agent)parent; } }

		[Displayable(4, typeof(IntField))]
		public int size { get; set; }

		[Displayable(5, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(6, typeof(CellObjectListField<Structure>), 2), Emphasized]
		public List<Structure> structures { get; set; }

		[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public GameEvent ongoing_event { get; set; }

		[Displayable(7, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly]
		public GameAction attack { get; set; }

		public Territory () : this(new TerritoryData()) { }

		public Territory (TerritoryData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			size = data.size;
			reputation = data.reputation;
			structures = data.structures.ConvertAll((structure) => MainClass.city.Get<Structure>(structure));
			foreach (Structure structure in structures) {
				DependencyManager.Connect(structure, this);
				structure.parent = this;
			}
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate {
					ongoing_event = new GameEvent(this);
					DependencyManager.Connect(ongoing_event, this);
					DependencyManager.Flag(ongoing_event);
					DependencyManager.TriggerAllFlags();
				},
				condition = delegate (Context context) {
					return ongoing_event == null;
				}
			};

		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(Threat.C, Graphics.GetColor(affiliation), MainClass.textSize),
								 false, false, (uint)(MainClass.textSize / 5));
				return new InspectableBox(header, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = Graphics.GetSmartHeader(context.butCompact, parent) });
				return headerBox;
			}
		}

		public override Widget GetCell (Context context) {

			//Creates the cell contents
			VBox structureBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Structure structure in structures) {
				InspectableBox header = (InspectableBox)structure.GetHeader(context.butCompact);
				header.DragEnd += delegate {
					Remove(structure);
					DependencyManager.TriggerAllFlags();
				};
				structureBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = structureBox, VisibleWindow = false };
			Drag.DestSet(eventBox, DestDefaults.All,
						 new TargetEntry[] { new TargetEntry(typeof(Structure).ToString(), TargetFlags.App, 0) },
						 Gdk.DragAction.Move);
			eventBox.DragDataReceived += delegate {
				if (Accepts(DragTmpVars.currentDragged)) {
					Add(DragTmpVars.currentDragged);
					DependencyManager.TriggerAllFlags();
				}
			};

			return new Gtk.Alignment(0, 0, 1, 1) { Child = eventBox, BorderWidth = 7 };
			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

		public override bool Accepts (object obj) => obj is Structure;
		public override bool Contains (object obj) => obj is Structure && structures.Contains((Structure)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				structures.Add((Structure)obj);
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				structures.Remove((Structure)obj);
				obj.parent = null;
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void Reload () {
			structures.Sort();
		}


	}
}
