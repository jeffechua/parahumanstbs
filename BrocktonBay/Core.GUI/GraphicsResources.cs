using Gtk;
using System;
using System.Collections.Generic;

namespace Parahumans.Core {

	public static class EnumTools {

		public static readonly string[] classSymbols = { "", "β", "δ", "Σ", "ψ", "μ", "φ", "ξ", "Ω", "Γ", "Λ", "Δ" };
		public static readonly string[] threatSymbols = { "●", "■", "▲", "☉" };
		public static readonly Gdk.Color[] healthColors = { new Gdk.Color(100, 100, 100), new Gdk.Color(230, 0, 0), new Gdk.Color(200, 200, 0), new Gdk.Color(0, 200, 0) };
		public static readonly Gdk.Color[] alignmentColors = { new Gdk.Color(0, 100, 230), new Gdk.Color(170, 140, 0), new Gdk.Color(100, 150, 0), new Gdk.Color(0, 0, 0), new Gdk.Color(150, 0, 175) };

		public static Gdk.Color GetColor (Health health) => healthColors[(int)health];
		public static Gdk.Color GetColor (Alignment alignment) => alignmentColors[2 - (int)alignment];
		public static string GetSymbol (Classification clssf) => classSymbols[(int)clssf];
		public static string GetSymbol (Threat threat) => threatSymbols[(int)threat];

		public static void SetAllStates (Widget widget, Gdk.Color color) {
			widget.ModifyFg(StateType.Normal, color);
			widget.ModifyFg(StateType.Prelight, color);
			widget.ModifyFg(StateType.Selected, color);
			widget.ModifyFg(StateType.Active, color);
			widget.ModifyFg(StateType.Insensitive, color);
		}

	}

}