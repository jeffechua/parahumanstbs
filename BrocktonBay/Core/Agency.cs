namespace BrocktonBay {

	public interface IAgent : IGUIComplete {
		Threat threat { get; }
		Alignment alignment { get; }
		Gdk.Color color { get; }
		Dossier knowledge { get; set; }
		bool active { get; set; }
		void TakeActionPhase ();
		void TakeResponsePhase ();
		void TakeMastermindPhase ();
	}

	public partial class Faction {

		public void TakeActionPhase () {

		}

		public void TakeResponsePhase () {

		}

		public void TakeMastermindPhase () {

		}

	}

	public partial class Team {

		public void TakeActionPhase () {

		}

		public void TakeResponsePhase () {

		}

		public void TakeMastermindPhase () {

		}

	}

	public partial class Parahuman {

		public void TakeActionPhase () {

		}

		public void TakeResponsePhase () {

		}

		public void TakeMastermindPhase () {

		}

	}

}
