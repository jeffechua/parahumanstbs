using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public enum StructureType {
		Tactical = 0,
		Economic = 1,
		Aesthetic = 2
	}

	public class StructureData {

		public string name = "New Landmark";
		public int ID = 0;
		public IntVector2 location = new IntVector2(0, 0);
		public StructureType type;
		public int[] buffs = { 0, 0, 0 };

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			location = structure.location;
			type = structure.type;
			buffs = new int[] { structure.buffs[0].value, structure.buffs[1].value, structure.buffs[2].value };
		}

	}

	public class Structure : GameObject, EventLocation, Affiliated {

		public override int order { get { return 1; } }

		[Displayable(2, typeof(IntVector2Field))]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public Agent affiliation { get { return (parent == null) ? null : (Agent)parent.parent; } }

		[Displayable(4, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		[BimorphicDisplayable(7, typeof(TabularLabeledValuesField<int>), typeof(LinearLabeledValuesField<int>)), EmphasizedIfVertical]
		public LabeledValue<int>[] buffs { get; set; }

		[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public GameEvent ongoing_event { get; set; }

		[Displayable(7, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly]
		public GameAction attack { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			type = data.type;
			buffs = new LabeledValue<int>[]{
				new LabeledValue<int>("Combat Strength", data.buffs[0]),
				new LabeledValue<int>("Resource income", data.buffs[1]),
				new LabeledValue<int>("Reputation income", data.buffs[2])
			};
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
				header.PackStart(Graphics.GetIcon(type, Graphics.GetColor(affiliation), MainClass.textSize),
								 false, false, (uint)(MainClass.textSize / 5));
				return new InspectableBox(header, this);

			} else {

				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);

				if (parent != null) {
					HBox row2 = new HBox(false, 0);
					row2.PackStart(new Label(), true, true, 0);
					row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent), false, false, 0);
					if (parent.parent != null) {
						row2.PackStart(new VSeparator(), false, false, 10);
						row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent.parent), false, false, 0);
					}
					row2.PackStart(new Label(), true, true, 0);
					headerBox.PackStart(row2);
				}

				return headerBox;

			}
		}

		public override Widget GetCell (Context context) {
			Label label = new Label("Strength + " + buffs[0].value + "\nResources + " + buffs[1].value + "\nReputation + " + buffs[2].value);
			label.Justify = Justification.Left;
			return new Gtk.Alignment(0, 0, 1, 1) { Child = label, BorderWidth = 10 };
		}

		public override void Reload () {
		}

	}
}
