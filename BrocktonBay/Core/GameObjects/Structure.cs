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
		public int rebuild_time;
		public int[] buffs = { 0, 0, 0 };
		public List<MechanicData> mechanics = new List<MechanicData>();

		public StructureData () { }

		public StructureData (Structure structure) {
			name = structure.name;
			ID = structure.ID;
			position = structure.position;
			type = structure.type;
			rebuild_time = structure.rebuild_time;
			buffs = structure.combat_buffs;
			mechanics = structure.mechanics.ConvertAll((input) => new MechanicData(input));
		}

	}

	public sealed class Structure : GameObject, IBattleground, MapMarked {

		public override int order { get { return 1; } }
		public Attack attacker { get; set; }
		public Defense defender { get; set; }
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

		[Displayable(8, typeof(TabularContainerField), "resource_income", "reputation_income",
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

		[ChildDisplayable("Resources", typeof(IntField))]
		public int resource_income { get; set; }
		[ChildDisplayable("Reputation", typeof(IntField))]
		public int reputation_income { get; set; }

		//[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public Battle ongoing_event { get; set; }

		[Displayable(9, typeof(MechanicCellTabularListField), 3, emphasized = true, verticalOnly = true)]
		public override List<Mechanic> mechanics { get; set; }

		[Displayable(10, typeof(ActionField), verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Action, editablePhases = Phase.Action,
					 topPadding = 20, bottomPadding = 10, leftPadding = 20, rightPadding = 20)]
		public GameAction attack { get; set; }

		[Displayable(11, typeof(ActionField), verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Response, editablePhases = Phase.Response,
					 topPadding = 10, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction defend { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			position = data.position;
			rebuild_time = data.rebuild_time;
			type = data.type;
			combat_buffs = data.buffs;
			mechanics = data.mechanics.ConvertAll((input) => Mechanic.Load(input));
			foreach (Mechanic mechanic in mechanics) {
				DependencyManager.Connect(mechanic, this);
				mechanic.parent = this;
			}
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate (Context context) {
					attacker = new Attack(this, context.agent);
					Game.city.activeBattlegrounds.Add(this);
					DependencyManager.Connect(this, attacker);
					DependencyManager.Flag(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => attacker == null && UIFactory.EditAuthorized(this, "attack")
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
				condition = (context) => attacker != null && defender == null && UIFactory.EditAuthorized(this, "defend")
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

		public override bool Accepts (object obj) => obj is Mechanic;
		public override bool Contains (object obj) => mechanics.Contains((Mechanic)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Mechanic mechanic = (Mechanic)obj;
				mechanics.Add(mechanic);
				mechanic.parent = this;
				DependencyManager.Connect(mechanic, this);
			}
			mechanics.Sort((a, b) => a.secrecy.CompareTo(b.secrecy));
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Mechanic mechanic = (Mechanic)obj;
				mechanics.Remove(mechanic);
				mechanic.parent = null;
				DependencyManager.DisconnectAll(mechanic);
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

		public IMapMarker[] GetMarkers (Map map) {
			return new IMapMarker[]{
				new StructureMarker(this, map),
				new StructureRebuildMarker(this, map),
				new BattleAlertMarker(this, map)
			};
		}

	}
}
