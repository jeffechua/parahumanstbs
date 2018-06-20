using Gtk;
using System.Collections.Generic;

namespace Parahumans.Core {

	public enum GameEventType {
		Attack = 0
	}

	public sealed class GameEvent : IGUIComplete {

		public int order { get { return 5; } }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();

		public Deployment[] deploys;

		public string name { get { return "Event at " + location.name; } }

		[Displayable(0, typeof(EnumField<GameEventType>))]
		public GameEventType type;

		[Displayable(1, typeof(ObjectField)), ForceHorizontal]
		public EventLocation location;

		[Displayable(2, typeof(ObjectField)), Emphasized, Padded(0, 10), ForceHorizontal]
		public Deployment initiators { get { return deploys[0]; } set { deploys[0] = value; } }
		/*

		[Displayable(3, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] victory { get; set; }

		[Displayable(4, typeof(FractionsBar)), Emphasized, Padded(10, 10)]
		public Fraction[] territory { get; set; }

		*/
		[Displayable(5, typeof(ObjectField)), Emphasized, Padded(0, 10), ForceHorizontal]
		public Deployment responders { get { return deploys[1]; } set { deploys[1] = value; } }


		public GameEvent (EventLocation location) {
			this.location = location;
			deploys = new Deployment[] { new Deployment(), new Deployment() };
			DependencyManager.Connect(initiators, this);
			DependencyManager.Connect(responders, this);
			Reload();
		}

		public void Reload () {

			/*
			Deployment.Compare(initiators, responders);

			victory = new Fraction[2];
			victory[0] = new Fraction(initiators.affiliation.name, initiators.strength.result / (initiators.strength.result + responders.strength.result),
										Graphics.GetColor(initiators.alignment));
			victory[1] = new Fraction(responders.name, responders.strength.result / (initiators.strength.result + responders.strength.result),
										Graphics.GetColor(responders.alignment));

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
			*/

		}

		public Widget GetHeader (Context context) {
			if (context.compact) {
				HBox frameHeader = new HBox(false, 0);
				frameHeader.PackStart(new Label(name), false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);

				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				/*
				HBox versusBox = new HBox();
				if (initiators.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(initiators.affiliation.GetHeader(context.butCompact));
				}
				versusBox.PackStart(new Label(" vs. "));
				if (responders.affiliation == null) {
					versusBox.PackStart(new Label("Nobody"));
				} else {
					versusBox.PackStart(responders.affiliation.GetHeader(context.butCompact));
				}
				headerBox.PackStart(versusBox, false, false, 0);
				*/
				return headerBox;
			}
		}

		public Widget GetCell (Context context) {
			VBox versusBox = new VBox();
			/*
			if (initiators.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(initiators.affiliation.GetHeader(context.butCompact));
			}
			versusBox.PackStart(new Label(" VERSUS "));
			if (responders.affiliation == null) {
				versusBox.PackStart(new Label("Nobody"));
			} else {
				versusBox.PackStart(responders.affiliation.GetHeader(context.butCompact));
			}
			*/
			return versusBox;
		}

	}

}
