using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public enum StructureType {
		Tactical = 0,
		Economic = 1,
		Aesthetic = 2
	}

	public class StructureData {

		public string name = "New Landmark";
		public int ID = 0;
		public IntVector2 location = new IntVector2(0, 0);
		public StructureType type;
		public int[] buffs = { 0, 0, 0 };

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			location = structure.location;
			type = structure.type;
			buffs = structure.combat_buffs;
		}

	}

	public class Structure : GameObject, IBattleground {

		public override int order { get { return 1; } }
		public Attack attacker { get; set; }
		public Defense defender { get; set; }
		public Battle battle { get; set; }

		[Displayable(2, typeof(IntVector2Field)), LimitVisibility(Phase.None)]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public override IAgent affiliation { get { return (parent == null) ? null : (IAgent)parent.parent; } }

		[Displayable(4, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		[BimorphicDisplayable(5, typeof(TabularContainerField), typeof(LinearContainerField),
		                      "strength_buff", "stealth_buff", "insight_buff" ), EmphasizedIfVertical]
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

		[Child("Strength"), Displayable(0, typeof(IntField))]
		public int strength_buff { get; set; }
		[Child("Stealth"), Displayable(0, typeof(IntField))]
		public int stealth_buff { get; set; }
		[Child("Insight"), Displayable(0, typeof(IntField))]
		public int insight_buff { get; set; }

		[BimorphicDisplayable(6, typeof(TabularContainerField), typeof(LinearContainerField),
							  "resource_income", "reputation_income"), EmphasizedIfVertical]
		public int[] incomes {
			get {
				return new int[] { resource_income, reputation_income };
			}
			set {
				resource_income = value[0];
				reputation_income = value[1];
			}
		}

		[Child("Resources"), Displayable(0, typeof(IntField))]
		public int resource_income { get; set; }
		[Child("Reputation"), Displayable(0, typeof(IntField))]
		public int reputation_income { get; set; }

		//[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public Battle ongoing_event { get; set; }

		[Displayable(7, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly, LimitVisibility(Phase.Action)]
		public GameAction attack { get; set; }

		[Displayable(10, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly, LimitVisibility(Phase.Response)]
		public GameAction defend { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			type = data.type;
			combat_buffs = data.buffs;
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate (Context context) {
					attacker = new Attack(this, BattleObjective.Raid, context.agent);
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

		public override void Reload () {
		}

	}
}
