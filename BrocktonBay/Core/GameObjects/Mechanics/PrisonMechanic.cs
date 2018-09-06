using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {
	public sealed class PrisonMechanic : Mechanic, IContainer, IAffiliated {

		public IAgent affiliation { get => parent.affiliation; }

		public override string effect {
			get {
				string output = "";
				foreach (Parahuman prisoner in prisoners)
					output += " " + prisoner.ID.ToString();
				return output.TrimStart(' ');
			}
			set {
				prisoners = new List<Parahuman>();
				List<string> ids = new List<string>(value.Split(' '));
				if (ids.Count == 1 && ids[0] == "") return;
				foreach (string id in ids) {
					Parahuman parahuman = Game.city.Get<Parahuman>(int.Parse(id));
					prisoners.Add(parahuman);
					(parent.affiliation as Faction).assignedCaptures.Add(parahuman);
					GetPrisonerMechanic(parahuman).prison = this;
				}
			}
		}

		public override InvocationTrigger trigger { get => InvocationTrigger.EventPhase; }

		[Displayable(3, typeof(CellTabularListField<Parahuman>), 3, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Parahuman> prisoners { get; set; }

		public PrisonMechanic (MechanicData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (Context context, object obj) {
			Faction faction = parent.affiliation as Faction;
			if (faction == null) return null;
			foreach (Parahuman prisoner in prisoners)
				faction.resources -= (int)prisoner.threat + 1;
			return null;
		}

		public bool Accepts (object obj) => GameObject.TryCast(obj, out Parahuman p) && GetPrisonerMechanic(p).imprisoners == parent.affiliation;
		public bool Contains (object obj) => GameObject.TryCast(obj, out Parahuman p) && prisoners.Contains(p);
		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (List<T> objs) {
			Faction faction = parent.affiliation as Faction;
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				PrisonerMechanic prisoner = GetPrisonerMechanic(parahuman);
				if (prisoner.prison != null)
					prisoner.prison.Remove(prisoner);
				prisoners.Add(parahuman);
				prisoner.prison = this;
				faction.assignedCaptures.Add(parahuman);
				faction.unassignedCaptures.Remove(parahuman);
				DependencyManager.Connect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(parent);
		}
		public void RemoveRange<T> (List<T> objs) {
			Faction faction = parent.affiliation as Faction;
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				PrisonerMechanic prisoner = GetPrisonerMechanic(parahuman);
				prisoners.Remove(parahuman);
				prisoner.prison = null;
				faction.assignedCaptures.Remove(parahuman);
				faction.unassignedCaptures.Add(parahuman);
				DependencyManager.Disconnect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(parent);
		}

		PrisonerMechanic GetPrisonerMechanic (Parahuman parahuman)
			=> (PrisonerMechanic)parahuman.mechanics.Find((m) => m is PrisonerMechanic);

		public override Widget GetCellContents (Context context) {

			bool editable = UIFactory.EditAuthorized(this, "prisoners");

			//Creates the cell contents
			VBox prisonersBox = new VBox(false, 0) { BorderWidth = 3 };
			foreach (Parahuman prisoner in prisoners) {
				InspectableBox header = (InspectableBox)prisoner.GetHeader(context.butCompact);
				if (editable)
					MyDragDrop.SetFailAction(header, delegate {
						Remove(prisoner);
						DependencyManager.TriggerAllFlags();
					});
				prisonersBox.PackStart(header, false, false, 0);
			}

			if (editable) {
				//Set up dropping
				EventBox eventBox = new EventBox { Child = prisonersBox, VisibleWindow = false };
				MyDragDrop.DestSet(eventBox, "Parahuman");
				MyDragDrop.DestSetDropAction(eventBox, delegate {
					if (Accepts(MyDragDrop.currentDragged)) {
						Add(MyDragDrop.currentDragged);
						DependencyManager.TriggerAllFlags();
					}
				});
				return new Gtk.Alignment(0, 0, 1, 0) { Child = eventBox, BorderWidth = 7 };
			} else {
				prisonersBox.BorderWidth += 7;
				return prisonersBox;
			}
		}

	}

	public sealed class PrisonerMechanic : Mechanic, IAffiliated {

		public override string effect {
			get => imprisonerID.ToString();
			set => imprisonerID = int.Parse(value);
		}
		public override InvocationTrigger trigger { get => InvocationTrigger.None; }

		int imprisonerID;
		Faction _imprisoners;
		[Displayable(3, typeof(ObjectField), forceHorizontal = true)]
		public Faction imprisoners {
			get {
				if (_imprisoners == null) _imprisoners = Game.city.Get<Faction>(imprisonerID);
				return _imprisoners;
			}
			set {
				_imprisoners = value;
				imprisonerID = value.ID;
			}
		}

		[Displayable(4, typeof(ObjectField), forceHorizontal = true, overrideLabel = "Prison")]
		public Structure prisonStructure { get => prison == null ? null : prison.parent as Structure; }
		public PrisonMechanic prison;

		public IAgent affiliation { get => imprisoners; }

		[Displayable(5, typeof(ActionField), verticalOnly = true, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind,
					 topPadding = 20, bottomPadding = 10, leftPadding = 20, rightPadding = 20)]
		public GameAction release { get; set; }

		[Displayable(6, typeof(ActionField), verticalOnly = true, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind,
					 topPadding = 10, bottomPadding = 20, leftPadding = 20, rightPadding = 20)]
		public GameAction execute { get; set; }

		public PrisonerMechanic (MechanicData data) : base(data) {
			effect = data.effect;
			release = new GameAction {
				name = "Release",
				description = "Release " + name + (prisonStructure == null ? "" : (" from " + prisonStructure.name + ".")),
				action = delegate (Context context) {
					((Parahuman)parent).health = Health.Healthy;
					DependencyManager.Flag(parent);
					DependencyManager.Flag(prison);
					if (prison != null) prison.Remove(parent);
					imprisoners.unassignedCaptures.Remove((Parahuman)parent);
					DependencyManager.Delete(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "release")
			};
			execute = new GameAction {
				name = "Execute",
				description = "Kill " + name + " in cold blood. This is an S-Class action.",
				action = delegate (Context context) {
					((Parahuman)parent).health = Health.Deceased;
					DependencyManager.Flag(parent);
					DependencyManager.Flag(prison);
					if (prison != null) prison.Remove(parent);
					imprisoners.unassignedCaptures.Remove((Parahuman)parent);
					DependencyManager.Delete(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "execute")
			};
		}

		public override object Invoke (Context context, object obj) {
			throw new System.NotImplementedException();
		}

		public override Widget GetCellContents (Context context) {
			VBox vBox = new VBox();
			vBox.PackStart(UIFactory.Align(new Label("Captured by:"), 0, 0), false, false, 0);
			vBox.PackStart(imprisoners.GetHeader(context.butCompact));
			if (prisonStructure != null) {
				vBox.PackStart(UIFactory.Align(new Label("Held at:"), 0, 0), false, false, 0);
				vBox.PackStart(prisonStructure.GetHeader(context.butCompact));
			}
			vBox.BorderWidth = 10;
			return vBox;
		}

	}

}
