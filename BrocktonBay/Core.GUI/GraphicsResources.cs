using Gtk;
using System;
using System.Collections.Generic;

namespace Parahumans.Core.GUI {

	public static class EnumTools {

		public static readonly string[] classSymbols = { "β", "δ", "Σ", "ψ", "μ", "φ", "ξ", "Ω", "Γ", "Λ", "Δ" };
		public static readonly string[] threatSymbols = { "●", "■", "▲", "☉" };
		public static readonly Gdk.Color[] healthColors = { new Gdk.Color(100, 100, 100), new Gdk.Color(230, 0, 0), new Gdk.Color(200, 200, 0), new Gdk.Color(0, 200, 0) };
		public static readonly Gdk.Color[] alignmentColors = { new Gdk.Color(0, 100, 230), new Gdk.Color(170, 140, 0), new Gdk.Color(100, 150, 0), new Gdk.Color(0, 0, 0), new Gdk.Color(150, 0, 175) };

		public static Gdk.Color GetColor (Health health) => healthColors[(int)health];
		public static Gdk.Color GetColor (Alignment alignment) => alignmentColors[2 - (int)alignment];
		public static string GetSymbol (Classification clssf) => classSymbols[(int)clssf];
		public static string GetSymbol (Threat threat) => threatSymbols[(int)threat];

		public static void SetAllStates (Widget widget, Gdk.Color color) {
			widget.ModifyFg(StateType.Normal, color);
			widget.ModifyFg(StateType.Prelight, color);
			widget.ModifyFg(StateType.Selected, color);
			widget.ModifyFg(StateType.Active, color);
			widget.ModifyFg(StateType.Insensitive, color);
		}

	}

	public static class TextTools {

		public static readonly string[] deploymentRows = { "Base", "Tinker", "Master", "Breaker", "Total" };

		public static readonly string[] multiplierExplain = {
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

		public static string PrintRatings (List<Rating> ratings) {
			string text = "";
			foreach (Rating rating in ratings) {
				text += "\n" + rating;
				foreach (Rating subrating in rating.subratings) {
					text += "\n\t" + subrating;
				}
			}
			return text.TrimStart('\n');
		}

		public static bool TryParseRatings (string text, out List<Rating> ratings) {
			ratings = new List<Rating>();
			string[] lines = text.Split('\n');
			foreach (string line in lines) {
				if (!TryParseRating(line.Trim(), out Rating rating)) return false;
				if(line[0]=='\t'){
					if (ratings.Count == 0) return false;
					ratings[ratings.Count - 1].subratings.Add(rating);
				}else {
					ratings.Add(rating);
				}
			}
			return true;
		}

		public static bool TryParseRating (string text, out Rating rating) {
			string[] parts = text.Split(' ');
			if (!Enum.TryParse(parts[0], true, out Classification type) || !int.TryParse(parts[1], out int val)) {
				rating = null;
				return false;
			}
			rating = new Rating(type, val);
			return true;
		}

		public static string ToReadable (string str) {
			string newStr = str[0].ToString().ToUpper();
			for (int i = 1; i < str.Length; i++) {
				if (str[i] == '_') {
					newStr += " ";
					newStr += str[i + 1].ToString().ToUpper();
					i++;
				} else {
					newStr += str[i];
				}
			}
			return newStr;
		}

	}

}