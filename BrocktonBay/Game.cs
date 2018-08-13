using System;
using System.Collections.Generic;
using Gtk;


namespace BrocktonBay {

	[Flags]
	public enum Phase {
		Action = 1 << 0,
		Response = 1 << 1,
		Resolution = 1 << 2,
		Mastermind = 1 << 3,
		All = int.MaxValue,
		None = 0
	}

	class Game {

		static Random random = new Random();
		public static float randomFloat { get => (float)random.NextDouble(); }

		public static City city;
		public static IAgent player;
		public static Phase phase = Phase.Action;
		public static int turn;
		public static List<IAgent> turnOrder;

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
			UpdateTurnOrder();
			MainWindow.Load();
		}

		public static void Unload () {
			city = null;
			MainWindow.Unload();
		}

		public static void RefreshUI () {
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
						phase = Phase.Mastermind;
					}
					break;
				case Phase.Resolution:
					phase = Phase.Mastermind;
					break;
				case Phase.Mastermind:
					if (turn < turnOrder.Count - 1) {
						turn++;
						if (turnOrder[turn] != player)
							turnOrder[turn].TakeMastermindPhase();
					} else {
						turn = 0;
						UpdateTurnOrder();
						phase = Phase.Action;
					}
					break;
				default:
					throw new Exception("Invalid current game phase.");
			}
			RefreshUI();
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
