using System;
namespace Parahumans.Core {
	public static class Profiler {

		public static double totalLoadTime { get { return dataLoadTime + updateTime + UICreateTime; } }

		public static double dataLoadTime { get { return parahumanLoadTime + teamLoadTime + factionLoadTime + structureLoadTime + territoryLoadTime + mapDataLoadTime; } }
		public static double parahumanLoadTime;
		public static double teamLoadTime;
		public static double factionLoadTime;
		public static double structureLoadTime;
		public static double territoryLoadTime;
		public static double knowledgeLoadTime;
		public static double mapDataLoadTime;

		public static double updateTime;

		public static double UICreateTime { get { return mapCreateTime + searchCreateTime; } }
		public static double mapCreateTime { get { return mapBackgroundCreateTime + mapTerritoriesPlaceTime + mapStructuresPlaceTime + mapBehaviourAssignTime; } }
		public static double mapBackgroundCreateTime;
		public static double mapTerritoriesPlaceTime;
		public static double mapStructuresPlaceTime;
		public static double mapBehaviourAssignTime;
		public static double searchCreateTime;

		public static DateTime currentTime = DateTime.Now;

		public static void Log () => currentTime = DateTime.Now;

		public static void Log (ref double logged) {
			logged = (DateTime.Now - currentTime).TotalMilliseconds;
			currentTime = DateTime.Now;
		}

		public static void Report () {
			Console.WriteLine("TOTAL LOAD TIME: " + totalLoadTime + " ms");
			Console.WriteLine();
			Console.WriteLine("Object data load time: " + dataLoadTime + " ms");
			Console.WriteLine("\t" + "Parahuman data: " + parahumanLoadTime + " ms");
			Console.WriteLine("\t" + "Team data: " + teamLoadTime + " ms");
			Console.WriteLine("\t" + "Faction data:" + factionLoadTime + " ms");
			Console.WriteLine("\t" + "Structure data: " + structureLoadTime + " ms");
			Console.WriteLine("\t" + "Territory data: " + territoryLoadTime + " ms");
			Console.WriteLine("\t" + "Knowledge data: " + knowledgeLoadTime + " ms");
			Console.WriteLine();
			Console.WriteLine("Object update time: " + updateTime + " ms");
			Console.WriteLine();
			Console.WriteLine("UI create time: " + UICreateTime + " ms");
			Console.WriteLine("\t" + "Map creation: " + mapCreateTime + " ms");
			Console.WriteLine("\t" + "\t" + "Map background: " + mapBackgroundCreateTime + " ms");
			Console.WriteLine("\t" + "\t" + "Map territories: " + mapTerritoriesPlaceTime + " ms");
			Console.WriteLine("\t" + "\t" + "Map structures: " + mapStructuresPlaceTime + " ms");
			Console.WriteLine("\t" + "\t" + "Map behaviour: " + mapBehaviourAssignTime + " ms");
			Console.WriteLine("\t" + "Search interface: " + searchCreateTime + " ms");
		}

		public static void WriteLog () {
			double logged = (DateTime.Now - currentTime).TotalMilliseconds;
			Console.WriteLine("Operation took " + logged + " ms.");
		}

	}
}