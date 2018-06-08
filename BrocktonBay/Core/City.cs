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
		public List<IDependable> dependents { get; set; } = new List<IDependable>();
		public List<IDependable> dependencies { get; set; } = new List<IDependable>();
		public void Reload () { }

		public List<GameObject> gameObjects = new List<GameObject>();

		public bool loadFailure;

		public City () { }

		public City (string path, bool setCurrentCity = false) {

			saveFolder = path;
			if (setCurrentCity) MainClass.currentCity = this;

			try {

				List<string> parahumanAddresses = new List<string>(Directory.GetFiles(path + "/Parahumans"));
				AddRange(parahumanAddresses.ConvertAll(
					(file) => new Parahuman(JsonConvert.DeserializeObject<ParahumanData>(File.ReadAllText(file)))));

				List<string> teamAddresses = new List<string>(Directory.GetFiles(path + "/Teams"));
				AddRange(teamAddresses.ConvertAll(
					(file) => new Team(JsonConvert.DeserializeObject<TeamData>(File.ReadAllText(file)))));

				List<string> factionAddresses = new List<string>(Directory.GetFiles(path + "/Factions"));
				AddRange(factionAddresses.ConvertAll(
					(file) => new Faction(JsonConvert.DeserializeObject<FactionData>(File.ReadAllText(file)))));

				Sort();
				loadFailure = false;

			} catch (Exception e) {
				MessageDialog errorMessage =
					new MessageDialog(MainClass.mainWindow,
									  DialogFlags.DestroyWithParent,
									  MessageType.Error,
									  ButtonsType.Close,
					                  "Error loading save from \"" + path + "\".");
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
				errorMessage.Run();
				errorMessage.Destroy();
				loadFailure = true;
			}

		}

		public T Get<T> (string name) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.name.ToLower() == name.ToLower());
		public T Get<T> (int ID) where T : GameObject => (T)gameObjects.Find(obj => obj is T && obj.ID == ID);

		//IContainer members
		public void Sort () => gameObjects.Sort((GameObject gameObject1, GameObject gameObject2) => gameObject1.ID.CompareTo(gameObject2.ID));
		public bool Accepts (object obj) => obj is GameObject;
		public bool Contains (object obj) => obj is GameObject && gameObjects.Contains((GameObject)obj);
		public void Add (GameObject obj) => AddRange(new List<object> { obj });
		public void Delete (GameObject obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				gameObjects.Add((GameObject)obj);
				DependencyManager.Connect((IDependable)obj, this);
				DependencyManager.Flag((IDependable)obj);
				Sort();
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