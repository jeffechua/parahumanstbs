using System.Reflection;
using System;
using Gtk;
using Gdk;

namespace Parahumans.Core {

	public struct Vector2 {
		public double x;
		public double y;
		public Vector2 (double n1, double n2) {
			x = n1;
			y = n2;
		}
		public static implicit operator Vector2 (IntVector2 v)
			=> new Vector2(v.x, v.y);
		public static Vector2 operator + (Vector2 a, Vector2 b)
			=> new Vector2(a.x + b.x, a.y + b.y);
		public static Vector2 operator - (Vector2 a, Vector2 b)
			=> new Vector2(a.x - b.x, a.y - b.y);
		public static Vector2 operator * (double k, Vector2 v)
			=> new Vector2(k * v.x, k * v.y);
		public static Vector2 operator * (Vector2 v, double k)
			=> new Vector2(k * v.x, k * v.y);
		public static Vector2 operator / (Vector2 v, double k)
			=> new Vector2(v.x / k, v.y / k);
		public override string ToString ()
			=> "(" + x.ToString() + ", " + y.ToString() + ")";
		public Point ToPoint ()
		=> new Point((int)Math.Round(x), (int)Math.Round(y));
	}

	public struct IntVector2 {
		public int x;
		public int y;
		public IntVector2 (int n1, int n2) {
			x = n1;
			y = n2;
		}
		public IntVector2 (double n1, double n2) {
			x = (int)Math.Round(n1);
			y = (int)Math.Round(n2);
		}
		public static implicit operator IntVector2 (Vector2 v)
			=> new IntVector2((int)Math.Round(v.x), (int)Math.Round(v.y));
		public static IntVector2 operator + (IntVector2 a, IntVector2 b)
			=> new IntVector2(a.x + b.x, a.y + b.y);
		public static IntVector2 operator - (IntVector2 a, IntVector2 b)
			=> new IntVector2(a.x - b.x, a.y - b.y);
		public static IntVector2 operator * (double k, IntVector2 v)
			=> new IntVector2(k * v.x, k * v.y);
		public static IntVector2 operator * (IntVector2 v, double k)
			=> new IntVector2(k * v.x, k * v.y);
		public static IntVector2 operator / (IntVector2 v, double k)
			=> new IntVector2(v.x / k, v.y / k);
		public override bool Equals (object obj)
			=> obj is IntVector2 && this == (IntVector2)obj;
		public override int GetHashCode ()
			=> base.GetHashCode();
		public static bool operator == (IntVector2 u, IntVector2 v)
			=> u.x == v.x && u.y == v.y;
		public static bool operator != (IntVector2 u, IntVector2 v)
		=> u.x != v.x || u.y != v.y;
		public override string ToString ()
			=> "(" + x.ToString() + ", " + y.ToString() + ")";
	}

	public class RatingsRadarChart : Gtk.Image {

		PropertyInfo property;
		RatingsProfile profile;
		Context context;
		int currentSize;

		public RatingsRadarChart (PropertyInfo property, object obj, Context context, object arg) {
			this.property = property;
			this.context = context;
			profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			if (context.vertical) {
				SetSizeRequest(10, -1);
			} else {
				SetSizeRequest(-1, 10);
			}
			SizeAllocated += OnSizeAllocated;
		}

		public void OnSizeAllocated (object obj, SizeAllocatedArgs args) {

			if (!IsRealized) return;

			int width = args.Allocation.Width;
			int height = args.Allocation.Height;
			int size = context.vertical ? width : height;

			//If this is true, then we don't need to change. Removing this line also traps us in a loop since Initialize() triggers SizeAllocated();
			if (size == currentSize) return;
			currentSize = size;

			int chartRadius = size;
			int labelRadius = 0;

			Pixmap pixmap = new Pixmap(Toplevel.GdkWindow, size, size);
			Pixmap mask = new Pixmap(Toplevel.GdkWindow, size, size);
			Gdk.GC[] contexts = {
				new Gdk.GC(pixmap) { RgbFgColor = new Gdk.Color(170, 140, 0) }, //Regular
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 150, 0) },       //Master
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 0, 200) },       //Tinker
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 0, 0) },         //Breaker and transparent
				new Gdk.GC(pixmap) { RgbFgColor = new Color(255, 255, 255) },   //White, for marking the mask.
				new Gdk.GC(pixmap) { RgbFgColor = new Color(200, 200, 200) },   //Grey, for the axes
				new Gdk.GC(pixmap) { RgbFgColor = new Color(125, 125, 125) },   //Darkish grey, for the axis markings
				new Gdk.GC(pixmap) { RgbFgColor = new Color(100, 100, 100) }       //Darker grey, for the axis labels
			};
			pixmap.DrawRectangle(contexts[4], true, new Rectangle(0, 0, size, size));
			mask.DrawRectangle(contexts[3], true, new Rectangle(0, 0, size, size));

			//The eight directions in which vertices lie
			Vector2[] directions = {
				new Vector2(0,-1),
				new Vector2(Math.Sqrt(2)/2, -Math.Sqrt(2)/2),
				new Vector2(1,0),
				new Vector2(Math.Sqrt(2)/2, Math.Sqrt(2)/2),
				new Vector2(0,1),
				new Vector2(-Math.Sqrt(2)/2, Math.Sqrt(2)/2),
				new Vector2(-1,0),
				new Vector2(-Math.Sqrt(2)/2, -Math.Sqrt(2)/2)
			};
			IntVector2 center = new IntVector2(size / 2, size / 2);


			//Compute magnitudes

			int[] indexMap = { 0, 1, 5, 3, 7, 0, 4, 6, 2 }; //old[0] -> new[1], old[1] -> new[5] etc.
			int[] reverseIndexMap = { 5, 1, 8, 3, 6, 2, 7, 4 }; //old[0] -> new[1], old[1] -> new[5] etc.
			float[,] magnitudes = new float[4, 8]; //Magnitudes of the positions of the vertices

			for (int i = 1; i <= 8; i++) {
				int j = indexMap[i];
				magnitudes[0, j] = profile.values[0, i];
				magnitudes[1, j] = profile.values[1, i] + magnitudes[0, j];
				magnitudes[2, j] = profile.values[2, i] + magnitudes[1, j];
				magnitudes[3, j] = profile.values[4, i]; // = profile.ratings[3, i] + magnitudes[2, j]
			}

			float greatestMagnitude = 0;
			for (int i = 0; i < 8; i++)
				if (magnitudes[3, i] > greatestMagnitude)
					greatestMagnitude = magnitudes[3, i];


			//Determing text radius and preload labels;
			Pango.Layout[] labels = new Pango.Layout[8];
			IntVector2[] labelSizes = new IntVector2[8];
			for (int i = 0; i < 8; i++) {
				//Label
				labels[i] = new Pango.Layout(PangoContext);
				labels[i].SetText(Graphics.classSymbols[reverseIndexMap[i]]);
				labels[i].GetSize(out int labelWidth, out int labelHeight);
				labelSizes[i] = new IntVector2(labelWidth, labelHeight) / Pango.Scale.PangoScale;
				int thisLabelRadius = Math.Max(labelSizes[i].x, labelSizes[i].y);
				if (thisLabelRadius > labelRadius) {
					labelRadius = thisLabelRadius;
					chartRadius = size / 2 - labelRadius * 2;
				}
			}

			for (int i = 0; i < 8; i++) {
				IntVector2 textPoint = directions[i] * (chartRadius + labelRadius) + (Vector2)center - (Vector2)labelSizes[i] / 2;
				pixmap.DrawLayout(contexts[7], textPoint.x, textPoint.y, labels[i]);
				mask.DrawLayout(contexts[4], textPoint.x, textPoint.y, labels[i]);
				//Line
				IntVector2 endPoint = (directions[i] * chartRadius) + (Vector2)center;
				pixmap.DrawLine(contexts[5], center.x, center.y, endPoint.x, endPoint.y);
				mask.DrawLine(contexts[4], center.x, center.y, endPoint.x, endPoint.y);
			}


			//Draw circles and axis markings
			float theoreticalPtInterval = (size / 6 + 20) / 2;
			float theoreticalRatingInterval = theoreticalPtInterval / chartRadius * greatestMagnitude; //(between 20pt markings)
			int ratingInterval = (int)Math.Round(theoreticalRatingInterval); //Round it off for neatness
			float ptInterval = ratingInterval / greatestMagnitude * chartRadius; //Now what's the pt interval for that?

			for (int i = 1; ptInterval * i < chartRadius; i++) {
				//Circle
				int radius = (int)(ptInterval * i);
				pixmap.DrawArc(contexts[5], false,
							   center.x - radius, center.y - radius,
							   radius * 2, radius * 2,
							   0, 23040); //Angles are in 1/64th degrees, 23040 = 360*64 = full circle.
				mask.DrawArc(contexts[4], false,
							 center.x - radius, center.y - radius,
							 radius * 2, radius * 2,
							 0, 23040);
				//Axis marking
				Pango.Layout mark = new Pango.Layout(PangoContext);
				mark.SetText("" + ratingInterval * i);
				mark.GetSize(out int markWidth, out int markHeight);
				IntVector2 markSize = new IntVector2(markWidth, markHeight) / Pango.Scale.PangoScale;
				IntVector2 markCenter = (Vector2)center + directions[2] * ptInterval * i;
				mask.DrawArc(contexts[3], true, markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2,
							 markSize.x, markSize.y, 0, 23040); //Clears a circular space for the mark
				pixmap.DrawLayout(contexts[6], markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2, mark); //Actually
				mask.DrawLayout(contexts[4], markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2, mark);   //draw the mark.
			}

			//Compute vertices
			IntVector2[,] vertices = new IntVector2[4, 8];
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 8; j++)
					vertices[i, j] = directions[j] * (chartRadius / greatestMagnitude) * magnitudes[i, j] + (Vector2)center;

			//Bump vertices by a pixel if they overlap.
			for (int i = 1; i < 4; i++)
				for (int j = 0; j < 8; j++)
					while (IsOverlappingSomething(vertices, i, j))
						vertices[i, j] += (IntVector2)directions[j];

			//Draw polygons
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 7; j++) { //j=7 is excluded as we need to connect it to vertex 0 manually.
					pixmap.DrawLine(contexts[i],
									vertices[i, j].x, vertices[i, j].y,          //This point
									vertices[i, j + 1].x, vertices[i, j + 1].y); //Next point
					mask.DrawLine(contexts[4],
									vertices[i, j].x, vertices[i, j].y,          //This point
									vertices[i, j + 1].x, vertices[i, j + 1].y); //Next point
				}
				pixmap.DrawLine(contexts[i],
								vertices[i, 7].x, vertices[i, 7].y,  //Point 7
								vertices[i, 0].x, vertices[i, 0].y); //Point 0
				mask.DrawLine(contexts[4],
								vertices[i, 7].x, vertices[i, 7].y,  //Point 7
								vertices[i, 0].x, vertices[i, 0].y); //Point 0
			}

			SetFromPixmap(pixmap, mask);
			ShowAll();

		}

		private bool IsOverlappingSomething (IntVector2[,] vertices, int i, int j) {
			for (int n = 0; n < i; n++)
				if (vertices[i, j] == vertices[n, j])
					return true;
			return false;
		}

	}

}
