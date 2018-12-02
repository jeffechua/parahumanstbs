using System;
using System.Collections.Generic;
using Gtk;


namespace BrocktonBay {

	[Flags]
	public enum Phase {
		Event = 1 << 0,
		Action = 1 << 1,
		Response = 1 << 2,
		Resolution = 1 << 3,
		Mastermind = 1 << 4,
		All = int.MaxValue,
		None = 0
	}

	public struct EventLog {
		string text;
		IGUIComplete[] elements;
		public EventLog (string text, params IGUIComplete[] elements) {
			this.text = text;
			this.elements = elements;
		}
		public HBox Print () {
			HBox box = new HBox();
			string[] fragments = text.Split('@');
			for (int i = 0; i < fragments.Length; i++) {
				box.PackStart(new Label(fragments[i]), false, false, 0);
				if (i != fragments.Length - 1)
					box.PackStart(elements[i].GetHeader(new Context(null, Game.player, true, true)), false, false, 0);
			}
			return box;
		}
	}


	class Game {

		static Random random = new Random();
		public static float randomFloat { get => (float)random.NextDouble(); }

		public static City city;
		public static IAgent player;
		public static Phase phase = Phase.Action;
		public static int turn;
		public static List<IAgent> turnOrder;

		public static int year;
		public static int month;

		public const int maxLogMemory = 50;
		public static int logNumber;
		public static List<EventLog> eventLogs;

		public static DependableShell UIKey; // A "key" connected to all IDependable UI elements. "Turned" (flagged) to induce a reload across the board.

		public static bool omniscient {
			get => MainWindow.omniscientToggle.Active;
			set => MainWindow.omniscientToggle.Active = value;
		}
		public static bool omnipotent {
			get => MainWindow.omnipotentToggle.Active;
			set => MainWindow.omnipotentToggle.Active = value;
		}

		public static void Main (string[] args) {

			UIKey = new DependableShell(0);

			//Inits application
			Application.Init();
			MainWindow.Initialize();
			Application.Run();

		}

		public static void Load (City city) {
			Game.city = city;
			phase = Phase.Action;
			turn = 0;
			logNumber = 0;
			eventLogs = new List<EventLog>();
			UpdateTurnOrder();
			MainWindow.Load();
			AppendLog(new EventLog("Save loaded."));
		}

		public static void Unload () {
			city = null;
			MainWindow.Unload();
			Map.Unload();
		}

		public static void SetPlayer (IAgent agent) {
			player = agent;
			foreach (IBattleground battleground in city.activeBattlegrounds) { // Since knowledge affects the values in deployments
				if (battleground.attackers != null) DependencyManager.Flag(battleground.attackers);
				if (battleground.defenders != null) DependencyManager.Flag(battleground.defenders);
			}
			RefreshUIAndTriggerAllFlags();
		}

		public static void RefreshUIAndTriggerAllFlags () {
			DependencyManager.Flag(UIKey);
			DependencyManager.TriggerAllFlags();
		}

		public static void AppendLog (EventLog log) {
			eventLogs.Add(log);
			MainWindow.mainInterface.eventLogsDisplay.PackStart(log.Print(), false, false, 3);
			logNumber++;
			if (eventLogs.Count > maxLogMemory) {
				eventLogs.RemoveAt(0);
				MainWindow.mainInterface.eventLogsDisplay.Remove(MainWindow.mainInterface.eventLogsDisplay.Children[0]);
			}
			MainWindow.mainInterface.eventLogsDisplay.ShowAll();
		}

		public static bool CanNext () {
			return true;
		}

		public static void Next () {
			switch (phase) {
				case Phase.Event:
					phase = Phase.Action;
					break;
				case Phase.Action:
					ResolveActionTurn();
					if (turn < turnOrder.Count - 1) {
						turn++;
						if (turnOrder[turn] != player)
							turnOrder[turn].TakeActionPhase();
					} else {
						turn = 0;
						phase = Phase.Response;
					}
					break;
				case Phase.Response:
					ResolveResponseTurn();
					if (turn < turnOrder.Count - 1) {
						turn++;
						if (turnOrder[turn] != player)
							turnOrder[turn].TakeResponsePhase();
					} else {
						turn = 0;
						phase = Phase.Resolution;
						ResolutionPhase();
					}
					break;
				case Phase.Resolution:
					phase = Phase.Mastermind;
					break;
				case Phase.Mastermind:
					if (GameObject.TryCast(Game.turnOrder[turn], out Faction faction)) {
						if (faction.unassignedCaptures.Count > 0) {
							MessageDialog dialog = new MessageDialog(MainWindow.main, 0, MessageType.Error, ButtonsType.Ok,
																	 "You have prisoners not assigned to any prison.\n" +
																	 "Check for tactical structures with the trait \"Prison\".");
							dialog.Run();
							dialog.Destroy();
							break;
						}
					}
					ResolveMastermindTurn();
					if (turn < turnOrder.Count - 1) {
						turn++;
						if (turnOrder[turn] != player)
							turnOrder[turn].TakeMastermindPhase();
					} else {
						turn = 0;
						phase = Phase.Event;
						EventPhase();
					}
					break;
				default:
					throw new Exception("Invalid current game phase.");
			}
			RefreshUIAndTriggerAllFlags();
		}

		static void ResolveMastermindTurn () {
		}

		static void EventPhase () {
			UpdateTurnOrder();
			GameObject.ClearEngagements();
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				battleground.battle = null;
				battleground.attackers = null;
				battleground.defenders = null;
			}
			city.activeBattlegrounds.Clear();
			GameObject[] gameObjects = city.gameObjects.ToArray();
			foreach (GameObject obj in gameObjects) {
				foreach (Trait trait in obj.traits)
					if (trait.trigger.Contains(EffectTrigger.EventPhase))
						trait.Invoke(EffectTrigger.EventPhase);
				if (obj.TryCast(out Faction faction)) {
					foreach (Territory territory in faction.territories) {
						faction.resources += territory.resource_income;
						territory.reputation += territory.reputation_income;
					}
				} else if (obj.TryCast(out Parahuman parahuman)) {
					if (parahuman.status == Status.Deceased) {
						DependencyManager.Destroy(parahuman);
					} else if ((int)parahuman.status > 0) {
						parahuman.status--;
					}
				} else if (obj.TryCast(out Structure structure)) {
					if (structure.rebuild_time > 0) {
						structure.rebuild_time--;
					}
				}
			}
		}

		static void ResolveActionTurn () {
			for (int i = 0; i < city.activeBattlegrounds.Count; i++) {
				IBattleground battleground = city.activeBattlegrounds[i];
				if (battleground.attackers.combined_roster.Count == 0) {
					battleground.attackers.cancel.action(new Context(null));
					i--; // Since no attack = no longer active battleground, the element is removed from the list.
				} else {
					string text;
					if (battleground.attackers.affiliation == turnOrder[turn]) { //Is the turn-taker leading the attack?
						if (battleground.attackers.isMixedAffiliation) {         //If so, Are there foreign forces in the attack?
							text = "[@] reinforced and took command of the [@]"; //If so, the turn-taker has reinforced and taken command of a pre-existing attack by numbers
						} else {
							text = "[@] launched an [@]";                        //Otherwise, the turn-taker has launched its own assault.
						}
					} else if (battleground.attackers.ContainsForcesFrom(turnOrder[turn])) { //If the turn-taker is not leading the attack, but is in it
						text = "[@] reinforced the [@]";                                     //then they have reinforced the attack.
					} else {
						continue;
					}
					AppendLog(new EventLog(text, turnOrder[turn], battleground.attackers));
				}
			}
		}

		static void ResolveResponseTurn () {
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				if (battleground.defenders == null) continue;
				if (battleground.defenders.combined_roster.Count == 0) {
					battleground.defenders.cancel.action(new Context(null));
				} else {
					string text;
					if (battleground.defenders.affiliation == turnOrder[turn]) { //Is the turn-taker leading the attack?
						if (battleground.defenders.isMixedAffiliation) {         //If so, Are there foreign forces in the attack?
							text = "[@] reinforced and took command of the [@]";              //If so, the turn-taker has reinforced and taken command of a pre-existing attack by numbers
						} else {
							text = "[@] mounted a [@]";                       //Otherwise, the turn-taker has launched its own assault.
						}
					} else if (battleground.defenders.ContainsForcesFrom(turnOrder[turn])) { //If the turn-taker is not leading the attack, but is in it
						text = "[@] reinforced the [@]";                                       //then they have reinforced the attack.
					} else {
						continue;
					}
					AppendLog(new EventLog(text, turnOrder[turn], battleground.defenders));
				}
			}
		}

		static void ResolutionPhase () {
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				battleground.battle = new Battle(battleground, battleground.attackers, battleground.defenders);
				DependencyManager.Flag(battleground);
			}
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				if (battleground.defenders == null) {
					AppendLog(new EventLog("[@] unpposed victory at [@]", battleground.attackers.affiliation, battleground));
				} else {
					AppendLog(new EventLog("[@] victory against [@] in [@]", battleground.battle.victor.affiliation, battleground.battle.loser.affiliation, battleground.battle));
				}
			}
			GameObject.ClearEngagements();
		}

		static void UpdateTurnOrder () {
			List<Faction> factions = new List<Faction>();
			List<Team> teams = new List<Team>();
			List<Parahuman> parahumans = new List<Parahuman>();
			foreach (IAgent agent in city.activeAgents) {
				if (GameObject.TryCast(agent, out Faction faction)) {
					factions.Add(faction);
				} else if (GameObject.TryCast(agent, out Team team)) {
					teams.Add(team);
				} else if (GameObject.TryCast(agent, out Parahuman parahuman)) {
					parahumans.Add(parahuman);
				}
			}
			factions.Sort();
			factions.Sort((x, y) => y.reputation.CompareTo(x.reputation));
			teams.Sort();
			teams.Sort((x, y) => x.insight_XP.CompareTo(y.insight_XP));
			parahumans.Sort();
			parahumans.Sort((x, y) => x.baseRatings.o_vals[4, 7].CompareTo(y.baseRatings.o_vals[4, 7]));
			turnOrder = new List<IAgent>();
			foreach (Faction faction in factions) turnOrder.Add(faction);
			foreach (Team team in teams) turnOrder.Add(team);
			foreach (Parahuman parahuman in parahumans) turnOrder.Add(parahuman);
		}

	}

}
