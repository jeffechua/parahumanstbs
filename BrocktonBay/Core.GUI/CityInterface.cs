using System;
using Gtk;

namespace Parahumans.Core {

	public class CityInterface : HBox {

		City city;

		public CityInterface (City city) {

			this.city = city;

			//Sets up main layout and inspector
			Notebook main = new Notebook();
			Inspector.main = new Inspector { BorderWidth = 10 };
			PackStart(main, true, true, 0);
			PackStart(Inspector.main, false, false, 0);

			//Map tab
			Map map = new Map(city); //Profiler called inside Map constructor
			Label mapTabLabel = new Label("Map");
			main.AppendPage(map, mapTabLabel);

			Profiler.Log();

			//Search tab
			Search search = new Search(null, (obj) => Inspector.main.Inspect(obj));
			Label searchLabel = new Label("Search");
			main.AppendPage(search, searchLabel);

			Profiler.Log(ref Profiler.searchCreateTime);

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

			Profiler.Log(ref Profiler.plannerCreateTime);

			Inspector.main.Inspect(new Deployment());

		}
	}
}