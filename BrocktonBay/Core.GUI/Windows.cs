using System;
using Gtk;
using Parahumans.Core;

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
			if (MainClass.city == null) {
				Application.Quit();
			} else {
				MessageDialog dialog = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save before quitting?");
				dialog.Response += delegate (object o, ResponseArgs response) {
					if (response.ResponseId == ResponseType.Yes)
						IO.SelectSave(MainClass.city);
					Application.Quit();
				};
				dialog.Run();
				dialog.Destroy();
			}
			args.RetVal = true;
		};
	}
}