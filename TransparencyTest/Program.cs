/*
using System;
using Gtk;

namespace TransparencyTest {
	class MainClass {
		public static void Main (string[] args) {

			Application.Init();
			Window win = new Window(WindowType.Toplevel);

			const byte TRANSPARENCY = 200;

			win.Mapped += delegate {
				Gdk.GC red = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(255, 0, 0) };
				Gdk.GC black = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(0, 0, 0) };
				Gdk.GC translucent = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(TRANSPARENCY, TRANSPARENCY, TRANSPARENCY) };
				Gdk.GC visible = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(255, 255, 255) };

				Gdk.Pixmap pixmap = new Gdk.Pixmap(win.GdkWindow, 200, 200);
				pixmap.DrawRectangle(black, true, 0, 0, 200, 200);
				pixmap.DrawArc(red, true, 20, 20, 160, 160, 0, 23040);

				Gdk.Pixmap mask = new Gdk.Pixmap(win.GdkWindow, 200, 200);
				mask.DrawRectangle(visible, true, 0, 0, 200, 200);
				mask.DrawArc(translucent, true, 0, 0, 100, 100, 0, 23040);
				mask.DrawArc(translucent, true, 100, 100, 100, 100, 0, 23040);

				Image image = new Image(pixmap, mask);
				win.Add(image);

				win.ShowAll();
			};

			win.ShowAll();
			Application.Run();
		}
	}
}
*/
using System;
using Gtk;

namespace TransparencyTest {
	class MainClass {
		public static void Main (string[] args) {

			Application.Init();
			Window win = new Window(WindowType.Toplevel);

			VBox vBox = new VBox();
			HScale transparencyScale = new HScale(0, 255, 1) { Value = 255 };
			Image image = new Image();
			image.SetSizeRequest(200, 200);

			vBox.PackStart(transparencyScale);
			vBox.PackStart(image);
			win.Add(vBox);

			transparencyScale.ValueChanged += delegate {

				int transparency = (int)transparencyScale.Value;
				byte[] tBytes = BitConverter.GetBytes(transparency);

				Gdk.GC red = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(255, 0, 0) };
				Gdk.GC black = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(0, 0, 0) };
				Gdk.GC translucent = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(tBytes[0], tBytes[0], tBytes[0]) };
				Gdk.GC visible = new Gdk.GC(win.GdkWindow) { RgbFgColor = new Gdk.Color(255, 255, 255) };

				Gdk.Pixmap pixmap = new Gdk.Pixmap(win.GdkWindow, 200, 200);
				pixmap.DrawRectangle(black, true, 0, 0, 200, 200);
				pixmap.DrawArc(red, true, 20, 20, 160, 160, 0, 23040);

				Gdk.Pixmap mask = new Gdk.Pixmap(win.GdkWindow, 200, 200);
				mask.DrawRectangle(visible, true, 0, 0, 200, 200);
				mask.DrawArc(translucent, true, 0, 0, 100, 100, 0, 23040);
				mask.DrawArc(translucent, true, 100, 100, 100, 100, 0, 23040);

				image.SetFromPixmap(pixmap, mask);
				image.ShowAll();

			};

			win.ShowAll();
			Application.Run();
		}
	}
}
