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

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			location = structure.location;
			type = structure.type;
		}

	}

	public class Structure : GameObject {

		public override int order { get { return 1; } }

		[Displayable(2, typeof(IntVector2Field))]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public Faction affiliation { get { return (parent == null) ? null : (Faction)parent.parent; } }

		[Displayable(4, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			type = data.type;
		}

		public override Widget GetHeader (bool compact) {

			if (compact) {

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
					row2.PackStart(parent.GetHeader(true), false, false, 0);
					if (parent.parent != null) {
						row2.PackStart(new VSeparator(), false, false, 10);
						row2.PackStart(parent.parent.GetHeader(true), false, false, 0);
					}
					row2.PackStart(new Label(), true, true, 0);
					headerBox.PackStart(row2);
				}

				return headerBox;

			}
		}

	}
}
