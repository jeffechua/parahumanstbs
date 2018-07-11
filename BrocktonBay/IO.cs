using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Gtk;

namespace Parahumans.Core {

	public static class IO {

		public static string currentSaveFolder;

		public static void AskIfSaveBefore (System.Action action) {
			if (MainClass.city == null) {
				action();
				return;
			}
			MessageDialog dialog = new MessageDialog(MainClass.mainWindow, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save current game?");
			dialog.Response += delegate (object obj, ResponseArgs response) {
				if (response.ResponseId == ResponseType.Yes)
					IO.SelectSave();
				action();
			};
			dialog.Run();
			dialog.Destroy();
		}

		public static void SelectOpen () {
			FileChooserDialog openDialog = new FileChooserDialog("Open save", MainClass.mainWindow, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
			openDialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					string path = new Uri(openDialog.Uri).AbsolutePath;
					Open(path);
				}
			};
			openDialog.Run();
			openDialog.Destroy();
		}

		public static void SelectSave () {
			if (MainClass.city.saveFolder == "") {
				SelectSaveAs();
			} else {
				SaveAs(MainClass.city, MainClass.city.saveFolder);
			}
		}

		public static void SelectSaveAs () {
			FileChooserDialog saveDialog = new FileChooserDialog("Save as", MainClass.mainWindow, FileChooserAction.CreateFolder, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
			saveDialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					string path = new Uri(saveDialog.Uri).AbsolutePath;
					SaveAs(MainClass.city, path);
					Console.WriteLine(path);
				}
			};
			saveDialog.Run();
			saveDialog.Destroy();
		}

		public static void Open (string path) {

			try {

				City city = new City();
				city.saveFolder = path;
				MainClass.city = city;

				Profiler.Log();

				List<string> parahumanAddresses = new List<string>(Directory.GetFiles(path + "/Parahumans"));
				city.AddRange(parahumanAddresses.ConvertAll(
					(file) => new Parahuman(JsonConvert.DeserializeObject<ParahumanData>(File.ReadAllText(file)))));

				Profiler.Log(ref Profiler.parahumanLoadTime);

				List<string> teamAddresses = new List<string>(Directory.GetFiles(path + "/Teams"));
				city.AddRange(teamAddresses.ConvertAll(
					(file) => new Team(JsonConvert.DeserializeObject<TeamData>(File.ReadAllText(file)))));

				Profiler.Log(ref Profiler.teamLoadTime);

				List<string> structureAddresses = new List<string>(Directory.GetFiles(path + "/Structures"));
				city.AddRange(structureAddresses.ConvertAll(
					(file) => new Structure(JsonConvert.DeserializeObject<StructureData>(File.ReadAllText(file)))));


				Profiler.Log(ref Profiler.structureLoadTime);

				List<string> territoryAddresses = new List<string>(Directory.GetFiles(path + "/Territories"));
				city.AddRange(territoryAddresses.ConvertAll(
					(file) => new Territory(JsonConvert.DeserializeObject<TerritoryData>(File.ReadAllText(file)))));


				Profiler.Log(ref Profiler.territoryLoadTime);

				List<string> factionAddresses = new List<string>(Directory.GetFiles(path + "/Factions"));
				city.AddRange(factionAddresses.ConvertAll(
					(file) => new Faction(JsonConvert.DeserializeObject<FactionData>(File.ReadAllText(file)))));

				Profiler.Log(ref Profiler.factionLoadTime);

				city.mapPngSource = File.ReadAllBytes(path + "/Map/map.png");
				city.mapDefaultWidth = int.Parse(File.ReadAllText(path + "/Map/dimensions.txt"));
				city.territorySizeScale = int.Parse(File.ReadAllText(path + "/Map/scale.txt"));

				Profiler.Log(ref Profiler.mapDataLoadTime);

				DependencyManager.TriggerAllFlags();

				Profiler.Log(ref Profiler.updateTime);

				MainClass.Load(city); //Profiler calls inside CityInterface constructor.
				MainClass.playerAgent = (Agent)city.Get<GameObject>(int.Parse(File.ReadAllText(path + "/player.txt")));

				Profiler.Report();

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
				MainClass.city = null;

			}

		}

		public static void SaveAs (City city, string destination) {

			Directory.CreateDirectory(destination);
			Empty(destination);

			Directory.CreateDirectory(destination + "/Parahumans");
			Directory.CreateDirectory(destination + "/Teams");
			Directory.CreateDirectory(destination + "/Factions");
			Directory.CreateDirectory(destination + "/Structures");
			Directory.CreateDirectory(destination + "/Territories");
			Directory.CreateDirectory(destination + "/Map");

			Empty(destination + "/Parahumans");
			Empty(destination + "/Teams");
			Empty(destination + "/Factions");
			Empty(destination + "/Structures");
			Empty(destination + "/Territories");

			List<GameObject> parahumans = city.gameObjects.FindAll((GameObject obj) => obj is Parahuman);
			List<ParahumanData> parahumanData = parahumans.ConvertAll((parahuman) => new ParahumanData((Parahuman)parahuman));
			foreach (ParahumanData data in parahumanData)
				File.WriteAllText(destination + "/Parahumans/" + data.name + data.ID + ".json", JsonConvert.SerializeObject(data));

			List<GameObject> teams = city.gameObjects.FindAll((GameObject obj) => obj is Team);
			List<TeamData> teamData = teams.ConvertAll((team) => new TeamData((Team)team));
			foreach (TeamData data in teamData)
				File.WriteAllText(destination + "/Teams/" + data.name + data.ID + ".json", JsonConvert.SerializeObject(data));

			List<GameObject> factions = city.gameObjects.FindAll((GameObject obj) => obj is Faction);
			List<FactionData> factionData = factions.ConvertAll((faction) => new FactionData((Faction)faction));
			foreach (FactionData data in factionData)
				File.WriteAllText(destination + "/Factions/" + data.name + data.ID + ".json", JsonConvert.SerializeObject(data));

			List<GameObject> structures = city.gameObjects.FindAll((GameObject obj) => obj is Structure);
			List<StructureData> structureData = structures.ConvertAll((structure) => new StructureData((Structure)structure));
			foreach (StructureData data in structureData)
				File.WriteAllText(destination + "/Structures/" + data.name + data.ID + ".json", JsonConvert.SerializeObject(data));

			List<GameObject> territories = city.gameObjects.FindAll((GameObject obj) => obj is Territory);
			List<TerritoryData> territoryData = territories.ConvertAll((territory) => new TerritoryData((Territory)territory));
			foreach (TerritoryData data in territoryData)
				File.WriteAllText(destination + "/Territories/" + data.name + data.ID + ".json", JsonConvert.SerializeObject(data));

			File.WriteAllBytes(destination + "/Map/map.png", city.mapPngSource);
			File.WriteAllText(destination + "/Map/dimensions.txt", "" + city.mapDefaultWidth);
			File.WriteAllText(destination + "/Map/scale.txt", "" + city.territorySizeScale);
			File.WriteAllText(destination + "/player.txt", ((GameObject)MainClass.playerAgent).ID.ToString());

			city.saveFolder = destination;

		}

		public static void Empty (string path) {
			foreach (string file in Directory.GetFiles(path)) {
				File.Delete(file);
			}
			foreach (string directory in Directory.GetDirectories(path)) {
				Empty(directory);
				Directory.Delete(directory);
			}
		}

	}
}
