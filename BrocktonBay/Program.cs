using System;
using System.Collections.Generic;
using Gtk;


namespace Parahumans.Core {

	class MainClass {

		public const String savefolder = "/Users/Jefferson/Desktop/Parahumans_Save";
		public static City city;

		public static MainInterface cityInterface;
		public static MainWindow mainWindow;
		public static VBox mainBox;

		static MenuBar menuBar;
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

		static MenuItem saveButton;
		static MenuItem saveAsButton;
		static MenuItem closeButton;

		public static int textSize;

		public static void Main (string[] args) {

			//Inits application
			Application.Init();

			mainWindow = new MainWindow {
				DefaultWidth = 1200,
				DefaultHeight = 700,
				Title = "Brockton Bay"
			};

			mainBox = new VBox();
			menuBar = new MenuBar();
			mainBox.PackStart(menuBar, false, false, 0);
			mainWindow.Add(mainBox);

			//Menu bar
			fileButton = new MenuItem("File");
			editButton = new MenuItem("Edit") { Sensitive = false };    //These menus serve no purpose with no city loaded.
			viewButton = new MenuItem("View") { Sensitive = false };    //
			toolsButton = new MenuItem("Tools") { Sensitive = false };  //
			windowButton = new MenuItem("Window") { Sensitive = false };//
			helpButton = new MenuItem("Help");
			menuBar.Append(fileButton);
			menuBar.Append(editButton);
			menuBar.Append(viewButton);
			menuBar.Append(toolsButton);
			menuBar.Append(windowButton);
			menuBar.Append(helpButton);

			//File menu
			fileMenu = new Menu();
			MenuItem newGamebutton = new MenuItem("New Game") { Sensitive = false }; //Implement
			MenuItem openButton = new MenuItem("Open");
			openButton.Activated += delegate {
				if (city == null) {
					IO.SelectOpen();
				} else {
					MessageDialog dialog = new MessageDialog(mainWindow, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save current game?");
					dialog.Response += delegate (object obj, ResponseArgs response) {
						if (response.ResponseId == ResponseType.Yes)
							IO.SelectSave(city);
						IO.SelectOpen();
					};
					dialog.Run();
					dialog.Destroy();
				}
			};
			saveButton = new MenuItem("Save") { Sensitive = false };
			saveButton.Activated += (o, a) => IO.SelectSave(city);
			saveAsButton = new MenuItem("Save As") { Sensitive = false };
			saveAsButton.Activated += (o, a) => IO.SelectSaveAs(city);
			closeButton = new MenuItem("Close") { Sensitive = false };
			closeButton.Activated += delegate {
				MessageDialog dialog = new MessageDialog(mainWindow, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save before closing?");
				dialog.Response += delegate (object obj, ResponseArgs response) {
					if (response.ResponseId == ResponseType.Yes)
						IO.SelectSave(city);
					Unload();
				};
				dialog.Run();
				dialog.Destroy();
			};
			MenuItem quitButton = new MenuItem("Quit");
			quitButton.Activated += delegate {
				if (city == null) {
					Application.Quit();
				} else {
					MessageDialog dialog = new MessageDialog(mainWindow, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save before quitting?");
					dialog.Response += delegate (object obj, ResponseArgs response) {
						if (response.ResponseId == ResponseType.Yes)
							IO.SelectSave(city);
						Application.Quit();
					};
					dialog.Run();
					dialog.Destroy();
				}
			};
			fileButton.Submenu = fileMenu;
			fileMenu.Append(newGamebutton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(openButton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(saveButton);
			fileMenu.Append(saveAsButton);
			fileMenu.Append(new SeparatorMenuItem());
			fileMenu.Append(closeButton);
			fileMenu.Append(quitButton);

			//Edit menu
			editMenu = new Menu();
			MenuItem undoButton = new MenuItem("Undo") { Sensitive = false };       //Implement
			MenuItem redoButton = new MenuItem("Redo") { Sensitive = false };       //Implement
			MenuItem createButton = new MenuItem("Create");
			Menu createMenu = new Menu();
			MenuItem createParahumanButton = new MenuItem("Create Parahuman");
			createParahumanButton.Activated += delegate {
				city.Add((Parahuman)typeof(Parahuman).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createTeamButton = new MenuItem("Create Team");
			createTeamButton.Activated += delegate {
				city.Add((Team)typeof(Team).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createFactionButton = new MenuItem("Create Faction");
			createFactionButton.Activated += delegate {
				city.Add((Faction)typeof(Faction).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createStructureButton = new MenuItem("Create Structure");
			createStructureButton.Activated += delegate {
				city.Add((Structure)typeof(Structure).GetConstructor(new Type[] { }).Invoke(new object[] { }));
				DependencyManager.TriggerAllFlags();
			};
			MenuItem createTerritoryButton = new MenuItem("Create Territory");
			createTerritoryButton.Activated += delegate {
				city.Add((Territory)typeof(Territory).GetConstructor(new Type[] { }).Invoke(new object[] { }));
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
			createMenu.Append(createStructureButton);
			createMenu.Append(createTerritoryButton);
			editMenu.Append(importButton);
			editMenu.Append(new SeparatorMenuItem());
			editMenu.Append(editModeButton);

			//View menu
			viewMenu = new Menu();
			CheckMenuItem inspectorEnabledButton = new CheckMenuItem("Inspector Panel") { Active = true };
			inspectorEnabledButton.Toggled += delegate {
				if (inspectorEnabledButton.Active) {
					mainWindow.inspectorEnabled = true;
				} else {
					mainWindow.inspectorEnabled = false;
					mainWindow.inspector.Inspect(null);
				}
			};
			CheckMenuItem omniscientToggle = new CheckMenuItem("Omniscient") { Active = true, Sensitive = false }; //Implement
			viewButton.Submenu = viewMenu;
			viewMenu.Append(inspectorEnabledButton);
			viewMenu.Append(new SeparatorMenuItem());
			viewMenu.Append(omniscientToggle);

			//Tools menu
			toolsMenu = new Menu();
			CheckMenuItem cheatsButton = new CheckMenuItem("Cheats"); //Implement
			toolsButton.Submenu = toolsMenu;
			toolsMenu.Append(cheatsButton);

			//Window menu
			windowMenu = new Menu();
			MenuItem newSearchWindowButton = new MenuItem("New Search Window");
			newSearchWindowButton.Activated += delegate {
				SecondaryWindow newWindow = new SecondaryWindow("Search Utility");
				newWindow.SetMainWidget(new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, newWindow)));
				newWindow.ShowAll();
			};
			MenuItem newMapWindowButton = new MenuItem("New Map Window");
			newMapWindowButton.Activated += delegate {
				SecondaryWindow newWindow = new SecondaryWindow("Map");
				newWindow.SetMainWidget(new Frame { Child = new Map(city) });
				newWindow.ShowAll();
			};
			windowButton.Submenu = windowMenu;
			windowMenu.Append(newSearchWindowButton);
			windowMenu.Append(newMapWindowButton);

			//Window menu
			helpMenu = new Menu();
			helpButton.Submenu = helpMenu;

			mainWindow.ShowAll();

			textSize = (int)Math.Round(MainClass.mainWindow.Style.FontDescription.Size / Pango.Scale.PangoScale);

			Application.Run();

		}

		public static void Load (City city) {
			Unload();
			MainClass.city = city;
			cityInterface = new MainInterface(city);
			mainBox.PackStart(cityInterface, true, true, 0);
			editButton.Sensitive = true;
			viewButton.Sensitive = true;
			toolsButton.Sensitive = true;
			windowButton.Sensitive = true;
			saveButton.Sensitive = true;
			saveAsButton.Sensitive = true;
			closeButton.Sensitive = true;
			mainWindow.ShowAll();
		}

		public static void Unload () {
			city = null;
			cityInterface = null;
			if (mainBox.Children.Length == 2) {
				mainBox.Children[1].Destroy();
			}
			editButton.Sensitive = false;
			viewButton.Sensitive = false;
			toolsButton.Sensitive = false;
			windowButton.Sensitive = false;
			saveButton.Sensitive = false;
			saveAsButton.Sensitive = false;
			closeButton.Sensitive = false;
			mainWindow.ShowAll();
		}

	}

}
