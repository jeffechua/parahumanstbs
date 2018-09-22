using System;
using Gtk;
using Gdk;
using BrocktonBay;

public class DefocusableWindow : Gtk.Window {

	public DefocusableWindow () : base(Gtk.WindowType.Toplevel) {
		KeyPressEvent += delegate (object obj, KeyPressEventArgs args) {
			if (args.Event.Key == Gdk.Key.Escape) Focus = null;
		};
	}

}

public class DefocusableWindowWithInspector : DefocusableWindow {
	public Inspector inspector;
	public bool inspectorEnabled = true;
}

public class SecondaryWindow : DefocusableWindowWithInspector {
	public HBox hBox;
	public Widget main;
	public SecondaryWindow (string title) {

		TransientFor = MainWindow.main;
		DefaultWidth = 1200;
		DefaultHeight = 700;
		Title = title;

		inspector = new Inspector { BorderWidth = 10 };
		VBox viewOptions = new VBox { BorderWidth = 5 };
		CheckButton inspectorVisible = new CheckButton { Active = true };
		inspectorVisible.Toggled += delegate {
			if (inspectorVisible.Active) {
				inspectorEnabled = true;
			} else {
				inspectorEnabled = false;
				inspector.Inspect(null);
			}
		};
		viewOptions.PackStart(inspectorVisible, false, false, 3);
		viewOptions.PackStart(new Label("Inspector Panel") { Angle = -90 }, false, false, 0);

		hBox = new HBox();
		hBox.PackEnd(viewOptions, false, false, 0);
		hBox.PackEnd(inspector, false, false, 0);
		Add(hBox);

	}
	public void SetMainWidget (Widget widget) {
		main = widget;
		hBox.PackEnd(main, true, true, 0);
	}
}

public partial class MainWindow : DefocusableWindowWithInspector {

	public static MainWindow main;

	public static VBox mainBox;
	public static MainInterface mainInterface;

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
	public static CheckMenuItem omniscientToggle;
	public static CheckMenuItem omnipotentToggle;

	static SeparatorMenuItem SEP { get => new SeparatorMenuItem(); }

	public static void Initialize () {
		main = new MainWindow();
		main.ShowAll();
	}

	MainWindow () {

		DeleteEvent += delegate (object obj, DeleteEventArgs args) {
			if (Game.city == null) {
				Application.Quit();
			} else {
				MessageDialog dialog = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Save before quitting?");
				dialog.Response += delegate (object o, ResponseArgs response) {
					if (response.ResponseId == ResponseType.Yes)
						IO.SelectSave();
					Application.Quit();
				};
				dialog.Run();
				dialog.Destroy();
			}
			args.RetVal = true;
		};


		DefaultWidth = 1200;
		DefaultHeight = 700;
		Title = "Brockton Bay";

		mainBox = new VBox();
		menuBar = new MenuBar();
		mainBox.PackStart(menuBar, false, false, 0);
		Add(mainBox);

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
		closeButton.Activated += (o, a) => IO.AskIfSaveBefore(Game.Unload);
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
			Game.city.Add((Parahuman)typeof(Parahuman).GetConstructor(new Type[] { }).Invoke(new object[] { }));
			DependencyManager.TriggerAllFlags();
		};
		MenuItem createTeamButton = new MenuItem("Create Team");
		createTeamButton.Activated += delegate {
			Game.city.Add((Team)typeof(Team).GetConstructor(new Type[] { }).Invoke(new object[] { }));
			DependencyManager.TriggerAllFlags();
		};
		MenuItem createFactionButton = new MenuItem("Create Faction");
		createFactionButton.Activated += delegate {
			Game.city.Add((Faction)typeof(Faction).GetConstructor(new Type[] { }).Invoke(new object[] { }));
			DependencyManager.TriggerAllFlags();
		};
		MenuItem createStructureButton = new MenuItem("Create Structure");
		createStructureButton.Activated += delegate {
			Game.city.Add((Structure)typeof(Structure).GetConstructor(new Type[] { }).Invoke(new object[] { }));
			DependencyManager.TriggerAllFlags();
		};
		MenuItem createTerritoryButton = new MenuItem("Create Territory");
		createTerritoryButton.Activated += delegate {
			Game.city.Add((Territory)typeof(Territory).GetConstructor(new Type[] { }).Invoke(new object[] { }));
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
				inspectorEnabled = true;
			} else {
				inspectorEnabled = false;
				inspector.Inspect(null);
			}
		};
		viewButton.Submenu = viewMenu;
		AppendMultiple(viewMenu, inspectorEnabledButton);

		//Tools menu
		toolsMenu = new Menu();
		omniscientToggle = new CheckMenuItem("Omniscient") { Active = true };
		omnipotentToggle = new CheckMenuItem("Omnipotent") { Active = true };
		omniscientToggle.Toggled += (o, a) => Game.RefreshUIAndTriggerAllFlags();
		omnipotentToggle.Toggled += (o, a) => Game.RefreshUIAndTriggerAllFlags();
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
			newWindow.SetMainWidget(new Frame { Child = new Map() });
			newWindow.ShowAll();
		};
		windowButton.Submenu = windowMenu;
		AppendMultiple(windowMenu, newSearchWindowButton, newMapWindowButton);

		//Window menu
		helpMenu = new Menu();
		helpButton.Submenu = helpMenu;

		Realized += Graphics.OnMainWindowInitialized;

	}

	static void AppendMultiple (MenuShell menu, params Widget[] widgets) {
		foreach (Widget widget in widgets)
			menu.Append(widget);
	}

	public static void Load () {
		if (mainInterface != null)
			mainInterface.Destroy();
		mainInterface = new MainInterface();
		mainBox.PackStart(mainInterface, true, true, 0);
		editButton.Sensitive = true;
		viewButton.Sensitive = true;
		toolsButton.Sensitive = true;
		windowButton.Sensitive = true;
		saveButton.Sensitive = true;
		saveAsButton.Sensitive = true;
		closeButton.Sensitive = true;
		main.ShowAll();
	}

	public static void Unload () {
		mainInterface.Destroy();
		mainInterface = null;
		editButton.Sensitive = false;
		viewButton.Sensitive = false;
		toolsButton.Sensitive = false;
		windowButton.Sensitive = false;
		saveButton.Sensitive = false;
		saveAsButton.Sensitive = false;
		closeButton.Sensitive = false;
		main.ShowAll();
	}

}