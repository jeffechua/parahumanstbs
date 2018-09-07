using System.Collections.Generic;
using System;
using System.IO;
using Gtk;
using Newtonsoft.Json;

namespace BrocktonBay {

	public class City : IContainer, IDependable {

		public string saveFolder;

		//IDependable members
		public int order { get { return 4; } }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public void Reload () {
			gameObjects.Sort();
			foreach (IAgent agent in activeAgents) {
				if (GameObject.TryCast(agent, out Faction faction)) {
					if (faction.alignment > 0) {
						if (heroicAuthority == null || faction.reputation > heroicAuthority.reputation) {
							heroicAuthority = faction;
						}
					} else if (faction.alignment < 0) {
						if (villainousAuthority == null || faction.reputation > villainousAuthority.reputation) {
							villainousAuthority = faction;
						}
					}
				}
			}
		}
		public void OnTriggerDestroyed (IDependable trigger) {
			if (Contains(trigger)) {
				Remove(trigger);
				DependencyManager.Flag(this);
			}
		}
		public void OnListenerDestroyed (IDependable listener) { }

		public List<GameObject> gameObjects = new List<GameObject>();
		public List<IAgent> activeAgents = new List<IAgent>();
		public List<IBattleground> activeBattlegrounds = new List<IBattleground>();
		public Faction heroicAuthority;
		public Faction villainousAuthority;

		public byte[] mapPngSource = { };
		public int mapDefaultWidth = 0;
		public int territorySizeScale = 0;

		public T Get<T> (string name) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.name.ToLower() == name.ToLower());
		public T Get<T> (int ID) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.ID == ID);
		public GameObject Get (int ID) => (GameObject)gameObjects.Find(obj => obj.ID == ID);

		//IContainer members
		public bool Accepts (object obj) => obj is GameObject;
		public bool Contains (object obj) => obj is GameObject && gameObjects.Contains((GameObject)obj);
		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				GameObject newGO = (GameObject)obj;
				gameObjects.Add(newGO);
				foreach (IAgent agent in activeAgents)
					if (agent.knowledge != null)
						agent.knowledge.Add(newGO, 0);
				if (GameObject.TryCast(obj, out MapMarked mapMarked))
					Map.Register(mapMarked);
				DependencyManager.Connect(newGO, this);
				DependencyManager.Flag(newGO);
			}
			DependencyManager.Flag(this);
		}
		public void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				GameObject removedGO = (GameObject)obj;
				gameObjects.Remove(removedGO);
				foreach (IAgent agent in activeAgents)
					if (agent.knowledge != null)
						agent.knowledge.Remove(removedGO);
				if (removedGO.TryCast(out IAgent tryAgent))
					activeAgents.Remove(tryAgent);
				if (GameObject.TryCast(obj, out MapMarked mapMarked))
					Map.Deregister(mapMarked);
				DependencyManager.Disconnect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
			}
			DependencyManager.Flag(this);
		}

	}

}