﻿using System;
using Gtk;
using System.Collections.Generic;

namespace BrocktonBay {

	public class BattleInterface : HBox, IDependable {

		public int order { get { return 6; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Battle battle;

		public BattleInterface (Battle battle) {
			this.battle = battle;
			Destroyed += (o, a) => DependencyManager.Destroy(this);
			DependencyManager.Connect(battle, this);
			DependencyManager.Connect(Game.UIKey, this);
			Reload();
		}

		public void Reload () {
			while (Children.Length > 0)
				Children[0].Destroy();

			PackStart(GenerateDeploymentInterface(battle.attackers, "Initiators"), true, true, 0);
			PackStart(new VSeparator(), false, false, 0);
			PackStart(GenerateEventCenter(), true, true, 0);
			PackStart(new VSeparator(), false, false, 0);
			PackStart(GenerateDeploymentInterface(battle.defenders, "Responders"), true, true, 0);
			ShowAll();
		}

		public void OnTriggerDestroyed (IDependable trigger) {
			if (trigger == battle) {
				Destroy();
				DependencyManager.DisconnectAll(this);
			}
		}

		public void OnListenerDestroyed (IDependable listener) { }

		public Widget GenerateEventCenter () {
			Context context = new Context(battle);
			VBox mainBox = new VBox { BorderWidth = 10 };
			mainBox.PackStart(battle.GetHeader(context), false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			ScrolledWindow scrolledWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never };
			scrolledWindow.AddWithViewport(UIFactory.Generate(context, battle));
			mainBox.PackStart(scrolledWindow, true, true, 5);
			return mainBox;
		}

		public Widget GenerateDeploymentInterface (Deployment deployment, string label) {
			VBox mainBox = new VBox { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(new Label(label), 0, 0, 1, 1), false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			ScrolledWindow scrolledWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never };
			scrolledWindow.AddWithViewport(UIFactory.Generate(new Context(battle), deployment));
			mainBox.PackStart(scrolledWindow, true, true, 5);
			return mainBox;
		}

	}
}