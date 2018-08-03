using System;
using Gtk;

namespace BrocktonBay {

	public sealed class RatingsTable : Table {


		public static readonly string[] deploymentRows = { " Ξ ", " Γ ", " ς ", " χ ", "  sum" };

		public static readonly string[] multiplierExplain = {
			"",
			"±10% / Mover lvl\n−10% / Striker lvl",   //Brute
			"±10% / Stranger lvl\n−10% / Shaker lvl", //Blaster
			"−10% / Brute lvl",                     //Shaker
			"−10% / Blaster lvl",                   //Striker
			"No multiplier",                      //Mover
			"No multiplier",                      //Stranger
			"No multiplier",                      //Thinker
			"No multiplier"                       //Trump
		};
		public static readonly string[] metamultiplierExplain = {
			"−10% / Trump lvl",                      //Base
			"−10% / Striker lvl",                   //Tinker
			"−10% / Shaker lvl",                    //Master
			"Nullifies all multipliers"           //Breaker
		};

		public RatingsTable (Context context, RatingsProfile profile, float[] multipliers = null, float[] metamultipliers = null) : base(7, 10, false) {

			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			ColumnSpacing = 5;
			RowSpacing = 5;
			BorderWidth = 10;

			Attach(new HSeparator(), 0, 7, 1, 2);
			Attach(new VSeparator(), 1, 2, 0, 10);

			//Row labels and multipliers
			for (uint i = 1; i <= 8; i++) {
				Label classLabel = new Label(" " + Ratings.symbols[i]) {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				Attach(classLabel, 0, 1, i + 1, i + 2);
			}
			//Fill in columns, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(deploymentRows[i]);
				rowLabel.SetAlignment(0.5f, 1f);
				Attach(rowLabel, i + 2, i + 3, 0, 1);
				//Numbers
				for (uint j = 1; j <= 8; j++) {
					Label numberLabel = new Label();
					if (o_vals[i, j] == Ratings.O_NULL) {
						numberLabel.Text = "-";
					} else {
						numberLabel.Text = values[i, j].ToString("0.0");
					}
					numberLabel.SetAlignment(1, 1);
					Attach(numberLabel, i + 2, i + 3, j + 1, j + 2);
				}
			}

			if (multipliers != null && metamultipliers != null) {
				//Row multipliers
				for (uint i = 1; i <= 8; i++) {
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small> ×" + multipliers[i].ToString("0.00") + "</small>",
						HasTooltip = true,
						TooltipText = multiplierExplain[i]
					};
					multiplier.SetAlignment(1, 0);
					Attach(multiplier, 7, 8, i + 1, i + 2);
				}

				//Column metamultipliers
				for (uint i = 0; i < 4; i++) {
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small>  ×" + metamultipliers[i].ToString("0.00") + ((i == 3) ? ", fix" : "") + "</small>",
						HasTooltip = true,
						TooltipText = metamultiplierExplain[i],
						Angle = -90
					};
					multiplier.SetAlignment(0, 0);
					Attach(multiplier, i + 2, i + 3, 10, 11);
				}
			}

		}

	}

}
