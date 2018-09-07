using System;
using System.Collections.Generic;
using Gdk;
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

		public List<T> sample;
		public List<ObjectWidgetPair<T>> cache;
		public Func<T, Widget> Generator;

		public CachingLister (Func<T, Widget> Generator) {
			PackStart(new Gtk.Alignment(0, 0, 1, 1) { HeightRequest = 5 }, false, false, 0);
			this.Generator = Generator;
			cache = new List<ObjectWidgetPair<T>>();
		}

		public Widget Retrieve (T obj) {
			ObjectWidgetPair<T> pair = cache.Find((element) => obj == element.obj);
			if (pair.obj == null) {
				pair = new ObjectWidgetPair<T>(obj, Generator(obj));
				pair.widget.Destroyed += (o, a) => cache.Remove(pair);
				PackStart(pair.widget, false, false, 0);
				cache.Add(pair);
			}
			return pair.widget;
		}

		public void Load (List<T> sample) {
			this.sample = sample;
			foreach (T item in sample) {
				Widget retrieved = Retrieve(item);
				ReorderChild(retrieved, -1);
			}
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

		protected override void OnShown () {
			base.OnShown();
			Render();
		}

	}

	public class CachingTesselator<T> : EventBox where T : class {

		public List<T> sample;
		public List<ObjectWidgetPair<T>> cache;
		public Func<T, Widget> Generator;

		Container shell;
		Fixed contents;
		int height;

		public CachingTesselator (Func<T, Widget> Generator, Container shell) {
			this.Generator = Generator;
			cache = new List<ObjectWidgetPair<T>>();
			this.shell = shell;
			contents = new Fixed();
			Add(contents);
			EnterNotifyEvent += (o, a) => Arrange(false);
		}

		public Widget Retrieve (T obj) {
			ObjectWidgetPair<T> pair = cache.Find((element) => obj == element.obj);
			if (pair.obj == null) {
				pair = new ObjectWidgetPair<T>(obj, Generator(obj));
				pair.widget.Destroyed += (o, a) => cache.Remove(pair);
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
			Arrange(true);
		}

		public void Arrange (bool force = true) {

			if (height == shell.Allocation.Height && !force) return;

			height = shell.Allocation.Height - 10; //A ScrolledWindow bevel counts into its width. As we don't have max_content_width in gtk+2, the 10pt buffer is used instead. Otherwise, horizontal overflow and a resulting horizontal scrollbar may pop up in certain caes.
			int y = 0;
			int x = 0;
			int rowWidth = 0;

			foreach (ObjectWidgetPair<T> pair in cache) {
				if (sample.Contains(pair.obj)) {
					Requisition size = pair.widget.SizeRequest();
					if (y + size.Height > height) {
						y = 0;
						x += rowWidth;
						rowWidth = 0;
					}
					contents.Move(pair.widget, x, y);
					y += size.Height;
					if (size.Width > rowWidth) rowWidth = size.Width;
				}
			}

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

		protected override void OnShown () {
			base.OnShown();
			Render();
		}

	}
}
