﻿using System.Collections.Generic;
using System;

namespace Parahumans.Core {

	public sealed partial class Deployment : IContainer, IRated, IDependable, IAffiliated {

		public int order { get { return 4; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		[Displayable(1, typeof(ObjectField)), ForceHorizontal]
		public Parahuman leader { get; set; }

		[Displayable(2, typeof(ObjectField)), ForceHorizontal]
		public IAgent affiliation { get { return leader == null ? null : leader.affiliation; } }

		[Displayable(3, typeof(BasicReadonlyField))]
		public Alignment alignment { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(5, typeof(EnumField<Threat>)), PlayerEditable(Phase.All)]
		public Threat authorized_force { get; set; }

		[Displayable(6, typeof(CellTabularListField<Team>), 2), Emphasized, PlayerEditable(Phase.All)]
		public List<Team> teams { get; set; }

		[Displayable(7, typeof(CellTabularListField<Parahuman>), 2), Emphasized, PlayerEditable(Phase.All)]
		public List<Parahuman> independents { get; set; }

		[Displayable(8, typeof(CellTabularListField<Parahuman>), 2), Emphasized, PlayerEditable(Phase.All)]
		public List<Parahuman> combined_roster { get; set; }

		[Displayable(9, typeof(RatingsMultiviewField), true), Emphasized, VerticalOnly, Expand]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }


		public Deployment () : this(new List<Team>(), new List<Parahuman>()) { }

		public Deployment (List<Team> teams, List<Parahuman> independents, Parahuman leader = null) {
			this.teams = teams;
			this.independents = independents;
			this.leader = leader;
			Reload();
		}

		public RatingsProfile GetRatingsProfile (Context context) => new RatingsProfile(context, teams, independents);

		public void Reload () {
			Sort();
			if (combined_roster.Count == 0) {
				leader = null;
				return;
			}
			if (!combined_roster.Contains(leader)) {
				leader = combined_roster[0];
				foreach (Parahuman parahuman in combined_roster)
					if (parahuman.reputation > leader.reputation)
						leader = parahuman;
			}
			threat = Threat.C;
			foreach (Parahuman parahuman in combined_roster)
				if (parahuman.threat > threat)
					threat = parahuman.threat;
			alignment = leader.alignment;
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
			teams.Sort();
			independents.Sort();

			//Load stuff into combined_roster
			CombineRoster();

			//Remove all invalids
			combined_roster.RemoveAll((input) => input.health != Health.Healthy);

		}

		public void CombineRoster () {
			combined_roster = new List<Parahuman>();
			combined_roster.AddRange(independents);
			for (int i = 0; i < teams.Count; i++)
				combined_roster.AddRange(teams[i].roster);
		}

		public static Deployment operator + (Deployment a, Deployment b) {
			List<Team> teams = new List<Team>();
			List<Parahuman> independents = new List<Parahuman>();
			Parahuman leader = (a.leader.reputation >= b.leader.reputation) ? a.leader : b.leader;
			teams.AddRange(a.teams);
			teams.AddRange(b.teams);
			independents.AddRange(a.independents);
			independents.AddRange(a.independents);
			Deployment newDeployment = new Deployment(teams, independents, leader);
			return newDeployment;
		}

	}
}