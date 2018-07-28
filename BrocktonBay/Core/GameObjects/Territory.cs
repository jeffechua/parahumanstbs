using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public class TerritoryData {

		public string name = "New Territory";
		public int ID = 0;
		public IntVector2 location = new IntVector2(0, 0);
		public int size = 0;
		public int reputation = 0;
		public List<int> structures = new List<int>();

		public TerritoryData () { }

		public TerritoryData (Territory territory) {
			name = territory.name;
			ID = territory.ID;
			location = territory.location;
			size = territory.size;
			reputation = territory.reputation;
			structures = territory.structures.ConvertAll((structure) => structure.ID);
		}

	}

	public class Territory : GameObject, IContainer, IBattleground {

		public override int order { get { return 2; } }
		public Attack attacker { get; set; }
		public Defense defender { get; set; }
		public Battle battle { get; set; }

		[Displayable(2, typeof(IntVector2Field)), PlayerInvisible]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public override IAgent affiliation { get { return (IAgent)parent; } }

		[Displayable(4, typeof(IntField))]
		public int size { get; set; }

		[Displayable(5, typeof(IntField))]
		public int reputation { get; set; }

		[BimorphicDisplayable(6, typeof(TabularContainerField), typeof(LinearContainerField),
							  "strength_buff", "stealth_buff", "insight_buff"), EmphasizedIfVertical]
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

		[Child("Strength"), Displayable(0, typeof(BasicReadonlyField))]
		public int strength_buff { get; set; }
		[Child("Stealth"), Displayable(0, typeof(BasicReadonlyField))]
		public int stealth_buff { get; set; }
		[Child("Insight"), Displayable(0, typeof(BasicReadonlyField))]
		public int insight_buff { get; set; }

		[BimorphicDisplayable(7, typeof(TabularContainerField), typeof(LinearContainerField),
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

		[Displayable(8, typeof(CellTabularListField<Structure>), 2), Emphasized, PlayerEditable(Phase.Mastermind)]
		public List<Structure> structures { get; set; }

		[Displayable(9, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly]
		public GameAction attack { get; set; }

		public Territory () : this(new TerritoryData()) { }

		public Territory (TerritoryData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			size = data.size;
			reputation = data.reputation;
			structures = data.structures.ConvertAll((structure) => Game.city.Get<Structure>(structure));
			foreach (Structure structure in structures) {
				DependencyManager.Connect(structure, this);
				structure.parent = this;
			}
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
		}

		public override void Reload () {
			structures.Sort();
			combat_buffs = new int[3];
			incomes = new int[2];
			foreach (Structure structure in structures) {
				strength_buff += structure.strength_buff;
				stealth_buff += structure.stealth_buff;
				insight_buff += structure.insight_buff;
				resource_income += structure.resource_income;
				reputation_income += structure.reputation_income;
			}
		}

		public int[] GetCombatBuffs (Context context) {
			int[] buffs = (int[])combat_buffs.Clone();
			return buffs;
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(Threat.C, Graphics.GetColor(affiliation), Graphics.textSize),
								 false, false, (uint)(Graphics.textSize / 5));
				return new InspectableBox(header, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox, WidthRequest = 200 };
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(UIFactory.Align(Graphics.GetSmartHeader(context.butCompact, parent), 0.5f, 0.5f, 0, 0));
				return headerBox;
			}
		}

		public override Widget GetCellContents (Context context) {

			//Creates the cell contents
			VBox structureBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Structure structure in structures) {
				InspectableBox header = (InspectableBox)structure.GetHeader(context.butCompact);
				header.DragEnd += delegate {
					Remove(structure);
					DependencyManager.TriggerAllFlags();
				};
				structureBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = structureBox, VisibleWindow = false };
			MyDragDrop.DestSet(eventBox, typeof(Structure).ToString());
			MyDragDrop.DestSetDropAction(eventBox, delegate {
				if (Accepts(MyDragDrop.currentDragged)) {
					Add(MyDragDrop.currentDragged);
					DependencyManager.TriggerAllFlags();
				}
			});

			return new Gtk.Alignment(0, 0, 1, 0) { Child = eventBox, BorderWidth = 7 };
			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

		public override bool Accepts (object obj) => obj is Structure && ((IAffiliated)obj).affiliation == affiliation;
		public override bool Contains (object obj) => obj is Structure && structures.Contains((Structure)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				structures.Add((Structure)obj);
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				structures.Remove((Structure)obj);
				obj.parent = null;
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}


	}
}
