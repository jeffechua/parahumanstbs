﻿using System;
using Gtk;
using System.Collections.Generic;


namespace BrocktonBay {

	public enum StructureType {
		Tactical = 0,
		Economic = 1,
		Aesthetic = 2
	}

	public class StructureData {

		public string name = "New Landmark";
		public int ID = 0;
		public IntVector2 position = new IntVector2(0, 0);
		public StructureType type;
		public int rebuild_time;
		public int[] buffs = { 0, 0, 0 };
		public List<TraitData> mechanics = new List<TraitData>();

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			position = structure.position;
			type = structure.type;
			rebuild_time = structure.rebuild_time;
			buffs = structure.combat_buffs;
			mechanics = structure.traits.ConvertAll((input) => new TraitData(input));
		}

	}

	public sealed class Structure : GameObject, IBattleground, MapMarked {

		public override int order { get { return 1; } }
		public Attack attackers { get; set; }
		public Defense defenders { get; set; }
		public Battle battle { get; set; }

		[Displayable(3, typeof(IntVector2Field), visiblePhases = Phase.None)]
		public IntVector2 position { get; set; }

		[Displayable(4, typeof(ObjectField), forceHorizontal = true)]
		public override IAgent affiliation { get { return (parent == null) ? null : (IAgent)parent.parent; } }

		[Displayable(5, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		[Displayable(6, typeof(IntField))]
		public int rebuild_time { get; set; }

		[Displayable(7, typeof(TabularContainerField), "strength_buff", "stealth_buff", "insight_buff",
					 altWidget = typeof(SlashDelimitedContainerField), emphasizedIfVertical = true)]
		public int[] combat_buffs {
			get {
				return new int[] { strength_buff, stealth_buff, insight_buff };
			}
			set {
				strength_buff = value[0];
				stealth_buff = value[1];
				insight_buff = value[2];
			}
		}

		[ChildDisplayable("Strength", typeof(IntField))]
		public int strength_buff { get; set; }
		[ChildDisplayable("Stealth", typeof(IntField))]
		public int stealth_buff { get; set; }
		[ChildDisplayable("Insight", typeof(IntField))]
		public int insight_buff { get; set; }

		[Displayable(8, typeof(TabularContainerField), "resource_income", "reputation_income",
					 altWidget = typeof(SlashDelimitedContainerField), emphasizedIfVertical = true)]
		public int[] incomes {
			get {
				return new int[] { resource_income, reputation_income };
			}
			set {
				resource_income = value[0];
				reputation_income = value[1];
			}
		}

		[ChildDisplayable("Resources", typeof(IntField))]
		public int resource_income { get; set; }
		[ChildDisplayable("Reputation", typeof(IntField))]
		public int reputation_income { get; set; }

		//[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public Battle ongoing_event { get; set; }

		[Displayable(9, typeof(MechanicCellTabularListField), 3, emphasized = true, verticalOnly = true)]
		public override List<Trait> traits { get; set; }

		[Displayable(10, typeof(ActionField), 10, verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Action, editablePhases = Phase.Action,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction attack { get; set; }

		[Displayable(11, typeof(ActionField), 10, verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Response, editablePhases = Phase.Response,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction defend { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			position = data.position;
			rebuild_time = data.rebuild_time;
			type = data.type;
			combat_buffs = data.buffs;
			traits = data.mechanics.ConvertAll((input) => Trait.Load(input));
			foreach (Trait mechanic in traits) {
				DependencyManager.Connect(mechanic, this);
				mechanic.parent = this;
			}
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate (Context context) {
					attackers = new Attack(this);
					Game.city.activeBattlegrounds.Add(this);
					DependencyManager.Connect(this, attackers);
					DependencyManager.Flag(this);
					DependencyManager.TriggerAllFlags();
					Inspector.InspectInNearestInspector(attackers, MainWindow.main);
				},
				condition = (context) => attackers == null && UIFactory.EditAuthorized(this, "attack")
			};
			defend = new GameAction {
				name = "Defend",
				description = "Mount a defense of " + name,
				action = delegate (Context context) {
					defenders = new Defense(this);
					DependencyManager.Connect(this, defenders);
					DependencyManager.Flag(this);
					DependencyManager.TriggerAllFlags();
					Inspector.InspectInNearestInspector(defenders, MainWindow.main);
				},
				condition = (context) => attackers != null && defenders == null && UIFactory.EditAuthorized(this, "defend")
			};
		}

		public int[] GetCombatBuffs (Context context) {
			int[] buffs = (int[])combat_buffs.Clone();
			return buffs;
		}

		public override void Reload () {
		}

		public override Widget GetHeader (Context context) {

			if (context.compact) {

				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(type, Graphics.GetColor(affiliation), Graphics.textSize),
								 false, false, (uint)(Graphics.textSize / 5));
				return new InspectableBox(header, this, context);

			} else {

				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this, context);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox, WidthRequest = 200 };
				headerBox.PackStart(align, false, false, 0);

				if (parent != null) {
					HBox row2 = new HBox(false, 0);
					row2.PackStart(new Label(), true, true, 0);
					row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent), false, false, 0);
					if (parent.parent != null) {
						row2.PackStart(new VSeparator(), false, false, 10);
						row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent.parent), false, false, 0);
					}
					row2.PackStart(new Label(), true, true, 0);
					headerBox.PackStart(row2);
				}

				return headerBox;

			}
		}

		public override bool Accepts (object obj) => obj is Trait;
		public override bool Contains (object obj) => traits.Contains((Trait)obj);

		public override void AddRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Trait trait = (Trait)obj;
				traits.Add(trait);
				trait.parent = this;
				DependencyManager.Flag(trait);
				DependencyManager.Connect(trait, this);
			}
			traits.Sort((a, b) => a.secrecy.CompareTo(b.secrecy));
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Trait trait = (Trait)obj;
				traits.Remove(trait);
				trait.parent = null;
				DependencyManager.Destroy(trait);
				DependencyManager.Disconnect(trait, this);
			}
			DependencyManager.Flag(this);
		}

		public override Widget GetCellContents (Context context) {
			Label label = new Label("Strength " + strength_buff.ToString("+#;-#;+0") + "\n" +
									"Resources " + stealth_buff.ToString("+#;-#;+0") + "\n" +
									"Reputation " + insight_buff.ToString("+#;-#;+0"));
			label.Justify = Justification.Left;
			return new Gtk.Alignment(0, 0, 1, 0) { Child = label, BorderWidth = 10 };
		}

		public override Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = base.GetRightClickMenu(context, rightClickedWidget);
			if (UIFactory.ViewAuthorized(this, "attack")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateActionButton(attack, context));
			}
			if (UIFactory.ViewAuthorized(this, "defend")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateActionButton(defend, context));
			}
			return rightClickMenu;
		}

		public override void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) {
			if (member is Trait && UIFactory.EditAuthorized(this, "traits")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			}
		}

		public IMapMarker[] GetMarkers (Map map) {
			return new IMapMarker[]{
				new StructureMarker(this, map),
				new StructureRebuildMarker(this, map),
				new BattleAlertMarker(this, map)
			};
		}

	}
}
