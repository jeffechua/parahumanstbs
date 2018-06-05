using System;
using System.Collections.Generic;
using Gtk;


namespace Parahumans.Core {

	class MainClass {

		public const String savefolder = "/Users/Jefferson/Desktop/Parahumans_Save";
		public static City currentCity;
		public static CityInterface cityInterface;
		public static MainWindow mainWindow;
		public static VBox mainBox;

		static MenuBar menus;
		static Menu fileMenu;
		static Menu editMenu;
		static Menu viewMenu;
		static Menu toolsMenu;
		static Menu windowMenu;
		static Menu helpMenu;

		static MenuItem fileButton;
		static MenuItem editButton;
		static MenuItem viewButton;
		static MenuItem toolsButton;
		static MenuItem windowButton;
		static MenuItem helpButton;

		public static void Main (string[] args) {

			//Inits application
			Application.Init();

			mainWindow = new MainWindow {
				DefaultWidth = 1200,
				DefaultHeight = 700,
				Title = "Brockton Bay"
			};

			mainBox = new VBox();
			MenuBar mainMenus = new MenuBar();
			mainBox.PackStart(mainMenus, false, false, 0);
			mainWindow.Add(mainBox);

			//Menu bar
			fileButton = new MenuItem("File");
			editButton = new MenuItem("Edit") { Sensitive = false };    //These menus serve no purpose with no city loaded.
			viewButton = new MenuItem("View") { Sensitive = false };    //
			toolsButton = new MenuItem("Tools") { Sensitive = false };  //
			windowButton = new MenuItem("Window") { Sensitive = false };//
			helpButton = new MenuItem("Help");
			mainMenus.Append(fileButton);
			mainMenus.Append(editButton);
			mainMenus.Append(viewButton);
			mainMenus.Append(toolsButton);
			mainMenus.Append(windowButton);
			mainMenus.Append(helpButton);

			//File menu
			fileMenu = new Menu();
			MenuItem newGamebutton = new MenuItem("New Game") { Sensitive = false }; //Implement
			MenuItem openButton = new MenuItem("Open");
			openButton.Activated += (o, a) => IO.SelectOpen();
			MenuItem saveButton = new MenuItem("Save");
			saveButton.Activated += (o, a) => IO.Save(currentCity);
			MenuItem saveAsButton = new MenuItem("Save As");
			saveAsButton.Activated += (o, a) => IO.SelectSave(currentCity);
			MenuItem closeButton = new MenuItem("Close");
			closeButton.Activated += (o, a) => Unload();
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
			editMenu = new Menu();
			MenuItem undoButton = new MenuItem("Undo") { Sensitive = false };       //Implement
			MenuItem redoButton = new MenuItem("Redo") { Sensitive = false };       //Implement
			MenuItem createButton = new MenuItem("Create");
			Menu createMenu = new Menu();
			MenuItem createParahumanButton = new MenuItem("Create Parahuman");
			createParahumanButton.Activated += delegate {
				currentCity.Add((Parahuman)typeof(Parahuman).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createTeamButton = new MenuItem("Create Team");
			createTeamButton.Activated += delegate {
				currentCity.Add((Team)typeof(Team).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createFactionButton = new MenuItem("Create Faction");
			createFactionButton.Activated += delegate {
				currentCity.Add((Faction)typeof(Faction).GetConstructor(new Type[] { }).Invoke(new object[] { }));
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
			viewMenu = new Menu();
			CheckMenuItem omniscientToggle = new CheckMenuItem("Omniscient") { Active = true, Sensitive = false }; //Implement
			viewButton.Submenu = viewMenu;
			viewMenu.Append(omniscientToggle);

			//Tools menu
			toolsMenu = new Menu();
			CheckMenuItem cheatsButton = new CheckMenuItem("Cheats"); //Implement
			toolsButton.Submenu = toolsMenu;
			toolsMenu.Append(cheatsButton);

			//Window menu
			windowMenu = new Menu();
			MenuItem newSearchWindowButton = new MenuItem("New Search Window") { Sensitive = false }; //Implement
			windowButton.Submenu = windowMenu;
			windowMenu.Append(newSearchWindowButton);

			mainWindow.ShowAll();

			Application.Run();

		}

		public static void Load (City city) {
			Unload();
			currentCity = city;
			cityInterface = new CityInterface(city);
			mainBox.PackStart(cityInterface, true, true, 0);
			editButton.Sensitive = true;
			viewButton.Sensitive = true;
			toolsButton.Sensitive = true;
			windowButton.Sensitive = true;
			mainWindow.ShowAll();
		}

		public static void Unload () {
			currentCity = null;
			if (mainBox.Children.Length == 2) {
				mainBox.Children[1].Destroy();
			}
			editButton.Sensitive = false;
			viewButton.Sensitive = false;
			toolsButton.Sensitive = false;
			windowButton.Sensitive = false;
			mainWindow.ShowAll();
		}

	}

}
