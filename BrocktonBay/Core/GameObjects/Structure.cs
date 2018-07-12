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
			buffs = structure.buffs;
		}

	}

	public class Structure : GameObject, EventLocation, IAffiliated {

		public override int order { get { return 1; } }

		[Displayable(2, typeof(IntVector2Field)), PlayerInvisible]
		public IntVector2 location { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
		public Agent affiliation { get { return (parent == null) ? null : (Agent)parent.parent; } }

		[Displayable(4, typeof(EnumField<StructureType>))]
		public StructureType type { get; set; }

		[BimorphicDisplayable(5, typeof(TabularContainerField), typeof(LinearContainerField),
							  new string[] { "strength_buff", "resource_buff", "reputation_buff" }), EmphasizedIfVertical]
		public int[] buffs {
			get {
				return new int[] { strength_buff, resource_buff, reputation_buff };
			}
			set {
				strength_buff = value[0];
				resource_buff = value[1];
				reputation_buff = value[2];
			}
		}

		[Child("Combat Strength"), Displayable(0, typeof(IntField))]
		public int strength_buff { get; set; }
		[Child("Resource Income"), Displayable(0, typeof(IntField))]
		public int resource_buff { get; set; }
		[Child("Reputation Income"), Displayable(0, typeof(IntField))]
		public int reputation_buff { get; set; }

		//[Displayable(7, typeof(ObjectField)), ForceHorizontal, Padded(10, 10, 10, 10), Emphasized]
		public GameEvent ongoing_event { get; set; }

		[Displayable(7, typeof(ActionField)), Padded(20, 20, 20, 20), VerticalOnly]
		public GameAction attack { get; set; }

		public Structure () : this(new StructureData()) { }

		public Structure (StructureData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			type = data.type;
			buffs = data.buffs;
			attack = new GameAction {
				name = "Attack",
				description = "Launch an attack on " + name,
				action = delegate {
					ongoing_event = new GameEvent(this);
					DependencyManager.Connect(ongoing_event, this);
					DependencyManager.Flag(ongoing_event);
					DependencyManager.TriggerAllFlags();
				},
				condition = delegate (Context context) {
					return ongoing_event == null;
				}
			};
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
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
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

		public override Widget GetCell (Context context) {
			Label label = new Label("Strength +" + strength_buff + "\n" +
			                        "Resources +" + resource_buff + "\n" +
			                        "Reputation +" + reputation_buff);
			label.Justify = Justification.Left;
			return new Gtk.Alignment(0, 0, 1, 1) { Child = label, BorderWidth = 10 };
		}

		public override void Reload () {
		}

	}
}
