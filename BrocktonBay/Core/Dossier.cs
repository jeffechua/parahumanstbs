using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public struct DossierData {
		public int knowerID;
		public List<Tuple<int, int>> tuples;
		public DossierData (Dossier dossier, GameObject knower) {
			knowerID = knower.ID;
			tuples = new List<Tuple<int, int>>();
			Console.WriteLine(dossier.Count);
			foreach (KeyValuePair<GameObject, int> pair in dossier)
				tuples.Add(new Tuple<int, int>(pair.Key.ID, pair.Value));
			Console.WriteLine(tuples.Count);
		}
	}

	public class Dossier : Dictionary<GameObject, int> {

		public Dossier () {
			foreach (GameObject obj in Game.city.gameObjects)
				Add(obj, 0);
		}

		public Dossier (IDictionary<GameObject, int> dictionary) : base(dictionary) { }

		public Dossier Clone () {
			return new Dossier(this);
		}

		public Dossier (DossierData data) {
			foreach(Tuple<int,int> tuple in data.tuples)
				Add(Game.city.Get(tuple.Item1), tuple.Item2); 
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
