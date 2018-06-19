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

	public struct RatingsProfile : IRated {
		
		public Func<Context, RatingsProfile> ratings { get { return This; } }
		public RatingsProfile This (Context context) => this;

		public float[,] values;
		public float strength;
		public float stealth;
		public float insight;

		public RatingsProfile(float[,] values) {
			this.values = values;
			strength = stealth = insight = 0; // So C# allows us to call a this. function
			EvaluateStats();
		}

		public RatingsProfile(Context context, params IEnumerable<IRated>[] ratedLists) {
			values = new float[5, 9];
			foreach (IEnumerable<IRated> ratedList in ratedLists) {
				foreach (IRated rated in ratedList) {
					for (int i = 0; i < 4; i++) {
						for (int j = 0; j <=8; j++) {
							values[i, j] += rated.ratings(context).values[i, j];
							values[4, j] += rated.ratings(context).values[i, j];
						}
					}
				}
			}
			strength = stealth = insight = 0; // So C# allows us to call a this. function
			EvaluateStats();
		}

		public RatingsProfile(Context context, params IRated[] rateds) {
			values = new float[5, 9];
			foreach (IRated rated in rateds) {
				for (int i = 0; i < 5; i++) {
					for (int j = 0; j <= 8; j++) {
						values[i, j] += rated.ratings(context).values[i, j];
						values[4, j] += rated.ratings(context).values[i, j];
					}
				}
			}
			strength = stealth = insight = 0; // So C# allows us to call a this. function
			EvaluateStats();
		}

		public void EvaluateStats() {
			strength = values[4, 1] + values[4, 2] + values[4, 3] / 2 + values[4, 4] / 2;
			stealth = values[4, 5] + values[4, 6];
			insight = values[4, 7] + values[4, 8];
		}

	}

	public interface IRated {
		Func<Context, RatingsProfile> ratings { get; }
	}

}
