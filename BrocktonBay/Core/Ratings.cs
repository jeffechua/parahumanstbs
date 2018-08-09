using System;
using System.Collections.Generic;

namespace BrocktonBay {

	public enum Alignment {
		Hero = 2,
		Vigilante = 1,
		Rogue = 0,
		Mercenary = -1,
		Villain = -2
	}

	public enum Threat {
		C = 0, //Default
		B = 1, //Confirmed team takedown
		A = 2, //Confirmed kill
		S = 3, //Kill order receievd
		X = 4  //World-ending
	}

	public enum Classification {
		Brute = 1,
		Blaster = 2,
		Shaker = 3,
		Striker = 4,
		Mover = 5,
		Stranger = 6,
		Thinker = 7,
		Trump = 8,
		Tinker = 9,
		Master = 10,
		Breaker = 11
	}
	public enum ClassificationSymbols {
		β = 1,
		δ = 2,
		Σ = 3,
		ψ = 4,
		μ = 5,
		φ = 6,
		ξ = 7,
		Ω = 8,
		Γ = 9,
		ς = 10,
		χ = 11
	}

	public struct EffectiveRatingsProfile {

		public RatingsProfile original;
		public RatingsProfile final;
		public float[] multipliers;
		public float[] metamultipliers;

		public EffectiveRatingsProfile (RatingsProfile original, RatingsProfile final, float[] multipliers, float[] metamultipliers) {
			this.original = original;
			this.final = final;
			this.multipliers = multipliers;
			this.metamultipliers = metamultipliers;
		}

	}

	public static class Ratings {

		public static readonly string[] symbols = { "", "β", "δ", "Σ", "ψ", "μ", "φ", "ξ", "Ω", "Γ", "ς", "χ" };

		public const int NULL = -22;
		public const int O_NULL = 0;
		public const int ZERO = 0;
		public const int O_ZERO = 2048;
		public static float[,] ALL_NULL {
			get {
				float[,] output = new float[5, 9];
				for (int i = 0; i < 5; i++)
					for (int j = 0; j < 9; j++)
						output[i, j] = NULL;
				return output;
			}
		}

		public static int Empower (float number) {
			if (number <= Ratings.NULL + 0.01) return Ratings.O_NULL;
			return (int)Math.Pow(2, number / 2 + 11);
		}
		public static float Logarize (int number) {
			if (number == Ratings.O_NULL) return Ratings.NULL;
			return (float)Math.Log(number, 2) * 2 - 22;
		}

		public static float[,] NullToZero (float[,] input) {
			float[,] output = new float[5, 9];
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 9; j++)
					output[i, j] = ApproxEquals(input[i, j], NULL) ? ZERO : input[i, j];
			return output;
		}
		public static int[,] NullToZero (int[,] input) {
			int[,] output = new int[5, 9];
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 9; j++)
					output[i, j] = input[i, j] == O_NULL ? O_ZERO : input[i, j];
			return output;
		}
		public static float[,] ZeroToNull (float[,] input) {
			float[,] output = new float[5, 9];
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 9; j++)
					output[i, j] = ApproxEquals(input[i, j], ZERO) ? NULL : input[i, j];
			return output;
		}
		public static int[,] ZeroToNull (int[,] input) {
			int[,] output = new int[5, 9];
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 9; j++)
					output[i, j] = input[i, j] == O_ZERO ? O_NULL : input[i, j];
			return output;
		}

		public static bool ApproxEquals (float a, float b) {
			return Math.Abs(a - b) < 0.01;
		}

		public static string Print (float[,] values, int[,] o_vals) {
			string text = "";
			for (int i = 1; i <= 8; i++)
				if (o_vals[0, i] != O_NULL)
					text += "\n" + PrintSingle(i, values[0, i]);
			for (int k = 1; k <= 3; k++) {
				if (o_vals[k, 0] != O_NULL) {
					text += "\n" + PrintSingle(k + 8, values[k, 0]);
					for (int i = 1; i <= 8; i++)
						if (o_vals[k, i] != O_NULL)
							text += "\n\t" + PrintSingle(i, values[k, i]);
				}
			}
			return text.TrimStart('\n');
		}

		public static string PrintCompact (float[,] values, int[,] o_vals) {
			string text = "";
			for (int i = 1; i <= 8; i++)
				if (o_vals[0, i] != O_NULL)
					text += "\n" + PrintSingle(i, values[0, i]);
			for (int k = 1; k <= 3; k++) {
				string wrapperName = symbols[k + 8];
				for (int i = 1; i <= 8; i++)
					if (o_vals[k, i] != O_NULL)
						text += "\n" + wrapperName + "/" + PrintSingle(i, values[k, i]);
			}
			return text.TrimStart('\n');
		}

		public static String PrintSingle (int classification, float number, bool wrapperStar = false) {
			return Enum.GetName(typeof(Classification), classification)
					   + ((classification > 7 && wrapperStar) ? "* " : " ")
					   + (Math.Round(number * 10) / 10).ToString();
		}

		public static bool TryParse (string text, out RatingsProfile? ratings) {
			ratings = null;
			int currentWrapper = 0;
			int[,] o_vals = new int[5, 9];
			string[] lines = text.Split('\n');
			if (!(lines.Length == 1 && lines[0] == "")) {
				foreach (string line in lines) {
					if (!TryParseSingle(line.Trim(), out Tuple<int, float> rating)) return false;
					if (line[0] == '\t') {
						// An indented entry makes no sense if we aren't in a wrapper or it's trying to declare a wrapper
						if (currentWrapper == 0 || rating.Item1 > 8) return false;
						// Everything is fine? According to the currentWrapper, append the entry to the relevant ratings
						o_vals[currentWrapper, rating.Item1] += Empower(rating.Item2);
						o_vals[4, rating.Item1] += Empower(rating.Item2);
					} else {
						currentWrapper = 0;
						if (rating.Item1 <= 8) { //If it's a normal entry, append it to the base ratings.
							o_vals[0, rating.Item1] += Empower(rating.Item2);
							o_vals[4, rating.Item1] += Empower(rating.Item2);
						} else { //Otherwise, it's a wrapper, so we "enter" it and log the wrapper metarating.
							currentWrapper = rating.Item1 - 8;
							o_vals[currentWrapper, 0] += Empower(rating.Item2);
						}
					}
				}
			}
			ratings = new RatingsProfile(o_vals);
			return true;
		}

		public static bool TryParseCompact (string text, out RatingsProfile? ratings) {
			ratings = null;
			int[,] o_vals = new int[5, 9];
			string[] lines = text.Split('\n');
			if (!(lines.Length == 1 && lines[0] == "")) {
				foreach (string line in lines) {
					if (line.Contains("/")) {
						string[] parts = line.Split('/');
						if (!Enum.TryParse(parts[0], true, out ClassificationSymbols wrapper)) return false;
						if (!TryParseSingle(parts[1], out Tuple<int, float> content)) return false;
						o_vals[(int)wrapper - 8, content.Item1] += Empower(content.Item2);
						o_vals[4, content.Item1] += Empower(content.Item2);
					} else {
						if (!TryParseSingle(line.Trim(), out Tuple<int, float> rating)) return false;
						o_vals[0, rating.Item1] += Empower(rating.Item2);
						o_vals[4, rating.Item1] += Empower(rating.Item2);
					}
				}
			}
			ratings = new RatingsProfile(o_vals);
			return true;
		}

		static bool TryParseSingle (string text, out Tuple<int, float> clssfNumPair) {
			string[] parts = text.Split(' ');
			if (!Enum.TryParse(parts[0], true, out Classification type) || !float.TryParse(parts[1], out float num)) {
				clssfNumPair = null;
				return false;
			}
			clssfNumPair = new Tuple<int, float>((int)type, num);
			return true;
		}

	}

	// Ratings have two important and linked numerical arrays: o_vals (original values) and values (scaled values). 
	// Values are the ones displayed in the UI, and they run on a log scale, with two steps being an order of magnitude.
	// (Orders of magnitude here scale by x2)
	// Thus, Brute 6 + Brute 6 = Brute 8, NOT Brute 12.
	// This is implemented through o_vals, which stores the exponented value of the values. A rating of 0 is 2^11 = 2048.
	// In accordance with the x2 per 2 steps rule, a rating of 2 is 2^12, 4 is 2^13 and so on.
	//
	// The "values" array is a property, not a field: the ratings are only "stored" in the integer form of o_vals, with
	// the scaled values requested as needed via the "values" propery.
	// This hence explains why zero is 2^11 and not 2^0: the "buffer" is necessary to store ratings with at least *some*
	// precision in the 0-2 range of the logarithmic scale. If zero is 2^0, then something Brute 0.6 would simply round
	// to Brute 0.5 when stored and accessed again.
	//
	// However, note that a rating of "zero" is distinct from "no rating": "no rating" is signified by o_val = 0, value
	// = -22. This is because two Brute 0s add to create a Brute 1, while a not-Brute plus a not-Brute is still a
	// not-Brute. Assigning it o_val = 0 means that adding any number of "no rating"s will still leave a "no rating".

	public struct RatingsProfile : IRated {

		public Func<Context, RatingsProfile> ratings { get { return This; } }
		public RatingsProfile This (Context context) => this;

		public int[,] o_vals;
		public float[,] values {
			get {
				float[,] output = new float[5, 9];
				for (int i = 0; i < 5; i++)
					for (int j = 0; j < 9; j++)
						output[i, j] = Ratings.Logarize(o_vals[i, j]);
				return output;
			}
			set {
				for (int i = 0; i < 5; i++)
					for (int j = 0; j < 9; j++)
						o_vals[i, j] = Ratings.Empower(value[i, j]);
			}
		}
		public float[] bonuses;

		public static RatingsProfile Null { get => new RatingsProfile(new int[5, 9]); }

		public RatingsProfile (int[,] o_values) {
			o_vals = o_values;
			bonuses = new float[3];
		}

		public RatingsProfile (float[,] values) {
			o_vals = new int[5, 9];
			bonuses = new float[3];
			this.values = values;
		}

		public RatingsProfile (Context context, params IEnumerable<IRated>[] ratedss) {
			o_vals = new int[5, 9];
			bonuses = new float[3];
			foreach (IEnumerable<IRated> rateds in ratedss) {
				List<RatingsProfile> profiles = new List<IRated>(rateds).ConvertAll((input) => input.ratings(context));
				foreach (RatingsProfile profile in profiles) {
					for (int i = 0; i < 4; i++) {
						for (int j = 0; j <= 8; j++) {
							o_vals[i, j] += profile.o_vals[i, j];
							o_vals[4, j] += profile.o_vals[i, j];
						}
					}
					for (int i = 0; i < 3; i++) {
						bonuses[i] += profile.bonuses[i];
					}
				}
			}
		}

		public RatingsProfile (Context context, params IRated[] rateds) {
			List<RatingsProfile> profiles = new List<IRated>(rateds).ConvertAll((input) => input.ratings(context));
			o_vals = new int[5, 9];
			bonuses = new float[3];
			foreach (RatingsProfile profile in profiles) {
				for (int i = 0; i < 4; i++) {
					for (int j = 0; j <= 8; j++) {
						o_vals[i, j] += profile.o_vals[i, j];
						o_vals[4, j] += profile.o_vals[i, j];
					}
				}
				for (int i = 0; i < 3; i++) {
					bonuses[i] += profile.bonuses[i];
				}
			}
		}

		public RatingsProfile Clone () {
			RatingsProfile profile = new RatingsProfile(o_vals);
			profile.bonuses = this.bonuses;
			return profile;
		}

		public static RatingsProfile operator * (RatingsProfile primary, RatingsProfile secondary) {
			float[,] values = primary.values;
			float[,] change = Ratings.NullToZero(secondary.values);
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 9; j++) {
					if (Math.Abs(change[i, j]) > 0.01) {
						if (Ratings.ApproxEquals(values[i, j], Ratings.NULL)) values[i, j] = Ratings.ZERO;
						if (Ratings.ApproxEquals(values[4, j], Ratings.NULL)) values[4, j] = Ratings.ZERO;
						values[i, j] += change[i, j];
						values[4, j] += change[i, j];
					}
				}
			}
			return new RatingsProfile(values);
		}

		public Expression[] GetStats (Threat force_employed) {

			Expression[] stats = {
				new Expression("@0 + @1 + ½(@2) + ½(@3) = @4\n" +
							   "@4 + @5 (bonus) = @6\n" +
							   "@6 × @7 (force) = @8",
							   "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.00", "0.0"),
				new Expression("@0 + @1 = @2\n" +
							   "@2 + @3 (bonus) = @4",
							   "0.0", "0.0", "0.0", "0.0", "0.0"),
				new Expression("@0 + @1 = @2\n" +
							   "@2 + @3 (bonus) = @4",
							   "0.0", "0.0", "0.0", "0.0", "0.0")
			};

			float[,] zValues = Ratings.NullToZero(values);

			//Strength
			float baseStrength = zValues[4, 1] + zValues[4, 2] + zValues[4, 3] / 2 + zValues[4, 4] / 2;
			float plusBonus = baseStrength + bonuses[0];
			if (plusBonus < 0) plusBonus = 0;
			float forceMult = Battle.ForceMult(force_employed);
			float plusForce = plusBonus * forceMult;
			stats[0].SetValues(zValues[4, 1], zValues[4, 2], zValues[4, 3], zValues[4, 4],
									baseStrength, bonuses[0], plusBonus, forceMult, plusForce);
			//Stealth
			stats[1].SetValues(zValues[4, 5], zValues[4, 6], zValues[4, 5] + zValues[4, 6],
									 bonuses[1], zValues[4, 5] + zValues[4, 6] + profile.bonuses[1]);
			//Insight
			stats[2].SetValues(zValues[4, 7], zValues[4, 8], zValues[4, 7] + zValues[4, 8],
									 bonuses[2], zValues[4, 7] + zValues[4, 8] + profile.bonuses[2]);

			return stats;

		}

	}

	public interface IRated {
		Func<Context, RatingsProfile> ratings { get; }
	}

}
