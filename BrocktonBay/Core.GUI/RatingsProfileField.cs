using System.Reflection;
using System;
using Gtk;
using Gdk;

namespace Parahumans.Core {

	public class RatingsProfile {
		public float[,] ratings;
		public float strength;
		public float stealth;
		public float insight;
		public RatingsProfile () {
			ratings = new float[5, 8];
		}
		public void Evaluate () {
			strength = ratings[4, 0] + ratings[4, 1] + ratings[4, 2] / 2 + ratings[4, 3] / 2;
			stealth = ratings[4, 4] + ratings[4, 5];
			insight = ratings[4, 6] + ratings[4, 7];
		}
	}

	public struct Vector2 {
		public double x;
		public double y;
		public Vector2 (double n1, double n2) {
			x = n1;
			y = n2;
		}
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
	}

	public class RatingsProfileField : Gtk.Image {

		PropertyInfo property;
		RatingsProfile profile;

		public RatingsProfileField (PropertyInfo p, object o, bool vert, object arg) {
			property = p;
			profile = (RatingsProfile)property.GetValue(o);
			SizeAllocated += Initialize;
			SetSizeRequest(500, 500);
		}

		public void Initialize (object obj, SizeAllocatedArgs args) {

			SizeAllocated -= Initialize;

			int width = args.Allocation.Width;
			int height = args.Allocation.Height;
			int size = System.Math.Min(width, height);

			Pixmap pixmap = new Pixmap(GdkWindow, size, size);
			Pixmap mask = new Pixmap(GdkWindow, size, size);
			Gdk.GC[] contexts = {
				new Gdk.GC(pixmap) { RgbFgColor = new Gdk.Color(170, 140, 0) }, //Regular
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 150, 0) },     //Master
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 0, 200) },     //Tinker
				new Gdk.GC(pixmap) { RgbFgColor = new Color(0, 0, 0) },       //Breaker
				new Gdk.GC(pixmap) { RgbFgColor = new Color(255, 255, 255) } //White, for marking the mask.
			};
			pixmap.DrawRectangle(contexts[4], true, new Rectangle(0, 0, size, size));

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

			//Compute magnitudes

			int[] indexMap = { 1, 5, 3, 7, 0, 4, 7, 2 }; //old[0] -> new[1], old[1] -> new[5] etc.
			float[,] magnitudes = new float[4, 8]; //Magnitudes of the positions of the vertices

			for (int i = 0; i < 8; i++) {
				int j = indexMap[i];
				magnitudes[0, j] = profile.ratings[0, i];
				magnitudes[1, j] = profile.ratings[1, i] + magnitudes[0, j];
				magnitudes[2, j] = profile.ratings[2, i] + magnitudes[1, j];
				magnitudes[3, j] = profile.ratings[4, i]; // = profile.ratings[3, i] + magnitudes[2, j]
			}

			float greatestMagnitude = 0;
			for (int i = 0; i < 8; i++)
				if (magnitudes[3, i] > greatestMagnitude)
					greatestMagnitude = magnitudes[3, i];

			//Compute vertices
			Vector2[,] vertices = new Vector2[4, 8];
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 8; j++)
					vertices[i, j] = directions[j] * (size / 2 / greatestMagnitude) * magnitudes[i, j]
						+ new Vector2(size / 2, size / 2);

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 7; j++) { //j=7 is excluded as we need to connect it to vertex 0 manually.
					pixmap.DrawLine(contexts[i],
									(int)vertices[i, j].x, (int)vertices[i, j].y,          //This point
									(int)vertices[i, j + 1].x, (int)vertices[i, j + 1].y); //Next point
					mask.DrawLine(contexts[4],
									(int)vertices[i, j].x, (int)vertices[i, j].y,          //This point
									(int)vertices[i, j + 1].x, (int)vertices[i, j + 1].y); //Next point
				}
				pixmap.DrawLine(contexts[i],
								(int)vertices[i, 7].x, (int)vertices[i, 7].y,  //Point 7
								(int)vertices[i, 0].x, (int)vertices[i, 0].y); //Point 0
				mask.DrawLine(contexts[4],
								(int)vertices[i, 7].x, (int)vertices[i, 7].y,  //Point 7
								(int)vertices[i, 0].x, (int)vertices[i, 0].y); //Point 0
			}

			SetFromPixmap(pixmap, mask);
			ShowAll();

		}

	}

}
