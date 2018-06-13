using Gtk;
using Gdk;
using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public static class EnumTools {

		public static readonly string[] classSymbols = { "", "β", "δ", "Σ", "ψ", "μ", "φ", "ξ", "Ω", "Γ", "Λ", "Δ" };
		public static readonly string[] threatSymbols = { "●", "■", "▲", "☉" };
		public static readonly Gdk.Color[] healthColors = { new Color(100, 100, 100), new Color(230, 0, 0), new Color(200, 200, 0), new Color(0, 200, 0) };
		public static readonly Gdk.Color[] alignmentColors = { new Color(0, 100, 230), new Color(170, 140, 0), new Color(100, 150, 0), new Color(0, 0, 0), new Color(150, 0, 175) };

		public static Gdk.Color GetColor (Health health) => healthColors[(int)health];
		public static Gdk.Color GetColor (Alignment alignment) => alignmentColors[2 - (int)alignment];
		public static string GetSymbol (Classification clssf) => classSymbols[(int)clssf];
		public static string GetSymbol (Threat threat) => threatSymbols[(int)threat];

		public static void SetAllStates (Widget widget, Gdk.Color color) {
			widget.ModifyFg(StateType.Normal, color);
			widget.ModifyFg(StateType.Prelight, color);
			widget.ModifyFg(StateType.Selected, color);
			widget.ModifyFg(StateType.Active, color);
			widget.ModifyFg(StateType.Insensitive, color);
		}

		public static Widget GetIcon (object iconified, Color color) {

			double finalSize = MainClass.textSize;
			Pixmap iconBase = new Pixmap(MainClass.mainWindow.GdkWindow, (int)finalSize, (int)finalSize);
			Pixmap mask = new Pixmap(MainClass.mainWindow.GdkWindow, (int)finalSize * 10, (int)finalSize * 10);

			Gdk.GC iconColor = new Gdk.GC(iconBase) { RgbFgColor = color };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(255, 255, 255) };

			double size = finalSize * 10; //Since 12 across is 0-11; we don't want to draw on 12 and lose pixels.

			iconBase.DrawRectangle(iconColor, true, new Rectangle(0, 0, (int)finalSize, (int)finalSize));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));

			if (iconified is Threat) {
				Threat threat = (Threat)iconified;
				switch (threat) {
					case Threat.C: // a circle
						mask.DrawArc(visible, true, (int)(size * 0.225), (int)(size * 0.225),
									 (int)(size * 0.55), (int)(size * 0.55), 0, 23040);
						break;
					case Threat.B: // A square
						double squareSize = 0.55 * size;
						double margin = (size - squareSize) / 2;
						mask.DrawPolygon(visible, true, new Point[]{
							new Point((int)margin, (int)margin),
							new Point((int)(margin + squareSize), (int)margin),
							new Point((int)(margin + squareSize), (int)(margin + squareSize)),
							new Point((int)margin, (int)(margin+squareSize))
						});
						break;
					case Threat.A: // A triangle with the point upwards.
						double width = size * 0.75;
						double height = width * Math.Sqrt(3) / 2;
						mask.DrawPolygon(visible, true, new Point[]{
							new Point((int)(size*0.1), (int)(size-height)/2),
							new Point((int)(size*0.9), (int)(size-height)/2),
							new Point((int)(size/2), (int)(size+height)/2)
						});
						break;
					case Threat.S: // A four-pointed star.
						mask.DrawPolygon(visible, true, new Point[]{
							new Point(0, (int)(size/2)),
							new Point((int)(size/3), (int)(size/3)),
							new Point((int)(size/2), 0),
							new Point((int)(size*2/3), (int)(size/3)),
							new Point((int)size, (int)(size/2)),
							new Point((int)(size*2/3), (int)(size*2/3)),
							new Point((int)(size/2), (int)size),
							new Point((int)(size/3), (int)(size*2/3))
						});
						break;
					case Threat.X:
						mask.DrawArc(visible, true, (int)(size * 0.05), (int)(size * 0.05), (int)(size * 0.9), (int)(size * 0.9), 0, 23040);
						mask.DrawArc(invisible, true, (int)(size * 0.15), (int)(size * 0.15),
									 (int)(size * 0.7), (int)(size * 0.7), 0, 23040);
						mask.DrawArc(visible, true, (int)(size * 0.4), (int)(size * 0.4),
									 (int)(size * 0.2), (int)(size * 0.2), 0, 23040);
						mask.DrawPoint(visible, (int)(size / 2), (int)(size / 2));
						break;
				}
			}

			Pixmap downsizedMask = new Pixmap(MainClass.mainWindow.GdkWindow, (int)finalSize, (int)finalSize);
			downsizedMask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)finalSize, (int)finalSize));
			Pixbuf.FromDrawable(mask, Colormap.System, 0, 0, 0, 0, (int)(size), (int)(size))
				  .ScaleSimple((int)(finalSize), (int)(finalSize), InterpType.Hyper)
				  .RenderToDrawable(downsizedMask, visible, 0, 0, 0, 0, (int)(finalSize), (int)(finalSize), RgbDither.Max, 0, 0);
			
			return new Gtk.Image(iconBase, downsizedMask);
		}

	}
}