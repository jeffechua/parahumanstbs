using System;

namespace Parahumans.Core {

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

		public static string PrintRatings (float[,] values, int[,] o_vals) {
			string text = "";
			for (int i = 1; i <= 8; i++) {
				if (o_vals[0, i] != Ratings.O_NULL) {
					text += "\n" + PrintRating(i, values[0, i]);
				}
			}
			for (int k = 1; k <= 3; k++) {
				if (o_vals[k, 0] != Ratings.O_NULL) {
					text += "\n" + PrintRating(k + 8, values[k, 0]);
					for (int i = 1; i <= 8; i++) {
						if (o_vals[k, i] != Ratings.O_NULL) {
							text += "\n\t" + PrintRating(i, values[k, i]);
						}
					}
				}
			}
			return text.TrimStart('\n');
		}

		public static String PrintRating (int classification, float number, bool wrapperStar = false) {
			return Enum.GetName(typeof(Classification), classification)
					   + ((classification > 7 && wrapperStar) ? "* " : " ")
					   + (Math.Round(number * 10) / 10).ToString();
		}

		public static bool TryParseRatings (string text, out RatingsProfile? ratings) {
			ratings = null;
			int currentWrapper = 0;
			int[,] o_vals = new int[5,9];
			string[] lines = text.Split('\n');
			foreach (string line in lines) {
				if (!TryParseRating(line.Trim(), out Tuple<int, float> rating)) return false;

				if (line[0] == '\t') {
					// An indented entry makes no sense if we aren't in a wrapper or it's trying to declare a wrapper
					if (currentWrapper == 0 || rating.Item1 > 8) return false;
					// Everything is fine? According to the currentWrapper, append the entry to the relevant ratings
					o_vals[currentWrapper, rating.Item1] += Ratings.Empower(rating.Item2);
					o_vals[4, rating.Item1] += Ratings.Empower(rating.Item2);
				} else {
					currentWrapper = 0;
					if (rating.Item1 <= 8) { //If it's a normal entry, append it to the base ratings.
						o_vals[0, rating.Item1] += Ratings.Empower(rating.Item2);
						o_vals[4, rating.Item1] += Ratings.Empower(rating.Item2);
					} else { //Otherwise, it's a wrapper, so we "enter" it and log the wrapper metarating.
						currentWrapper = rating.Item1 - 8;
						o_vals[currentWrapper, 0] += Ratings.Empower(rating.Item2);
					}
				}

			}
			ratings = new RatingsProfile(o_vals);
			return true;
		}

		public static bool TryParseRating (string text, out Tuple<int, float> clssfNumPair) {
			string[] parts = text.Split(' ');
			if (!Enum.TryParse(parts[0], true, out Classification type) || !float.TryParse(parts[1], out float num)) {
				clssfNumPair = null;
				return false;
			}
			clssfNumPair = new Tuple<int, float>((int)type, num);
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
