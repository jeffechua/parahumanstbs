﻿using System;
namespace Parahumans.Core {
	public interface IBattleground {
		string name { get; }
		IAgent affiliation { get; }
		Attack attacker { get; set; }
		Defense defender { get; set; }
		Battle battle { get; set; }
		int[] GetCombatBuffs (Context context);
	}
}
