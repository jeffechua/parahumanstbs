using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using BrocktonBay;
using BrocktonBay.TUI;

namespace Parahumans.Editor {

	public static class MasterConsole {

		public const String savefolder = "/Users/jefferson/Desktop/Parahumans_Save";
		public const String welcomeText = "You are accessing the master interface.\n";

		public static void Main (string[] args) {

			Directory.CreateDirectory(savefolder);
			Directory.CreateDirectory(savefolder + "/Parahumans");

			Access();

		}

		public static void Access () {

			Console.Clear();
			Console.WriteLine(welcomeText);
			String input;
			String[] keys;

			while (true) {

				Console.Write("> ");
				input = Console.ReadLine();
				keys = input.Split(' ');

				switch (keys[0].ToLower()) {

					case "import":
						if (keys.Length >= 3) {
							Type type = LanguageTools.ParseType(keys[1]);
							if (type == null) {
								Console.WriteLine("The type \"" + keys[1] + "\" does not exist.\n");
								break;
							}
							String path = savefolder + "/" + type + "/" + LanguageTools.GetWordsStarting(keys, 2) + ".json";
							if (!File.Exists(path)) {
								Console.WriteLine("No " + type + " file named \"" + LanguageTools.GetWordsStarting(keys, 2) + "\" found.\n");
								break;
							}
							GameObject imported = JsonConvert.DeserializeObject<GameObject[]>(File.ReadAllText(path))[0];

							if (!ConsoleTools.ConfirmOverwriteIfConflict(imported)) break;

							if (imported is IQueriable) ((IQueriable)imported).Query();

							if (ConsoleTools.Confirms("\nThe above " + type + " will be imported. Confirm?", true)) {
								City.Add(imported);
								Console.WriteLine(imported.name + " has been imported.\n");
							} else {
								Console.WriteLine("Operation cancelled\n");
							}

							City.Sort();
							break;

						}

						if (keys.Length == 2) {
							if (keys[1] == "all") {
								String[] files;

								files = Directory.GetFiles(savefolder + "/Parahumans");
								for (int i = 0; i < files.Length; i++) {
									City.Add(JsonConvert.DeserializeObject<Parahuman[]>(File.ReadAllText(files[i]))[0]);
								}

								files = Directory.GetFiles(savefolder + "/Teams");
								for (int i = 0; i < files.Length; i++) {
									Team imported = JsonConvert.DeserializeObject<Team[]>(File.ReadAllText(files[i]))[0];
									for (int j = 0; j < imported.roster.Count; j++) {
										if (City.Get<Parahuman>(imported.roster[j]) == null) {
											imported.roster.RemoveAt(j);
											j--;
										}
									}
									City.Add(imported);
								}

								Console.WriteLine("Imported all\n");
								City.Sort();
								break;
							}
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "export":
						if (keys.Length >= 3) {
							GameObject obj = City.Get(LanguageTools.ParseType(keys[1]), LanguageTools.GetWordsStarting(keys, 2));
							if (obj == null) {
								ConsoleTools.PrintNotFound<GameObject>(LanguageTools.GetWordsStarting(keys, 2));
								break;
							}
							String path = savefolder + "/" + obj.GetType() + "/" + obj.name + ".json";
							if (File.Exists(path)) {
								if (!ConsoleTools.Confirms("A " + obj.GetType() + " file called \"" + obj.name + "\" already exists. Overwrite?", true)) {
									Console.WriteLine("Operation cancelled\n");
									break;
								}
							}
							File.WriteAllText(path, JsonConvert.SerializeObject(new GameObject[] { obj }));
							Console.WriteLine(obj.name + " has been exported to " + path + ".\n");

							break;
						}

						ConsoleTools.PrintInvalidSyntax();
						break;

					case "create":
						if (keys.Length == 2) {
							Type type = LanguageTools.ParseType(keys[1]);
							if (type == null || type.IsAssignableFrom(typeof(GameObject))) {
								ConsoleTools.PrintTypeNotFound(keys[1]);
								break;
							}
							GameObject newObj = (GameObject)type.GetConstructor(new Type[] { }).Invoke(new object[] { });
							List<FieldInfo> fields = new List<FieldInfo>(newObj.GetType().GetFields());
							fields.Sort((x, y) => ((OrderAttribute)x.GetCustomAttribute(typeof(OrderAttribute))).order.CompareTo(
								((OrderAttribute)y.GetCustomAttribute(typeof(OrderAttribute))).order));
							for (int i = 0; i < fields.Count; i++) {
								ConsoleTools.RequestFieldAndSet(fields[i], newObj);
							}
							if (!ConsoleTools.ConfirmOverwriteIfConflict(newObj)) {
								break;
							}
							ConsoleTools.QueryObject(newObj);
							if (!ConsoleTools.Confirms("The above team will be created. Confirm?", true)) {
								Console.WriteLine("Operation cancelled\n");
								break;
							}
							City.Add(newObj);
							Console.WriteLine(newObj.name + " has been created. \n");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "query":
						if (keys.Length >= 3) {
							GameObject obj = City.Get(LanguageTools.ParseType(keys[1]), LanguageTools.GetWordsStarting(keys, 2));
							if (obj == null) {
								ConsoleTools.PrintNotFound<GameObject>(LanguageTools.GetWordsStarting(keys, 2));
								break;
							}
							List<FieldInfo> fields = new List<FieldInfo>(obj.GetType().GetFields());
							fields.Sort((x, y) => ((OrderAttribute)x.GetCustomAttribute(typeof(OrderAttribute))).order.CompareTo(
								((OrderAttribute)y.GetCustomAttribute(typeof(OrderAttribute))).order));
							for (int i = 0; i < fields.Count; i++) {
								ConsoleTools.QueryField(fields[i], obj);
							}
							break;
						}
						if (keys.Length == 2) {
							if (keys[1] == "teams") {
								foreach (Team team in City.GetAll<Team>()) team.Query();
								break;
							}
							if (keys[1] == "parahumans") {
								foreach (Parahuman parahuman in City.GetAll<Parahuman>()) parahuman.Query();
								break;
							}
							if (keys[1] == "external") {
								String[] files;

								files = Directory.GetFiles(savefolder + "/Parahumans");
								Console.WriteLine("\n Parahumans:");
								for (int i = 0; i < files.Length; i++) {
									Console.WriteLine("   " + files[i]);
								}

								files = Directory.GetFiles(savefolder + "/Teams");
								Console.WriteLine("\n Teams:");
								for (int i = 0; i < files.Length; i++) {
									Console.WriteLine("   " + files[i]);
								}
								Console.WriteLine("");
								break;
							}
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "delete":
						if (keys.Length == 2) {
							GameObject obj = City.Get(LanguageTools.ParseType(keys[1]), LanguageTools.GetWordsStarting(keys, 2));
							if (obj == null) {
								ConsoleTools.PrintNotFound<GameObject>(LanguageTools.GetWordsStarting(keys, 2));
								break;
							}
							City.Delete(obj);
							City.Sort();
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "edit":
						if (keys.Length >= 2) {
							Parahuman parahuman = City.Get<Parahuman>(LanguageTools.GetWordsStarting(keys, 1));
							if (parahuman == null) {
								ConsoleTools.PrintNotFound<Parahuman>(LanguageTools.GetWordsStarting(keys, 1));
								break;
							}
							ParahumanConsole.Access(parahuman);
							Console.WriteLine(welcomeText);
							City.Sort();
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "move":
						if (keys.Length >= 2) {

							Type type = LanguageTools.ParseType(keys[1]);
							if (type == null) {
								ConsoleTools.PrintTypeNotFound(keys[1]);
								break;
							}
							GameObject obj = City.Get(type, LanguageTools.GetWordsStarting(keys, 2));
							if (obj == null) {
								ConsoleTools.PrintNotFound(LanguageTools.GetWordsStarting(keys, 2));
								break;
							}

							ContainerGameObject container1 = City.GetParentOf(obj);
							if (container1 != null) container1.Remove(obj);

							GameObject container2 = ConsoleTools.RequestObject("Enter destination: ");
							if (container2 == null) {
								break;
							}
							if (!(container2 is ContainerGameObject)) {
								Console.WriteLine(container2.name + " cannot contain objects.\n");
								break;
							}
							if (!((ContainerGameObject)container2).Add(obj)) {
								Console.WriteLine(container2.name + " cannot hold objects of type " + obj.GetType() + ".\n");
								break;
							}

							Console.WriteLine(obj.name + " has been moved" + ((container1 == null) ? "" : (" from " + ((GameObject)container1).name)) + " to " + container2.name + ".\n");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "set":
						if (keys.Length >= 4) {
							GameObject obj = City.Get(LanguageTools.ParseType(keys[2]), LanguageTools.GetWordsStarting(keys, 3));
							if (obj == null) {
								ConsoleTools.PrintNotFound<GameObject>(LanguageTools.GetWordsStarting(keys, 3));
								break;
							}
							FieldInfo field = obj.GetType().GetField(keys[1]);
							if (field == null) {
								ConsoleTools.PrintNotFound<FieldInfo>(keys[1]);
								break;
							}
							if (!ConsoleTools.RequestFieldAndSet(field, obj)) {
								Console.WriteLine("This field type is not supported");
							}
							Console.WriteLine("");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "quit":
						Console.WriteLine("");
						return;

					default:
						ConsoleTools.PrintInvalidCommand();
						break;

				}

			}

		}

	}

}
