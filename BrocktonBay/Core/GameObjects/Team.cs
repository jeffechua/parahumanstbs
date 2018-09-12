using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public sealed class TeamData {
		public string name = "New Team";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public int unused_xp = 0;
		public int[] spent_xp = { 0, 0, 0 };
		public List<int> roster = new List<int>();
		public List<TraitData> mechanics = new List<TraitData>();

		public TeamData () { }

		public TeamData (Team team) {
			name = team.name;
			ID = team.ID;
			alignment = team.alignment;
			unused_xp = team.unused_XP;
			spent_xp = team.spent_XP;
			roster = team.roster.ConvertAll((parahuman) => parahuman.ID);
			mechanics = team.traits.ConvertAll((input) => new TraitData(input));
		}

	}

	public sealed partial class Team : GameObject, IRated, IAgent {

		public override int order { get { return 2; } }
		public Gdk.Color color { get { return new Gdk.Color(150, 150, 150); } }
		public Dossier knowledge { get; set; }
		bool _active;
		[Displayable(3, typeof(BasicReadonlyField), visiblePhases = Phase.None)]
		public bool active {
			get => _active;
			set {
				if (value) {
					if (!Game.city.activeAgents.Contains(this)) Game.city.activeAgents.Add(this);
					if (knowledge == null) knowledge = new Dossier();
				} else {
					Game.city.activeAgents.Remove(this);
					knowledge = null;
				}
				_active = value;
			}
		}

		[Displayable(4, typeof(ObjectField), forceHorizontal = true)]
		public override IAgent affiliation { get { return (IAgent)(parent ?? this); } }

		[Displayable(5, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(6, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(7, typeof(BasicReadonlyField))]
		public int reputation { get; set; }

		[Displayable(8, typeof(IntField))]
		public int unused_XP { get; set; }

		[Displayable(9, typeof(TabularContainerField), "strength_XP", "stealth_XP", "insight_XP",
					 altWidget = typeof(SlashDelimitedContainerField), emphasizedIfVertical = true)]
		public int[] spent_XP {
			get {
				return new int[] { strength_XP, stealth_XP, insight_XP };
			}
			set {
				strength_XP = value[0];
				stealth_XP = value[1];
				insight_XP = value[2];
			}
		}

		[ChildDisplayableAttribute("Strength", typeof(IntField))]
		public int strength_XP { get; set; }
		[ChildDisplayableAttribute("Stealth", typeof(IntField))]
		public int stealth_XP { get; set; }
		[ChildDisplayableAttribute("Insight", typeof(IntField))]
		public int insight_XP { get; set; }

		[Displayable(10, typeof(CellTabularListField<Parahuman>), 3, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Parahuman> roster { get; set; }

		[Displayable(11, typeof(MechanicCellTabularListField), 3, emphasized = true, verticalOnly = true)]
		public override List<Trait> traits { get; set; }

		[Displayable(12, typeof(RatingsMultiviewField), true, emphasized = true, verticalOnly = true, expand = true)]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }


		public Team () : this(new TeamData())
			=> active = true;

		public Team (TeamData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			unused_XP = data.unused_xp;
			spent_XP = data.spent_xp;
			roster = data.roster.ConvertAll((parahuman) => Game.city.Get<Parahuman>(parahuman));
			foreach (Parahuman parahuman in roster) {
				DependencyManager.Connect(parahuman, this);
				parahuman.parent = this;
			}
			traits = data.mechanics.ConvertAll((input) => Trait.Load(input));
			foreach (Trait mechanic in traits) {
				DependencyManager.Connect(mechanic, this);
				mechanic.parent = this;
			}
		}

		public RatingsProfile GetRatingsProfile (Context context) {
			RatingsProfile profile = new RatingsProfile(context, roster);
			profile.bonuses[0] += spent_XP[0];
			profile.bonuses[1] += spent_XP[1];
			profile.bonuses[2] += spent_XP[2];
			return profile;
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
				return new InspectableBox(frameHeader, this, context);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this, context);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox, WidthRequest = 200 };
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(UIFactory.Align(Graphics.GetSmartHeader(context.butCompact, parent), 0.5f, 0.5f, 0, 0));
				return headerBox;
			}
		}

		public override Widget GetCellContents (Context context) {

			bool editable = UIFactory.EditAuthorized(this, "roster");

			//Creates the cell contents
			VBox rosterBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman parahuman in roster) {
				InspectableBox header = (InspectableBox)parahuman.GetHeader(context.butInUIContext(this));
				if (editable)
					MyDragDrop.SetFailAction(header, delegate {
						Remove(parahuman);
						DependencyManager.TriggerAllFlags();
					});
				rosterBox.PackStart(header, false, false, 0);
			}

			if (editable) {
				//Set up dropping
				EventBox eventBox = new EventBox { Child = rosterBox, VisibleWindow = false };
				MyDragDrop.DestSet(eventBox, "Parahuman");
				MyDragDrop.DestSetDropAction(eventBox, delegate {
					if (Accepts(MyDragDrop.currentDragged)) {
						Add(MyDragDrop.currentDragged);
						DependencyManager.TriggerAllFlags();
					}
				});
				return new Gtk.Alignment(0, 0, 1, 0) { Child = eventBox, BorderWidth = 7 };
			} else {
				rosterBox.BorderWidth += 7;
				return rosterBox;
			}

			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

		public override void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) {
			if (member is Parahuman && UIFactory.EditAuthorized(this, "roster")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateMoveButton((IGUIComplete)member));
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			} else if (member is Trait && UIFactory.EditAuthorized(this, "traits")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			}
		}

		public override bool Accepts (object obj) => obj is Parahuman && (Game.omnipotent || ((IAffiliated)obj).affiliation == affiliation);
		public override bool Contains (object obj) => obj is Parahuman && roster.Contains((Parahuman)obj);

		public override void AddRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				if (parahuman.parent != null) parahuman.parent.Remove(obj);
				parahuman.parent = this;
				if (parahuman.knowledge != null)
					affiliation.knowledge = affiliation.knowledge | parahuman.knowledge;
				parahuman.active = false;
				roster.Add(parahuman);
				DependencyManager.Connect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				parahuman.parent = null;
				parahuman.knowledge = affiliation.knowledge.Clone();
				parahuman.active = true;
				roster.Remove(parahuman);
				DependencyManager.Disconnect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(this);
		}

	}

}