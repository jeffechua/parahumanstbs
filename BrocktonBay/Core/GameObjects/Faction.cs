using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	/*
	public class Asset : GameObject {
		public override int order { get { return 0; } }
		[Displayable(2, typeof(StringField))] public String description;
	}
	*/

	public sealed class FactionData {
		public String name = "New Faction";
		public int ID = 0;
		public Gdk.Color color = new Gdk.Color(0, 0, 0);
		public Alignment alignment = Alignment.Rogue;
		public int resources = 0;
		public List<int> roster = new List<int>();
		public List<int> teams = new List<int>();
		public List<int> territories = new List<int>();

		public FactionData () { }

		public FactionData (Faction faction) {
			name = faction.name;
			ID = faction.ID;
			color = faction.color;
			alignment = faction.alignment;
			resources = faction.resources;
			roster = faction.roster.ConvertAll((parahuman) => parahuman.ID);
			teams = faction.teams.ConvertAll((team) => team.ID);
			territories = faction.territories.ConvertAll((territory) => territory.ID);
		}

	}

	public sealed class Faction : GameObject, IRated, Agent {

		public override int order { get { return 3; } }

		[Displayable(2, typeof(ColorField))]
		public Gdk.Color color { get; set; }

		[Displayable(3, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(5, typeof(IntField))]
		public int resources { get; set; }

		[Displayable(6, typeof(BasicReadonlyField))]
		public int reputation { get; set; }

		[Displayable(7, typeof(CellObjectListField<Parahuman>), 3), Emphasized]
		public List<Parahuman> roster { get; set; }

		[Displayable(8, typeof(CellObjectListField<Team>), 2), Emphasized]
		public List<Team> teams { get; set; }

		[Displayable(9, typeof(CellObjectListField<Territory>), 2), Emphasized]
		public List<Territory> territories { get; set; }

		//public List<Asset> assets { get; set; }

		[Displayable(11, typeof(RatingsSumField), true), Emphasized, VerticalOnly]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		[Displayable(12, typeof(RatingsRadarChart), true), Emphasized, VerticalOnly]
		public Func<Context, RatingsProfile> ratings_profile_radar { get { return ratings; } }

		public Faction () : this(new FactionData()) { }

		public Faction (FactionData data) {
			name = data.name;
			ID = data.ID;
			color = data.color;
			alignment = data.alignment;
			resources = data.resources;
			roster = data.roster.ConvertAll((parahuman) => MainClass.city.Get<Parahuman>(parahuman));
			foreach (Parahuman parahuman in roster) {
				DependencyManager.Connect(parahuman, this);
				parahuman.parent = this;
			}
			teams = data.teams.ConvertAll((team) => MainClass.city.Get<Team>(team));
			foreach (Team team in teams) {
				DependencyManager.Connect(team, this);
				team.parent = this;
			}
			territories = data.territories.ConvertAll((territory) => MainClass.city.Get<Territory>(territory));
			foreach (Territory territory in territories) {
				DependencyManager.Connect(territory, this);
				territory.parent = this;
			}
			teams.Sort();
			roster.Sort();
			territories.Sort();
			Reload();
		}

		public RatingsProfile GetRatingsProfile (Context context) {
			return new RatingsProfile(context, roster, teams);
		}

		public override void Reload () {

			teams.Sort();
			roster.Sort();
			territories.Sort();

			threat = Threat.C;
			for (int i = 0; i < roster.Count; i++)
				if (roster[i].threat > threat)
					threat = roster[i].threat;
			for (int i = 0; i < teams.Count; i++)
				if (teams[i].threat > threat)
					threat = teams[i].threat;

			reputation = 0;
			foreach (Parahuman parahuman in roster)
				reputation += parahuman.reputation;
			foreach (Team team in teams)
				reputation += team.reputation;

		}

		public override bool Contains (object obj) {
			if (obj is Team) return teams.Contains((Team)obj);
			//if (obj is Asset) return assets.Contains((Asset)obj);
			if (obj is Parahuman) {
				if (roster.Contains((Parahuman)obj))
					return true;
				for (int i = 0; i < teams.Count; i++)
					if (teams[i].Contains(obj))
						return true;
			}
			return false;
		}

		public override bool Accepts (object obj) => obj is Parahuman || obj is Team || /*obj is Asset ||*/ obj is Territory;

		public override void AddRange<T> (List<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				DependencyManager.Connect(obj, this);
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				if (obj is Team) teams.Add((Team)obj);
				if (obj is Parahuman) roster.Add((Parahuman)obj);
				//if (obj is Asset) assets.Add((Asset)obj);
				if (obj is Territory) territories.Add((Territory)obj);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				DependencyManager.Disconnect(obj, this);
				if (obj is Team) teams.Remove((Team)obj);
				if (obj is Parahuman) roster.Remove((Parahuman)obj);
				//if (obj is Asset) assets.Remove((Asset)obj);
				if (obj is Territory) territories.Remove((Territory)obj);
				obj.parent = null;
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, color, MainClass.textSize),
								 false, false, (uint)(MainClass.textSize / 5));
				return new InspectableBox(header, this);
			} else { //A dependable wrapper is not necessary as noncompact headers only exist in inspectors of this object, which will reload anyway.
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				return headerBox;
			}
		}

		public override Widget GetCell (Context context) {

			//Creates the cell contents
			VBox childrenBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman parahuman in roster) {
				InspectableBox header = (InspectableBox)parahuman.GetHeader(context.butCompact);
				header.DragEnd += delegate {
					Remove(parahuman);
					DependencyManager.TriggerAllFlags();
				};
				childrenBox.PackStart(header, false, false, 0);
			}
			foreach (Team team in teams) {
				InspectableBox header = (InspectableBox)team.GetHeader(context.butCompact);
				header.DragEnd += delegate {
					Remove(team);
					DependencyManager.TriggerAllFlags();
				};
				childrenBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = childrenBox, VisibleWindow = false };
			Drag.DestSet(eventBox, DestDefaults.All,
						 new TargetEntry[] { new TargetEntry(typeof(Parahuman).ToString(), TargetFlags.App, 0),
											 new TargetEntry(typeof(Team).ToString(), TargetFlags.App, 0) },
						 Gdk.DragAction.Move);
			eventBox.DragDataReceived += delegate {
				if (Accepts(DragTmpVars.currentDragged)) {
					Add(DragTmpVars.currentDragged);
					DependencyManager.TriggerAllFlags();
				}
			};

			return new Gtk.Alignment(0, 0, 1, 1) { Child = eventBox, BorderWidth = 7 };
			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

	}
}
