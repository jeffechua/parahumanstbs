using Gtk;
using Gdk;
using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public struct IconRequest {
		readonly string iconified;
		readonly Color color;
		readonly int size;
		public IconRequest (object iconified, Color color, int size) {
			this.iconified = iconified.ToString();
			this.color = color;
			this.size = size;
		}
		public override bool Equals (object obj) {
			if (!(obj is IconRequest)) return false;
			IconRequest request = (IconRequest)obj;
			return iconified.Equals(request.iconified) && color.Equal(request.color) && size == request.size;
		}
		public override int GetHashCode () {
			return new Tuple<Tuple<object, Color>, int>(new Tuple<object, Color>(iconified, color), size).GetHashCode();
		}
	}

	public struct Icon {
		public Pixmap color;
		public Pixmap mask;
		public Icon (Pixmap color, Pixmap mask) {
			this.color = color;
			this.mask = mask;
		}
	}

	public static class Graphics {

		public static readonly string[] classSymbols = { "", "β", "δ", "Σ", "ψ", "μ", "φ", "ξ", "Ω", "Γ", "Λ", "Δ" };
		public static readonly string[] threatSymbols = { "●", "■", "▲", "☉" };
		public static readonly Gdk.Color[] healthColors = { new Color(100, 100, 100), new Color(230, 0, 0), new Color(200, 200, 0), new Color(0, 200, 0) };
		public static readonly Gdk.Color[] alignmentColors = { new Color(0, 100, 230), new Color(170, 140, 0), new Color(100, 150, 0), new Color(0, 0, 0), new Color(150, 0, 175) };

		public static Dictionary<IconRequest, Icon> iconCache = new Dictionary<IconRequest, Icon>();

		public static Color GetColor (Health health) => healthColors[(int)health];
		public static Color GetColor (Alignment alignment) => alignmentColors[2 - (int)alignment];
		public static Color GetColor (Agent agent) => (agent == null) ? new Color(125, 125, 125) : agent.color;
		public static string GetSymbol (Classification clssf) => classSymbols[(int)clssf];
		public static string GetSymbol (Threat threat) => threatSymbols[(int)threat];

		public static void SetAllFg (Widget widget, Gdk.Color color) {
			widget.ModifyFg(StateType.Normal, color);
			widget.ModifyFg(StateType.Prelight, color);
			widget.ModifyFg(StateType.Selected, color);
			widget.ModifyFg(StateType.Active, color);
			widget.ModifyFg(StateType.Insensitive, color);
		}

		public static void SetAllBg (Widget widget, Gdk.Color color) {
			widget.ModifyBg(StateType.Normal, color);
			widget.ModifyBg(StateType.Prelight, color);
			widget.ModifyBg(StateType.Selected, color);
			widget.ModifyBg(StateType.Active, color);
			widget.ModifyBg(StateType.Insensitive, color);
		}

		public static Gtk.Image GetIcon (object iconified, Color color, int iconSize) {

			IconRequest request = new IconRequest(iconified, color, iconSize);

			if (iconCache.ContainsKey(request))
				return new Gtk.Image(iconCache[request].color, iconCache[request].mask);

			double pixelSize = iconSize;
			double size = pixelSize * 10; //Since 12 across is 0-11; we don't want to draw on 12 and lose pixels.
			Pixmap iconBase = new Pixmap(MainClass.mainWindow.GdkWindow, (int)size, (int)size);
			Pixmap mask = new Pixmap(MainClass.mainWindow.GdkWindow, (int)size, (int)size);

			Gdk.GC iconColor = new Gdk.GC(iconBase) { RgbFgColor = color };
			Gdk.GC black = new Gdk.GC(iconBase) { RgbFgColor = new Color(0, 0, 0) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(255, 255, 255) };

			iconBase.DrawRectangle(iconColor, true, new Rectangle(0, 0, (int)size, (int)size));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));

			if (iconified is Threat) {
				switch ((Threat)iconified) {
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

			if (iconified is StructureType) {
				switch ((StructureType)iconified) {
					case StructureType.Tactical:
						double width = size * 0.7;
						double height = size * 0.75;
						double xMargin = (size - width) / 2;
						double yMargin = (size - height) / 2;
						double dipHeight = size * 0.1;
						double peakHeight = size * 0.3;
						Vector2 upperLeft = new Vector2(xMargin, yMargin);
						Vector2 upperRight = upperLeft + new Vector2(width, 0);
						Vector2 lowerLeft = upperLeft + new Vector2(0, height - peakHeight);
						Vector2 lowerRight = upperRight + new Vector2(0, height - peakHeight);
						Vector2 upperMiddle = new Vector2(size / 2, yMargin + dipHeight);
						Vector2 lowerMiddle = new Vector2(size / 2, size - yMargin);
						mask.DrawPolygon(visible, true, new Point[]{
							upperLeft.ToPoint(),
							upperMiddle.ToPoint(),
							upperRight.ToPoint(),
							lowerRight.ToPoint(),
							lowerMiddle.ToPoint(),
							lowerLeft.ToPoint()
						});
						break;
					case StructureType.Economic:
						Vector2 center = new Vector2(size / 2, size / 2);
						double radii = 0.2 * size;
						double diameter = radii * 2;
						Vector2 topCenter = center - new Vector2(0, 2 * radii / Math.Sqrt(3));
						Vector2 leftCenter = center + new Vector2(radii, Math.Sqrt(2) / 2 * radii);
						Vector2 rightCenter = center + new Vector2(-radii, Math.Sqrt(2) / 2 * radii);
						Vector2 topCorner = topCenter - new Vector2(radii, radii);
						Vector2 leftCorner = leftCenter - new Vector2(radii, radii);
						Vector2 rightCorner = rightCenter - new Vector2(radii, radii);
						mask.DrawArc(visible, true, (int)topCorner.x, (int)topCorner.y, (int)diameter, (int)diameter, 0, 23040);
						mask.DrawArc(visible, true, (int)leftCorner.x, (int)leftCorner.y, (int)diameter, (int)diameter, 0, 23040);
						mask.DrawArc(visible, true, (int)rightCorner.x, (int)rightCorner.y, (int)diameter, (int)diameter, 0, 23040);
						break;
					case StructureType.Aesthetic:
						double radius = 0.55 * size;
						double d = radius * 2;
						double eyeballRadius = radius * 0.45;
						double pupilRadius = eyeballRadius * 0.45;
						Vector2 upCenter = new Vector2(size / 2, size / 2 - radius / 2);
						Vector2 downCenter = new Vector2(size / 2, size / 2 + radius / 2);
						Vector2 upCorner = upCenter - new Vector2(radius, radius);
						Vector2 downCorner = downCenter - new Vector2(radius, radius);
						mask.DrawArc(visible, true, (int)upCorner.x, (int)upCorner.y, (int)d, (int)d, -30 * 64, -120 * 64);
						mask.DrawArc(visible, true, (int)downCorner.x, (int)downCorner.y, (int)d, (int)d, 30 * 64, 120 * 64);
						mask.DrawArc(invisible, true, (int)(size / 2 - eyeballRadius), (int)(size / 2 - eyeballRadius),
									 (int)(eyeballRadius * 2), (int)(eyeballRadius * 2), 0, 23040);
						mask.DrawArc(visible, true, (int)(size / 2 - pupilRadius), (int)(size / 2 - pupilRadius),
									 (int)(pupilRadius * 2), (int)(pupilRadius * 2), 0, 23040);
						break;
				}
			}

			if (iconified is GameEventType) {
				//The background is black this time
				iconBase.DrawRectangle(black, true, new Rectangle(0, 0, (int)size, (int)size));
				//The shaft
				double width = size / 4.5;
				double height = width * 3;
				Vector2 corner = new Vector2(size / 2 - width / 2, 0);
				double margin = width / 5;
				double width2 = width - margin * 2;
				double height2 = height - margin * 2;
				Vector2 corner2 = corner + new Vector2(margin, margin);
				mask.DrawRectangle(visible, true, new Rectangle((int)corner.x, (int)corner.y, (int)width, (int)height));
				iconBase.DrawRectangle(iconColor, true, new Rectangle((int)corner2.x, (int)corner2.y, (int)width2, (int)height2));
				//The bulb
				double diameter = width;
				double radius = diameter / 2;
				corner = new Vector2(size / 2 - radius, size - diameter);
				double diameter2 = diameter - margin * 2;
				double radius2 = diameter2 / 2;
				corner2 = corner + new Vector2(margin, margin);
				mask.DrawArc(visible, true, (int)corner.x, (int)corner.y, (int)diameter, (int)diameter, 0, 23040);
				iconBase.DrawArc(iconColor, true, (int)corner2.x, (int)corner2.y, (int)diameter2, (int)diameter2, 0, 23040);
			}

			if (iconified is DirectionType) {
				switch ((DirectionType)iconified) {
					case DirectionType.Left:
						mask.DrawPolygon(visible, true, new Point[]{
							new Point((int)size, 0),
							new Point(0, (int)(size / 2)),
							new Point((int)size, (int)size)
						});
						break;
					case DirectionType.Right:
						mask.DrawPolygon(visible, true, new Point[]{
							new Point(0, 0),
							new Point((int)size, (int)(size / 2)),
							new Point(0, (int)size)
						});
						break;
				}
			}

			Pixmap scaledIconBase = Scale(iconBase, size, size, 0.1);
			Pixmap scaledMask = Scale(mask, size, size, 0.1);

			iconCache.Add(request, new Icon(scaledIconBase, scaledMask));
			return new Gtk.Image(scaledIconBase, scaledMask);

		}

		public static Gtk.Image GetCircle (Color circleColor, byte alpha, int radius) {

			Pixmap color = new Pixmap(MainClass.mainWindow.GdkWindow, radius * 2, radius * 2);
			Pixmap mask = new Pixmap(MainClass.mainWindow.GdkWindow, radius * 2, radius * 2);

			Gdk.GC background = new Gdk.GC(color) { RgbFgColor = circleColor };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(alpha, alpha, alpha) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };

			color.DrawRectangle(background, true, new Rectangle(0, 0, radius * 2, radius * 2));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, radius * 2, radius * 2));

			mask.DrawArc(visible, true, 0, 0, radius * 2, radius * 2, 0, 23040);

			return new Gtk.Image(color, mask);

		}

		public static Gtk.Image GetLocationPin (Color pinColor, int pinWidth, int pinHeight) {

			double pixelWidth = pinWidth;
			double pixelHeight = pinHeight;

			double width = pixelWidth * 10;
			double height = pixelHeight * 10;

			Pixmap color = new Pixmap(MainClass.mainWindow.GdkWindow, (int)width, (int)height);
			Pixmap mask = new Pixmap(MainClass.mainWindow.GdkWindow, (int)width, (int)height);

			Gdk.GC markerShape = new Gdk.GC(color) { RgbFgColor = pinColor };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(255, 255, 255) };
			Gdk.GC translucent = new Gdk.GC(mask) { RgbFgColor = new Color(150, 150, 150) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };

			color.DrawRectangle(markerShape, true, new Rectangle(0, 0, (int)width, (int)height));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)width, (int)height));

			mask.DrawArc(visible, true, 0, 0, (int)width, (int)width, 0, 23040);

			// The "triangle" here refers to the triangle formed by the bottom vertex, a tangent point and the bottom of the image.
			//    ______
			//  /        \
			// |          |
			// |          |
			//  \        /.
			//   \      / .
			//    \    /  .  <-- this triangle.
			//     \  /   .
			//      \/.....

			double triangleHypotenuse = Math.Sqrt(height * height - height * width);
			double triangleWidth = width / (2 * height - width) * triangleHypotenuse;
			double triangleHeight = Math.Sqrt(triangleHypotenuse * triangleHypotenuse - triangleWidth * triangleWidth);

			Vector2 bottomVertex = new Vector2(width / 2, height);
			Vector2 leftVertex = bottomVertex + new Vector2(-triangleWidth, -triangleHeight);
			Vector2 rightVertex = bottomVertex + new Vector2(triangleWidth, -triangleHeight);

			mask.DrawPolygon(visible, true, new Point[] { bottomVertex.ToPoint(), leftVertex.ToPoint(), rightVertex.ToPoint() });

			double coreRadius = width / 5;
			double coreCenter = width / 2;

			mask.DrawArc(translucent, true,
						  (int)(coreCenter - coreRadius), (int)(coreCenter - coreRadius),
						  (int)(coreRadius * 2), (int)(coreRadius * 2),
						  0, 23040);

			return new Gtk.Image(Graphics.Scale(color, width, height, 0.1), Graphics.Scale(mask, width, height, 0.1));

		}

		public static Pixmap Scale (Pixmap pixmap, double originalWidth, double originalHeight, double factor) {
			Gdk.GC visible = new Gdk.GC(pixmap) { RgbFgColor = new Color(255, 255, 255) };
			double finalWidth = originalWidth * factor;
			double finalHeight = originalHeight * factor;
			Pixmap newPixmap = new Pixmap(MainClass.mainWindow.GdkWindow, (int)finalWidth, (int)finalHeight);
			Pixbuf.FromDrawable(pixmap, Colormap.System, 0, 0, 0, 0, (int)originalWidth, (int)originalHeight)
				  .ScaleSimple((int)finalWidth, (int)finalHeight, InterpType.Hyper)
				  .RenderToDrawable(newPixmap, visible, 0, 0, 0, 0, (int)finalWidth, (int)finalHeight, RgbDither.Max, 0, 0);
			return newPixmap;
		}

		public static void SetAllocationTrigger (Widget widget, System.Action action) {
			SizeAllocatedHandler handler = null;
			handler = delegate {
				widget.SizeAllocated -= handler;
				action();
			};
			widget.SizeAllocated += handler;
		}

		public static void SetAllocationTrigger<T1, T2> (Widget widget, System.Action<T1, T2> action, T1 arg1, T2 arg2) {
			SizeAllocatedHandler handler = null;
			handler = delegate {
				widget.SizeAllocated -= handler;
				action(arg1, arg2);
			};
			widget.SizeAllocated += handler;
		}

		public static void RemainInvisible (object obj, EventArgs args) => ((Widget)obj).Hide();

		public static Widget GetSmartHeader (Context context, IGUIComplete obj) {
			DependableShell shell = new DependableShell(obj.order + 1);
			shell.ReloadEvent += delegate {
				if (shell.Child != null) shell.Child.Destroy();
				shell.Add(obj.GetHeader(context));
				shell.ShowAll();
			};
			shell.Reload();
			DependencyManager.Connect(obj, shell);
			return shell;
		}

	}
}