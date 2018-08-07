using System;
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
		public int[] buffs = { 0, 0, 0 };

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			position = structure.position;
			type = structure.type;
			buffs = structure.combat_buffs;
		}

	}

	public class Structure : GameObject, IBattleground, MapMarked {

		public override int order { get { return 1; } }
		public Attack attacker { get; set; }
		public Defense defender { get; set; }
		public Battle battle { get; set; }

		[Displayable(2, typeof(IntVector2Field), visiblePhases = Phase.None)]
		public IntVector2 position { get; set; }

		[Displayable(3, typeof(ObjectField), forceHorizontal = true)]
		public override IAgent affiliation { get { return (parent == null) ? null : (IAgent)parent.parent; } }

		[Displayable(4, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		[Displayable(5, typeof(TabularContainerField), "strength_buff", "stealth_buff", "insight_buff",
					 altWidget = typeof(LinearContainerField), emphasizedIfVertical = true)]
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

		[Displayable(6, typeof(TabularContainerField), "resource_income", "reputation_income",
					 altWidget = typeof(LinearContainerField), emphasizedIfVertical = true)]
		public int[] incomes {
			get {
				return new int[] { resource_income, reputation_income };
			}
			set {
				resource_income = value[0];
				reputation_income = value[1];
			}
		}

		[ChildDisplayableAttribute("Resources", typeof(IntField))]
		public int resource_income { get; set; }
		[ChildDisplayableAttribute("Reputation", typeof(IntField))]
		public int reputation_income { get; set; }

		//[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public Battle ongoing_event { get; set; }

		[Displayable(7, typeof(ActionField), verticalOnly = true, visiblePhases = Phase.Action,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction attack { get; set; }

		[Displayable(10, typeof(ActionField), verticalOnly = true, visiblePhases = Phase.Response,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction defend { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			position = data.position;
			type = data.type;
			combat_buffs = data.buffs;
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate (Context context) {
					attacker = new Attack(this, context.agent);
					DependencyManager.Connect(this, attacker);
					DependencyManager.Flag(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = delegate (Context context) {
					return Game.phase == Phase.Action && attacker == null;
				}
			};
			defend = new GameAction {
				name = "Defend",
				description = "Mount a defense of " + name,
				action = delegate (Context context) {
					defender = new Defense(this, context.agent);
					DependencyManager.Connect(this, defender);
					DependencyManager.Flag(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = delegate (Context context) {
					return Game.phase == Phase.Response && attacker != null && defender == null;
				}
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
				return new InspectableBox(header, this);

			} else {

				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this);
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

		public override Widget GetCellContents (Context context) {
			Label label = new Label("Strength " + strength_buff.ToString("+#;-#;+0") + "\n" +
									"Resources " + stealth_buff.ToString("+#;-#;+0") + "\n" +
									"Reputation " + insight_buff.ToString("+#;-#;+0"));
			label.Justify = Justification.Left;
			return new Gtk.Alignment(0, 0, 1, 0) { Child = label, BorderWidth = 10 };
		}

		public IMapMarker[] GetMarkers (Map map) {
			return new IMapMarker[]{
				new StructureMarker(this, map),
				new BattleAlertMarker(this, map)
			};
		}

	}
}
