using System;
namespace BrocktonBay {
	public interface IBattleground : IGUIComplete {
		IntVector2 position { get; }
		IAgent affiliation { get; }
		Attack attackers { get; set; }
		Defense defenders { get; set; }
		Battle battle { get; set; }
		int[] GetCombatBuffs (Context context);
	}
}
