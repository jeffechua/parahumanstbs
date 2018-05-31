using System;
using Gtk;

public class DefocusableWindow : Gtk.Window {

	public DefocusableWindow () : base(Gtk.WindowType.Toplevel) {
		KeyPressEvent += OnKeyPressEvent;
	}

	private void OnKeyPressEvent (object obj, KeyPressEventArgs args) {
		if (args.Event.Key == Gdk.Key.Escape) Focus = null;
	}

}

public partial class MainWindow : DefocusableWindow {
	public MainWindow () {
		DeleteEvent += delegate (object obj, DeleteEventArgs args) {
			Application.Quit();
			args.RetVal = true;
		};
	}
}