using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public sealed class TeamData {

		public String name = "New Team";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public int unused_XP = 0;
		public StringFloatPair[] spent_XP = new StringFloatPair[3] {
			new StringFloatPair("Strength", 0),
			new StringFloatPair("Mobility", 0),
			new StringFloatPair("Insight", 0)
		};
		public List<int> roster = new List<int>();

		public TeamData () { }

		public TeamData (Team team) {
			name = team.name;
			ID = team.ID;
			alignment = team.alignment;
			unused_XP = team.unused_XP;
			spent_XP = team.spent_XP;
			roster = team.roster.ConvertAll((parahuman) => parahuman.ID);
		}

	}

	public sealed class Team : GameObject, Rated {

		public override int order { get { return 2; } }

		[Displayable(2, typeof(ObjectField)), ForceHorizontal]
		public Faction affiliation { get { return (Faction)parent; } }

		[Displayable(3, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(5, typeof(BasicReadonlyField))]
		public int reputation { get; set; }

		[Displayable(6, typeof(IntField))]
		public int unused_XP { get; set; }

		[BimorphicDisplayable(7, typeof(TabularStringFloatPairsField), typeof(LinearStringFloatPairsField)), EmphasizedIfVertical]
		public StringFloatPair[] spent_XP { get; set; }

		[Displayable(8, typeof(CellObjectListField<Parahuman>), 3), Emphasized, Padded(0, 5)]
		public List<Parahuman> roster { get; set; }

		[Displayable(9, typeof(RatingsSumField), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings { get { return new RatingsProfile(roster); } }

		[Displayable(10, typeof(RatingsRadarChart), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings_profile_radar { get { return ratings; } }

		public Team () : this(new TeamData()) { }

		public Team (TeamData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			unused_XP = data.unused_XP;
			spent_XP = data.spent_XP;
			roster = data.roster.ConvertAll((parahuman) => MainClass.city.Get<Parahuman>(parahuman));
			foreach (Parahuman parahuman in roster) {
				DependencyManager.Connect(parahuman, this);
				parahuman.parent = this;
			}
			Reload();
		}

		public override void Reload () {

			roster.Sort();

			threat = Threat.C;
			for (int i = 0; i < roster.Count; i++)
				if (roster[i].threat > threat)
					threat = roster[i].threat;

			reputation = 0;
			foreach (Parahuman parahuman in roster)
				reputation += parahuman.reputation;

		}

		public override Widget GetHeader (bool compact) {
			if (compact) {
				HBox frameHeader = new HBox(false, 0);
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(Graphics.GetIcon(threat, Graphics.GetColor(alignment), MainClass.textSize),
									  false, false, (uint)(MainClass.textSize / 5));
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = parent.GetSmartHeader(true) });
				return headerBox;
			}
		}

		public override Widget GetCell () {

			//Creates the cell contents
			VBox rosterBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman parahuman in roster) {
				InspectableBox header = (InspectableBox)parahuman.GetHeader(true);
				header.DragEnd += delegate {
					Remove(parahuman);
					DependencyManager.TriggerAllFlags();
				};
				rosterBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = rosterBox, VisibleWindow = false };
			Drag.DestSet(eventBox, DestDefaults.All,
						 new TargetEntry[] { new TargetEntry(typeof(Parahuman).ToString(), TargetFlags.App, 0) },
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

		public override bool Contains (object obj) => obj is Parahuman && roster.Contains((Parahuman)obj);
		public override bool Accepts (object obj) => obj is Parahuman;

		public override void AddRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				roster.Add((Parahuman)obj);
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				roster.Remove((Parahuman)obj);
				((Parahuman)obj).parent = null;
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

	}

}