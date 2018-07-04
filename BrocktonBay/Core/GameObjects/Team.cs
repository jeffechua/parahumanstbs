using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public sealed class TeamData {

		public String name = "New Team";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public int unused_XP = 0;
		public int[] spent_XP = { 0, 0, 0 };
		public List<int> roster = new List<int>();

		public TeamData () { }

		public TeamData (Team team) {
			name = team.name;
			ID = team.ID;
			alignment = team.alignment;
			unused_XP = team.unused_XP;
			spent_XP = new int[] { team.spent_XP[0].value, team.spent_XP[1].value, team.spent_XP[2].value };
			roster = team.roster.ConvertAll((parahuman) => parahuman.ID);
		}

	}

	public sealed class Team : GameObject, IRated, Agent {

		public override int order { get { return 2; } }
		public Gdk.Color color { get { return new Gdk.Color(0, 0, 0); } }

		[Displayable(2, typeof(ObjectField)), ForceHorizontal]
		public Agent affiliation { get { return (Agent)(parent ?? this); } }

		[Displayable(3, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(4, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(5, typeof(BasicReadonlyField))]
		public int reputation { get; set; }

		[Displayable(6, typeof(IntField))]
		public int unused_XP { get; set; }

		[BimorphicDisplayable(7, typeof(TabularLabeledValuesField<int>), typeof(LinearLabeledValuesField<int>)), EmphasizedIfVertical]
		public LabeledValue<int>[] spent_XP { get; set; }

		[Displayable(8, typeof(CellObjectListField<Parahuman>), 3), Emphasized, Padded(0, 5)]
		public List<Parahuman> roster { get; set; }

		[Displayable(9, typeof(RatingsMultiviewField), true), Emphasized, VerticalOnly]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		//[Displayable(10, typeof(RatingsRadarChart), true), Emphasized, VerticalOnly]
		//public Func<Context, RatingsProfile> ratings_profile_radar { get { return ratings; } }

		public Team () : this(new TeamData()) { }

		public Team (TeamData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			unused_XP = data.unused_XP;
			spent_XP = new LabeledValue<int>[]{
				new LabeledValue<int>("Strength", data.spent_XP[0]),
				new LabeledValue<int>("Mobility", data.spent_XP[1]),
				new LabeledValue<int>("Insight", data.spent_XP[2])
			};
			roster = data.roster.ConvertAll((parahuman) => MainClass.city.Get<Parahuman>(parahuman));
			foreach (Parahuman parahuman in roster) {
				DependencyManager.Connect(parahuman, this);
				parahuman.parent = this;
			}
			Reload();
		}

		public RatingsProfile GetRatingsProfile (Context context) {
			return new RatingsProfile(context, roster);
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

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox frameHeader = new HBox(false, 0);
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(Graphics.GetIcon(threat, Graphics.GetColor(alignment), Graphics.textSize),
									  false, false, (uint)(Graphics.textSize / 5));
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = Graphics.GetSmartHeader(context.butCompact, parent) });
				return headerBox;
			}
		}

		public override Widget GetCell (Context context) {

			//Creates the cell contents
			VBox rosterBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman parahuman in roster) {
				InspectableBox header = (InspectableBox)parahuman.GetHeader(context.butCompact);
				MyDragDrop.SetFailAction(header, delegate {
					Remove(parahuman);
					DependencyManager.TriggerAllFlags();
				});
				rosterBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = rosterBox, VisibleWindow = false };
			MyDragDrop.DestSet(eventBox, typeof(Parahuman).ToString());
			MyDragDrop.DestSetDropAction(eventBox, delegate {
				if (Accepts(MyDragDrop.currentDragged)) {
					Add(MyDragDrop.currentDragged);
					DependencyManager.TriggerAllFlags();
				}
			});

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