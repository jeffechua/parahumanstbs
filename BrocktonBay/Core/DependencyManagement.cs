//#define RELOAD_TRACKING

using System;
using System.Collections.Generic;

namespace BrocktonBay {

	public interface IDependable {
		int order { get; }
		bool destroyed { get; set; }
		List<IDependable> triggers { get; set; }
		List<IDependable> listeners { get; set; }
		void OnTriggerDestroyed (IDependable trigger);
		void OnListenerDestroyed (IDependable listener);
		void Reload ();
	}

	public class DependableShell : Gtk.Alignment, IDependable {
		public int order { get; set; }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public EventHandler ReloadEvent = new EventHandler(delegate { });
		public DependableShell (int order) : base(0, 0, 1, 1) {
			this.order = order;
			Destroyed += (o, a) => DependencyManager.DisconnectAll(this); //Theoretically we could Delete(obj), but that's unnecessary
		}
		public void Reload () => ReloadEvent.Invoke(this, new EventArgs());
		public void OnListenerDestroyed (IDependable listener) { }
		public void OnTriggerDestroyed (IDependable trigger) { }
	}

	public static class DependencyManager {

		static SortedList<int, List<IDependable>> flagged = new SortedList<int, List<IDependable>>();

		public static void Flag (IDependable obj) {
			if (!flagged.ContainsKey(obj.order))
				flagged.Add(obj.order, new List<IDependable>());
			if (!flagged[obj.order].Contains(obj))
				flagged[obj.order].Add(obj);
		}

#if !RELOAD_TRACKING
		// As a general rule, utility methods such as AddRange, RemoveRange should not call TriggerAllFlags: the
		// first initiating callback/method is responsible for this such as to minimize unnecessary work.
		public static void TriggerAllFlags () {
			for (int i = 0; i < flagged.Count; i++)
				for (int j = 0; j < flagged.Values[i].Count; j++)
					for (int k = 0; k < flagged.Values[i][j].listeners.Count; k++)
						Flag(flagged.Values[i][j].listeners[k]);
			for (int i = 0; i < flagged.Count; i++)
				for (int j = 0; j < flagged.Values[i].Count; j++)
					flagged.Values[i][j].Reload();
			flagged.Clear();
		}
#endif
#if RELOAD_TRACKING
		public static void TriggerAllFlags () {
			for (int i = 0; i < flagged.Count; i++)
				for (int j = 0; j < flagged.Values[i].Count; j++)
					for (int k = 0; k < flagged.Values[i][j].listeners.Count; k++)
						Flag(flagged.Values[i][j].listeners[k]);
			for (int i = 0; i < flagged.Count; i++) {
				if (flagged.Values[i].Count > 0) {
					Console.WriteLine("Reloading order " + flagged.Values[i][0].order + " IDependents...");
					DateTime metaCurrentTime = DateTime.Now;
					for (int j = 0; j < flagged.Values[i].Count; j++) {
						DateTime currentTime = DateTime.Now;
						flagged.Values[i][j].Reload();
						double interval = (DateTime.Now - currentTime).TotalMilliseconds;
						if (flagged.Values[i][j] is IGUIComplete) {
							Console.WriteLine("\tReloaded " + ((IGUIComplete)flagged.Values[i][j]).name + " in " + interval + " ms.");
						} else {
							Console.WriteLine("\tReloaded " + flagged.Values[i][j] + " in " + interval + " ms.");
						}
					}
					double metaInterval = (DateTime.Now - metaCurrentTime).TotalMilliseconds;
					Console.WriteLine("Reloaded order " + flagged.Values[i][0].order + " IDependents in " + metaInterval + " ms.");
				}
			}
			Console.WriteLine("\n");
			flagged.Clear();
		}
#endif

		public static void Connect (IDependable trigger, IDependable listener) {
			if (trigger.listeners.Contains(listener))
				return;
			trigger.listeners.Add(listener);
			listener.triggers.Add(trigger);
		}

		public static void Disconnect (IDependable trigger, IDependable listener) { //Unnecessary unless Gtk decides to throw warnings
			trigger.listeners.RemoveAll((input) => input == listener);
			listener.triggers.RemoveAll((input) => input == trigger);
		}

		public static void DisconnectAll (IDependable obj) {
			foreach (IDependable listener in obj.listeners)
				listener.triggers.RemoveAll((input) => input == obj);
			foreach (IDependable trigger in obj.triggers)
				trigger.listeners.RemoveAll((input) => input == obj);
			obj.listeners.Clear();
			obj.triggers.Clear();
		}

		public static void Destroy (IDependable obj) {

			if (obj.destroyed) return;

			obj.destroyed = true;

			IDependable[] triggers = obj.triggers.ToArray();
			foreach (IDependable trigger in triggers)
				trigger.OnListenerDestroyed(obj);

			IDependable[] listeners = obj.listeners.ToArray();
			foreach (IDependable listener in listeners)
				listener.OnTriggerDestroyed(obj);

			DisconnectAll(obj);

		}

	}
}
