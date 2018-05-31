using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Parahumans.Core;
using Parahumans.Core.GUI;
using Gtk;


namespace BrocktonBay {

	class MainClass {

		public const String savefolder = "/Users/Jefferson/Desktop/Parahumans_Save";

		public static void Main(string[] args) {

			//Loads stuff from save
			List<string> parahumanAddresses = new List<string>(Directory.GetFiles(savefolder + "/Parahumans"));
			City.city.AddRange(parahumanAddresses.ConvertAll(
				(file) => new Parahuman(JsonConvert.DeserializeObject<ParahumanData[]>(File.ReadAllText(file))[0])));

			List<string> teamAddresses = new List<string>(Directory.GetFiles(savefolder + "/Teams"));
			City.city.AddRange(teamAddresses.ConvertAll(
				(file) => new Team(JsonConvert.DeserializeObject<TeamData[]>(File.ReadAllText(file))[0])));

			City.city.Sort();

			DependencyManager.TriggerAllFlags();

			//Inits application
			Application.Init();

			//Creates main window
			MainWindow win = new MainWindow() { DefaultWidth = 1200, DefaultHeight = 700 };
			win.Title = "Brockton Bay";

			//Sets up main layout and inspector
			VBox masterBox = new VBox();
			MenuBar mainMenus = new MenuBar();
			HBox mainBox = new HBox();
			Notebook main = new Notebook();
			Inspector.main = new Inspector { BorderWidth = 10 };
			mainBox.PackStart(main, true, true, 0);
			mainBox.PackStart(Inspector.main, false, false, 0);
			masterBox.PackStart(mainMenus, false, false, 0);
			masterBox.PackStart(mainBox, true, true, 0);
			win.Add(masterBox);


			//Menu bar
			MenuItem fileButton = new MenuItem("File");
			MenuItem editButton = new MenuItem("Edit");
			MenuItem viewButton = new MenuItem("View");
			MenuItem toolsButton = new MenuItem("Tools");
			MenuItem windowButton = new MenuItem("Window");
			MenuItem helpButton = new MenuItem("Help");
			mainMenus.Append(fileButton);
			mainMenus.Append(editButton);
			mainMenus.Append(viewButton);
			mainMenus.Append(toolsButton);
			mainMenus.Append(windowButton);
			mainMenus.Append(helpButton);

			//File menu
			Menu fileMenu = new Menu();
			MenuItem newGamebutton = new MenuItem("New Game") { Sensitive = false }; //Implement
			MenuItem openButton = new MenuItem("Open") { Sensitive = false };        //Implement
			MenuItem saveButton = new MenuItem("Save") { Sensitive = false };        //Implement
			MenuItem saveAsButton = new MenuItem("Save As") { Sensitive = false };   //Implement
			MenuItem closeButton = new MenuItem("Close") { Sensitive = false };      //Impelment
			fileButton.Submenu = fileMenu;
			fileMenu.Append(newGamebutton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(openButton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(saveButton);
			fileMenu.Append(saveAsButton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(closeButton);

			//Edit menu
			Menu editMenu = new Menu();
			MenuItem undoButton = new MenuItem("Undo") { Sensitive = false };       //Implement
			MenuItem redoButton = new MenuItem("Redo") { Sensitive = false };       //Implement
			MenuItem createButton = new MenuItem("Create");
			Menu createMenu = new Menu();
			MenuItem createParahumanButton = new MenuItem("Create Parahuman");
			createParahumanButton.Activated += delegate {
				City.city.Add((Parahuman)typeof(Parahuman).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createTeamButton = new MenuItem("Create Team");
			createTeamButton.Activated += delegate {
				City.city.Add((Team)typeof(Team).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createFactionButton = new MenuItem("Create Faction");
			createFactionButton.Activated += delegate {
				City.city.Add((Faction)typeof(Faction).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem importButton = new MenuItem("Import...") { Sensitive = false };//Implement
			MenuItem editModeButton = new CheckMenuItem("Edit Mode") { Sensitive = false };
			editButton.Submenu = editMenu;
			editMenu.Append(undoButton);
			editMenu.Append(redoButton);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(createButton);
			createButton.Submenu = createMenu;
			createMenu.Append(createParahumanButton);
			createMenu.Append(createTeamButton);
			createMenu.Append(createFactionButton);
			editMenu.Append(importButton);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(editModeButton);

			//View menu
			Menu viewMenu = new Menu();
			CheckMenuItem omniscientToggle = new CheckMenuItem("Omniscient") { Active = true, Sensitive = false }; //Implement
			viewButton.Submenu = viewMenu;
			viewMenu.Append(omniscientToggle);

			//Tools menu
			Menu toolsMenu = new Menu();
			CheckMenuItem cheatsButton = new CheckMenuItem("Cheats"); //Implement
			toolsButton.Submenu = toolsMenu;
			toolsMenu.Append(cheatsButton);

			//Window menu
			Menu windowMenu = new Menu();
			MenuItem newSearchWindowButton = new MenuItem("New Search Window") { Sensitive = false }; //Implement
			windowButton.Submenu = windowMenu;
			windowMenu.Append(newSearchWindowButton);

			//Help menu


			//Map tab
			VBox map = new VBox(false, 0);
			Image mapImg = new Image();
			Label mapLabel = new Label("The map of Brockton Bay.");
			Label mapTabLabel = new Label("Map");
			map.PackStart(mapImg, true, true, 0);
			map.PackEnd(mapLabel, false, false, 0);
			main.AppendPage(map, mapTabLabel);

			//Search tab
			Search search = new Search(null, (obj) => Inspector.main.Inspect(obj));
			Label searchLabel = new Label("Search");
			main.AppendPage(search, searchLabel);

			//Battle sandbox tab
			Deployment actors = new Deployment();
			Deployment reactors = new Deployment();
			DeploymentPlanner actorsPlanner = new DeploymentPlanner(actors);
			DeploymentPlanner reactorsPlanner = new DeploymentPlanner(reactors);
			Label APLabel = new Label("Plan actors");
			Label RPLabel = new Label("Plan reactors");
			main.AppendPage(actorsPlanner, APLabel);
			main.AppendPage(reactorsPlanner, RPLabel);
			Battle battle = new Battle(actors, reactors);

			//Wraps up
			win.ShowAll();
			Application.Run();

		}
	}
}
