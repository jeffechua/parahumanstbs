using System;
using Gtk;
using System.Collections.Generic;

namespace Parahumans.Core {

	public class EventInterface : HBox, IDependable {

		public int order { get { return 6; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public GameEvent gameEvent;

		public EventInterface (GameEvent gameEvent) {
			this.gameEvent = gameEvent;
			DependencyManager.Connect(gameEvent, this);
			Reload();
		}

		public void Reload () {
			while (Children.Length > 0)
				Children[0].Destroy();

			PackStart(GenerateDeploymentInterface(gameEvent.initiators, "Initiators"), true, true, 0);
			PackStart(new VSeparator(), false, false, 0);
			PackStart(GenerateEventCenter(), true, true, 0);
			PackStart(new VSeparator(), false, false, 0);
			PackStart(GenerateDeploymentInterface(gameEvent.responders, "Responders"), true, true, 0);
			ShowAll();
		}

		public Widget GenerateEventCenter () {
			Context context = new Context(Game.player, gameEvent);
			VBox mainBox = new VBox { BorderWidth = 10 };
			mainBox.PackStart(gameEvent.GetHeader(context), false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			ScrolledWindow scrolledWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never};
			scrolledWindow.AddWithViewport(UIFactory.Generate(context, gameEvent));
			mainBox.PackStart(scrolledWindow, true, true, 5);
			return mainBox;
		}

		public Widget GenerateDeploymentInterface (Deployment deployment, string label) {
			VBox mainBox = new VBox { BorderWidth = 10 };
			mainBox.PackStart(UIFactory.Align(new Label(label), 0, 0, 1, 1), false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			ScrolledWindow scrolledWindow = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never };
			scrolledWindow.AddWithViewport(UIFactory.Generate(new Context(Game.player, gameEvent), deployment));
			mainBox.PackStart(scrolledWindow, true, true, 5);
			return mainBox;
		}

	}
}