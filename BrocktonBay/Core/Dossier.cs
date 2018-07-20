using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public class Dossier : Dictionary<GameObject, int> {

		public Dossier () {
			foreach (GameObject obj in Game.city.gameObjects)
				Add(obj, 0);
		}

		public Dossier (IDictionary<GameObject, int> dictionary) : base(dictionary) { }

		public Dossier Clone () {
			return new Dossier(this);
		}

		public Dossier Choose (IAgent agent, bool maxUnchosen) {
			Dossier dossier = new Dossier();
			foreach (GameObject obj in dossier.Keys) {
				if (obj.affiliation == agent) {
					dossier[obj] = this[obj];
				} else if (maxUnchosen) {
					dossier[obj] = int.MaxValue;
				}
			}
			return dossier;
		}

		public static Dossier operator | (Dossier a, Dossier b) {
			Dossier c = new Dossier();
			foreach (GameObject obj in a.Keys)
				c[obj] = Math.Max(a[obj], b[obj]);
			return c;
		}

		public static Dossier operator & (Dossier a, Dossier b) {
			Dossier c = new Dossier();
			foreach (GameObject obj in a.Keys)
				c[obj] = Math.Min(a[obj], b[obj]);
			return c;
		}

	}

}
