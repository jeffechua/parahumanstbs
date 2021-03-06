﻿using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public sealed class FactionData {
		public string name = "New Faction";
		public int ID = 0;
		public Gdk.Color color = new Gdk.Color(0, 0, 0);
		public Alignment alignment = Alignment.Rogue;
		public int resources = 0;
		public List<int> roster = new List<int>();
		public List<int> teams = new List<int>();
		public List<int> territories = new List<int>();
		public List<TraitData> mechanics = new List<TraitData>();

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
			mechanics = faction.traits.ConvertAll((input) => new TraitData(input));
		}

	}

	public sealed partial class Faction : GameObject, IRated, IAgent {

		public override int order { get { return 3; } }
		public Dossier knowledge { get; set; }
		public override IAgent affiliation { get => this; }
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

		[Displayable(4, typeof(ColorField))]
		public Gdk.Color color { get; set; }

		[Displayable(5, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(6, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(7, typeof(IntField))]
		public int resources { get; set; }

		[Displayable(8, typeof(BasicReadonlyField))]
		public int reputation { get; set; }

		[Displayable(9, typeof(CellTabularListField<Parahuman>), 3, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Parahuman> roster { get; set; }

		[Displayable(10, typeof(CellTabularListField<Team>), 2, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Team> teams { get; set; }

		[Displayable(11, typeof(CellTabularListField<Territory>), 2, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Territory> territories { get; set; }

		[Displayable(12, typeof(MechanicCellTabularListField), 3, emphasized = true, verticalOnly = true)]
		public override List<Trait> traits { get; set; }

		[Displayable(13, typeof(RatingsMultiviewField), true, emphasized = true, verticalOnly = true, expand = true)]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		public List<Parahuman> unassignedCaptures = new List<Parahuman>();
		public List<Parahuman> assignedCaptures = new List<Parahuman>();

		public Faction () : this(new FactionData())
			=> active = true;

		public Faction (FactionData data) {
			name = data.name;
			ID = data.ID;
			color = data.color;
			alignment = data.alignment;
			resources = data.resources;
			roster = data.roster.ConvertAll((parahuman) => Game.city.Get<Parahuman>(parahuman));
			foreach (Parahuman parahuman in roster) {
				DependencyManager.Connect(parahuman, this);
				parahuman.parent = this;
			}
			teams = data.teams.ConvertAll((team) => Game.city.Get<Team>(team));
			foreach (Team team in teams) {
				DependencyManager.Connect(team, this);
				team.parent = this;
			}
			territories = data.territories.ConvertAll((territory) => Game.city.Get<Territory>(territory));
			foreach (Territory territory in territories) {
				DependencyManager.Connect(territory, this);
				territory.parent = this;
			}
			traits = data.mechanics.ConvertAll((input) => Trait.Load(input));
			foreach (Trait mechanic in traits) {
				DependencyManager.Connect(mechanic, this);
				mechanic.parent = this;
			}
			teams.Sort();
			roster.Sort();
			territories.Sort();
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
			foreach (Territory territory in territories)
				reputation += territory.reputation;

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

		public override bool Accepts (object obj) => (obj is Parahuman || obj is Team || obj is Territory) &&
													 (Game.omnipotent || ((IAffiliated)obj).affiliation == affiliation);

		public override void AddRange<T> (IEnumerable<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				if (obj.TryCast(out Parahuman parahuman)) {
					roster.Add(parahuman);
				} else if (obj.TryCast(out Team team)) {
					teams.Add(team);
					foreach (Parahuman child in team.roster)
						DependencyManager.Flag(child); //To notify extended family of changes in leadership
				} else if (obj.TryCast(out Territory territory)) {
					territories.Add(territory);
					foreach (Structure child in territory.structures)
						DependencyManager.Flag(child); //To notify extended family of changes in leadership
				}
				if (obj.TryCast(out IAgent agent)) {
					if (agent.knowledge != null)
						knowledge = knowledge | agent.knowledge;
					agent.active = false;
				}
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);

			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (IEnumerable<T> objs) { //It is assumed that the invoker has already checked if we Accept(obj).
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				obj.parent = null;
				if (obj.TryCast(out Parahuman parahuman)) {
					roster.Remove(parahuman);
				} else if (obj.TryCast(out Team team)) {
					teams.Remove(team);
					foreach (Parahuman child in team.roster)
						DependencyManager.Flag(child); //To notify extended family of changes in leadership
				} else if (obj.TryCast(out Territory territory)) {
					territories.Remove(territory);
					foreach (Structure child in territory.structures)
						DependencyManager.Flag(child); //To notify extended family of changes in leadership
				}
				if (obj.TryCast(out IAgent agent)) {
					agent.knowledge = knowledge.Clone();
					agent.active = false;
				}
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, color, Graphics.textSize),
								 false, false, (uint)(Graphics.textSize / 5));
				return new InspectableBox(header, this, context);
			} else { //A dependable wrapper is not necessary as noncompact headers only exist in inspectors of this object, which will reload anyway.
				return new Gtk.Alignment(0.5f, 0.5f, 0, 0) {
					Child = new InspectableBox(new Label(name), this, context),
					WidthRequest = 200
				};
			}
		}

		public override void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) {
			if ((member is Parahuman && UIFactory.EditAuthorized(this, "roster")) ||
			   (member is Team && UIFactory.EditAuthorized(this, "teams")) ||
			   (member is Territory && UIFactory.EditAuthorized(this, "territories"))) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateMoveButton((IGUIComplete)member));
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			} else if (member is Trait && UIFactory.EditAuthorized(this, "traits")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			}
		}

		public override Widget GetCellContents (Context context) {

			bool editable = UIFactory.EditAuthorized(this, "roster");

			//Creates the cell contents
			VBox childrenBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman parahuman in roster) {
				InspectableBox header = (InspectableBox)parahuman.GetHeader(context.butInUIContext(this));
				if (editable)
					MyDragDrop.SetFailAction(header, delegate {
						Remove(parahuman);
						DependencyManager.TriggerAllFlags();
					});
				childrenBox.PackStart(header, false, false, 0);
			}
			foreach (Team team in teams) {
				InspectableBox header = (InspectableBox)team.GetHeader(context.butInUIContext(this));
				if (editable)
					MyDragDrop.SetFailAction(header, delegate {
						Remove(team);
						DependencyManager.TriggerAllFlags();
					});
				childrenBox.PackStart(header, false, false, 0);
			}

			if (editable) {
				//Set up dropping
				EventBox eventBox = new EventBox { Child = childrenBox, VisibleWindow = false };
				MyDragDrop.DestSet(eventBox, "Parahuman", "Team");
				MyDragDrop.DestSetDropAction(eventBox, delegate {
					if (Accepts(MyDragDrop.currentDragged)) {
						Add(MyDragDrop.currentDragged);
						DependencyManager.TriggerAllFlags();
					}
				});
				return new Gtk.Alignment(0, 0, 1, 0) { Child = eventBox, BorderWidth = 7 };
			} else {
				childrenBox.BorderWidth += 7;
				return childrenBox;
			}

			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

	}
}
