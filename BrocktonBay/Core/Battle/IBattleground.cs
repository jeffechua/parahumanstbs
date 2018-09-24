using System;
namespace BrocktonBay {
	public interface IBattleground : IGUIComplete {
		IntVector2 position { get; }
		IAgent affiliation { get; }
		GameAction attack { get; set; }
		GameAction defend { get; set; }
		Attack attackers { get; set; }
		Defense defenders { get; set; }
		Battle battle { get; set; }
		int[] GetCombatBuffs (Context context);
	}
}
