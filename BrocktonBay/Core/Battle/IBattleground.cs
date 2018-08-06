using System;
namespace BrocktonBay {
	public interface IBattleground : IGUIComplete {
		IntVector2 position { get; }
		IAgent affiliation { get; }
		Attack attacker { get; set; }
		Defense defender { get; set; }
		Battle battle { get; set; }
		int[] GetCombatBuffs (Context context);
	}
}
