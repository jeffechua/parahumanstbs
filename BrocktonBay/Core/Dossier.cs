using System;

namespace Parahumans.Core {

	public struct InfoState {
		
		bool _identity;
		public bool identity {
			get {
				return MainClass.omniscient ? true : _identity;
			}
		}

		int _research;
		public int research {
			get {
				return MainClass.omniscient ? int.MaxValue : _research;
			}
		}

		public InfoState (bool identity, int research) {
			_identity = identity;
			_research = research;
		}

		public static InfoState operator + (InfoState infoState)
			=> new InfoState(true, infoState._research);
		public static InfoState operator - (InfoState infoState)
			=> new InfoState(false, infoState._research);
		public static InfoState operator + (InfoState infoState, int increment)
			=> new InfoState(infoState._identity, infoState._research + increment);
		public static InfoState operator ++ (InfoState infoState)
			=> new InfoState(infoState._identity, infoState._research + 1);

	}

}
