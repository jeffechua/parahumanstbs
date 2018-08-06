using System;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace BrocktonBay {
	public class MapAlert : ClickableEventBox {

		public Map map;

		public Widget line;
		public Gtk.Window popup;
		public int size;
		public Vector2 position;

		public IBattleground battleground;

		public bool Relevant (IAgent agent)
			=> battleground.affiliation == agent ||
					   (battleground.attacker != null && battleground.attacker.affiliation == agent) ||
					   (battleground.defender != null && battleground.defender.affiliation == agent);

		public MapAlert (IBattleground battleground, Map map) {

			this.map = map;
			this.battleground = battleground;

			size = battleground is Structure ? StructureMarker.markerSize * 6 / 5 : TerritoryMarker.markerHeight;

			map.stage.Put(this, 0, 0);
			VisibleWindow = false;

			line = new HSeparator();
			line.SetSizeRequest(size * 2, 4);

			EnterNotifyEvent += delegate {
				if (popup != null) popup.Destroy();
				popup = new StructurePopup(this);
				map.stage.Put(line, (int)position.x, (int)position.y - 2);
				line.GdkWindow.Raise();
				line.ShowAll();
			};
			LeaveNotifyEvent += delegate {
				if (popup != null) {
					popup.Destroy();
					popup = null;
				}
				map.stage.Remove(line);
			};

			AlertIconType alertType = battleground.defender == null ? AlertIconType.Unopposed : AlertIconType.Opposed;
			Color primaryColor = battleground.attacker.affiliation.color;
			Color secondaryColor = battleground.defender == null ? primaryColor : battleground.defender.affiliation.color;
			Color trim;
			if (Relevant(Game.player)) {
				trim = new Color(0, 0, 0);
			} else {
				trim = new Color(50, 50, 50);
				primaryColor.Red = (ushort)((primaryColor.Red + 150) / 2);
				primaryColor.Green = (ushort)((primaryColor.Green + 150) / 2);
				primaryColor.Blue = (ushort)((primaryColor.Blue + 150) / 2);
				secondaryColor.Red = (ushort)((secondaryColor.Red + 150) / 2);
				secondaryColor.Green = (ushort)((secondaryColor.Green + 150) / 2);
				secondaryColor.Blue = (ushort)((secondaryColor.Blue + 150) / 2);
			}

			Add(Graphics.GetAlert(alertType, size, primaryColor, secondaryColor, trim));

			if (!Relevant(Game.player))
				return;

			if (Game.phase == Phase.Action || (Game.phase == Phase.Response && battleground.defender == null)) {
				Clicked += delegate(object obj, ButtonPressEventArgs args) {
					if(args.Event.Button!=1)
					Inspector.InspectInNearestInspector(battleground, this);
				}
				return;
			} else if (Game.phase == Phase.Response) {
				
				return;
			} else if (Game.phase == Phase.Mastermind) {
				if (battleground.battle == null) battleground.battle = new Battle(battleground, battleground.attacker, battleground.defender);
				Clicked += delegate {
					SecondaryWindow eventWindow = new SecondaryWindow("Battle at " + battleground.name);
					eventWindow.SetMainWidget(new BattleInterface(battleground.battle));
					eventWindow.ShowAll();
				};
				return;
			}

			throw new Exception("Invalid game phase");

		}
	}
}
