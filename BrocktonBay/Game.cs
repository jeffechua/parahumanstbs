using System;
using System.Collections.Generic;
using Gtk;


namespace Parahumans.Core {

	public enum Phase {
		All = -2,
		None = -1,
		Action = 0,
		Response = 1,
		Mastermind = 2
	}

	class Game {

		public const String savefolder = "/Users/Jefferson/Desktop/Parahumans_Save";
		public static City city;
		public static IAgent player;
		public static Phase phase;

		public static MainInterface cityInterface;
		public static MainWindow mainWindow;
		public static VBox mainBox;
		public static DependableShell UIKey; // A "key" connected to all IDependable UI elements. "Turned" (flagged) to induce a reload across the board.

		public static bool omniscient {
			get {
				return omniscientToggle.Active;
			}
			set {
				omniscientToggle.Active = value;
			}
		}
		public static bool omnipotent {
			get {
				return omnipotentToggle.Active;
			}
			set {
				omnipotentToggle.Active = value;
			}
		}

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
		static CheckMenuItem omniscientToggle;
		static CheckMenuItem omnipotentToggle;

		static SeparatorMenuItem SEP { get => new SeparatorMenuItem(); }

		public static void Main (string[] args) {

			UIKey = new DependableShell(0);

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
			AppendMultiple(menuBar, fileButton, editButton, viewButton, toolsButton, windowButton, helpButton);

			//File menu
			fileMenu = new Menu();
			MenuItem newGamebutton = new MenuItem("New Game") { Sensitive = false }; //Implement
			MenuItem openButton = new MenuItem("Open");
			openButton.Activated += (o, a) => IO.AskIfSaveBefore(IO.SelectOpen);
			saveButton = new MenuItem("Save") { Sensitive = false };
			saveButton.Activated += (o, a) => IO.SelectSave();
			saveAsButton = new MenuItem("Save As") { Sensitive = false };
			saveAsButton.Activated += (o, a) => IO.SelectSaveAs();
			closeButton = new MenuItem("Close") { Sensitive = false };
			closeButton.Activated += (o, a) => IO.AskIfSaveBefore(Unload);
			MenuItem quitButton = new MenuItem("Quit");
			quitButton.Activated += (o, a) => IO.AskIfSaveBefore(Application.Quit);
			fileButton.Submenu = fileMenu;
			AppendMultiple(fileMenu, newGamebutton, SEP, openButton, SEP, saveButton, saveAsButton, SEP, closeButton, quitButton);

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
			AppendMultiple(editMenu, undoButton, redoButton, SEP, createButton, importButton, SEP, editModeButton);
			createButton.Submenu = createMenu;
			AppendMultiple(createMenu, createParahumanButton, createTeamButton, createFactionButton, createStructureButton, createTerritoryButton);

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
			viewButton.Submenu = viewMenu;
			AppendMultiple(viewMenu, inspectorEnabledButton);

			//Tools menu
			toolsMenu = new Menu();
			omniscientToggle = new CheckMenuItem("Omniscient") { Active = true };
			omnipotentToggle = new CheckMenuItem("Omnipotent") { Active = true };
			omniscientToggle.Toggled += (o, a) => RefreshUI();
			omnipotentToggle.Toggled += (o, a) => RefreshUI();
			toolsButton.Submenu = toolsMenu;
			AppendMultiple(toolsMenu, omniscientToggle, omnipotentToggle);

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
			AppendMultiple(windowMenu, newSearchWindowButton, newMapWindowButton);

			//Window menu
			helpMenu = new Menu();
			helpButton.Submenu = helpMenu;

			mainWindow.Realized += Graphics.OnMainWindowInitialized;
			mainWindow.ShowAll();

			Application.Run();

		}

		static void AppendMultiple (MenuShell menu, params Widget[] widgets) {
			foreach (Widget widget in widgets)
				menu.Append(widget);
		}

		public static void Load (City city) {
			Unload();
			Game.city = city;
			phase = Phase.Action;
			cityInterface = new MainInterface();
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

		public static void RefreshUI () {
			DependencyManager.Flag(UIKey);
			DependencyManager.TriggerAllFlags();
		}

	}

}
