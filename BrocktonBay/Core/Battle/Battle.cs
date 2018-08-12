using Gtk;
using System;
using System.Collections.Generic;

namespace BrocktonBay {

	public sealed class Battle : IGUIComplete {

		public int order { get { return 5; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public void Reload () { }

		public Deployment[] deployments;
		public Deployment attackers { get { return deployments[0]; } set { deployments[0] = value; } }
		public Deployment defenders { get { return deployments[1]; } set { deployments[1] = value; } }
		public EffectiveRatingsProfile[] profiles;

		public string name { get { return "Battle at " + location.name; } }

		[Displayable(1, typeof(ObjectField), forceHorizontal = true)]
		public IBattleground location;



		[Displayable(2, typeof(EffectiveRatingsMultiview), emphasized = true, expand = true)]
		public EffectiveRatingsProfile attacker_profile { get { return profiles[0]; } set { profiles[0] = value; } }

		[Displayable(3, typeof(TabularContainerField), "attacker_strength", "attacker_stealth", "attacker_insight",
					 altWidget = typeof(LinearContainerField), emphasizedIfVertical = true)]
		public Expression[] attacker_stats {
			get {
				return new Expression[] { attacker_strength, attacker_stealth, attacker_insight };
			}
			set {
				attacker_strength = value[0];
				attacker_stealth = value[1];
				attacker_insight = value[2];
			}
		}
		[ChildDisplayable("Strength", typeof(ExpressionField), tooltipText = "β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression attacker_strength { get; set; }
		[ChildDisplayable("Stealth", typeof(ExpressionField), tooltipText = "μ + φ + bonuses")]
		public Expression attacker_stealth { get; set; }
		[ChildDisplayable("Insight", typeof(ExpressionField), tooltipText = "ξ + Ω + bonuses")]
		public Expression attacker_insight { get; set; }

		[Displayable(4, typeof(FractionsBar), false, emphasized = true, tooltipText = "Injury chance = STR<sub>enemy</sub> / STR<sub>total</sub> / 2")]
		public Fraction[] attacker_injury { get; set; } //Chance of being injured, per member
		[Displayable(5, typeof(FractionsBar), false, emphasized = true, tooltipText = "Captured chance = INS<sub>enemy</sub> / STL<sub>self</sub> / 4")]
		public Fraction[] attacker_escape { get; set; } //Chance of escaping, per member

		[Displayable(6, typeof(Banner), emphasized = true, topPadding = 25, bottomPadding = 25)]
		public string victor_display { get; set; }
		public Deployment victor;

		[Displayable(7, typeof(FractionsBar), false, emphasized = true, tooltipText = "Injury chance = STR<sub>enemy</sub> / STR<sub>total</sub> / 2")]
		public Fraction[] defender_injury { get; set; } //Chance of being injured, per member
		[Displayable(8, typeof(FractionsBar), false, emphasized = true, tooltipText = "Capture chance = INS<sub>enemy</sub> / STL<sub>self</sub> / 4")]
		public Fraction[] defender_escape { get; set; } //Chance of capture, per member

		[Displayable(9, typeof(TabularContainerField), "defender_strength", "defender_stealth", "defender_insight",
					 altWidget = typeof(LinearContainerField), emphasizedIfVertical = true)]
		public Expression[] defender_stats {
			get {
				return new Expression[] { defender_strength, defender_stealth, defender_insight };
			}
			set {
				defender_strength = value[0];
				defender_stealth = value[1];
				defender_insight = value[2];
			}
		}
		[ChildDisplayableAttribute("Strength", typeof(ExpressionField), tooltipText = "β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression defender_strength { get; set; }
		[ChildDisplayableAttribute("Stealth", typeof(ExpressionField), tooltipText = "μ + φ + bonuses")]
		public Expression defender_stealth { get; set; }
		[ChildDisplayableAttribute("Insight", typeof(ExpressionField), tooltipText = "ξ + Ω + bonuses")]
		public Expression defender_insight { get; set; }

		[Displayable(10, typeof(EffectiveRatingsMultiview), emphasized = true, expand = true)]
		public EffectiveRatingsProfile defender_profile { get { return profiles[1]; } set { profiles[1] = value; } }




		public Dossier apparentKnowledge;

		public Battle (IBattleground location, Deployment attackers, Deployment defenders) {
			this.location = location;
			deployments = new Deployment[] { attackers, defenders };
			if (defenders != null) {
				profiles = new EffectiveRatingsProfile[2];
				Evaluate();
			} else {
				victor = attackers;
			}
		}

		public void Evaluate () {

			RatingsProfile attacker_base_profile = attackers.ratings(new Context(defenders.affiliation, attackers));
			RatingsProfile defender_base_profile = defenders.ratings(new Context(attackers.affiliation, defenders));
			attacker_profile = CompareProfiles(attacker_base_profile, defender_base_profile);
			defender_profile = CompareProfiles(defender_base_profile, attacker_base_profile);

			attacker_stats = attacker_profile.final.GetStats(attackers.force_employed);
			defender_stats = defender_profile.final.GetStats(defenders.force_employed);

			victor = (attacker_strength.result >= defender_strength.result) ? attackers : defenders;

			victor_display = "Battle Victor:<small><small>\n\n</small></small>\n<big><big>" +
							 (victor == attackers ? "ATTACKERS" : "DEFENDERS") + victor.affiliation.name
							 + "</big></big>";

			Tuple<Fraction[], Fraction[]> fractions;
			fractions = CompareStats(attacker_stats, defender_stats, defenders.force_employed);
			attacker_injury = fractions.Item1; attacker_escape = fractions.Item2;
			fractions = CompareStats(defender_stats, attacker_stats, attackers.force_employed);
			defender_injury = fractions.Item1; defender_escape = fractions.Item2;

			attackers.Apply(attacker_injury, attacker_escape);
			defenders.Apply(defender_injury, defender_escape);

		}

		public Tuple<Fraction[], Fraction[]> CompareStats (Expression[] original, Expression[] enemy, Threat force) {

			Fraction[] injury = new Fraction[4];
			float base_chance = enemy[0].result < 0.01 ? 0 : (enemy[0].result / (original[0].result + enemy[0].result) / 2);
			if (base_chance > 1) base_chance = 1;
			float injury_chance;
			float down_chance;
			float death_chance;
			float healthy_chance;
			switch (force) {
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


			Fraction[] escape;
			if (original[0].result < enemy[0].result) {
				escape = new Fraction[2];
				float capture_chance = original[1].result < 0.01 ? 1 : enemy[2].result / original[1].result / 4;
				if (capture_chance > 1) capture_chance = 1;
				escape[0] = new Fraction("Captured", capture_chance, Graphics.GetColor(Alignment.Villain));
				escape[1] = new Fraction("Escaped", 1 - capture_chance, Graphics.GetColor(Alignment.Hero));
			} else {
				escape = new Fraction[] { new Fraction("N/A", 1, new Gdk.Color(100, 100, 100)) };
			}

			return new Tuple<Fraction[], Fraction[]>(injury, escape);
		}

		public EffectiveRatingsProfile CompareProfiles (RatingsProfile original, RatingsProfile enemy) {

			float[,] originalValues = Ratings.NullToZero(original.values);
			float[,] enemyValues = Ratings.NullToZero(enemy.values);

			// Calculate the multipliers. Reference for matching indices:
			//  1   2   3   4   5   6   7   8   9   10  11
			// BRT BLS SHK SRK MOV SRN THK TRM TNK MSR BRK

			float[] multipliers = new float[9];
			float[] metamultipliers = new float[5];

			multipliers[0] = 1;                                     //The [0] slot is reserved for the wrapper's cosmetic rating
			multipliers[1] = Mult(enemyValues[4, 4]) * Mult(enemyValues[4, 5]);    //Brute:    Mover -10%, Striker -10%
			multipliers[2] = Mult(enemyValues[4, 3]) * Mult(enemyValues[4, 6]);    //Blaster:  Stranger -10%, Shaker -10%
			multipliers[3] = Mult(enemyValues[4, 1]);                              //Shaker:   Brute -10%
			multipliers[4] = Mult(enemyValues[4, 2]);                              //Striker:  Blaster -10%
			multipliers[5] = multipliers[6] = multipliers[7] = multipliers[8] = 1; //Mover, Stranger, Thinker, Trump: NONE

			metamultipliers[0] = Mult(enemyValues[4, 8]); //Base:     Trump -10%
			metamultipliers[1] = Mult(enemyValues[4, 4]); //Tinker:   Striker -10%
			metamultipliers[2] = Mult(enemyValues[4, 3]); //Master:   Shaker -10%
			metamultipliers[3] = Mult(enemyValues[4, 7]); //Breaker:  Thinker -10%

			// Multiply the multipliers
			int[,] finalOValues = new int[5, 9];
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 9; j++)
					finalOValues[i, j] = Ratings.Empower(originalValues[i, j] * metamultipliers[i] * (i == 3 ? 1 : multipliers[j]));
			//Breakers are immune to regular multipliers, only vulnerable to its wrapper metamultiplier (Thinker)

			finalOValues = Ratings.ZeroToNull(finalOValues);

			for (int i = 0; i < 9; i++) //The fourth row is the "sum" row.
				finalOValues[4, i] = finalOValues[0, i] + finalOValues[1, i] + finalOValues[2, i] + finalOValues[3, i];

			RatingsProfile final = original.Clone();
			final.o_vals = finalOValues;

			return new EffectiveRatingsProfile(original, final, multipliers, metamultipliers);

		}

		public static float Mult (float number) => (float)Math.Pow(0.95, number);
		public static float ForceMult (Threat force) => 1 + ((int)force - 1) * 0.1f;

		public Widget GetHeader (Context context) {
			if (context.compact) {
				HBox frameHeader = new HBox(false, 0);
				frameHeader.PackStart(new Label(name), false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);

				Label nameLabel = new Label(name) { Justify = Justification.Center };
				nameLabel.WidthRequest = 200;
				headerBox.PackStart(nameLabel, false, false, 0);

				HBox versusBox = new HBox();
				if (attackers.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(attackers.affiliation.GetHeader(context.butCompact));
				}
				versusBox.PackStart(new Label(" vs. "), false, false, 5);
				if (defenders.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(defenders.affiliation.GetHeader(context.butCompact));
				}
				headerBox.PackStart(UIFactory.Align(versusBox, 0.5f, 0, 0, 0), false, false, 0);

				return headerBox;
			}
		}

		public Widget GetCellContents (Context context) {
			VBox versusBox = new VBox();

			if (attackers.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(attackers.affiliation.GetHeader(context.butCompact));
			}
			versusBox.PackStart(new Label(" VERSUS "));
			if (defenders.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(defenders.affiliation.GetHeader(context.butCompact));
			}

			return versusBox;
		}

	}

}
