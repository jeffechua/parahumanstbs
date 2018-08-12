using System.Reflection;
using System;
using Gtk;
using Gdk;

namespace BrocktonBay {

	public class RatingsRadarChart : Gtk.Image {

		float[,] values;
		int[,] o_vals;
		float[] multipliers;
		float[] metamultipliers;
		Context context;

		int currentSize;
		Pixmap color;
		Pixmap mask;

		public RatingsRadarChart (Context context, RatingsProfile profile, float[] multipliers = null, float[] metamultipliers = null) {
			this.context = context;
			values = profile.values;
			o_vals = profile.o_vals;
			this.multipliers = multipliers;
			this.metamultipliers = metamultipliers;
			SetSizeRequest(0, 0);
			SizeAllocated += DrawChart;
		}

		public void DrawChart (object obj, SizeAllocatedArgs args) {

			int width = args.Allocation.Width;
			int height = args.Allocation.Height;
			int size = Math.Min(width, height);

			//If this is true, then we don't need to update.
			if (size == currentSize) return;
			currentSize = size;

			currentSize = size;
			int chartRadius = size;
			int labelRadius = 0;

			color = new Pixmap(MainWindow.main.GdkWindow, size, size);
			mask = new Pixmap(MainWindow.main.GdkWindow, size, size);

			Gdk.GC visible = new Gdk.GC(mask) { RgbFgColor = new Color(255, 255, 255) };
			Gdk.GC invisible = new Gdk.GC(mask) { RgbFgColor = new Color(0, 0, 0) };

			Gdk.GC white = new Gdk.GC(color) { RgbFgColor = new Color(255, 255, 255) };      // for marking the mask.
			Gdk.GC lightGrey = new Gdk.GC(color) { RgbFgColor = new Color(200, 200, 200) };  // for the axes
			Gdk.GC grey = new Gdk.GC(color) { RgbFgColor = new Color(125, 125, 125) };       // for the axis markings
			Gdk.GC darkGrey = new Gdk.GC(color) { RgbFgColor = new Color(100, 100, 100) };   // for the axis labels
			Gdk.GC[] wrapperColors = {
				new Gdk.GC(color) { RgbFgColor = new Gdk.Color(170, 140, 0) }, //Regular
				new Gdk.GC(color) { RgbFgColor = new Color(0, 0, 200) },       //Tinker
				new Gdk.GC(color) { RgbFgColor = new Color(0, 150, 0) },       //Master
				new Gdk.GC(color) { RgbFgColor = new Color(0, 0, 0) },         //Breaker
			};

			color.DrawRectangle(white, true, new Rectangle(0, 0, size, size));
			mask.DrawRectangle(invisible, true, new Rectangle(0, 0, size, size));

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
			float[] magnitudes = new float[8]; //Magnitudes of the positions of the vertices
			float[,] fractions = new float[8, 3]; //Fractional contribution of each wrapper of each rating classification

			for (int i = 1; i <= 8; i++) {
				int j = indexMap[i];
				magnitudes[j] = values[4, i] > 0 ? values[4, i] : 0;
				if (o_vals[4, i] != 0) {
					fractions[j, 0] = (float)o_vals[0, i] / o_vals[4, i];
					fractions[j, 1] = (float)(o_vals[0, i] + o_vals[1, i]) / o_vals[4, i];
					fractions[j, 2] = (float)(o_vals[0, i] + o_vals[1, i] + o_vals[2, i]) / o_vals[4, i];
				}
			}

			float greatestMagnitude = 0;
			for (int i = 0; i < 8; i++)
				if (magnitudes[i] > greatestMagnitude)
					greatestMagnitude = magnitudes[i];
			if (greatestMagnitude < 0.01) return;

			//Determing text radius and preload labels;
			Pango.Layout[] labels = new Pango.Layout[8];
			IntVector2[] labelSizes = new IntVector2[8];
			for (int i = 0; i < 8; i++) {
				//Label
				labels[i] = new Pango.Layout(PangoContext) { Alignment = Pango.Alignment.Center };
				if (multipliers == null) {
					labels[i].SetText(Ratings.symbols[reverseIndexMap[i]]);
				} else {
					labels[i].SetMarkup(Ratings.symbols[reverseIndexMap[i]] + "\n<small>×" + multipliers[reverseIndexMap[i]].ToString("0.0") + "</small>");
				}
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
				color.DrawLayout(darkGrey, textPoint.x, textPoint.y, labels[i]);
				mask.DrawLayout(visible, textPoint.x, textPoint.y, labels[i]);
				//Line
				IntVector2 endPoint = (directions[i] * chartRadius) + (Vector2)center;
				color.DrawLine(lightGrey, center.x, center.y, endPoint.x, endPoint.y);
				mask.DrawLine(visible, center.x, center.y, endPoint.x, endPoint.y);
			}


			//Draw circles and axis markings
			float theoreticalPtInterval = (size / 6 + 20) / 2;
			float theoreticalRatingInterval = theoreticalPtInterval / chartRadius * greatestMagnitude; //(between 20pt markings)
			int ratingInterval = (int)Math.Round(theoreticalRatingInterval); //Round it off for neatness
			float ptInterval = ratingInterval / greatestMagnitude * chartRadius; //Now what's the pt interval for that?

			for (int i = 1; ptInterval * i < chartRadius; i++) {
				//Circle
				int radius = (int)(ptInterval * i);
				color.DrawArc(lightGrey, false,
							   center.x - radius, center.y - radius,
							   radius * 2, radius * 2,
							   0, 23040); //Angles are in 1/64th degrees, 23040 = 360*64 = full circle.
				mask.DrawArc(visible, false,
							 center.x - radius, center.y - radius,
							 radius * 2, radius * 2,
							 0, 23040);
				//Axis marking
				Pango.Layout mark = new Pango.Layout(PangoContext);
				mark.SetText("" + ratingInterval * i);
				mark.GetSize(out int markWidth, out int markHeight);
				IntVector2 markSize = new IntVector2(markWidth, markHeight) / Pango.Scale.PangoScale;
				IntVector2 markCenter = (Vector2)center + directions[2] * ptInterval * i;
				mask.DrawArc(invisible, true, markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2,
							 markSize.x, markSize.y, 0, 23040); //Clears a circular space for the mark
				color.DrawLayout(grey, markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2, mark); //Actually
				mask.DrawLayout(visible, markCenter.x - markSize.x / 2, markCenter.y - markSize.y / 2, mark);   //draw the mark.
			}

			//Compute vertices
			IntVector2[,] vertices = new IntVector2[4, 8];
			for (int i = 0; i < 8; i++) {
				for (int j = 0; j < 3; j++)
					vertices[j, i] = directions[i] * (chartRadius / greatestMagnitude) * magnitudes[i] * fractions[i, j] + (Vector2)center;
				vertices[3, i] = directions[i] * (chartRadius / greatestMagnitude) * magnitudes[i] + (Vector2)center;
			}

			//Bump vertices by a pixel if they overlap.
			bool changed = true;
			while (changed) {
				changed = false;
				for (int i = 3; i > 1; i--) {
					for (int j = 0; j < 8; j++) {
						if (vertices[i, j] == vertices[i - 1, j]) {
							vertices[i, j] += (IntVector2)directions[j];
							changed = true;
						}
					}
				}
			}

			//Draw polygons
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 7; j++) { //j=7 is excluded as we need to connect it to vertex 0 manually.
					color.DrawLine(wrapperColors[i],
									vertices[i, j].x, vertices[i, j].y,          //This point
									vertices[i, j + 1].x, vertices[i, j + 1].y); //Next point
					mask.DrawLine(visible,
									vertices[i, j].x, vertices[i, j].y,          //This point
									vertices[i, j + 1].x, vertices[i, j + 1].y); //Next point
				}
				color.DrawLine(wrapperColors[i],
								vertices[i, 7].x, vertices[i, 7].y,  //Point 7
								vertices[i, 0].x, vertices[i, 0].y); //Point 0
				mask.DrawLine(visible,
								vertices[i, 7].x, vertices[i, 7].y,  //Point 7
								vertices[i, 0].x, vertices[i, 0].y); //Point 0
			}

			if (metamultipliers != null) {
				int currentY = size - 5;
				for (int i = 3; i >= 0; i--) {
					Pango.Layout label = new Pango.Layout(PangoContext) { Alignment = Pango.Alignment.Center };
					label.SetMarkup("<small>x" + metamultipliers[i].ToString("0.0") + "</small>");
					label.GetSize(out int labelWidth, out int labelHeight);
					labelWidth = (int)(labelWidth / Pango.Scale.PangoScale);
					labelHeight = (int)(labelHeight / Pango.Scale.PangoScale);
					currentY -= labelHeight;
					color.DrawLayout(wrapperColors[i], size - labelWidth - 5, currentY, label);
					mask.DrawLayout(visible, size - labelWidth - 5, currentY, label);
				}
			}

			SetFromPixmap(color, mask);


		}

		private bool IsOverlappingSomething (IntVector2[,] vertices, int i, int j) {
			for (int n = 0; n < i; n++)
				if (vertices[i, j] == vertices[n, j])
					return true;
			return false;
		}

	}

}
