using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public enum Health {
		Deceased = 0,
		Down = 1,
		Injured = 2,
		Healthy = 3
	}

	public sealed class ParahumanData {
		public String name = "New Trigger";
		public int ID = 0;
		public string civilianName = "";
		public Alignment alignment = Alignment.Rogue;
		public Threat threat = Threat.C;
		public Health health = Health.Healthy;
		public int reputation = 0;
		public float[,] ratings = new float[5,9];

		public ParahumanData () { }

		public ParahumanData (Parahuman parahuman) {
			name = parahuman.name;
			ID = parahuman.ID;
			civilianName = parahuman.civilian_name;
			alignment = parahuman.alignment;
			threat = parahuman.threat;
			health = parahuman.health;
			reputation = parahuman.reputation;
			ratings = parahuman.ratings.values;
		}

	}

	public sealed class Parahuman : GameObject, Rated {

		public override int order { get { return 1; } }

		[Displayable(2, typeof(StringField))]
		public String civilian_name { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public Faction affiliation { get { return (parent==null)?null:(Faction)parent.parent; } }

		[Displayable(4, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(5, typeof(EnumField<Threat>))]
		public Threat threat { get; set; }

		[Displayable(6, typeof(EnumField<Health>))]
		public Health health { get; set; }

		[Displayable(7, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(8, typeof(RatingListField)), Padded(5, 5), EmphasizedIfHorizontal]
		public RatingsProfile ratings { get; set; }

		public Parahuman () : this(new ParahumanData()) { }

		public Parahuman (ParahumanData data) {
			name = data.name;
			ID = data.ID;
			civilian_name = data.civilianName;
			alignment = data.alignment;
			threat = data.threat;
			health = data.health;
			ratings = new RatingsProfile(data.ratings);
		}

		public override void Sort () {
		}

		public override Widget GetHeader (bool compact) {

			if (compact) {

				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, Graphics.GetColor(health)), false, false, (uint)(MainClass.textSize / 5));
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

		public override Widget GetCell () {
			
			VBox ratingsBox = new VBox(false, 0) { BorderWidth = 5 };

			for (int i = 1; i <= 8; i++) {
				if (ratings.values[0, i] > 0) {
					Label ratingLabel = new Label(TextTools.PrintRating(i, ratings.values[0, i]));
					ratingLabel.SetAlignment(0, 0);
					ratingsBox.PackStart(ratingLabel, false, false, 0);
				}
			}

			for (int k = 1; k <= 3; k++) {
				if (ratings.values[k,0] > 0) {

					Label ratingLabel = new Label(TextTools.PrintRating(k+8, ratings.values[k,0], true));
					ratingLabel.SetAlignment(0, 0);

					List<String> subratings = new List<String>();
					for (int i = 1; i <= 8; i++)
						if(ratings.values[k,i]>0)
							subratings.Add(TextTools.PrintRating(i, ratings.values[k,i]));
					ratingLabel.TooltipText = String.Join("\n", subratings);

					ratingsBox.PackStart(ratingLabel, false, false, 0);

				}
			}

			ClickableEventBox clickableEventBox = new ClickableEventBox { BorderWidth = 5 };
			clickableEventBox.Add(ratingsBox);
			clickableEventBox.DoubleClicked += (o, a) => new RatingsEditorDialog(this);
			return clickableEventBox;

		}
	}

}