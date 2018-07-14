using System.Collections.Generic;
using System;
using System.IO;
using Gtk;
using Newtonsoft.Json;

namespace Parahumans.Core {

	public class City : IContainer, IDependable {

		public string saveFolder;

		//IDependable members
		public int order { get { return 4; } }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public void Reload () => gameObjects.Sort();

		public List<GameObject> gameObjects = new List<GameObject>();
		public Dictionary<IAgent, Dictionary<GameObject, InfoState>> intrigue;

		public byte[] mapPngSource = {};
		public int mapDefaultWidth = 0;
		public int territorySizeScale = 0;

		public T Get<T> (string name) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.name.ToLower() == name.ToLower());
		public T Get<T> (int ID) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.ID == ID);

		//IContainer members
		public bool Accepts (object obj) => obj is GameObject;
		public bool Contains (object obj) => obj is GameObject && gameObjects.Contains((GameObject)obj);
		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				gameObjects.Add((GameObject)obj);
				DependencyManager.Connect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
			}
			DependencyManager.Flag(this);
		}
		public void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				gameObjects.Remove((GameObject)obj);
				DependencyManager.Disconnect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
			}
			DependencyManager.Flag(this);
		}

	}

}