using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Gtk;

namespace Parahumans.Core {
	
	public static class IO {

		public static string currentSaveFolder;

		public static void SelectOpen () {
			FileChooserDialog openDialog = new FileChooserDialog("Open save", MainClass.mainWindow, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
			openDialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					string path = new Uri(openDialog.Uri).AbsolutePath;
					City city = new City(path, true);
					if (!city.loadFailure) {
						MainClass.Load(city);
						DependencyManager.TriggerAllFlags();
					}
				}
			};
			openDialog.Run();
			openDialog.Destroy();
		}

		public static void SelectSave (City city) {
			FileChooserDialog saveDialog = new FileChooserDialog("Save as", MainClass.mainWindow, FileChooserAction.CreateFolder, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
			saveDialog.Response += delegate (object obj, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					string path = new Uri(saveDialog.Uri).AbsolutePath;
					SaveAs(city, path);
				}
			};
			saveDialog.Run();
			saveDialog.Destroy();
		}

		public static void Save (City city) {
			
			Empty(city.saveFolder + "/Parahumans");
			Empty(city.saveFolder + "/Teams");
			Empty(city.saveFolder + "/Factions");

			List<GameObject> parahumans = city.gameObjects.FindAll((GameObject obj) => obj is Parahuman);
			List<ParahumanData> parahumanData = parahumans.ConvertAll((parahuman) => new ParahumanData((Parahuman)parahuman));

			List<GameObject> teams = city.gameObjects.FindAll((GameObject obj) => obj is Team);
			List<TeamData> teamData = teams.ConvertAll((team) => new TeamData((Team)team));

			List<GameObject> factions = city.gameObjects.FindAll((GameObject obj) => obj is Faction);
			List<FactionData> factionData = factions.ConvertAll((faction) => new FactionData((Faction)faction));

			foreach (ParahumanData parahumanDatum in parahumanData) {
				File.WriteAllText(city.saveFolder + "/Parahumans/" + parahumanDatum.name + parahumanDatum.ID + ".json",
								  JsonConvert.SerializeObject(parahumanDatum));
			}
			foreach (TeamData teamDatum in teamData) {
				File.WriteAllText(city.saveFolder + "/Teams/" + teamDatum.name + teamDatum.ID + ".json",
								  JsonConvert.SerializeObject(teamDatum));
			}
			foreach (FactionData factionDatum in factionData) {
				File.WriteAllText(city.saveFolder + "/Factions/" + factionDatum.name + factionDatum.ID + ".json",
								  JsonConvert.SerializeObject(factionDatum));
			}

		}

		public static void SaveAs (City city, string destination) {

			if (Directory.Exists(destination)) {
				Empty(destination);
			} else {
				Directory.CreateDirectory(destination);
			}
			Directory.CreateDirectory(destination + "/Parahumans");
			Directory.CreateDirectory(destination + "/Teams");
			Directory.CreateDirectory(destination + "/Factions");
			Directory.CreateDirectory(destination + "/Map");

			List<GameObject> parahumans = city.gameObjects.FindAll((GameObject obj) => obj is Parahuman);
			List<ParahumanData> parahumanData = parahumans.ConvertAll((parahuman) => new ParahumanData((Parahuman)parahuman));

			List<GameObject> teams = city.gameObjects.FindAll((GameObject obj) => obj is Team);
			List<TeamData> teamData = teams.ConvertAll((team) => new TeamData((Team)team));

			List<GameObject> factions = city.gameObjects.FindAll((GameObject obj) => obj is Faction);
			List<FactionData> factionData = factions.ConvertAll((faction) => new FactionData((Faction)faction));

			foreach(ParahumanData parahumanDatum in parahumanData) {
				File.WriteAllText(destination + "/Parahumans/" + parahumanDatum.name + parahumanDatum.ID + ".json",
				                  JsonConvert.SerializeObject(parahumanDatum));
			}
			foreach (TeamData teamDatum in teamData) {
				File.WriteAllText(destination + "/Teams/" + teamDatum.name + teamDatum.ID + ".json",
				                  JsonConvert.SerializeObject(teamDatum));
			}
			foreach (FactionData factionDatum in factionData) {
				File.WriteAllText(destination + "/Factions/" + factionDatum.name + factionDatum.ID + ".json",
				                  JsonConvert.SerializeObject(factionDatum));
			}

			File.Copy(city.saveFolder + "/Map/map.png", destination + "/Map/map.png");
			File.Copy(city.saveFolder + "/Map/dimensions.txt", destination + "/Map/dimensions.txt");

			city.saveFolder = destination;

		}

		public static void Empty (string path) {
			foreach(string file in Directory.GetFiles(path)){
				File.Delete(file);
			}
			foreach(string directory in Directory.GetDirectories(path)){
				Empty(directory);
				Directory.Delete(directory);
			}
		}

	}
}
