using System;
using System.Collections.Generic;

namespace Parahumans.Core {
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

	public struct RatingsProfile : Rated {
		
		public RatingsProfile ratings { get { return this; } }

		public float[,] values;
		public float strength;
		public float stealth;
		public float insight;

		public RatingsProfile(float[,] ratings) {
			this.values = ratings;
			strength = stealth = insight = 0; //So C# allows us to call a this. function
			EvaluateStats();
		}

		public RatingsProfile(params IEnumerable<Rated>[] ratedLists) {
			values = new float[5, 9];
			foreach (IEnumerable<Rated> ratedList in ratedLists) {
				foreach (Rated rated in ratedList) {
					for (int i = 0; i < 4; i++) {
						for (int j = 0; j <=8; j++) {
							values[i, j] += rated.ratings.values[i, j];
							values[4, j] += rated.ratings.values[i, j];
						}
					}
				}
			}
			strength = stealth = insight = 0; //So C# allows us to call a this. function
			EvaluateStats();
		}

		public RatingsProfile(params Rated[] rateds) {
			values = new float[5, 9];
			foreach (Rated rated in rateds) {
				for (int i = 0; i < 5; i++) {
					for (int j = 0; j <= 8; j++) {
						values[i, j] += rated.ratings.values[i, j];
						values[4, j] += rated.ratings.values[i, j];
					}
				}
			}
			strength = stealth = insight = 0; //So C# allows us to call a this. function
			EvaluateStats();
		}

		public void EvaluateStats() {
			strength = values[4, 1] + values[4, 2] + values[4, 3] / 2 + values[4, 4] / 2;
			stealth = values[4, 5] + values[4, 6];
			insight = values[4, 7] + values[4, 8];
		}

	}

	public interface Rated {
		RatingsProfile ratings { get; }
	}
}
