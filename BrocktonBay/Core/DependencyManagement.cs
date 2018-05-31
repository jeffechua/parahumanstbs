using System;
using System.Collections.Generic;

namespace Parahumans.Core.GUI {

	public interface IDependable {
		int order { get; }
		List<WeakReference<IDependable>> dependencies { get; set; }
		List<WeakReference<IDependable>> dependents { get; set; }
		void Reload();
	}

	public static class DependencyManager {

		static SortedList<int, List<IDependable>> flagged = new SortedList<int, List<IDependable>>();

		public static void Flag(IDependable obj) {
			if (!flagged.ContainsKey(obj.order))
				flagged.Add(obj.order, new List<IDependable>());
			if (!flagged[obj.order].Contains(obj))
				flagged[obj.order].Add(obj);
		}

		public static void TriggerAllFlags() {
			for (int i = 0; i < flagged.Count; i++) {
				for (int j = 0; j < flagged.Values[i].Count; j++) {
					for (int k = 0; k < flagged.Values[i][j].dependents.Count; k++) {
						if (flagged.Values[i][j].dependents[k].TryGetTarget(out IDependable dependent)) {
							Flag(dependent);
						} else {
							flagged.Values[i][j].dependents.RemoveAt(k);
							i--;
						}
					}
				}
			}
			for (int i = 0; i < flagged.Count; i++)
				for (int j = 0; j < flagged.Values[i].Count; j++)
					flagged.Values[i][j].Reload();
			flagged.Clear();
		}

		public static void Connect(IDependable trigger, IDependable responder) {
			for (int i = 0; i < trigger.dependents.Count; i++) {
				if (trigger.dependents[i].TryGetTarget(out IDependable target)) {
					if (target == responder) {
						return;
					}
				} else {
					trigger.dependents.RemoveAt(i);
					i--;
				}
			}
			trigger.dependents.Add(new WeakReference<IDependable>(responder));
			responder.dependencies.Add(new WeakReference<IDependable>(trigger));
		}

		public static void Disconnect(IDependable trigger, IDependable responder) { //Unnecessary unless Gtk decides to throw warnings
			trigger.dependents.RemoveAll((input) => !input.TryGetTarget(out IDependable target) || target == responder);
			responder.dependencies.RemoveAll((input) => !input.TryGetTarget(out IDependable target) || target == trigger);
		}

		public static void DisconnectAll(IDependable obj) {
			for (int i = 0; i < obj.dependents.Count; i++)
				if (obj.dependents[i].TryGetTarget(out IDependable dependent))
					dependent.dependencies.RemoveAll((input) => !input.TryGetTarget(out IDependable target) || target == obj);
			for (int i = 0; i < obj.dependencies.Count; i++)
				if (obj.dependencies[i].TryGetTarget(out IDependable dependency))
					dependency.dependents.RemoveAll((input) => !input.TryGetTarget(out IDependable target) || target == obj);
			obj.dependents.Clear();
			obj.dependencies.Clear();
		}

		// Utility function to not only DisconnectAll(obj), but also
		//  - Call obj.RemoveRange(dependencies) obj is an IContainer
		//  - Call dependent.Remove(obj) for all dependents that are IContainers
		// This is because we assume that parents are dependent on children,
		// and children are dependencies of parents.
		public static void Delete(IDependable obj) {
			if (obj is IContainer) {
				((IContainer)obj).RemoveRange(
					obj.dependencies.ConvertAll(
						delegate (WeakReference<IDependable> element) {
							element.TryGetTarget(out IDependable result);
							return result;
						})
					.FindAll(
						delegate (IDependable element) {
							return ((IContainer)obj).Contains(element);
						}
					)
				);
			}
			bool done = false;;
			while (!done) {
				int length = obj.dependents.Count;
				for (int i = 0; i < obj.dependents.Count; i++) {
					if (obj.dependents[i].TryGetTarget(out IDependable dependent)) {
						if (dependent is IContainer) {
							((IContainer)dependent).RemoveRange(new List<object> { obj });
						}
					}
				}
				done = obj.dependents.Count == length;
			}
			Flag(obj);
			TriggerAllFlags();
			DisconnectAll(obj);
		}

	}
}
