using System;
using System.Collections.Generic;
using Gtk;


// Note that this is kind of a memory leak. Deleted gameObjects will leave a broken cached ObjectWidgetPair
// that never gets summoned but sits there in memory.
namespace BrocktonBay {

	public struct ObjectWidgetPair<T> {
		public T obj;
		public Widget widget;
		public ObjectWidgetPair (T o, Widget w) {
			obj = o;
			widget = w;
		}
	}

	public class CachingLister<T> : VBox where T : class {

		public List<T> population;
		public List<T> sample;
		public List<ObjectWidgetPair<T>> cache;
		public Func<T, Widget> Generator;

		public CachingLister (List<T> population, Func<T, Widget> Generator) {
			PackStart(new Gtk.Alignment(0, 0, 1, 1) { HeightRequest = 5 }, false, false, 0);
			this.population = population;
			this.Generator = Generator;
			cache = new List<ObjectWidgetPair<T>>();
		}

		public Widget Retrieve (T obj) {
			ObjectWidgetPair<T> pair = cache.Find((element) => obj == element.obj);
			if (pair.obj == null) {
				pair = new ObjectWidgetPair<T>(obj, Generator(obj));
				PackStart(pair.widget, false, false, 0);
				cache.Add(pair);
			}
			return pair.widget;
		}

		public void Load (List<T> s) {
			sample = s;
			foreach (T item in sample)
				ReorderChild(Retrieve(item), -1);
		}

		public void Render () {
			foreach (ObjectWidgetPair<T> pair in cache) {
				if (sample.Contains(pair.obj)) {
					pair.widget.ShowAll();
				} else {
					pair.widget.HideAll();
				}
			}
		}

	}

	public class CachingTesselator<T> : EventBox where T : class {

		public List<T> population;
		public List<T> sample;
		public List<ObjectWidgetPair<T>> cache;
		public Func<T, Widget> Generator;

		Container shell;
		Fixed contents;
		int width;

		public CachingTesselator (List<T> population, Func<T, Widget> Generator, Container shell) {
			this.population = population;
			this.Generator = Generator;
			cache = new List<ObjectWidgetPair<T>>();
			this.shell = shell;
			contents = new Fixed();
			Add(contents);
			EnterNotifyEvent += (o, a) => Render(false);
		}

		public Widget Retrieve (T obj) {
			ObjectWidgetPair<T> pair = cache.Find((element) => obj == element.obj);
			if (pair.obj == null) {
				pair = new ObjectWidgetPair<T>(obj, Generator(obj));
				contents.Put(pair.widget, 0, 0);
				contents.ShowAll();
				cache.Add(pair);
			}
			return pair.widget;
		}

		public void Load (List<T> s) {
			sample = s;
			foreach (T item in sample)
				Retrieve(item);
		}

		public void Render (bool force = true) {

			if (width == shell.Allocation.Width && !force) return;

			width = shell.Allocation.Width - 10; //A ScrolledWindow bevel counts into its width. As we don't have max_content_width in gtk+2, the 10pt buffer is used instead. Otherwise, horizontal overflow and a resulting horizontal scrollbar may pop up in certain caes.
			int x = 0;
			int y = 0;
			int rowHeight = 0;

			foreach (ObjectWidgetPair<T> pair in cache) {
				if (sample.Contains(pair.obj)) {
					Requisition size = pair.widget.SizeRequest();
					if (x + size.Width > width) {
						x = 0;
						y += rowHeight;
						rowHeight = 0;
					}
					contents.Move(pair.widget, x, y);
					x += size.Width;
					if (size.Height > rowHeight) rowHeight = size.Height;
					pair.widget.ShowAll();
				} else {
					pair.widget.HideAll();
				}
			}

		}
	}
}
