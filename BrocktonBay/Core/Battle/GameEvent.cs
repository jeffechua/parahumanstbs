using Gtk;
using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public enum GameEventType {
		Attack = 0
	}

	public sealed class GameEvent : IGUIComplete {

		public int order { get { return 5; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Deployment[] deployments;
		public Deployment initiators { get { return deployments[0]; } set { deployments[0] = value; } }
		public Deployment responders { get { return deployments[1]; } set { deployments[1] = value; } }
		public EffectiveRatingsProfile[] profiles;

		public string name { get { return "Event at " + location.name; } }

		[Displayable(0, typeof(EnumField<GameEventType>))]
		public GameEventType type;

		[Displayable(1, typeof(ObjectField)), ForceHorizontal]
		public EventLocation location;

		[Displayable(2, typeof(EffectiveRatingsMultiview)), EmphasizedAttribute]
		public EffectiveRatingsProfile initiator_profile { get { return profiles[0]; } set { profiles[0] = value; } }


		[BimorphicDisplayable(3, typeof(TabularContainerField), typeof(LinearContainerField),
		                      new string[] { "initiator_strength", "initiator_stealth", "initiator_insight" }), EmphasizedIfVertical]
		public Expression[] initiator_stats {
			get {
				return new Expression[] { initiator_strength, initiator_stealth, initiator_insight };
			}
			set {
				initiator_strength = value[0];
				initiator_stealth = value[1];
				initiator_insight = value[2];
			}
		}

		[Child("Strength"), Displayable(0, typeof(ExpressionField)), TooltipText("β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression initiator_strength { get; set; }
		[Child("Stealth"), Displayable(0, typeof(ExpressionField)), TooltipText("μ + φ + bonuses")]
		public Expression initiator_stealth { get; set; }
		[Child("Insight"), Displayable(0, typeof(ExpressionField)), TooltipText("ξ + Ω + bonuses")]
		public Expression initiator_insight { get; set; }


		/*
		[Displayable(3, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] victory { get; set; }

		[Displayable(4, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] territory { get; set; }
		*/


		[BimorphicDisplayable(4, typeof(TabularContainerField), typeof(LinearContainerField),
		                      new string[] { "responder_strength", "responder_stealth", "responder_insight" }), EmphasizedIfVertical]
		public Expression[] responder_stats {
			get {
				return new Expression[] { responder_strength, responder_stealth, responder_insight };
			}
			set {
				responder_strength = value[0];
				responder_stealth = value[1];
				responder_insight = value[2];
			}
		}

		[Child("Strength"), Displayable(0, typeof(ExpressionField)), TooltipText("β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression responder_strength { get; set; }
		[Child("Stealth"), Displayable(0, typeof(ExpressionField)), TooltipText("μ + φ + bonuses")]
		public Expression responder_stealth { get; set; }
		[Child("Insight"), Displayable(0, typeof(ExpressionField)), TooltipText("ξ + Ω + bonuses")]
		public Expression responder_insight { get; set; }


		[Displayable(5, typeof(EffectiveRatingsMultiview)), EmphasizedAttribute]
		public EffectiveRatingsProfile responder_profile { get { return profiles[1]; } set { profiles[1] = value; } }

		Context context;

		public GameEvent (EventLocation location) {
			this.location = location;
			context = new Context(MainClass.playerAgent, this);
			deployments = new Deployment[] { new Deployment(), new Deployment() };
			profiles = new EffectiveRatingsProfile[2];
			DependencyManager.Connect(initiators, this);
			DependencyManager.Connect(responders, this);
			Reload();
		}

		public void Reload () {

			initiator_profile = GetEffectiveProfile(initiators.ratings(context), responders.ratings(context));
			responder_profile = GetEffectiveProfile(responders.ratings(context), initiators.ratings(context));

			initiator_stats = GetStats(0);
			responder_stats = GetStats(1);

			/*
			Deployment.Compare(initiators, responders);

			victory = new Fraction[2];
			victory[0] = new Fraction(initiators.affiliation.name, initiators.strength.result / (initiators.strength.result + responders.strength.result),
										Graphics.GetColor(initiators.alignment));
			victory[1] = new Fraction(responders.name, responders.strength.result / (initiators.strength.result + responders.strength.result),
										Graphics.GetColor(responders.alignment));

			if (victory[0].val > 0.33) {
				territory = new Fraction[3];
				territory[0] = new Fraction("Capture", victory[0].val * victory[0].val, new Gdk.Color(0, 200, 0));
				territory[1] = new Fraction("Raze", victory[0].val - victory[0].val * victory[0].val, new Gdk.Color(200, 0, 0));
				territory[2] = new Fraction("Safe", victory[1].val, new Gdk.Color(0, 0, 200));
			} else {
				territory = new Fraction[2];
				territory[0] = new Fraction("Raze", victory[0].val, new Gdk.Color(200, 0, 0));
				territory[1] = new Fraction("Safe", victory[1].val, new Gdk.Color(0, 0, 200));
			}
			*/

		}

		public Expression[] GetStats (int i) {

			Expression[] expressions = {
				new Expression("@0 + @1 + ½(@2) + ½(@3) = @4\n" +
							   "@4 + @5 (bonus) = @6\n" +
							   "@6 × @7 (force) = @8",
							   "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.00", "0.0"),
				new Expression("@0 + @1 = @2\n" +
							   "@2 + @3 = @4",
							   "0.0", "0.0", "0.0", "0.0", "0.0"),
				new Expression("@0 + @1 = @2\n" +
							   "@2 + @3 = @4",
							   "0.0", "0.0", "0.0", "0.0", "0.0")
			};

			RatingsProfile profile = profiles[i].final;
			float[,] values = profile.values;

			//Strength
			float baseStrength = values[4, 1] + values[4, 2] + values[4, 3] / 2 + values[4, 4] / 2;
			float plusBonus = baseStrength + profile.bonuses[0];
			float forceMult = ForceMult(deployments[i].authorized_force);
			float plusForce = plusBonus * forceMult;
			expressions[0].SetValues(values[4, 1], values[4, 2], values[4, 3], values[4, 4],
									 baseStrength, profile.bonuses[0], plusBonus, forceMult, plusForce);
			//Stealth
			expressions[1].SetValues(values[4, 5], values[4, 6], values[4, 5] + values[4, 6],
									 profile.bonuses[1], values[4, 5] + values[4, 6] + profile.bonuses[1]);
			//Insight
			expressions[2].SetValues(values[4, 7], values[4, 8], values[4, 7] + values[4, 8],
									 profile.bonuses[2], values[4, 7] + values[4, 8] + profile.bonuses[2]);

			return expressions;

		}

		public EffectiveRatingsProfile GetEffectiveProfile (RatingsProfile original, RatingsProfile enemy) {
			
			float[,] originalValues = original.values;
			float[,] enemyValues = enemy.values;

			// Calculate the multipliers. Reference for matching indices:
			//  1   2   3   4   5   6   7   8   9   10  11
			// BRT BLS SHK SRK MOV SRN THK TRM TNK MSR BRK

			float[] multipliers = new float[9];
			float[] metamultipliers = new float[5];

			multipliers[0] = 1;                                     //The [0] slot is reserved for the wrapper's cosmetic rating
			multipliers[1] = Mult(enemyValues[4, 4]) * Mult(enemyValues[4, 5]); //Brute:    Mover -10%, Striker -10%
			multipliers[2] = Mult(enemyValues[4, 3]) * Mult(enemyValues[4, 6]); //Blaster:  Stranger -10%, Shaker -10%
			multipliers[3] = Mult(enemyValues[4, 1]);                            //Shaker:   Brute -10%
			multipliers[4] = Mult(enemyValues[4, 2]);                            //Striker:  Blaster -10%
			multipliers[5] = 1;                                     //Mover:     NONE
			multipliers[6] = 1;                                     //Stranger:  NONE
			multipliers[7] = 1;                                     //Thinker:   NONE
			multipliers[8] = 1;                                     //Trump:     NONE

			metamultipliers[0] = Mult(enemyValues[4, 8]); //Base:     Trump -10%
			metamultipliers[1] = Mult(enemyValues[4, 4]); //Tinker:   Striker -10%
			metamultipliers[2] = Mult(enemyValues[4, 3]); //Master:   Shaker -10%
			metamultipliers[3] = Mult(enemyValues[4, 7]); //Breaker:  Thinker -10%

			// Multiply the multipliers

			float[,] finalValues = new float[5, 9];

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 9; j++) {
					if (i == 3) {
						finalValues[i, j] = originalValues[i, j] * metamultipliers[i];
						//Breakers are immune to regular multipliers, only vulnerable to its wrapper metamultiplier (Thinker)
					} else {
						finalValues[i, j] = originalValues[i, j] * multipliers[j] * metamultipliers[i];
					}
				}
			}

			for (int i = 0; i < 9; i++) //The fourth row is the "sum" row.
				finalValues[4, i] = finalValues[0, i] + finalValues[1, i] + finalValues[2, i] + finalValues[3, i];

			RatingsProfile final = new RatingsProfile(finalValues);

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
				if (initiators.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(initiators.affiliation.GetHeader(context.butCompact));
				}
				versusBox.PackStart(new Label(" vs. "), false, false, 5);
				if (responders.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(responders.affiliation.GetHeader(context.butCompact));
				}
				headerBox.PackStart(new Gtk.Alignment(0.5f, 0, 0, 0) { Child = versusBox }, false, false, 0);

				return headerBox;
			}
		}

		public Widget GetCell (Context context) {
			VBox versusBox = new VBox();

			if (initiators.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(initiators.affiliation.GetHeader(context.butCompact));
			}
			versusBox.PackStart(new Label(" VERSUS "));
			if (responders.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(responders.affiliation.GetHeader(context.butCompact));
			}

			return versusBox;
		}

	}

}
