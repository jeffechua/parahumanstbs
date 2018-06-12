using System;
using System.IO;
using Gtk;


namespace Parahumans.Core {

	public class Map : EventBox {

		Fixed stage;
		Fixed positioner;
		Image imageWidget;
		VScale zoomScale;
		Gdk.Pixbuf baseImage;

		double currentDragX;
		double currentDragY;
		double currentPanX;
		double currentPanY;
	    double currentMagnif;
		bool dragging;

		const double maxZoom = 5;
		const double zoomFactor = 1.25; //Factor of magnification per step
		double maxMagnif = Math.Pow(zoomFactor, maxZoom);

		public Map () {}

		public Map (City city) {

			stage = new Fixed();
			positioner = new Fixed();
			positioner.Put(stage, 0, 0);
			Add(positioner);

			baseImage = new Gdk.Pixbuf(city.mapPngSource);
			baseImage = baseImage.ScaleSimple(
				(int)(city.mapDefaultWidth * maxMagnif),
				(int)(city.mapDefaultWidth * baseImage.Height * maxMagnif / baseImage.Width),
				Gdk.InterpType.Hyper);
			imageWidget = new Image();
			stage.Put(imageWidget, 0, 0);

			zoomScale = new VScale(0, maxZoom, 1);
			zoomScale.Value = 0;
			currentMagnif = 1;
			zoomScale.HeightRequest = 100;
			positioner.Put(zoomScale, 0, 0);
			zoomScale.ValueChanged += (o,a) => Zoom();

			ButtonPressEvent += delegate (object obj, ButtonPressEventArgs args) {
				if (args.Event.Button == 1) {
					dragging = true;
					currentDragX = args.Event.XRoot;
					currentDragY = args.Event.YRoot;
				}
			};
			ButtonReleaseEvent += delegate (object obj, ButtonReleaseEventArgs args) {
				if (args.Event.Button == 1) {
					dragging = false;
				}
			};
			MotionNotifyEvent += delegate (object obj, MotionNotifyEventArgs args) {
				if (dragging) {
					currentPanX += args.Event.XRoot - currentDragX;
					currentPanY += args.Event.YRoot - currentDragY;
					currentDragX = args.Event.XRoot;
					currentDragY = args.Event.YRoot;
					positioner.Move(stage, (int)Math.Round(currentPanX), (int)Math.Round(currentPanY));
				}
			};

			SetSizeRequest(0, 0);
			SizeAllocated += InitialZoom;

		}

		public void InitialZoom(object obj, SizeAllocatedArgs args) {
			SizeAllocated -= InitialZoom;
			Zoom();
		}

		public void Zoom () {
			double newMagnif = Math.Pow(zoomFactor, zoomScale.Value);
			imageWidget.Pixbuf = baseImage.ScaleSimple((int)(baseImage.Width / maxMagnif * newMagnif), (int)(baseImage.Height / maxMagnif * newMagnif), Gdk.InterpType.Nearest);
			currentPanX -= (Allocation.Width / 2 - currentPanX) * (newMagnif - currentMagnif) / currentMagnif;  //Math!
			currentPanY -= (Allocation.Height / 2 - currentPanY) * (newMagnif - currentMagnif) / currentMagnif; //Math!
			positioner.Move(stage, (int)Math.Round(currentPanX), (int)Math.Round(currentPanY));
			currentMagnif = newMagnif;
			ShowAll();
		}

	}

}
