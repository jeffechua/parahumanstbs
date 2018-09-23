using System.Collections.Generic;
using System;
using Gtk;

namespace BrocktonBay {

	public sealed class Attack : Deployment {

		public override string name { get => "Attack on " + location.name; }

		[Displayable(5, typeof(DeploymentForceSelectionField), editLocks = Locks.Turn, editablePhases = Phase.Action)]
		public object placemarker { get => this; } // Useless property to host DeploymentForceSelectionField

		[Displayable(6, typeof(SelectivelyEditableCellTabularListField<Team>), 2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Action)]
		public override List<Team> teams { get; set; }

		[Displayable(7, typeof(SelectivelyEditableCellTabularListField<Parahuman>), 2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Action)]
		public override List<Parahuman> independents { get; set; }

		[Displayable(8, typeof(SelectivelyEditableCellTabularListField<Parahuman>), -2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Action)]
		public override List<Parahuman> combined_roster { get; set; }

		[Displayable(98, typeof(ActionField), 5, fillSides = false, viewLocks = Locks.All, visiblePhases = Phase.Action, editablePhases = Phase.Action)]
		public GameAction cancel { get; set; }

		[Displayable(99, typeof(ActionField), 5, fillSides = false, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Response, editablePhases = Phase.Response)]
		public GameAction oppose { get; set; }

		public Attack (IBattleground location) : this(location, new List<Team>(), new List<Parahuman>()) { }

		public Attack (IBattleground location, List<Team> teams, List<Parahuman> independents) {
			proposedForces = new Dictionary<IAgent, Threat>();
			proposedForces.Add(Game.turnOrder[Game.turn], Threat.C);
			this.location = location;
			this.teams = teams;
			this.independents = independents;
			DependencyManager.Connect(location, this);
			cancel = new GameAction {
				name = "Cancel attack",
				description = "Cancel this attack.",
				action = delegate {
					location.attackers = null;
					Game.city.activeBattlegrounds.Remove(location);
					RemoveRange(teams.ToArray());
					RemoveRange(independents.ToArray());
					DependencyManager.Destroy(this);
					DependencyManager.Flag(location);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "cancel")
			};
			oppose = new GameAction {
				name = "Mount opposition",
				description = "Mount a defense of " + location.name + " against this attack force",
				action = delegate (Context context) {
					location.defenders = new Defense(location);
					DependencyManager.Connect(location, location.defenders);
					DependencyManager.Flag(location);
					DependencyManager.TriggerAllFlags();
					Inspector.InspectInNearestInspector(location.defenders, MainWindow.main);
				},
				condition = (context) => location.attackers != null && location.defenders == null && UIFactory.EditAuthorized(this, "oppose")
			};
			Reload();
		}

		public override Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = base.GetRightClickMenu(context, rightClickedWidget);
			if (UIFactory.EditAuthorized(this, "cancel")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateActionButton(cancel, context));
			}
			if (UIFactory.EditAuthorized(this, "oppose")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateActionButton(oppose, context));
			}
			return rightClickMenu;
		}

	}

	public sealed class Defense : Deployment {

		public override string name { get => "Defense of " + location.name; }

		[Displayable(5, typeof(DeploymentForceSelectionField), editLocks = Locks.Turn, editablePhases = Phase.Response)]
		public object placemarker { get => this; } // Useless property to host DeploymentForceSelectionField

		[Displayable(6, typeof(SelectivelyEditableCellTabularListField<Team>), 2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Response)]
		public override List<Team> teams { get; set; }

		[Displayable(7, typeof(SelectivelyEditableCellTabularListField<Parahuman>), 2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Response)]
		public override List<Parahuman> independents { get; set; }

		[Displayable(8, typeof(SelectivelyEditableCellTabularListField<Parahuman>), -2, emphasized = true, editLocks = Locks.Turn, editablePhases = Phase.Response)]
		public override List<Parahuman> combined_roster { get; set; }

		[Displayable(99, typeof(ActionField), 5, fillSides = false, viewLocks = Locks.All, visiblePhases = Phase.Response, editablePhases = Phase.Response)]
		public GameAction cancel { get; set; }

		public Defense (IBattleground location) : this(location, new List<Team>(), new List<Parahuman>()) { }

		public Defense (IBattleground location, List<Team> teams, List<Parahuman> independents) {
			proposedForces = new Dictionary<IAgent, Threat>();
			proposedForces.Add(Game.turnOrder[Game.turn], Threat.C);
			this.location = location;
			this.teams = teams;
			this.independents = independents;
			DependencyManager.Connect(location, this);
			cancel = new GameAction {
				name = "Cancel defense",
				description = "Cancel this defense.",
				action = delegate {
					location.defenders = null;
					RemoveRange(teams.ToArray());
					RemoveRange(independents.ToArray());
					DependencyManager.Destroy(this);
					DependencyManager.Flag(location);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "cancel")
			};
			Reload();
		}

		public override Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = base.GetRightClickMenu(context, rightClickedWidget);
			if (UIFactory.EditAuthorized(this, "cancel")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateActionButton(cancel, context));
			}
			return rightClickMenu;
		}

	}

	public abstract class Deployment : IGUIComplete, IContainer, IRated, IDependable, IAffiliated {

		public int order { get { return 4; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public abstract string name { get; }
		public IBattleground location;

		public Threat force_employed { get => proposedForces[affiliation]; }
		public Dictionary<IAgent, Threat> proposedForces;
		public abstract List<Team> teams { get; set; }
		public abstract List<Parahuman> independents { get; set; }
		public abstract List<Parahuman> combined_roster { get; set; }


		[Displayable(3, typeof(ObjectField), forceHorizontal = true)]
		public IAgent affiliation { get; set; }
		public bool isMixedAffiliation { get => combined_roster.Find((p) => p.affiliation != affiliation) != null; }
		public bool ContainsForcesFrom (IAgent agent) => combined_roster.Find((p) => p.affiliation == agent) != null;
		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(9, typeof(TabularContainerField), "strength", "stealth", "insight",
					 altWidget = typeof(SlashDelimitedContainerField), emphasizedIfVertical = true)]
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
		[ChildDisplayable("Strength", typeof(ExpressionField), tooltipText = "β + δ + ½Σ + ½ψ + bonuses\n× force multiplier")]
		public Expression strength { get; set; }
		[ChildDisplayable("Stealth", typeof(ExpressionField), tooltipText = "μ + φ + bonuses")]
		public Expression stealth { get; set; }
		[ChildDisplayable("Insight", typeof(ExpressionField), tooltipText = "ξ + Ω + bonuses")]
		public Expression insight { get; set; }

		[Displayable(10, typeof(RatingsMultiviewField), true, emphasized = true, verticalOnly = true, expand = true)]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		public Widget GetHeader (Context context) => new InspectableBox(new Label(name), this, context, false);
		public Widget GetCellContents (Context context) => new Label(name);
		public virtual Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = new Menu();
			rightClickMenu.Append(MenuFactory.CreateInspectButton(this, rightClickedWidget));
			rightClickMenu.Append(MenuFactory.CreateInspectInNewWindowButton(this));
			return rightClickMenu;
		}
		public void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) {
			if (Contains(member) && UIFactory.EditAuthorized(this, "teams")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			}
		}

		public RatingsProfile GetRatingsProfile (Context context) {
			RatingsProfile profile = new RatingsProfile(context, teams, independents);
			if (affiliation != null) {
				int[] buffs = location.GetCombatBuffs(new Context(this, affiliation));
				for (int n = 0; n < 3; n++)
					if (this is Defense)
						profile.bonuses[n] += buffs[n];
			}
			return profile;
		}

		public void Reload () {

			//So the newly damaged parahumans don't get ejected from the Deployment the moment it Applies a battle.
			if ((Game.phase & (Phase.Action | Phase.Response)) == Phase.None) return;

			Sort();

			//Determine threat - the max amongst all in deployment
			threat = Threat.C;
			foreach (Parahuman parahuman in combined_roster)
				if (parahuman.threat > threat)
					threat = parahuman.threat;

			//Determine affiliation - the faction that has the most deployed in this deployment
			Dictionary<IAgent, int> counts = new Dictionary<IAgent, int>();
			foreach (IAgent agent in proposedForces.Keys) counts.Add(agent, 0);
			foreach (Parahuman parahuman in combined_roster) counts[parahuman.affiliation]++;
			foreach (KeyValuePair<IAgent, int> pair in counts)
				if (affiliation == null || pair.Value > counts[affiliation])
					affiliation = pair.Key;

			//Evaluate the base strength, stealth and insight of the deployment
			base_stats = ratings(new Context(this, Game.player)).GetStats(force_employed);

		}

		public void OnTriggerDestroyed (IDependable trigger) {
			if (Contains(trigger)) {
				Remove(trigger);
				DependencyManager.Flag(this);
			}
		}
		public void OnListenerDestroyed (IDependable listener) { }

		public void Apply (Fraction[] injury, Fraction[] escape, Deployment enemy) {
			float deathThresh = injury[0].val;
			float downThresh = injury[1].val + deathThresh;
			float injureThresh = injury[2].val + deathThresh;
			foreach (Parahuman parahuman in combined_roster) {
				float roll = Game.randomFloat;
				if (roll <= deathThresh) {
					parahuman.status = Status.Deceased;
				} else if (roll < downThresh) {
					parahuman.status = Status.Down;
				} else if (roll < injureThresh) {
					parahuman.status = Status.Injured;
				} else {
					parahuman.status = Status.Resting;
				}
			}
			if (escape.Length > 1) {
				Faction prisonerDestination = (enemy.affiliation as Faction) ??
					(enemy.affiliation.alignment < 0 ? Game.city.villainousAuthority : Game.city.heroicAuthority);
				foreach (Parahuman parahuman in combined_roster) {
					float roll = Game.randomFloat;
					if (roll < escape[0].val) {
						parahuman.status = Status.Captured;
						TraitData prisonerData = new TraitData {
							name = "Prisoner",
							type = "Prisoner",
							secrecy = 0,
							description = "This parahuman has been captured by another faction.\nThey will remain in captivity until violently liberated or released.",
							effect = prisonerDestination.ID.ToString()
						};
						parahuman.Add(Trait.Load(prisonerData));
						prisonerDestination.unassignedCaptures.Add(parahuman);
					}
				}
			}
		}

		public static float GetMultiplier (float x, float from, float to, float rate)
			=> (float)((from - to) * Math.Exp(rate * x / (from - to)) + to);

		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });

		public void AddRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				if (GameObject.TryCast(obj, out Team team) && !teams.Contains(team)) {
					RemoveRange(team.roster);
					teams.Add(team);
					team.Engage();
					if (!proposedForces.ContainsKey(team.affiliation)) proposedForces.Add(team.affiliation, Threat.C);
					DependencyManager.Connect(team, this);
				}
				if (GameObject.TryCast(obj, out Parahuman parahuman) && !combined_roster.Contains(parahuman)) {
					independents.Add(parahuman);
					parahuman.Engage();
					if (!proposedForces.ContainsKey(parahuman.affiliation)) proposedForces.Add(parahuman.affiliation, Threat.C);
					DependencyManager.Connect(parahuman, this);
					if (parahuman.parent is Team &&
						((Team)parahuman.parent).roster.TrueForAll((member) => independents.Contains(member)))
						Add(parahuman.parent);
				}
			}
			MainWindow.mainInterface.assetsBar.UpdateEngagement();
			DependencyManager.Flag(location);
		}

		public void RemoveRange<T> (IEnumerable<T> objs) {
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
			MainWindow.mainInterface.assetsBar.UpdateEngagement();
			DependencyManager.Flag(location);
		}

		public bool Accepts (object obj) => (obj is Team || (obj is Parahuman && ((Parahuman)obj).status == Status.Healthy)) && !((GameObject)obj).isEngaged;
		public bool Contains (object obj) => (obj is Parahuman && independents.Contains((Parahuman)obj)) || (obj is Team && teams.Contains((Team)obj));

		public void Sort () {

			//Sort stuff.
			teams.Sort();
			independents.Sort();

			//Load stuff into combined_roster
			CombineRoster();

			//Remove all invalids
			combined_roster.RemoveAll((input) => input.status != Status.Healthy);

		}

		public void CombineRoster () {
			combined_roster = new List<Parahuman>();
			combined_roster.AddRange(independents);
			for (int i = 0; i < teams.Count; i++)
				combined_roster.AddRange(teams[i].roster);
		}

	}

}