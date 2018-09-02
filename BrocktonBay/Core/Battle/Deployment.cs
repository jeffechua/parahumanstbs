using System.Collections.Generic;
using System;

namespace BrocktonBay {

	public sealed class Attack : Deployment {

		public override string name { get => "Attack on " + location.name; }

		[Displayable(1, typeof(ObjectField), forceHorizontal = true)]
		public override Parahuman leader { get; set; }

		[Displayable(5, typeof(ThreatSelectionField), turnLocked = true, affiliationLocked = true, editablePhases = Phase.Action)]
		public override Threat force_employed { get; set; }

		[Displayable(6, typeof(CellTabularListField<Team>), 2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Action)]
		public override List<Team> teams { get; set; }

		[Displayable(7, typeof(CellTabularListField<Parahuman>), 2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Action)]
		public override List<Parahuman> independents { get; set; }

		[Displayable(8, typeof(CellTabularListField<Parahuman>), -2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Action)]
		public override List<Parahuman> combined_roster { get; set; }

		[Displayable(99, typeof(ActionField), 5, fillSides = false, visiblePhases = Phase.Action)]
		public GameAction cancel { get; set; }

		public Attack (IBattleground location, IAgent affiliation) : this(location, affiliation, new List<Team>(), new List<Parahuman>()) { }

		public Attack (IBattleground location, IAgent affiliation, List<Team> teams, List<Parahuman> independents, Parahuman leader = null) {
			this.location = location;
			this.affiliation = affiliation;
			this.teams = teams;
			this.independents = independents;
			this.leader = leader;
			cancel = new GameAction {
				name = "Cancel attack",
				description = "Cancel this attack.",
				action = delegate {
					location.attacker = null;
					Game.city.activeBattlegrounds.Remove(location);
					DependencyManager.Delete(this);
					DependencyManager.Flag(location);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => Game.player == affiliation
			};
			Reload();
		}

	}

	public sealed class Defense : Deployment {

		public override string name { get => "Defense of " + location.name; }

		[Displayable(1, typeof(ObjectField), forceHorizontal = true)]
		public override Parahuman leader { get; set; }

		[Displayable(5, typeof(ThreatSelectionField), turnLocked = true, affiliationLocked = true, editablePhases = Phase.Response)]
		public override Threat force_employed { get; set; }

		[Displayable(6, typeof(CellTabularListField<Team>), 2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Response)]
		public override List<Team> teams { get; set; }

		[Displayable(7, typeof(CellTabularListField<Parahuman>), 2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Response)]
		public override List<Parahuman> independents { get; set; }

		[Displayable(8, typeof(CellTabularListField<Parahuman>), -2, emphasized = true, turnLocked = true, affiliationLocked = true, editablePhases = Phase.Response)]
		public override List<Parahuman> combined_roster { get; set; }

		[Displayable(99, typeof(ActionField), 5, fillSides = false, visiblePhases = Phase.Response)]
		public GameAction cancel { get; set; }

		public Defense (IBattleground location, IAgent affiliation) : this(location, affiliation, new List<Team>(), new List<Parahuman>()) { }

		public Defense (IBattleground location, IAgent affiliation, List<Team> teams, List<Parahuman> independents, Parahuman leader = null) {
			this.location = location;
			this.affiliation = affiliation;
			this.teams = teams;
			this.independents = independents;
			this.leader = leader;
			cancel = new GameAction {
				name = "Cancel defense",
				description = "Cancel this defense.",
				action = delegate {
					location.defender = null;
					DependencyManager.Delete(this);
					DependencyManager.Flag(location);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => Game.player == affiliation
			};
			Reload();
		}

	}

	public abstract class Deployment : IGUIComplete, IContainer, IRated, IDependable, IAffiliated {

		public int order { get { return 4; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public abstract string name { get; }
		public IBattleground location;

		public abstract Parahuman leader { get; set; }
		public abstract Threat force_employed { get; set; }
		public abstract List<Team> teams { get; set; }
		public abstract List<Parahuman> independents { get; set; }
		public abstract List<Parahuman> combined_roster { get; set; }

		[Displayable(3, typeof(ObjectField), forceHorizontal = true)]
		public IAgent affiliation { get; set; }
		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(9, typeof(TabularContainerField), "strength", "stealth", "insight",
					 altWidget = typeof(LinearContainerField), emphasizedIfVertical = true)]
		public Expression[] base_stats {
			get {
				return new Expression[] { strength, stealth, insight };
			}
			set {
				strength = value[0];
				stealth = value[1];
				insight = value[2];
			}
		}
		[ChildDisplayableAttribute("Strength", typeof(ExpressionField), tooltipText = "β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression strength { get; set; }
		[ChildDisplayableAttribute("Stealth", typeof(ExpressionField), tooltipText = "μ + φ + bonuses")]
		public Expression stealth { get; set; }
		[ChildDisplayableAttribute("Insight", typeof(ExpressionField), tooltipText = "ξ + Ω + bonuses")]
		public Expression insight { get; set; }

		[Displayable(10, typeof(RatingsMultiviewField), true, emphasized = true, verticalOnly = true, expand = true)]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		public Gtk.Widget GetHeader (Context context) => new Gtk.Label(name);
		public Gtk.Widget GetCellContents (Context context) => new Gtk.Label(name);

		public RatingsProfile GetRatingsProfile (Context context) {
			RatingsProfile profile = new RatingsProfile(context, teams, independents);
			if (affiliation != null) {
				int[] buffs = location.GetCombatBuffs(new Context(affiliation, this));
				for (int n = 0; n < 3; n++) {
					if (this is Attack) { //Negative bonus equals buff for attackers
						if (buffs[n] < 0) //Positive bonus equals buff for defenders
							profile.bonuses[n] -= buffs[n];
					} else {
						if (buffs[n] > 0)
							profile.bonuses[n] += buffs[n];
					}
				}
			}
			return profile;
		}

		public void Reload () {
			Sort();
			if (leader == null || !combined_roster.Contains(leader)) {
				if (combined_roster.Count > 0) {
					leader = combined_roster[0];
					foreach (Parahuman parahuman in combined_roster)
						if (parahuman.reputation > leader.reputation)
							leader = parahuman;
				} else {
					leader = null;
				}
			}
			threat = Threat.C;
			foreach (Parahuman parahuman in combined_roster)
				if (parahuman.threat > threat)
					threat = parahuman.threat;

			base_stats = ratings(new Context(affiliation, this)).GetStats(force_employed);

		}

		public void Apply (Fraction[] injury, Fraction[] escape) {
			float deathThresh = injury[0].val;
			float downThresh = injury[1].val + deathThresh;
			float injureThresh = injury[2].val + deathThresh;
			foreach (Parahuman parahuman in combined_roster) {
				float roll = Game.randomFloat;
				if (roll <= deathThresh) {
					parahuman.health = Health.Deceased;
				} else if (roll < downThresh) {
					parahuman.health = Health.Down;
				} else if (roll < injureThresh) {
					parahuman.health = Health.Injured;
				}
			}
			if (escape.Length > 1) {
				foreach (Parahuman parahuman in combined_roster) {
					float roll = Game.randomFloat;
					if (roll < escape[0].val)
						parahuman.health = Health.Captured;
				}
			}
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

		public bool Accepts (object obj) => (obj is Team || (obj is Parahuman && ((Parahuman)obj).health == Health.Healthy))
			&& !((GameObject)obj).isEngaged && (affiliation == null || ((IAffiliated)obj).affiliation == affiliation);
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

	}
}