using System;
using System.Collections.Generic;
using BrocktonBay;
using BrocktonBay.TUI;

namespace Parahumans.Editor {

	public static class ParahumanConsole {

		public static void Access (Parahuman parahuman) {

			Console.Clear();
			String welcomeText = "You are accessing the editor for " + parahuman.name + ".\n";
			Console.WriteLine(welcomeText);
			String input;
			String[] keys;

			while (true) {

				Console.Write("> ");
				input = Console.ReadLine();
				keys = input.Split(' ');

				switch (keys[0].ToLower()) {

					case "printall":
						parahuman.Query();
						break;

					case "rename":
						if (keys.Length >= 2) {
							if (keys.Length >= 3 && keys[1] == "civilian") {
								parahuman.civilian_name = LanguageTools.GetWordsStarting(keys, 2);
								Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " civilian name is now " + parahuman.civilian_name + ".\n");
								break;
							} else {
								parahuman.name = LanguageTools.GetWordsStarting(keys, 1);
								Console.WriteLine(LanguageTools.Possessive(parahuman.civilian_name) + " cape name is now " + parahuman.name + ".\n");
								break;
							}
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "health":
						if (keys.Length == 2) {
							if (Enum.TryParse(keys[1], true, out Health health)) {
								parahuman.health = health;
								Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " state of health is now " + parahuman.health.ToString().ToLower() + ".\n");
								break;
							}
							if (int.TryParse(keys[1], out int healthint)) {
								parahuman.health = (Health)healthint;
								Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " state of health is now " + parahuman.health.ToString().ToLower() + ".\n");
								break;
							}
							Console.WriteLine("\"" + keys[1] + "\" is not a valid health setting.\nOptions: Deceased, Down, Injured, Healthy OR 0, 1, 2, 3.\n");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "alignment":
						if (keys.Length == 1) {
							parahuman.alignment = ConsoleTools.RequestAlignment("> Enter new alignment: ");
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "pid":
						if (keys.Length == 1) {
							parahuman.ID = ConsoleTools.RequestInt("> Enter new PID: ");
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "threshold":

						if (keys.Length == 2) {
							if (Enum.TryParse(keys[1], out Threat threshold)) {
								parahuman.reveal_threshold = threshold;
								Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " escalation threshold has been set to " + threshold + ".\n");
								break;
							} else {
								Console.WriteLine("\"" + keys[1] + "\" is not a valid escalation threshold. It must be an integer from 0-4 (inclusive).\n");
								break;
							}
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "set":
						if (keys.Length % 2 == 1) {
							Rating parentRating = null;
							bool writesub = false;
							if (keys[1] == "subrating") {
								if (!Enum.TryParse(keys[2], true, out Classification parentClssf)) {
									Console.WriteLine("\"" + keys[2] + "\" is not a valid rating wrapper.\nOptions: Master, Breaker, Tinker.\n");
									break;
								}
								if (parentClssf != Classification.Master && parentClssf != Classification.Breaker && parentClssf != Classification.Tinker) {
									Console.WriteLine("\"" + keys[2] + "\" is not a valid rating wrapper.\nOptions: Master, Breaker, Tinker.\n");
									break;
								}
								parentRating = parahuman.ratings.Find(rat => rat.clssf == parentClssf);
								if (parentRating == null) {
									Console.WriteLine(parahuman.name + "does not have a " + keys[2] + "rating.\n");
									break;
								}
								writesub = true;
							}
							for (int i = (writesub ? 3 : 1); i < keys.Length; i += 2) {
								if (!Enum.TryParse(keys[i], true, out Classification clssf)) {
									PrintInvalidWrapper(keys[i]);
									break;
								}
								if (!int.TryParse(keys[i + 1], out int num)) {
									Console.WriteLine("\"" + keys[i + 1] + "\" is not an integer.");
									break;
								}
								if (num < 0 || num > 12) {
									Console.WriteLine("Requested " + clssf.ToString() + " rating number out of range. Must be within 0-12 (inclusive).");
									break;
								}
								if (writesub) { //Write to subrating of parentRating
									Rating subrating = parentRating.subratings.Find(rat => rat.clssf == clssf);
									if (subrating == null) {
										subrating = new Rating(clssf, num);
										parentRating.subratings.Add(new Rating(clssf, num));
										Console.WriteLine(parahuman.name + " has gained a " + clssf.ToString() + " subrating of " + num.ToString() + " under " + parentRating.clssf.ToString());
									} else {
										subrating.num = num;
										Console.WriteLine(parahuman.name + "'s " + clssf.ToString() + " subrating (under " + parentRating.clssf.ToString() + ") has been changed to " + num.ToString());
									}
								} else { //Write to rating
									Rating rating = parahuman.ratings.Find(rat => rat.clssf == clssf);
									if (rating == null) {
										rating = new Rating(clssf, num);
										parahuman.ratings.Add(new Rating(clssf, num));
										Console.WriteLine(parahuman.name + " has gained a " + clssf.ToString() + " rating of " + num.ToString());
									} else {
										rating.num = num;
										Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " " + clssf.ToString() + " rating has been changed to " + num.ToString());
									}
								}
							}
							Console.WriteLine("");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "trueform":
						if (keys.Length >= 2) {
							String name = LanguageTools.GetWordsStarting(keys, 1);
							Parahuman para = City.Get<Parahuman>(name);
							if (para == null) {
								ConsoleTools.PrintNotFound<Parahuman>(name);
								break;
							}
							Console.WriteLine("");
							para.Query();
							Console.WriteLine("");
							if (ConsoleTools.Confirms(LanguageTools.Possessive(parahuman.name) + " true form will be set to the above. Confirm?", true)) {
								parahuman.true_form = para;
								Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " true form has been set.\n");
								City.Delete(para);
								para.ID = parahuman.ID;
								break;
							} else {
								Console.WriteLine("Operation cancelled");
								break;
							}
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "edit":
						if (keys.Length == 2 && keys[1] == "trueform") {
							Access(parahuman.true_form);
							Console.WriteLine(welcomeText);
						}
						break;

					case "delete":
						if (keys.Length == 2) {

							if (keys[1] == "trueform") {
								parahuman.true_form = null;
								Console.WriteLine("The true form of " + parahuman.name + " has been deleted.\n");
								break;
							}

							int index = -1;
							if (!Enum.TryParse(keys[1], true, out Classification clssf)) {
								PrintInvalidPower(keys[1]);
								break;
							}
							index = parahuman.ratings.FindIndex(rating => rating.clssf == clssf);
							if (index == -1) {
								Console.WriteLine(parahuman.name + " does not have a " + clssf.ToString() + " rating.\n");
								break;
							}
							parahuman.ratings.RemoveAt(index);
							Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " " + clssf.ToString() + " rating has been removed.\n");
							break;
						}
						if (keys.Length == 3) {
							Rating parentRating;
							int index = -1;
							if (!Enum.TryParse(keys[1], true, out Classification parentClssf)) {
								PrintInvalidWrapper(keys[1]);
								break;
							}
							if (parentClssf != Classification.Master && parentClssf != Classification.Breaker && parentClssf != Classification.Tinker) {
								PrintInvalidWrapper(keys[1]);
								break;
							}
							parentRating = parahuman.ratings.Find(rat => rat.clssf == parentClssf);
							if (parentRating == null) {
								Console.WriteLine(parahuman.name + " does not have a " + keys[1] + " rating.\n");
								break;
							}

							if (!Enum.TryParse(keys[2], true, out Classification subClssf)) {
								PrintInvalidPower(keys[2]);
								break;
							}
							index = parentRating.subratings.FindIndex(rat => rat.clssf == subClssf);
							if (index == -1) {
								Console.WriteLine(parahuman.name + " does not have a " + keys[2] + " subrating under " + keys[1] + ".\n");
								break;
							}
							parentRating.subratings.RemoveAt(index);
							Console.WriteLine(LanguageTools.Possessive(parahuman.name) + " " + subClssf.ToString() + " subrating under " + parentClssf.ToString() + " has been removed.\n");
							break;
						}
						ConsoleTools.PrintInvalidSyntax();
						break;

					case "exit":
						Console.Clear();
						return;

					default:
						ConsoleTools.PrintInvalidCommand();
						break;

				}
			}
		}

		public static void PrintInvalidPower (String str) {
			Console.WriteLine("\"" + str + "\" is not a valid power classification.\n" +
							  "Options: Brute, Blaster, Shaker, Striker, Mover, Thinker, Tinker, Stranger, Master, Breaker, Trump.\n");
		}

		public static void PrintInvalidWrapper (String str) {
			Console.WriteLine("\"" + str + "\" is not a valid rating wrapper.\n" +
							  "Options: Master, Breaker, Tinker.\n");
		}

	}

}
