using System;
using System.Collections.Generic;

namespace Parahumans.Core {

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
		public const int NULL = -22;
		public const int O_NULL = 0;
		public const int ZERO = 0;
		public const int O_ZERO = 2048;
		public static float[,] ALL_NULL {
			get {
				float[,] output = new float[5, 9];
				for (int i = 0; i < 5; i++)
					for (int j = 0; j < 5; j++)
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
					output[i, j] = ApproxEquals(input[i,j], NULL) ? ZERO : input[i, j];
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

	}

	public interface IRated {
		Func<Context, RatingsProfile> ratings { get; }
	}

}
