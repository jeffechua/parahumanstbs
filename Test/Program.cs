using System;
using Gtk;

namespace Test {

	class TestBox : EventBox {
	}

	class MainClass {
		public static void Main (string[] args) {
			Application.Init();

			Window win = new Window();
			Table table = new Table(2, 2, true);

			Notebook notebook1 = new Notebook();
			notebook1.ModifyBg(StateType.Normal, new Gdk.Color(255, 0, 0));
			notebook1.AppendPage(GetTestPair(), new Label("Test"));
			table.Attach(notebook1, 0, 1, 1, 2);

			Notebook notebook2 = new Notebook();
			notebook2.AppendPage(GetTestPair(), new Label("Test"));
			table.Attach(notebook2, 1, 2, 1, 2);

			EventBox eventbox = new EventBox();
			eventbox.ModifyBg(StateType.Normal, new Gdk.Color(255, 0, 0));
			eventbox.Add(GetTestPair());
			table.Attach(eventbox, 0, 1, 0, 1);

			table.Attach(GetTestPair(), 1, 2, 0, 1);

			win.Add(table);
			win.ShowAll();
			Application.Run();
		}

		public static Widget GetTestPair () {
			HBox boxes = new HBox { BorderWidth = 5 };
			EventBox box1 = new EventBox() { Child = new Frame("EventBox") };
			TestBox box2 = new TestBox() { Child = new Frame("TestBox") };
			boxes.PackStart(box1);
			boxes.PackStart(box2);
			return boxes;
		}
	}
}
