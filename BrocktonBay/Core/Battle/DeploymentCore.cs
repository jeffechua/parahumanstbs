using System.Collections.Generic;
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

		[Displayable(5, typeof(CellObjectListField<Team>), 2), Emphasized]
		public List<Team> teams { get; set; }

		[Displayable(6, typeof(CellObjectListField<Parahuman>), 3), Emphasized]
		public List<Parahuman> independents { get; set; }

		[Displayable(7, typeof(CellObjectListField<Parahuman>), 3), Emphasized]
		public List<Parahuman> combined_roster { get; set; }

		public RatingsProfile ratings_profile { get { return new RatingsProfile(new Context(this, 0), teams, independents); } }

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
			ratings.values[0] = ratings_profile.values;

		}

		public static float GetMultiplier (float x, float from, float to, float rate)
			=> (float)((from - to) * Math.Exp(rate * x / (from - to)) + to);

		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });

		public void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				if (obj is Team && !teams.Contains((Team)obj)) {
					Team team = (Team)obj;
					RemoveRange(team.roster);
					teams.Add(team);
					team.Engage();
					DependencyManager.Connect(team, this);
				}
				if (obj is Parahuman && !combined_roster.Contains((Parahuman)obj)) {
					Parahuman parahuman = (Parahuman)obj;
					independents.Add(parahuman);
					parahuman.Engage();
					DependencyManager.Connect(parahuman, this);
					if (parahuman.parent is Team &&
						((Team)parahuman.parent).roster.TrueForAll((member) => independents.Contains(member)))
						Add(parahuman.parent);
				}
			}
			DependencyManager.Flag(this);
		}

		public void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				if (obj is Team) {
					Team team = (Team)obj;
					teams.Remove(team);
					team.Disengage();
					foreach (Parahuman parahuman in team.roster)
						parahuman.Disengage();
					DependencyManager.Disconnect(team, this);
				}
				if (obj is Parahuman) {
					Parahuman parahuman = (Parahuman)obj;
					if (independents.Contains(parahuman)) {
						independents.Remove(parahuman);
						parahuman.Disengage();
						DependencyManager.Disconnect(parahuman, this);
					} else if (combined_roster.Contains(parahuman)) {
						Team team = (Team)parahuman.parent;
						Remove(team);
						CombineRoster(); //Add(parahuman) rejects if combined_roster contains parahuman.
						foreach (Parahuman member in team.roster)
							if (member != parahuman)
								Add(member);
					}
				}
			}
			DependencyManager.Flag(this);
		}

		public bool Accepts (object obj) => (obj is Team || (obj is Parahuman && ((Parahuman)obj).health == Health.Healthy)) && !((GameObject)obj).isEngaged;

		public bool Contains (object obj) => (obj is Parahuman && independents.Contains((Parahuman)obj)) || (obj is Team && teams.Contains((Team)obj));

		public void Sort () {

			//Sort stuff.
			teams.Sort((x, y) => y.ID.CompareTo(x.ID));
			teams.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			teams.Sort((x, y) => y.threat.CompareTo(x.threat));
			independents.Sort((x, y) => y.ID.CompareTo(x.ID));
			independents.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			independents.Sort((x, y) => y.threat.CompareTo(x.threat));

			//Load all teams into roster and remove all invalids
			CombineRoster();
			combined_roster.RemoveAll((input) => input.health != Health.Healthy);

			//Sort more stuff
			combined_roster.Sort((x, y) => y.ID.CompareTo(x.ID));
			combined_roster.Sort((x, y) => x.alignment.CompareTo(y.alignment));
			combined_roster.Sort((x, y) => y.threat.CompareTo(x.threat));

		}

		public void CombineRoster () {
			combined_roster = new List<Parahuman>();
			combined_roster.AddRange(independents);
			for (int i = 0; i < teams.Count; i++)
				combined_roster.AddRange(teams[i].roster);
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