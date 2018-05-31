using System.Collections.Generic;
using System;
using Parahumans.Core.GUI;

namespace Parahumans.Core {

	public class City : IContainer, IDependable {

		public static City city = new City();

		//IDependable members
		public int order { get { return 4; } }
		public List<WeakReference<IDependable>> dependents { get; set; } = new List<WeakReference<IDependable>>();
		public List<WeakReference<IDependable>> dependencies { get; set; } = new List<WeakReference<IDependable>>();
		public void Reload() { }

		public List<GameObject> gameObjects = new List<GameObject>();

		public T Get<T>(string name) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.name.ToLower() == name.ToLower());
		public T Get<T>(int ID) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.ID == ID);

		//IContainer members
		public void Sort() => gameObjects.Sort((GameObject gameObject1, GameObject gameObject2) => gameObject1.ID.CompareTo(gameObject2.ID));
		public bool Accepts(object obj) => obj is GameObject;
		public bool Contains(object obj) => obj is GameObject && gameObjects.Contains((GameObject)obj);
		public void Add(GameObject obj) => AddRange(new List<object> { obj });
		public void Delete(GameObject obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T>(List<T> objs) {
			foreach (object obj in objs) {
				gameObjects.Add((GameObject)obj);
				DependencyManager.Connect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
				Sort();
			}
			DependencyManager.Flag(this);
		}
		public void RemoveRange<T>(List<T> objs) {
			foreach (object obj in objs) {
				gameObjects.Remove((GameObject)obj);
				DependencyManager.Disconnect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
			}
			DependencyManager.Flag(this);
		}


	}

}