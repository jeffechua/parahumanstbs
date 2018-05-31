using System.Collections.Generic;
using Parahumans.Core.GUI;
using System;

namespace Parahumans.Core {

	public sealed partial class Deployment : GUIComplete, IContainer {

		public override int order { get { return 4; } }

		public override string name {
			get {
				if (teams.Count > 0) {
					return teams[0].name + ((teams.Count > 1) ? (" [+" + (teams.Count - 1) + "]") : "");
				} else {
					return "Empty";
				}
			}
		}

		[Displayable(2, typeof(BasicReadonlyField))]
		public Alignment alignment { get; set; }

		[Displayable(3, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat authorized_force { get; set; }

		[Displayable(5, typeof(CellObjectListField<Team>), -2), Emphasized]
		public List<Team> teams { get; set; }

		[Displayable(6, typeof(CellObjectListField<Parahuman>), -2), Emphasized]
		public List<Parahuman> independents { get; set; }

		[Displayable(7, typeof(CellObjectListField<Parahuman>), -3), Emphasized]
		public List<Parahuman> combined_roster { get; set; }

		[Displayable(8, typeof(RatingsComparisonField), false), Emphasized]
		public RatingsComparison ratings { get; set; }

		[Displayable(9, typeof(ExpressionField)), TooltipText("β + δ + Σ + ψ\n+ pop. + xp\n+ bonuses")]
		public Expression strength { get; set; }

		[Displayable(10, typeof(ExpressionField)), TooltipText("μ + φ\n+ (3 - pop) + xp\n+ bonuses")]
		public Expression mobility { get; set; }

		[Displayable(11, typeof(ExpressionField)), TooltipText("ξ + Ω\n+ 1 + xp\n + bonuses")]
		public Expression insight { get; set; }

		[Displayable(12, typeof(FractionsBar)), TooltipText("STR<sub>enemy</sub> / STR<sub>self</sub> / 4"), Emphasized]
		public Fraction[] injury { get; set; } //Chance of being injured, per member

		[Displayable(13, typeof(FractionsBar)), TooltipText("MOB<sub>self</sub> / MOB<sub>enemy</sub> / 2"), Emphasized]
		public Fraction[] escape { get; set; } //Chance of escaping, per member

		[Displayable(14, typeof(FractionsBar)), TooltipText("INS / 10"), Emphasized]
		public Fraction[] appraisal { get; set; } //Chance of gaining an insight, per member

		public Deployment enemy;

		public Deployment () {
			teams = new List<Team>();
			independents = new List<Parahuman>();
			Reload();
		}

		public override void Reload () {

			ratings = new RatingsComparison();

			Sort();
			if (combined_roster.Count == 0) return;

			threat = combined_roster[0].threat;
			alignment = combined_roster[0].alignment;

			//Load all ratings
			for (int i = 0; i < combined_roster.Count; i++) {
				for (int j = 0; j < combined_roster[i].ratings.Count; j++) {
					if ((int)combined_roster[i].ratings[j].clssf <= 7) {
						ratings.values[0][4, (int)combined_roster[i].ratings[j].clssf] += combined_roster[i].ratings[j].num;
						ratings.values[0][0, (int)combined_roster[i].ratings[j].clssf] += combined_roster[i].ratings[j].num;
					} else {
						for (int k = 0; k < combined_roster[i].ratings[j].subratings.Count; k++) {
							Rating subrating = combined_roster[i].ratings[j].subratings[k];
							ratings.values[0][4, (int)subrating.clssf] += subrating.num;
							ratings.values[0][(int)combined_roster[i].ratings[j].clssf - 7, (int)subrating.clssf] += subrating.num;
						}
					}
				}
			}

		}

		public static float GetMultiplier (float x, float from, float to, float rate)
			=> (float)((from - to) * Math.Exp(rate * x / (from - to)) + to);

		public void AddRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj is Team && !teams.Contains((Team)obj)) {
					RemoveRange(((Team)obj).roster);
					teams.Add((Team)obj);
					obj.Engage();
					DependencyManager.Connect(obj, this);
				}
				if (obj is Parahuman) {
					independents.Add((Parahuman)obj);
					obj.Engage();
					DependencyManager.Connect(obj, this);
				}
			}
			DependencyManager.Flag(this);
		}

		public void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj is Team) {
					teams.Remove((Team)obj);
					obj.Disengage();
					DependencyManager.Disconnect(obj, this);
				}
				if (obj is Parahuman) {
					independents.Remove((Parahuman)obj);
					obj.Disengage();
					DependencyManager.Disconnect(obj, this);
				}
			}
			DependencyManager.Flag(this);
		}

		public bool Accepts (object obj) => (obj is Team || (obj is Parahuman && ((Parahuman)obj).health == Health.Healthy)) && !((GameObject)obj).isEngaged;

		public bool Contains (object obj) => (obj is Parahuman && independents.Contains((Parahuman)obj)) || (obj is Team && teams.Contains((Team)obj));

		public void Sort () {

			combined_roster = new List<Parahuman>();

			//Sort stuff.
			teams.Sort((x, y) => y.ID.CompareTo(x.ID));
			teams.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			teams.Sort((x, y) => y.threat.CompareTo(x.threat));
			independents.Sort((x, y) => y.ID.CompareTo(x.ID));
			independents.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			independents.Sort((x, y) => y.threat.CompareTo(x.threat));

			//Load all teams into roster and remove all invalids
			combined_roster.AddRange(independents);
			for (int i = 0; i < teams.Count; i++)
				combined_roster.AddRange(teams[i].roster);
			combined_roster.RemoveAll((input) => input.health != Health.Healthy);

			//Sort more stuff
			combined_roster.Sort((x, y) => y.ID.CompareTo(x.ID));
			combined_roster.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			combined_roster.Sort((x, y) => y.threat.CompareTo(x.threat));

		}

		public static Deployment operator + (Deployment a, Deployment b) {
			Deployment newDeployment = new Deployment();
			newDeployment.teams.AddRange(a.teams);
			newDeployment.teams.AddRange(b.teams);
			newDeployment.independents.AddRange(a.independents);
			newDeployment.independents.AddRange(a.independents);
			return newDeployment;
		}

	}
}