using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public class LandmarkData {
		public string name = "New Landmark";
		public int ID = 0;
		public LandmarkData () {}
		public LandmarkData (Landmark landmark) {
			name = landmark.name;
			ID = landmark.ID;
		}
	}

	public class Landmark : GameObject {

		public override int order { get { return 1; } }

		public Landmark () : this(new LandmarkData()) {}

		public Landmark (LandmarkData data) {
			name = data.name;
			ID = data.ID;
		}

		public override Widget GetHeader (bool compact) {

			if (compact) {

				HBox header = new HBox(false, 0);
				Label icon = new Label(" ● ");
				//EnumTools.SetAllStates(icon, EnumTools.GetColor(health));
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(icon, false, false, 0);
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
