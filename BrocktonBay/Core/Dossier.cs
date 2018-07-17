using System;

namespace Parahumans.Core {

	public struct InfoState {
		
		bool _identity;
		public bool identity {
			get {
				return MainClass.omniscient ? true : _identity;
			}
		}

		int _intel;
		public int intel {
			get {
				return MainClass.omniscient ? int.MaxValue : _intel;
			}
		}

		public InfoState (bool identity, int research) {
			_identity = identity;
			_intel = research;
		}

		public static InfoState operator + (InfoState infoState)
			=> new InfoState(true, infoState._intel);
		public static InfoState operator - (InfoState infoState)
			=> new InfoState(false, infoState._intel);
		public static InfoState operator + (InfoState infoState, int increment)
			=> new InfoState(infoState._identity, infoState._intel + increment);
		public static InfoState operator ++ (InfoState infoState)
			=> new InfoState(infoState._identity, infoState._intel + 1);

	}

}
