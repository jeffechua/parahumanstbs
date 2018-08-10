using System;
using System.Collections.Generic;
using Gtk;


namespace BrocktonBay {

	public enum Phase {
		All = -2,
		None = -1,
		Action = 0,
		Response = 1,
		Mastermind = 2
	}

	class Game {

		static Random random = new Random();
		public static float randomFloat { get => (float)random.NextDouble(); }

		public static City city;
		public static IAgent player;
		public static Phase phase;

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

	}

}
