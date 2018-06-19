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
			while(Children.Length>0)
				Remove(Children[0]);
			PackStart(GenerateDeploymentInterface(gameEvent.initiators));
			PackStart(new VSeparator());
			PackStart(GenerateEventCenter());
			PackStart(new VSeparator());
			PackStart(GenerateDeploymentInterface(gameEvent.responders));
			ShowAll();
		}

		public Widget GenerateEventCenter () {
			Context context = new Context(MainClass.playerEntity, gameEvent);
			VBox mainBox = new VBox{BorderWidth = 10};
			mainBox.PackStart(gameEvent.GetHeader(context), false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			mainBox.PackStart(UIFactory.GenerateVertical(context, gameEvent), false, false, 5);
			return mainBox;
		}

		public Widget GenerateDeploymentInterface (Deployment deployment) {
			VBox mainBox = new VBox { BorderWidth = 10 };
			mainBox.PackStart(new Gtk.Alignment(0, 0, 1, 1) { Child = new Label("Initiators") }, false, false, 10);
			mainBox.PackStart(new HSeparator(), false, false, 0);
			mainBox.PackStart(UIFactory.GenerateVertical(new Context(MainClass.playerEntity, gameEvent), deployment), false, false, 5);
			return mainBox;
		}

	}
}