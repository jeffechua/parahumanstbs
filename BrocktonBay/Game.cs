﻿using System;
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
			eventLogs = new List<EventLog>();
			UpdateTurnOrder();
			MainWindow.Load();
		}

		public static void Unload () {
			city = null;
			MainWindow.Unload();
			Map.Unload();
		}

		public static void SetPlayer (IAgent agent) {
			player = agent;
			foreach (IBattleground battleground in city.activeBattlegrounds) { // Since knowledge affects the values in deployments
				if (battleground.attacker != null) DependencyManager.Flag(battleground.attacker);
				if (battleground.defender != null) DependencyManager.Flag(battleground.defender);
			}
			RefreshUIAndTriggerAllFlags();
		}

		public static void RefreshUIAndTriggerAllFlags () {
			DependencyManager.Flag(UIKey);
			DependencyManager.TriggerAllFlags();
		}

		public static bool CanNext () {
			return true;
		}

		public static void Next () {
			switch (phase) {
				case Phase.Action:
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
				case Phase.Event:
					phase = Phase.Action;
					break;
				default:
					throw new Exception("Invalid current game phase.");
			}
			RefreshUIAndTriggerAllFlags();
		}

		static void EventPhase () {
			UpdateTurnOrder();
			GameObject.ClearEngagements();
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				battleground.battle = null;
				battleground.attacker = null;
				battleground.defender = null;
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

		static void ResolutionPhase () {
			foreach (IBattleground battleground in city.activeBattlegrounds) {
				battleground.battle = new Battle(battleground, battleground.attacker, battleground.defender);
				DependencyManager.Flag(battleground);
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
