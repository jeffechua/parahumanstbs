using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {
	
	public class StructureMarker : InspectableBox, IDependable {

		public int order { get { return 2; } }
		public bool destroyed { get; set; }
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public List<IDependable> triggers { get; set; } = new List<IDependable>();

		public Map map;

		public static readonly int markerSize = 25;
		public Image markerImage;
		public Vector2 scaledPosition;

		public Structure structure;
		public IntVector2 location; //We round this to prevent floating point precision errors
		public Faction affiliation;
		public StructureType type;

		public StructureMarker (Structure structure, Map map) : base(structure) {

			this.map = map;

			this.structure = structure;
			location = structure.location;
			affiliation = structure.affiliation;
			type = structure.type;

			VisibleWindow = false;
			Redraw();
			map.stage.Put(this, 0, 0);
			Repin();

			DependencyManager.Connect(structure, this);
			if (structure.parent != null) DependencyManager.Connect(structure.parent, this);

		}

		public void Redraw () {
			markerImage = Graphics.GetIcon(structure.type, Graphics.GetColor(affiliation), markerSize);
			if (Child != null) Remove(Child);
			Add(markerImage);
		}

		public void Repin () {
			scaledPosition = location * map.currentMagnif;
			scaledPosition -= new Vector2(markerSize / 2, markerSize / 2);
			map.stage.Move(this, (int)scaledPosition.x, (int)scaledPosition.y);
		}

		public void Reload () {
			if (affiliation != structure.affiliation || type != structure.type) {
				affiliation = structure.affiliation;
				type = structure.type;
				Redraw();
			}
			if (location != structure.location) {
				location = structure.location;
				Repin();
			}
		}

	}

}