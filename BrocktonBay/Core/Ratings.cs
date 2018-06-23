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

	public struct RatingsProfile : IRated {

		public Func<Context, RatingsProfile> ratings { get { return This; } }
		public RatingsProfile This (Context context) => this;

		public float[,] values;
		public float[] bonuses;

		public RatingsProfile (float[,] values) {
			this.values = values;
			bonuses = new float[3];
		}

		public RatingsProfile (Context context, params IEnumerable<IRated>[] ratedss) {
			values = new float[5, 9];
			bonuses = new float[3];
			foreach (IEnumerable<IRated> rateds in ratedss) {
				List<RatingsProfile> profiles = new List<IRated>(rateds).ConvertAll((input) => input.ratings(context));
				foreach (RatingsProfile profile in profiles) {
					for (int i = 0; i < 4; i++) {
						for (int j = 0; j <= 8; j++) {
							values[i, j] += profile.values[i, j];
							values[4, j] += profile.values[i, j];
						}
					}
					for (int i = 0; i < 3; i++) {
						bonuses[i] += profile.bonuses[i];
					}
				}
			}
		}
		// This is a horrible mess, but essentially it converts each IEnumerable<IRated> into a RatingsProfile using
		// the constructor below, hence replacing IEnumerable<IRated>[] with a RatingsProfile[].
		// As RatingsProfile is IRated, we can pass the created RatingsProfile[] into the constructor blow again.

		public RatingsProfile (Context context, params IRated[] rateds) {
			List<RatingsProfile> profiles = new List<IRated>(rateds).ConvertAll((input) => input.ratings(context));
			values = new float[5, 9];
			bonuses = new float[3];
			foreach (RatingsProfile profile in profiles) {
				for (int i = 0; i < 4; i++) {
					for (int j = 0; j <= 8; j++) {
						values[i, j] += profile.values[i, j];
						values[4, j] += profile.values[i, j];
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
