﻿using System;
using Gtk;

namespace Parahumans.Core {

	public class MainInterface : HBox {

		public City city;
		public Map map;

		public MainInterface (City city) {

			this.city = city;

			//Sets up main layout and inspector
			Notebook notebook = new Notebook();
			Inspector inspector = new Inspector { BorderWidth = 10 };
			PackStart(notebook, true, true, 0);
			PackStart(inspector, false, false, 0);

			//Map tab
			map = new Map(city); //Profiler called inside Map constructor
			Label mapTabLabel = new Label("Map");
			notebook.AppendPage(map, mapTabLabel);

			Profiler.Log();

			//Search tab
			Search search = new Search(null, (obj) => Inspector.InspectInNearestInspector(obj, this));
			Label searchLabel = new Label("Search");
			notebook.AppendPage(search, searchLabel);

			Profiler.Log(ref Profiler.searchCreateTime);

			/*
			Deployment actors = new Deployment();
			Deployment reactors = new Deployment();
			DeploymentPlanner actorsPlanner = new DeploymentPlanner(actors);
			DeploymentPlanner reactorsPlanner = new DeploymentPlanner(reactors);
			Label APLabel = new Label("Plan actors");
			Label RPLabel = new Label("Plan reactors");
			main.AppendPage(actorsPlanner, APLabel);
			main.AppendPage(reactorsPlanner, RPLabel);
			Battle battle = new Battle(actors, reactors);
			*/

			MainClass.mainWindow.inspector = inspector;

		}
	}
}