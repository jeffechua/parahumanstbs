using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public interface IDependable {
		int order { get; }
		List<IDependable> dependencies { get; set; }
		List<IDependable> dependents { get; set; }
		void Reload ();
	}

	public static class DependencyManager {

		static SortedList<int, List<IDependable>> flagged = new SortedList<int, List<IDependable>>();

		public static void Flag (IDependable obj) {
			if (!flagged.ContainsKey(obj.order))
				flagged.Add(obj.order, new List<IDependable>());
			if (!flagged[obj.order].Contains(obj))
				flagged[obj.order].Add(obj);
		}

		public static void TriggerAllFlags () {
			for (int i = 0; i < flagged.Count; i++) {
				for (int j = 0; j < flagged.Values[i].Count; j++) {
					for (int k = 0; k < flagged.Values[i][j].dependents.Count; k++) {
						Flag(flagged.Values[i][j].dependents[k]);
					}
				}
			}
			for (int i = 0; i < flagged.Count; i++)
				for (int j = 0; j < flagged.Values[i].Count; j++)
					flagged.Values[i][j].Reload();
			flagged.Clear();
		}

		public static void Connect (IDependable trigger, IDependable responder) {
			if (trigger.dependents.Contains(responder))
				return;
			trigger.dependents.Add(responder);
			responder.dependencies.Add(trigger);
		}

		public static void Disconnect (IDependable trigger, IDependable responder) { //Unnecessary unless Gtk decides to throw warnings
			trigger.dependents.RemoveAll((input) => input == responder);
			responder.dependencies.RemoveAll((input) => input == trigger);
		}

		public static void DisconnectAll (IDependable obj) {
			foreach (IDependable dependent in obj.dependents)
				dependent.dependencies.RemoveAll((input) => input == obj);
			foreach (IDependable dependency in obj.dependencies)
				dependency.dependents.RemoveAll((input) => input == obj);
			obj.dependents.Clear();
			obj.dependencies.Clear();
		}

		// Utility function to not only DisconnectAll(obj), but also
		//  - Call obj.RemoveRange(dependencies) if obj is an IContainer
		//  - Call dependent.Remove(obj) for all dependents that are IContainers
		// This is because we assume that parents are dependent on children,
		// and children are dependencies of parents.
		public static void Delete (IDependable obj) {
			if (obj is IContainer)
				((IContainer)obj).RemoveRange(obj.dependencies.FindAll(
					(element) => ((IContainer)obj).Contains(element)));
			while (true) {
				int length = obj.dependents.Count;
				for (int i = 0; i < obj.dependents.Count; i++) {
					if (obj.dependents[i] is IContainer) {
						((IContainer)obj.dependents[i]).RemoveRange(new List<object> { obj });
					}
				}
				if (obj.dependents.Count == length) break; //Has the length been further reduced?
			}
			Flag(obj);
			TriggerAllFlags();
			DisconnectAll(obj);
		}

	}
}
