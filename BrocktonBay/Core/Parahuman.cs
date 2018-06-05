using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public enum Classification {
		Brute = 0,
		Blaster = 1,
		Shaker = 2,
		Striker = 3,
		Mover = 4,
		Stranger = 5,
		Thinker = 6,
		Trump = 7,
		Tinker = 8,
		Master = 9,
		Breaker = 10
	}

	public enum Health {
		Deceased = 0,
		Down = 1,
		Injured = 2,
		Healthy = 3
	}

	public sealed class Rating {
		public Classification clssf;
		public int num;
		public List<Rating> subratings;

		public Rating (Classification classification, int number) {
			clssf = classification;
			num = number;
			subratings = new List<Rating>();
		}

		public override String ToString () {
			return clssf.ToString() + " " + num.ToString();
		}

		public String ToString (bool star) {
			return clssf.ToString() + ((subratings.Count > 0 && star) ? "* " : " ") + num.ToString();
		}

	}

	public sealed class ParahumanData {
		public String name = "New Trigger";
		public int ID = 0;
		public string civilianName = "";
		public Alignment alignment = Alignment.Rogue;
		public Threat threat = Threat.C;
		public Health health = Health.Healthy;
		public int reputation = 0;
		public List<Rating> ratings = new List<Rating>();

		public ParahumanData () {}

		public ParahumanData (Parahuman parahuman) {
			name = parahuman.name;
			ID = parahuman.ID;
			civilianName = parahuman.civilian_name;
			alignment = parahuman.alignment;
			threat = parahuman.threat;
			health = parahuman.health;
			reputation = parahuman.reputation;
			ratings = parahuman.ratings;
		}

	}

	public sealed class Parahuman : GameObject {

		public override int order { get { return 1; } }

		[Displayable(2, typeof(StringField))]
		public String civilian_name { get; set; }

		[Displayable(3, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(4, typeof(EnumField<Threat>))]
		public Threat threat { get; set; }

		[Displayable(5, typeof(EnumField<Health>))]
		public Health health { get; set; }

		[Displayable(6, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(7, typeof(RatingListField)), Padded(5, 5), EmphasizedIfHorizontal]
		public List<Rating> ratings { get; set; }

		public Parahuman () : this(new ParahumanData()) { }

		public Parahuman (ParahumanData data) {
			name = data.name;
			ID = data.ID;
			civilian_name = data.civilianName;
			alignment = data.alignment;
			threat = data.threat;
			health = data.health;
			ratings = data.ratings;
		}

		public override void Sort () {
			ratings.Sort((x, y) => ((x.num == y.num) ? ((x.clssf < y.clssf) ? 1 : -1) : ((x.num < y.num) ? 1 : -1)));
			for (int i = 0; i < ratings.Count; i++)
				if (ratings[i].subratings.Count > 0)
					ratings[i].subratings.Sort((x, y) => ((x.num == y.num) ? ((x.clssf < y.clssf) ? 1 : -1) : ((x.num < y.num) ? 1 : -1)));
		}

		public override Widget GetHeader (bool compact) {

			if (compact) {

				HBox header = new HBox(false, 0);
				Label icon = new Label(" ● ");
				EnumTools.SetAllStates(icon, EnumTools.GetColor(health));
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

		public override Widget GetCell () {
			VBox ratingsBox = new VBox(false, 0) { BorderWidth = 5 };
			for (int i = 0; i < ratings.Count; i++)
				ratingsBox.PackStart(new RatingListFieldElement(ratings, i, false), false, false, 0);
			ClickableEventBox clickableEventBox = new ClickableEventBox { BorderWidth = 5 };
			clickableEventBox.Add(ratingsBox);
			clickableEventBox.DoubleClicked += (o, a) => new RatingsEditorDialog(this);
			return clickableEventBox;
		}
	}

}