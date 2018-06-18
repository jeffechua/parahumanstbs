using Gtk;

namespace Parahumans.Core {

	public enum GameEventType {
		Attack = 0
	}

	public sealed class GameEvent : GUIComplete {

		public override int order { get { return 5; } }

		public Deployment[] deploys;

		public override string name { get { return actors.name + " vs." + reactors.name; } }

		[Displayable(0, typeof(EnumField<GameEventType>))] 
		public GameEventType type;

		[Displayable(1, typeof(ObjectField)), ForceHorizontal]
		public EventLocation location;

		[Displayable(2, typeof(ObjectField)), Emphasized, Padded(0, 10)]
		public Deployment actors { get { return deploys[0]; } set { deploys[0] = value; } }

		[Displayable(3, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] victory { get; set; }

		[Displayable(4, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] territory { get; set; }

		[Displayable(5, typeof(ObjectField)), Emphasized, Padded(0, 10)]
		public Deployment reactors { get { return deploys[1]; } set { deploys[1] = value; } }


		public GameEvent (EventLocation location) {
			this.location = location;
			deploys = new Deployment[] { new Deployment(), new Deployment() };
			DependencyManager.Connect(actors, this);
			DependencyManager.Connect(reactors, this);
			Reload();
		}

		public override void Reload () {

			Deployment.Compare(actors, reactors);

			victory = new Fraction[2];
			victory[0] = new Fraction(actors.name, actors.strength.result / (actors.strength.result + reactors.strength.result),
										Graphics.GetColor(actors.alignment));
			victory[1] = new Fraction(reactors.name, reactors.strength.result / (actors.strength.result + reactors.strength.result),
										Graphics.GetColor(reactors.alignment));

			if (victory[0].val > 0.33) {
				territory = new Fraction[3];
				territory[0] = new Fraction("Capture", victory[0].val * victory[0].val, new Gdk.Color(0, 200, 0));
				territory[1] = new Fraction("Raze", victory[0].val - victory[0].val * victory[0].val, new Gdk.Color(200, 0, 0));
				territory[2] = new Fraction("Safe", victory[1].val, new Gdk.Color(0, 0, 200));
			} else {
				territory = new Fraction[2];
				territory[0] = new Fraction("Raze", victory[0].val, new Gdk.Color(200, 0, 0));
				territory[1] = new Fraction("Safe", victory[1].val, new Gdk.Color(0, 0, 200));
			}

		}

		public override Widget GetHeader (Context context) {
			Label label = new Label("Battle of " + location.name);
			if (!context.compact) label.WidthRequest = 300;
			return new InspectableBox(label, this);
		}

	}

}
