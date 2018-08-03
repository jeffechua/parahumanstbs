using Gtk;
using Gdk;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace BrocktonBay {

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

		const int FULL_CIRCLE = 23040;
		const double BLACK_TRIM_WIDTH = 0002;
		const double RESOLUTION_FACTOR = 10; //The icon is rendered at this times the requested size then scaled down.
		static Assembly assembly;

		public static readonly Gdk.Color[] healthColors = { new Color(100, 100, 100), new Color(230, 0, 0), new Color(200, 200, 0), new Color(0, 200, 0) };
		public static readonly Gdk.Color[] alignmentColors = { new Color(0, 100, 230), new Color(170, 140, 0), new Color(100, 150, 0), new Color(0, 0, 0), new Color(150, 0, 175) };

		public static Dictionary<IconRequest, Icon> iconCache = new Dictionary<IconRequest, Icon>();

		public static Color GetColor (Health health) => healthColors[(int)health];
		public static Color GetColor (Alignment alignment) => alignmentColors[2 - (int)alignment];
		public static Color GetColor (IAgent agent) => (agent == null) ? new Color(125, 125, 125) : agent.color;

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

		public static int textSize;
		static Gdk.GC visible;
		static Gdk.GC translucent;
		static Gdk.GC film;
		static Gdk.GC invisible;
		static Gdk.GC black;

		public static void OnMainWindowInitialized (object obj, EventArgs args) {
			assembly = Assembly.GetExecutingAssembly();
			textSize = (int)Math.Round(Game.mainWindow.Style.FontDescription.Size / Pango.Scale.PangoScale);
			visible = new Gdk.GC(Game.mainWindow.GdkWindow) { RgbFgColor = new Color(255, 255, 255) };
			translucent = new Gdk.GC(Game.mainWindow.GdkWindow) { RgbFgColor = new Color(150, 150, 150) };
			film = new Gdk.GC(Game.mainWindow.GdkWindow) { RgbFgColor = new Color(80, 80, 80) };
			invisible = new Gdk.GC(Game.mainWindow.GdkWindow) { RgbFgColor = new Color(0, 0, 0) };
			black = invisible;
			Game.mainWindow.Realized -= OnMainWindowInitialized;
		}

		public static Stream GetResource (string path) => assembly.GetManifestResourceStream(path);

		public static Gtk.Image GetIcon (object iconified, Color iconColor, int iconSize, bool decor = false) {

			IconRequest request = new IconRequest(iconified, iconColor, iconSize);

			if (iconCache.ContainsKey(request))
				return new Gtk.Image(iconCache[request].color, iconCache[request].mask);

			double pixelSize = iconSize;
			double size = pixelSize * RESOLUTION_FACTOR; //Since 12 across is 0-11; we don't want to draw on 12 and lose pixels.

			Pixmap color = new Pixmap(Game.mainWindow.GdkWindow, (int)size, (int)size);
			Pixmap mask = new Pixmap(Game.mainWindow.GdkWindow, (int)size, (int)size);

			Gdk.GC colorGC = new Gdk.GC(color) { RgbFgColor = iconColor };

			if (iconified is Threat) {

				color.DrawRectangle(colorGC, true, new Rectangle(0, 0, (int)size, (int)size));
				mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));

				switch ((Threat)iconified) {
					case Threat.C: // a circle
						mask.DrawArc(visible, true, (int)(size * 0.225), (int)(size * 0.225),
									 (int)(size * 0.55), (int)(size * 0.55), 0, FULL_CIRCLE);
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
						mask.DrawArc(visible, true, (int)(size * 0.05), (int)(size * 0.05), (int)(size * 0.9), (int)(size * 0.9), 0, FULL_CIRCLE);
						mask.DrawArc(invisible, true, (int)(size * 0.15), (int)(size * 0.15),
									 (int)(size * 0.7), (int)(size * 0.7), 0, FULL_CIRCLE);
						mask.DrawArc(visible, true, (int)(size * 0.4), (int)(size * 0.4),
									 (int)(size * 0.2), (int)(size * 0.2), 0, FULL_CIRCLE);
						mask.DrawPoint(visible, (int)(size / 2), (int)(size / 2));
						break;
				}
			}

			if (iconified is StructureType) {
				if (decor) {
					color.DrawRectangle(black, true, new Rectangle(0, 0, (int)size, (int)size));
					mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));
					mask.DrawArc(translucent, true, 0, 0, (int)size, (int)size, 0, FULL_CIRCLE);
				} else {
					color.DrawRectangle(colorGC, true, new Rectangle(0, 0, (int)size, (int)size));
					mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));
				}
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
						if (decor)
							color.DrawPolygon(colorGC, true, new Point[]{
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
						mask.DrawArc(visible, true, (int)topCorner.x, (int)topCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						if (decor) color.DrawArc(colorGC, true, (int)topCorner.x, (int)topCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						mask.DrawArc(visible, true, (int)leftCorner.x, (int)leftCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						if (decor) color.DrawArc(colorGC, true, (int)leftCorner.x, (int)leftCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						mask.DrawArc(visible, true, (int)rightCorner.x, (int)rightCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						if (decor) color.DrawArc(colorGC, true, (int)rightCorner.x, (int)rightCorner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
						break;
					case StructureType.Aesthetic:
						double radius1 = size * 0.4;
						double margin1 = size / 2 - radius1;
						double radius2 = radius1 * 0.75;
						double margin2 = size / 2 - radius2;
						mask.DrawArc(visible, true, (int)margin1, (int)margin1, (int)(radius1 * 2), (int)(radius1 * 2), 0, FULL_CIRCLE);
						if (decor) {
							color.DrawArc(colorGC, true, (int)margin1, (int)margin1, (int)(radius1 * 2), (int)(radius1 * 2), 0, FULL_CIRCLE);
							color.DrawArc(black, true, (int)margin2, (int)margin1, (int)(radius2 * 2), (int)(radius2 * 2), 0, FULL_CIRCLE);
							mask.DrawArc(translucent, true, (int)margin2, (int)margin1, (int)(radius2 * 2), (int)(radius2 * 2), 0, FULL_CIRCLE);
						} else {
							mask.DrawArc(invisible, true, (int)margin2, (int)margin1, (int)(radius2 * 2), (int)(radius2 * 2), 0, FULL_CIRCLE);
						}
						break;
				}
			}

			/*
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
									 (int)(eyeballRadius * 2), (int)(eyeballRadius * 2), 0, FULL_CIRCLE);
						mask.DrawArc(visible, true, (int)(size / 2 - pupilRadius), (int)(size / 2 - pupilRadius),
									 (int)(pupilRadius * 2), (int)(pupilRadius * 2), 0, FULL_CIRCLE);
			 */

			if (iconified is AlertIconType) {
				AlertIconType alert = (AlertIconType)iconified;
				//The background is black this time
				double margin = BLACK_TRIM_WIDTH * RESOLUTION_FACTOR;
				color.DrawRectangle(black, true, new Rectangle(0, 0, (int)size, (int)size));
				mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));
				if (alert == AlertIconType.Opposed || alert == AlertIconType.Unopposed) {
					double bsize = size * 0.15; //The antidiagonal size of the blade (width/sqrt(2))
					Vector2 A = new Vector2(0, size - bsize);
					Vector2 B = new Vector2(bsize, size);
					Vector2 C = new Vector2(size - bsize, 0);
					Vector2 D = new Vector2(size, bsize);
					Vector2 E = new Vector2(size, 0);
					mask.DrawPolygon(visible, true, new Point[] { A, B, D, E, C });
					Vector2 P = new Vector2(0.05, 0.65) * size;
					Vector2 Q = new Vector2(0.2, 0.5) * size;
					Vector2 R = new Vector2(0.35, 0.95) * size;
					Vector2 S = new Vector2(0.5, 0.8) * size;
					mask.DrawPolygon(visible, true, new Point[] { P, Q, S, R });
					double m2 = margin * Math.Sqrt(2);
					double m3 = m2 - margin;
					Vector2 A2 = A + new Vector2(m2, 0);
					Vector2 B2 = B - new Vector2(0, m2);
					Vector2 C2 = C + new Vector2(m3, margin);
					Vector2 D2 = D - new Vector2(margin, m3);
					Vector2 E2 = E - new Vector2(margin, -margin);
					color.DrawPolygon(colorGC, true, new Point[] { A2, B2, D2, E2, C2 });
					Vector2 P2 = P + new Vector2(m2, 0);
					Vector2 Q2 = Q + new Vector2(0, m2);
					Vector2 R2 = R - new Vector2(0, m2);
					Vector2 S2 = S - new Vector2(m2, 0);
					color.DrawPolygon(colorGC, true, new Point[] { P2, Q2, S2, R2 });
					if (alert == AlertIconType.Opposed) {
						Vector2 a = new Vector2(size, size - bsize);
						Vector2 b = new Vector2(size - bsize, size);
						Vector2 c = new Vector2(bsize, 0);
						Vector2 d = new Vector2(0, bsize);
						Vector2 e = new Vector2(0, 0);
						mask.DrawPolygon(visible, true, new Point[] { a, b, d, e, c });
						Vector2 p = new Vector2(0.95, 0.65) * size;
						Vector2 q = new Vector2(0.8, 0.5) * size;
						Vector2 r = new Vector2(0.65, 0.95) * size;
						Vector2 s = new Vector2(0.5, 0.8) * size;
						mask.DrawPolygon(visible, true, new Point[] { p, q, s, r });
						Vector2 a2 = a - new Vector2(m2, 0);
						Vector2 b2 = b - new Vector2(0, m2);
						Vector2 c2 = c + new Vector2(-m3, margin);
						Vector2 d2 = d - new Vector2(-margin, m3);
						Vector2 e2 = e + new Vector2(margin, margin);
						color.DrawPolygon(colorGC, true, new Point[] { a2, b2, d2, e2, c2 });
						Vector2 p2 = p - new Vector2(m2, 0);
						Vector2 q2 = q + new Vector2(0, m2);
						Vector2 r2 = r - new Vector2(0, m2);
						Vector2 s2 = s + new Vector2(m2, 0);
						color.DrawPolygon(colorGC, true, new Point[] { p2, q2, s2, r2 });
					}
				} else {
					//The shaft
					double width = size / 4.5;
					double height = width * 3;
					Vector2 corner = new Vector2(size / 2 - width / 2, 0);
					double width2 = width - margin * 2;
					double height2 = height - margin * 2;
					Vector2 corner2 = corner + new Vector2(margin, margin);
					mask.DrawRectangle(visible, true, new Rectangle((int)corner.x, (int)corner.y, (int)width, (int)height));
					color.DrawRectangle(colorGC, true, new Rectangle((int)corner2.x, (int)corner2.y, (int)width2, (int)height2));
					//The bulb
					double diameter = width;
					double radius = diameter / 2;
					corner = new Vector2(size / 2 - radius, size - diameter);
					double diameter2 = diameter - margin * 2;
					double radius2 = diameter2 / 2;
					corner2 = corner + new Vector2(margin, margin);
					mask.DrawArc(visible, true, (int)corner.x, (int)corner.y, (int)diameter, (int)diameter, 0, FULL_CIRCLE);
					color.DrawArc(colorGC, true, (int)corner2.x, (int)corner2.y, (int)diameter2, (int)diameter2, 0, FULL_CIRCLE);
				}
			}

			if (iconified is DirectionType) {
				color.DrawRectangle(colorGC, true, new Rectangle(0, 0, (int)size, (int)size));
				mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)size, (int)size));
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

			Pixmap scaledColor = Scale(color, size, size, 0.1);
			Pixmap scaledMask = Scale(mask, size, size, 0.1);
			if (PlatformDetection.os == OS.Linux)
				scaledMask = Bitmapize(scaledMask, 75);
			iconCache.Add(request, new Icon(scaledColor, scaledMask));
			return new Gtk.Image(scaledColor, scaledMask);
		}

		public static Pixmap Bitmapize (Pixmap pixmap, int threshold) {
			pixmap.GetSize(out int width, out int height);
			Gdk.Image image = pixmap.GetImage(0, 0, width, height);
			Pixmap bitmap = new Pixmap(null, width, height, 1);
			Gdk.GC bitSetter = new Gdk.GC(bitmap) { Function = Gdk.Function.Set };
			Gdk.GC bitClearer = new Gdk.GC(bitmap) { Function = Gdk.Function.Clear };
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					if (BitConverter.GetBytes(image.GetPixel(x, y))[0] >= threshold) {
						bitmap.DrawPoint(bitSetter, x, y);
					} else {
						bitmap.DrawPoint(bitClearer, x, y);
					}
				}
			}
			return bitmap;
		}

		public static Gtk.Image GetCircle (Color circleColor, byte alpha, int radius) {

			Pixmap color = new Pixmap(Game.mainWindow.GdkWindow, radius * 2, radius * 2);
			Pixmap mask = new Pixmap(Game.mainWindow.GdkWindow, radius * 2, radius * 2);

			Gdk.GC background = new Gdk.GC(color) { RgbFgColor = circleColor };
			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(alpha, alpha, alpha) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };

			color.DrawRectangle(background, true, new Rectangle(0, 0, radius * 2, radius * 2));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, radius * 2, radius * 2));

			mask.DrawArc(visible, true, 0, 0, radius * 2, radius * 2, 0, FULL_CIRCLE);

			if (PlatformDetection.os == OS.Linux)
				mask = Bitmapize(mask, 128);

			return new Gtk.Image(color, mask);

		}

		public static Gtk.Image GetLocationPin (Color pinColor, int pinWidth, int pinHeight) {

			double pixelWidth = pinWidth;
			double pixelHeight = pinHeight;

			//Independent quantities
			double w = pixelWidth * RESOLUTION_FACTOR;
			double r = w / 2;
			double h = pixelHeight * RESOLUTION_FACTOR;
			double m = BLACK_TRIM_WIDTH * RESOLUTION_FACTOR;

			//Dependent quantities
			double v = Math.Sqrt(h * h - h * w);
			double u = r / (h - r);

			Pixmap color = new Pixmap(Game.mainWindow.GdkWindow, (int)w, (int)h);
			Pixmap mask = new Pixmap(Game.mainWindow.GdkWindow, (int)w, (int)h);

			Gdk.GC markerShape = new Gdk.GC(color) { RgbFgColor = pinColor };

			color.DrawRectangle(black, true, new Rectangle(0, 0, (int)w, (int)h));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, (int)w, (int)h));

			color.DrawArc(markerShape, true, (int)m, (int)m, (int)(w - m * 2), (int)(w - m * 2), 0, FULL_CIRCLE);
			mask.DrawArc(visible, true, 0, 0, (int)w, (int)w, 0, FULL_CIRCLE);

			// The "triangle" here refers to the triangle formed by the bottom vertex, a tangent point and the bottom of the image.
			//
			//    -------     ---   Independent quantities:
			//  /    |    \    |    r = radius of circle
			// |     |_____|   |    h = height of pin
			// |     |  r  |   |    m = thickness of black trim
			//  \    |    /.   h    
			//   \   |   / .   |    Dependent quantities:
			//    \  |θ /v .   |    θ = angle between axis of symmetry and the sides of the spear
			//     \ |^/   .   |    u = sin θ
			//      \|/.....  _|_   v = hypotenuse of the spear
			//           r
			//
			//      [center] O___
			//               |   ---___[innerRight]
			//               |         O--___
			//               |________/______--O [outerRight]
			//               |       /        /
			//               |      /        /
			//               |     /        /
			//               |    /        /
			//               |   /        /
			//               |  /        /
			//               | /        /
			//               |/        / v
			// [innerBottom] O--___   /
			//               |   m --/
			//               |      /
			//               |     /
			//               |    /
			//               |   /
			//               |θ /
			//               |^/
			//               |/
			// [outerBottom] O

			Vector2 center = new Vector2(r, r);
			Vector2 outerBottom = new Vector2(w / 2, h);
			Vector2 innerBottom = new Vector2(r, h - m / u);
			Vector2 outerLeft = center + new Vector2(-u * v, u * r);
			Vector2 outerRight = center + new Vector2(u * v, u * r);
			Vector2 innerLeft = outerLeft - new Vector2(-m * u * v / r, u * m);
			Vector2 innerRight = outerRight - new Vector2(m * u * v / r, u * m); //u*v/r = cos θ.

			color.DrawPolygon(markerShape, true, new Point[] { innerBottom.ToPoint(), innerLeft.ToPoint(), innerRight.ToPoint() });
			mask.DrawPolygon(visible, true, new Point[] { outerBottom.ToPoint(), outerLeft.ToPoint(), outerRight.ToPoint() });

			double coreRadius = w / 5;
			double coreCenter = w / 2;

			color.DrawArc(black, true,
						  (int)(coreCenter - coreRadius - m), (int)(coreCenter - coreRadius - m),
						 (int)(coreRadius * 2 + m * 2), (int)(coreRadius * 2 + m * 2),
						  0, FULL_CIRCLE);
			mask.DrawArc(film, true,
						  (int)(coreCenter - coreRadius), (int)(coreCenter - coreRadius),
						  (int)(coreRadius * 2), (int)(coreRadius * 2),
						  0, FULL_CIRCLE);

			Pixmap scaledColor = Scale(color, w, h, 0.1);
			Pixmap scaledMask = Scale(mask, w, h, 0.1);
			if (PlatformDetection.os == OS.Linux)
				scaledMask = Bitmapize(scaledMask, 128);

			return new Gtk.Image(scaledColor, scaledMask);
		}

		public static Pixmap Scale (Pixmap pixmap, double originalWidth, double originalHeight, double factor) {
			Gdk.GC visible = new Gdk.GC(pixmap);
			double finalWidth = originalWidth * factor;
			double finalHeight = originalHeight * factor;
			Pixmap newPixmap = new Pixmap(pixmap, (int)finalWidth, (int)finalHeight);
			Pixbuf pixbuf = Pixbuf.FromDrawable(pixmap, pixmap.Colormap, 0, 0, 0, 0, (int)originalWidth, (int)originalHeight);
			Pixbuf scaledPixbuf = pixbuf.ScaleSimple((int)finalWidth, (int)finalHeight, InterpType.Hyper);
			scaledPixbuf.RenderToDrawable(newPixmap, visible, 0, 0, 0, 0, (int)finalWidth, (int)finalHeight, RgbDither.Max, 0, 0);
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