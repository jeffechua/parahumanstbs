using System;
using Gtk;

namespace Parahumans.Core {

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


	public sealed partial class Deployment {
		/*
		public static void Compare(Deployment l1, Deployment l2) {
			l1.enemy = l2;
			l2.enemy = l1;
			l1.EvalStage1(); //Deducts thinker rating of enemy from self mover and stranger ratings
			l2.EvalStage1();
			l1.EvalStage2(); //Calculates and applies multipliers
			l2.EvalStage2();
			l1.EvalStage3(); //Evaluates strength, mobility and insight
			l2.EvalStage3();
			l1.EvalStage4(); //Calculates the actual probabilities of death and destruction etc.
			l2.EvalStage4();
		}

		public void EvalStage1() {

			if (alignment == Alignment.Hero) {
				authorized_force = enemy.threat;
			} else {
				authorized_force = (Threat)Math.Max((int)threat, (int)enemy.threat);
			}

			if (teams.Count == 0) return;

			ratings.values[1] = (float[,])ratings.values[0].Clone();

			float remainingThinker = enemy.ratings.values[0][4, 6];
			while (remainingThinker > 0) {
				int deductTarget = (ratings.values[0][4, 5] > ratings.values[0][4, 6]) ? 5 : 6;
				if (ratings.values[1][0, deductTarget] > 0) {
					ratings.values[1][0, deductTarget]--;
				} else if (ratings.values[1][1, deductTarget] > 0) {
					ratings.values[1][1, deductTarget]--;
				} else if (ratings.values[1][2, deductTarget] > 0) {
					ratings.values[1][2, deductTarget]--;
				} else {
					break;
				}
				ratings.values[1][4, deductTarget]--;
				remainingThinker--;
			}

		}

		public void EvalStage2() {

			

		}

		public void EvalStage3() {

		}

		public void EvalStage4() {

			injury = new Fraction[4];
			float base_chance = enemy.strength.result / strength.result / 4;
			if (base_chance > 1) base_chance = 1;
			float injury_chance;
			float down_chance;
			float death_chance;
			float healthy_chance;
			switch (enemy.authorized_force) {
				case Threat.C:
					death_chance = 0;
					down_chance = 0;
					injury_chance = base_chance;
					healthy_chance = 1 - base_chance;
					break;
				case Threat.B:
					death_chance = 0;
					down_chance = base_chance * base_chance;
					injury_chance = base_chance - down_chance;
					healthy_chance = 1 - base_chance;
					break;
				case Threat.A:
					death_chance = base_chance * base_chance * base_chance;
					down_chance = base_chance * base_chance - death_chance;
					injury_chance = base_chance - base_chance * base_chance;
					healthy_chance = 1 - base_chance;
					break;
				default:
					death_chance = base_chance * base_chance;
					down_chance = 0;
					injury_chance = base_chance - death_chance;
					healthy_chance = 1 - base_chance;
					break;
			}
			injury[0] = new Fraction("Killed", death_chance, Graphics.GetColor(Health.Deceased));
			injury[1] = new Fraction("Downed", down_chance, Graphics.GetColor(Health.Down));
			injury[2] = new Fraction("Injured", injury_chance, Graphics.GetColor(Health.Injured));
			injury[3] = new Fraction("Unharmed", healthy_chance, Graphics.GetColor(Health.Healthy));

			escape = new Fraction[2];
			float escape_chance = mobility.result / enemy.mobility.result / 2;
			if (escape_chance > 1) escape_chance = 1;
			escape[0] = new Fraction("Captured", 1 - escape_chance, Graphics.GetColor(Alignment.Villain));
			escape[1] = new Fraction("Escaped", escape_chance, Graphics.GetColor(Alignment.Hero));

			appraisal = new Fraction[2];
			float appraise_chance = insight.result / 10;
			if (appraise_chance > 1) appraise_chance = 1;
			appraisal[0] = new Fraction("Successful", appraise_chance, new Gdk.Color(200, 200, 200));
			appraisal[1] = new Fraction("Unsuccessful", 1 - appraise_chance, new Gdk.Color(50, 50, 50));
		}
		*/
	}

}