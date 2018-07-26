using System;
namespace Parahumans.Core {
	public interface EventLocation {
		string name { get; }
		IAgent affiliation { get; }
		int[] GetCombatBuffs(Context context);
	}
}
