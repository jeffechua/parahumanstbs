using System;
using Gtk;
using Gdk;
using BrocktonBay;

public class DefocusableWindow : Gtk.Window {

	public DefocusableWindow () : base(Gtk.WindowType.Toplevel) {
		KeyPressEvent += delegate (object obj, KeyPressEventArgs args) {
			if (args.Event.Key == Gdk.Key.Escape) Focus = null;
		};
	}

}

public class DefocusableWindowWithInspector : DefocusableWindow {
	public Inspector inspector;
	public bool inspectorEnabled = true;
}

public class SecondaryWindow : DefocusableWindowWithInspector {
	public HBox hBox;
	public Widget main;
	public SecondaryWindow (string title) {

		DefaultWidth = 1200;
		DefaultHeight = 700;
		Title = title;

		inspector = new Inspector { BorderWidth = 10 };
		VBox viewOptions = new VBox { BorderWidth = 5 };
		CheckButton inspectorVisible = new CheckButton { Active = true };
		inspectorVisible.Toggled += delegate {
			if (inspectorVisible.Active) {
				inspectorEnabled = true;
			} else {
				inspectorEnabled = false;
				inspector.Inspect(null);
			}
		};
		viewOptions.PackStart(inspectorVisible, false, false, 3);
		viewOptions.PackStart(new Label("Inspector Panel") { Angle = -90 }, false, false, 0);

		hBox = new HBox();
		hBox.PackEnd(viewOptions, false, false, 0);
		hBox.PackEnd(inspector, false, false, 0);
		Add(hBox);

	}
	public void SetMainWidget (Widget widget) {
		main = widget;
		hBox.PackEnd(main, true, true, 0);
	}
}

public partial class MainWindow : DefocusableWindowWithInspector {
	public MainWindow () {
		DeleteEvent += delegate (object obj, DeleteEventArgs args) {
			if (Game.city == null) {
				Application.Quit();
			} else {
				MessageDialog dialog = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save before quitting?");
				dialog.Response += delegate (object o, ResponseArgs response) {
					if (response.ResponseId == ResponseType.Yes)
						IO.SelectSave();
					Application.Quit();
				};
				dialog.Run();
				dialog.Destroy();
			}
			args.RetVal = true;
		};
	}
}