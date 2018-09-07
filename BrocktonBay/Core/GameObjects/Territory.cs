using System;
using Gtk;
using System.Collections.Generic;


namespace BrocktonBay {

	public class TerritoryData {

		public string name = "New Territory";
		public int ID = 0;
		public IntVector2 position = new IntVector2(0, 0);
		public int size = 0;
		public int reputation = 0;
		public List<int> structures = new List<int>();
		public List<MechanicData> mechanics = new List<MechanicData>();

		public TerritoryData () { }

		public TerritoryData (Territory territory) {
			name = territory.name;
			ID = territory.ID;
			position = territory.position;
			size = territory.size;
			reputation = territory.reputation;
			structures = territory.structures.ConvertAll((structure) => structure.ID);
			mechanics = territory.traits.ConvertAll((input) => new MechanicData(input));
		}

	}

	public sealed class Territory : GameObject, IContainer, IBattleground, MapMarked {

		public override int order { get { return 2; } }
		public Attack attacker { get; set; }
		public Defense defender { get; set; }
		public Battle battle { get; set; }

		[Displayable(2, typeof(IntVector2Field), visiblePhases = Phase.None)]
		public IntVector2 position { get; set; }

		[Displayable(3, typeof(ObjectField), forceHorizontal = true)]
		public override IAgent affiliation { get { return (IAgent)parent; } }

		[Displayable(4, typeof(IntField))]
		public int size { get; set; }

		[Displayable(5, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(6, typeof(TabularContainerField), "strength_buff", "stealth_buff", "insight_buff",
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

		[ChildDisplayableAttribute("Strength", typeof(BasicReadonlyField))]
		public int strength_buff { get; set; }
		[ChildDisplayableAttribute("Stealth", typeof(BasicReadonlyField))]
		public int stealth_buff { get; set; }
		[ChildDisplayableAttribute("Insight", typeof(BasicReadonlyField))]
		public int insight_buff { get; set; }

		[Displayable(7, typeof(TabularContainerField), "resource_income", "reputation_income",
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

		[ChildDisplayableAttribute("Resources", typeof(IntField))]
		public int resource_income { get; set; }
		[ChildDisplayableAttribute("Reputation", typeof(IntField))]
		public int reputation_income { get; set; }

		[Displayable(8, typeof(CellTabularListField<Structure>), 2, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Structure> structures { get; set; }

		[Displayable(9, typeof(MechanicCellTabularListField), 3, emphasized = true, verticalOnly = true)]
		public override List<Trait> traits { get; set; }


		[Displayable(10, typeof(ActionField), 10, verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Action, editablePhases = Phase.Action,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction attack { get; set; }

		[Displayable(11, typeof(ActionField), 10, verticalOnly = true, viewLocks = Locks.Turn, editLocks = Locks.Turn, visiblePhases = Phase.Response, editablePhases = Phase.Response,
					 topPadding = 20, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction defend { get; set; }

		public Territory () : this(new TerritoryData()) { }

		public Territory (TerritoryData data) {
			name = data.name;
			ID = data.ID;
			position = data.position;
			size = data.size;
			reputation = data.reputation;
			structures = data.structures.ConvertAll((structure) => Game.city.Get<Structure>(structure));
			foreach (Structure structure in structures) {
				DependencyManager.Connect(structure, this);
				structure.parent = this;
			}
			traits = data.mechanics.ConvertAll((input) => Trait.Load(input));
			foreach (Trait mechanic in traits) {
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
					Inspector.InspectInNearestInspector(attacker, MainWindow.main);
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
					Inspector.InspectInNearestInspector(defender, MainWindow.main);
				},
				condition = (context) => attacker != null && defender == null && UIFactory.EditAuthorized(this, "defend")
			};
		}

		public override void Reload () {
			structures.Sort();
			combat_buffs = new int[3];
			incomes = new int[2];
			foreach (Structure structure in structures) {
				if (structure.rebuild_time > 0) {
					strength_buff += structure.strength_buff;
					stealth_buff += structure.stealth_buff;
					insight_buff += structure.insight_buff;
					resource_income += structure.resource_income;
					reputation_income += structure.reputation_income;
				}
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

			bool editable = UIFactory.EditAuthorized(this, "structures");

			//Creates the cell contents
			VBox structureBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Structure structure in structures) {
				InspectableBox header = (InspectableBox)structure.GetHeader(context.butCompact);
				if (editable)
					MyDragDrop.SetFailAction(header, delegate {
						Remove(structure);
						DependencyManager.TriggerAllFlags();
					});
				structureBox.PackStart(header, false, false, 0);
			}

			if (editable) {
				//Set up dropping
				EventBox eventBox = new EventBox { Child = structureBox, VisibleWindow = false };
				MyDragDrop.DestSet(eventBox, "Structure");
				MyDragDrop.DestSetDropAction(eventBox, delegate {
					if (Accepts(MyDragDrop.currentDragged)) {
						Add(MyDragDrop.currentDragged);
						DependencyManager.TriggerAllFlags();
					}
				});
				return new Gtk.Alignment(0, 0, 1, 0) { Child = eventBox, BorderWidth = 7 };
			} else {
				structureBox.BorderWidth += 7;
				return structureBox;
			}

			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

		public override bool Accepts (object obj) => obj is Structure && (Game.omnipotent || ((IAffiliated)obj).affiliation == affiliation);
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

		public IMapMarker[] GetMarkers (Map map) {
			return new IMapMarker[]{
				new TerritoryZoneMarker(this, map),
				new TerritoryMarker(this, map),
				new BattleAlertMarker(this, map)
			};
		}

	}
}
