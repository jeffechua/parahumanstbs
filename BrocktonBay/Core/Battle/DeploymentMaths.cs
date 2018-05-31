using System;
using Parahumans.Core.GUI;
using Gtk;

namespace Parahumans.Core {

	public class RatingsComparison {

		public float[][,] values;
		public float[] multipliers;
		public float[] metamultipliers;

		public RatingsComparison () {
			values = new float[3][,] { new float[5, 8], new float[5, 8], new float[5, 8] };
			multipliers = new float[8];
			metamultipliers = new float[4];
		}

	}


	public sealed partial class Deployment : GUIComplete {

		public static void Compare (Deployment l1, Deployment l2) {
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

		public void EvalStage1 () {

			if (alignment == Alignment.Hero) {
				authorized_force = enemy.threat;
			} else {
				authorized_force = (Threat)Math.Max((int)threat, (int)enemy.threat);
			}

			if (teams.Count == 0) return;

			ratings.values[1] = (float[,])ratings.values[0].Clone();

			float remainingThinker = enemy.ratings.values[0][4, 6];
			while (remainingThinker > 0) {
				int deductTarget = (ratings.values[0][4, 4] > ratings.values[0][4, 5]) ? 4 : 5;
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

		public void EvalStage2 () {

			//  0   1   2   3   4   5   6   7   8   9   10
			// BRT BLS SHK SRK MOV SRN THK TRM TNK MSR BRK");

			ratings.multipliers[0] = (float)(GetMultiplier(ratings.values[1][4, 4], 1, 2, 0.05f) *
											 GetMultiplier(enemy.ratings.values[1][4, 4], 1, 0.5f, -0.05f) *
											 GetMultiplier(enemy.ratings.values[1][4, 3], 1, 0.5f, -0.05f)); //Brute:   Mover ±10%, Striker -10%
			ratings.multipliers[1] = (float)(GetMultiplier(ratings.values[1][4, 5], 1, 2, 0.05f) *
											 GetMultiplier(enemy.ratings.values[1][4, 5], 1, 0.5f, -0.05f) *
											 GetMultiplier(enemy.ratings.values[1][4, 2], 1, 0.5f, -0.05f)); //Blaster: Stranger ±10%, Shaker -10%
			ratings.multipliers[2] = (float)GetMultiplier(enemy.ratings.values[1][4, 0], 1, 0.5f, -0.05f); //Shaker:   Brute -10%
			ratings.multipliers[3] = (float)GetMultiplier(enemy.ratings.values[1][4, 1], 1, 0.5f, -0.05f); //Striker:  Blaster -10%
			ratings.multipliers[4] = 1;                                     //Mover:    NONE
			ratings.multipliers[5] = 1;                                     //Thinker:  NONE
			ratings.multipliers[6] = 1;                                     //Stranger: NONE
			ratings.multipliers[7] = 1;                                     //Trump:    NONE

			ratings.metamultipliers[0] = (float)GetMultiplier(enemy.ratings.values[1][4, 7], 1, 0.5f, -0.05f); //Base:     Trump -10%
			ratings.metamultipliers[1] = (float)GetMultiplier(enemy.ratings.values[1][4, 3], 1, 0.5f, -0.05f); //Tinker:   Striker -10%
			ratings.metamultipliers[2] = (float)GetMultiplier(enemy.ratings.values[1][4, 2], 1, 0.5f, -0.05f); //Master:   Shaker -10%
			ratings.metamultipliers[3] = 1;                                   //Breaker:  NONE

			ratings.values[2] = new float[5, 8];
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 8; j++) {
					ratings.values[2][i, j] = ratings.values[1][i, j] * ratings.multipliers[j] * ratings.metamultipliers[i];
					ratings.values[2][4, j] += ratings.values[1][i, j] * ratings.multipliers[j] * ratings.metamultipliers[i];
				}
			}
			for (int j = 0; j < 8; j++) {
				ratings.values[2][3, j] = ratings.values[1][3, j] * ratings.multipliers[j] * ratings.metamultipliers[3];
				ratings.values[2][4, j] += ratings.values[1][3, j] * ratings.multipliers[j] * ratings.metamultipliers[3];
			}

		}

		public void EvalStage3 () {

			strength = new Expression("@0 + @1 + @2 + @3 = @4\n" +
									  "@4 + @5 + @6 = @7"
									  , "0", "0", "0", "0", "0.0", "0", "0", "0.0");
			strength.terms[0].val = ratings.values[2][4, 0]; //Brute
			strength.terms[1].val = ratings.values[2][4, 1]; //Blaster
			strength.terms[2].val = ratings.values[2][4, 2]; //Shaker
			strength.terms[3].val = ratings.values[2][4, 3]; //Striker
			strength.terms[4].val = strength.terms[0].val + strength.terms[1].val + strength.terms[2].val + strength.terms[3].val;
			strength.terms[5].val = combined_roster.Count;
			for (int i = 0; i < teams.Count; i++) strength.terms[6].val += teams[i].spent_XP[0].val;
			strength.terms[7].val = strength.terms[4].val + strength.terms[5].val + strength.terms[6].val;

			if (authorized_force != Threat.B) {
				strength.text += "\n[" + (authorized_force == Threat.C ? "caution" : "brutality") + "]  @8 = @9";
				strength.terms.Add(new StringFloatPair("+##%;−##%;+0", (float)(((int)authorized_force - 1) * 0.1)));
				strength.terms.Add(new StringFloatPair("0.0", strength.terms[7].val * (1 + strength.terms[8].val)));
			}

			mobility = new Expression("@0 + @1 = @2\n" +
									  "@2 @3 + @4 = @5", "0", "0", "0.0", "+#;−#;+0", "0", "0.0");
			mobility.terms[0].val = ratings.values[2][4, 4]; //Mover
			mobility.terms[1].val = ratings.values[2][4, 5]; //Stranger
			mobility.terms[2].val = mobility.terms[0].val + mobility.terms[1].val;
			mobility.terms[3].val = 3 - combined_roster.Count;
			for (int i = 0; i < teams.Count; i++) mobility.terms[4].val += teams[i].spent_XP[1].val;
			mobility.terms[5].val = mobility.terms[2].val + mobility.terms[3].val + mobility.terms[4].val;

			insight = new Expression("@0 + @1 = @2\n" +
									 "@2 + @3 + @4 = @5", "0", "0", "0.0", "0", "0", "0.0");
			insight.terms[0].val = ratings.values[2][4, 6]; //Thinker
			insight.terms[1].val = ratings.values[2][4, 7]; //Trump
			insight.terms[2].val = insight.terms[0].val + insight.terms[1].val;
			insight.terms[3].val = 1;
			for (int i = 0; i < teams.Count; i++) insight.terms[4].val += teams[i].spent_XP[2].val;
			insight.terms[5].val = insight.terms[2].val + insight.terms[3].val + insight.terms[4].val;

		}

		public void EvalStage4 () {

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
			injury[0] = new Fraction("Killed", death_chance, EnumTools.GetColor(Health.Deceased));
			injury[1] = new Fraction("Downed", down_chance, EnumTools.GetColor(Health.Down));
			injury[2] = new Fraction("Injured", injury_chance, EnumTools.GetColor(Health.Injured));
			injury[3] = new Fraction("Unharmed", healthy_chance, EnumTools.GetColor(Health.Healthy));

			escape = new Fraction[2];
			float escape_chance = mobility.result / enemy.mobility.result / 2;
			if (escape_chance > 1) escape_chance = 1;
			escape[0] = new Fraction("Captured", 1 - escape_chance, EnumTools.GetColor(Alignment.Villain));
			escape[1] = new Fraction("Escaped", escape_chance, EnumTools.GetColor(Alignment.Hero));

			appraisal = new Fraction[2];
			float appraise_chance = insight.result / 10;
			if (appraise_chance > 1) appraise_chance = 1;
			appraisal[0] = new Fraction("Successful", appraise_chance, new Gdk.Color(200, 200, 200));
			appraisal[1] = new Fraction("Unsuccessful", 1 - appraise_chance, new Gdk.Color(50, 50, 50));
		}

	}

}