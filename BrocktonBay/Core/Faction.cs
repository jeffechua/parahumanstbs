using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public class Asset : GameObject {
		public override int order { get { return 0; } }
		[Displayable(2, typeof(StringField))] public String description;
	}

	public sealed class FactionData {
		public String name = "New Faction";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public int resources = 0;
		public List<int> roster = new List<int>();
		public List<int> teams = new List<int>();

		public FactionData () { }

		public FactionData (Faction faction) {
			name = faction.name;
			ID = faction.ID;
			alignment = faction.alignment;
			resources = 0;
			roster = faction.roster.ConvertAll((parahuman) => parahuman.ID);
			teams = faction.teams.ConvertAll((team) => team.ID);
		}

	}

	public sealed class Faction : GameObject, Rated {

		public override int order { get { return 3; } }

		[Displayable(2, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(3, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(4, typeof(IntField))]
		public int resources { get; set; }

		[Displayable(5, typeof(BasicReadonlyField))]
		public int reputation {
			get {
				int rep = 0;
				foreach (Parahuman parahuman in roster) {
					rep += parahuman.reputation;
				}
				foreach (Team team in teams) {
					rep += team.reputation;
				}
				return rep;
			}
		}

		public List<Asset> assets { get; set; }

		[Displayable(6, typeof(CellObjectListField<Parahuman>), 3), Emphasized]
		public List<Parahuman> roster { get; set; }

		[Displayable(6, typeof(CellObjectListField<Team>), 2), Emphasized]
		public List<Team> teams { get; set; }

		[Displayable(8, typeof(RatingsSumField), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings { get { return new RatingsProfile(roster, teams); } }

		[Displayable(8, typeof(RatingsRadarChart), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings_profile_radar { get { return ratings; } }

		public Faction () : this(new FactionData()) { }

		public Faction (FactionData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			resources = data.resources;
			roster = data.roster.ConvertAll((input) => MainClass.currentCity.Get<Parahuman>(input));
			for (int i = 0; i < roster.Count; i++) {
				DependencyManager.Connect(roster[i], this);
				roster[i].parent = this;
			}
			teams = data.teams.ConvertAll((input) => MainClass.currentCity.Get<Team>(input));
			for (int i = 0; i < teams.Count; i++) {
				DependencyManager.Connect(teams[i], this);
				teams[i].parent = this;
			}
			Reload();
		}

		public override void Reload () {
			threat = Threat.C;
			for (int i = 0; i < roster.Count; i++)
				if (roster[i].threat > threat)
					threat = roster[i].threat;
			for (int i = 0; i < teams.Count; i++)
				if (teams[i].threat > threat)
					threat = teams[i].threat;
		}

		public override bool Contains (object obj) {
			if (obj is Team) return teams.Contains((Team)obj);
			if (obj is Asset) return assets.Contains((Asset)obj);
			if (obj is Parahuman) {
				if (roster.Contains((Parahuman)obj))
					return true;
				for (int i = 0; i < teams.Count; i++)
					if (teams[i].Contains(obj))
						return true;
			}
			return false;
		}

		public override bool Accepts (object obj) => obj is Parahuman || obj is Team || obj is Asset;

		public override void Sort () {
			teams.Sort();
			roster.Sort();
			assets.Sort();
		}

		public override void AddRange<T> (List<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				DependencyManager.Connect(obj, this);
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				if (obj is Team) {
					teams.Add((Team)obj);
					teams.Sort();
				}
				if (obj is Parahuman) {
					roster.Add((Parahuman)obj);
					roster.Sort();
				}
				if (obj is Asset) {
					assets.Add((Asset)obj);
					assets.Sort();
				}
				DependencyManager.Flag(obj);
			}
		}

		public override void RemoveRange<T> (List<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				DependencyManager.Disconnect(obj, this);
				if (obj is Team) teams.Remove((Team)obj);
				if (obj is Parahuman) roster.Remove((Parahuman)obj);
				if (obj is Asset) assets.Remove((Asset)obj);
				obj.parent = null;
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override Widget GetHeader (bool compact) {
			if (compact) {
				HBox frameHeader = new HBox(false, 0);
				Label teamIcon = new Label(" " + EnumTools.GetSymbol(threat) + " ");
				EnumTools.SetAllStates(teamIcon, EnumTools.GetColor(alignment));
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(teamIcon, false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				return headerBox;
			}
		}

		public override Widget GetCell () {
			VBox membersBox = new VBox(false, 0) { BorderWidth = 10 };
			for (int i = 0; i < roster.Count; i++)
				membersBox.PackStart(roster[i].GetHeader(true), false, false, 0);
			for (int i = 0; i < teams.Count; i++)
				membersBox.PackStart(teams[i].GetHeader(true), false, false, 0);
			return membersBox;
		}

	}
}
