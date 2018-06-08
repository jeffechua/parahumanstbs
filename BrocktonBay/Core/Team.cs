using System;
using System.Collections;
using System.Collections.Generic;
using Parahumans.Core;
using Gtk;

namespace Parahumans.Core {

	public enum Alignment {
		Hero = 2,
		Vigilante = 1,
		Rogue = 0,
		Mercenary = -1,
		Villain = -2
	}

	public enum Threat {
		C = 0, //Default
		B = 1, //Confirmed team takedown
		A = 2, //Confirmed kill
		S = 3, //Kill order receievd
		X = 4  //World-ending
	}

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

	public sealed class Team : GameObject {

		public override int order { get { return 2; } }

		[Displayable(2, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(3, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public int reputation {
			get {
				int rep = 0;
				foreach (Parahuman parahuman in roster) {
					rep += parahuman.reputation;
				}
				return rep;
			}
		}

		[Displayable(5, typeof(IntField))]
		public int unused_XP { get; set; }

		[BimorphicDisplayable(6, typeof(TabularStringFloatPairsField), typeof(LinearStringFloatPairsField)), EmphasizedIfVertical]
		public StringFloatPair[] spent_XP { get; set; }

		[Displayable(7, typeof(CellObjectListField<Parahuman>), 3), Emphasized, Padded(0, 5)]
		public List<Parahuman> roster { get; set; }

		[Displayable(8, typeof(RatingsSumField), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings_profile_2 { get { return ratings_profile; } }

		[Displayable(8, typeof(RatingsProfileField), true), Emphasized, VerticalOnly]
		public RatingsProfile ratings_profile { get; set; }

		public Team () : this(new TeamData()) { }

		public Team (TeamData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			unused_XP = data.unused_XP;
			spent_XP = data.spent_XP;
			roster = data.roster.ConvertAll((input) => MainClass.currentCity.Get<Parahuman>(input));
			for (int i = 0; i < roster.Count; i++) {
				DependencyManager.Connect(roster[i], this);
				roster[i].parent = this;
			}
			ratings_profile = new RatingsProfile();
			Reload();
		}

		public override void Reload () {
			ratings_profile.ratings = new float[5, 8];
			for (int i = 0; i < roster.Count; i++) {
				for (int j = 0; j < roster[i].ratings.Count; j++) {
					if ((int)roster[i].ratings[j].clssf <= 7) {
						ratings_profile.ratings[4, (int)roster[i].ratings[j].clssf] += roster[i].ratings[j].num;
						ratings_profile.ratings[0, (int)roster[i].ratings[j].clssf] += roster[i].ratings[j].num;
					} else {
						for (int k = 0; k < roster[i].ratings[j].subratings.Count; k++) {
							Rating subrating = roster[i].ratings[j].subratings[k];
							ratings_profile.ratings[4, (int)subrating.clssf] += subrating.num;
							ratings_profile.ratings[(int)roster[i].ratings[j].clssf - 7, (int)subrating.clssf] += subrating.num;
						}
					}
				}
			}
			ratings_profile.Evaluate();
			threat = Threat.C;
			for (int i = 0; i < roster.Count; i++)
				if (roster[i].threat > threat)
					threat = roster[i].threat;
		}

		public override Widget GetHeader (bool compact) {
			if (compact) {
				HBox frameHeader = new HBox(false, 0);
				Label icon = new Label(" " + EnumTools.GetSymbol(threat) + " ");
				EnumTools.SetAllStates(icon, EnumTools.GetColor(alignment));
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(icon, false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = parent.GetHeader(true) });
				return headerBox;
			}
		}

		public override Widget GetCell () {

			//Creates the cell contents
			VBox rosterBox = new VBox(false, 0) { BorderWidth = 3 };
			for (int i = 0; i < roster.Count; i++) {
				Parahuman parahuman = roster[i]; //roster[i] not directly used below since i changes
				InspectableBox header = (InspectableBox)parahuman.GetHeader(true);
				header.DragEnd += delegate {
					Remove(parahuman);
					DependencyManager.TriggerAllFlags();
				};
				rosterBox.PackStart(header, false, false, 0);
			}

			//Set up drag/drop
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
		public override void Sort () => roster.Sort();
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
			Sort();
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