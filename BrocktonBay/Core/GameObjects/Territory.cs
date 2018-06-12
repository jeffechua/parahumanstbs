using System;
using Gtk;
using System.Collections.Generic;


namespace Parahumans.Core {

	public class TerritoryData {

		public string name = "New Territory";
		public int ID = 0;
		public Vector2 location = new Vector2(0, 0);
		public int size = 0;
		public int reputation = 0;
		public List<int> landmarks = new List<int>();

		public TerritoryData () { }

		public TerritoryData (Territory territory) {
			name = territory.name;
			ID = territory.ID;
			location = territory.location;
			size = territory.size;
			reputation = territory.reputation;
			territory.landmarks.ConvertAll((landmark) => landmark.ID);
		}

	}

	public class Territory : GameObject, IContainer {

		public override int order { get { return 2; } }

		[Displayable(1, typeof(Vector2Field))]
		public Vector2 location { get; set; }

		[Displayable(1, typeof(IntField))]
		public int size { get; set; }

		[Displayable(2, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(5, typeof(CellObjectListField<Landmark>), 3), Emphasized]
		public List<Landmark> landmarks { get; set; }

		public Territory () : this(new TerritoryData()) { }

		public Territory (TerritoryData data) {
			name = data.name;
			ID = data.ID;
			location = data.location;
			size = data.size;
			reputation = data.reputation;
			landmarks = data.landmarks.ConvertAll((landmark) => MainClass.currentCity.Get<Landmark>(landmark));
			foreach (Landmark landmark in landmarks) {
				DependencyManager.Connect(landmark, this);
				landmark.parent = this;
			}
		}

		public override Widget GetHeader (bool compact) {
			if (compact) {
				HBox frameHeader = new HBox(false, 0);
				Label icon = new Label(" ● ");
				if (parent == null) {
					icon.State = StateType.Insensitive;
				} else {
					EnumTools.SetAllStates(icon, EnumTools.GetColor(((Faction)parent).alignment));
				}
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(icon, false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = new Label(name), WidthRequest = 200 }, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = parent.GetHeader(true) });
				return headerBox;
			}
		}

		public override Widget GetCell () {

			//Creates the cell contents
			VBox landmarkBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach(Landmark landmark in landmarks){
				InspectableBox header = (InspectableBox)landmark.GetHeader(true);
				header.DragEnd += delegate {
					Remove(landmark);
					DependencyManager.TriggerAllFlags();
				};
				landmarkBox.PackStart(header, false, false, 0);
			}

			//Set up dropping
			EventBox eventBox = new EventBox { Child = landmarkBox, VisibleWindow = false };
			Drag.DestSet(eventBox, DestDefaults.All,
						 new TargetEntry[] { new TargetEntry(typeof(Parahuman).ToString(), TargetFlags.App, 0) },
						 Gdk.DragAction.Move);
			eventBox.DragDataReceived += delegate {
				if (Accepts(DragTmpVars.currentDragged)) {
					Add(DragTmpVars.currentDragged);
					DependencyManager.TriggerAllFlags();
				}
			};

			return new Gtk.Alignment(0, 0, 1, 1) { Child = eventBox, BorderWidth = 7 };
			//For some reason drag/drop highlights include BorderWidth.
			//The Alignment makes the highlight actually appear at the 3:7 point in the margin.
		}

		public override bool Accepts (object obj) => obj is Landmark;
		public override bool Contains (object obj) => obj is Landmark && landmarks.Contains((Landmark)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach(object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				landmarks.Add((Landmark)obj);
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				landmarks.Remove((Landmark)obj);
				obj.parent = null;
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}


	}
}
