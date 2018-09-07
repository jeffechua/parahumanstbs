using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace BrocktonBay {
	public sealed class PrisonTrait : Trait, IContainer, IAffiliated {

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
					GetPrisonerTrait(parahuman).prison = this;
				}
			}
		}

		public override EffectTrigger trigger { get => EffectTrigger.EventPhase; }

		[Displayable(3, typeof(CellTabularListField<Parahuman>), 3, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Parahuman> prisoners { get; set; }

		public PrisonTrait (MechanicData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (Context context, object obj) {
			Faction faction = parent.affiliation as Faction;
			if (faction == null) return null;
			foreach (Parahuman prisoner in prisoners)
				faction.resources -= (int)prisoner.threat + 1;
			return null;
		}

		public override void OnTriggerDestroyed (IDependable trigger) {
			if (Contains(trigger)) {
				Remove(trigger);
				DependencyManager.Flag(this);
			}
		}

		public override void OnListenerDestroyed (IDependable listener) {
			if(listener == parent){
				RemoveRange(prisoners);
				DependencyManager.Destroy(this);
			}
		}

		public bool Accepts (object obj) => GameObject.TryCast(obj, out Parahuman p) && TryGetPrisonerTrait(p, out PrisonerTrait trait) && trait.imprisoners == parent.affiliation;
		public bool Contains (object obj) => GameObject.TryCast(obj, out Parahuman p) && prisoners.Contains(p);
		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (List<T> objs) {
			Faction faction = parent.affiliation as Faction;
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				PrisonerTrait prisoner = GetPrisonerTrait(parahuman);
				if (prisoner.prison != null)
					prisoner.prison.Remove(prisoner);
				prisoners.Add(parahuman);
				prisoner.prison = this;
				faction.assignedCaptures.Add(parahuman);
				faction.unassignedCaptures.Remove(parahuman);
				DependencyManager.Connect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(this);
		}
		public void RemoveRange<T> (List<T> objs) {
			Faction faction = parent.affiliation as Faction;
			foreach (object obj in objs) {
				Parahuman parahuman = (Parahuman)obj;
				PrisonerTrait prisoner = GetPrisonerTrait(parahuman);
				prisoners.Remove(parahuman);
				prisoner.prison = null;
				faction.assignedCaptures.Remove(parahuman);
				faction.unassignedCaptures.Add(parahuman);
				DependencyManager.Disconnect(parahuman, this);
				DependencyManager.Flag(parahuman);
			}
			DependencyManager.Flag(this);
		}

		PrisonerTrait GetPrisonerTrait (Parahuman parahuman)
			=> (PrisonerTrait)parahuman.traits.Find((m) => m is PrisonerTrait);

		bool TryGetPrisonerTrait (Parahuman parahuman, out PrisonerTrait trait) {
			trait = (PrisonerTrait)parahuman.traits.Find((m) => m is PrisonerTrait);
			return trait != null;
		}

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

	public sealed class PrisonerTrait : Trait, IAffiliated {

		public override string effect {
			get => imprisonerID.ToString();
			set => imprisonerID = int.Parse(value);
		}
		public override EffectTrigger trigger { get => EffectTrigger.None; }

		int imprisonerID;
		Faction _imprisoners;
		[Displayable(3, typeof(ObjectField), emphasized = true, forceHorizontal = true)]
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

		[Displayable(4, typeof(ObjectField), emphasized = true, forceHorizontal = true, overrideLabel = "Prison")]
		public Structure prisonStructure { get => prison == null ? null : prison.parent as Structure; }
		public PrisonTrait prison;

		public IAgent affiliation { get => imprisoners; }

		[Displayable(5, typeof(BasicHContainerField), "move", "release", "execute")]
		public GameAction[] actions { get => new GameAction[] { move, release, execute }; }

		[ChildDisplayable("move", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction move { get; set; }

		[ChildDisplayable("release", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction release { get; set; }

		[ChildDisplayable("execute", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction execute { get; set; }

		public PrisonerTrait (MechanicData data) : base(data) {
			effect = data.effect;
			move = new GameAction {
				name = "Assign",
				description = "Assign this captive to a prison of your choice.",
				action = delegate (Context context) {
					SelectorDialog selector = new SelectorDialog(
						"Choose a prison.",
						(obj) => (obj.affiliation == imprisoners) && obj is Structure && (obj.traits.Find((trait) => trait is PrisonTrait) != null),
						delegate (GameObject obj) {
							PrisonTrait chosenPrison = (PrisonTrait)obj.traits.Find((trait) => trait is PrisonTrait);
							chosenPrison.Add(parent);
							DependencyManager.TriggerAllFlags();
						}
					);
				},
				condition = (context) => UIFactory.EditAuthorized(this, "move")
			};
			release = new GameAction {
				name = "Release",
				description = "Release this captive" + (prisonStructure == null ? "." : (" from " + prisonStructure.name + ".")),
				action = delegate (Context context) {
					((Parahuman)parent).health = Health.Healthy;
					if (prison != null) prison.Remove(parent);
					imprisoners.unassignedCaptures.Remove((Parahuman)parent);
					DependencyManager.Destroy(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "release")
			};
			execute = new GameAction {
				name = "Execute [S]",
				description = "Kill this captive in cold blood. This is an S-Class action.",
				action = delegate (Context context) {
					((Parahuman)parent).health = Health.Deceased;
					if (prison != null) prison.Remove(parent);
					imprisoners.unassignedCaptures.Remove((Parahuman)parent);
					DependencyManager.Destroy(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "execute")
			};
		}

		public override void Reload () {
			if (move.name == "Move" && prison == null) {
				move = new GameAction {
					name = "Assign",
					description = "Assign this captive to a prison of your choice.",
					action = move.action,
					condition = move.condition
				};
			}
			if (move.name == "Assign" && prison != null) {
				move = new GameAction {
					name = "Move",
					description = "Move this captive to a prison of your choice.",
					action = move.action,
					condition = move.condition
				};
			}
		}

		public override object Invoke (Context context, object obj) {
			throw new System.NotImplementedException();
		}

		public override Widget GetCellContents (Context context) {
			VBox vBox = new VBox();
			vBox.PackStart(UIFactory.Align(new Label("Captured by:"), 0, 0), false, false, 0);
			vBox.PackStart(imprisoners.GetHeader(context.butCompact));
			if (prisonStructure != null) {
				vBox.PackStart(new Label() { HeightRequest = 3 });
				vBox.PackStart(UIFactory.Align(new Label("Held at:"), 0, 0), false, false, 0);
				vBox.PackStart(prisonStructure.GetHeader(context.butCompact));
			}
			vBox.PackStart(new Label() { HeightRequest = 5 });
			if (UIFactory.EditAuthorized(this, "move")) {
				ActionField action = (ActionField)UIFactory.Fabricate(this, "move", new Context(Game.player, vBox));
				((Gtk.Alignment)action.Child).BorderWidth = 1;
				vBox.PackStart(action, false, false, 0);
			}
			vBox.BorderWidth = 10;
			return vBox;
		}

	}

}
