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
					PrisonerTrait prisonerTrait = GetPrisonerTrait(Game.city.Get<Parahuman>(int.Parse(id)));
					prisoners.Add(prisonerTrait.parahuman);
					prisonerTrait.imprisoners.assignedCaptures.Add(prisonerTrait.parahuman);
					prisonerTrait.prison = this;
				}
			}
		}

		public override List<EffectTrigger> trigger { get => new List<EffectTrigger> { EffectTrigger.EventPhase }; }

		[Displayable(3, typeof(CellTabularListField<Parahuman>), 3, emphasized = true, editablePhases = Phase.Mastermind)]
		public List<Parahuman> prisoners { get; set; }

		public PrisonTrait (TraitData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (EffectTrigger trigger, Context context, object obj) {
			Faction faction = parent.affiliation as Faction;
			if (faction == null) return null;
			foreach (Parahuman prisoner in prisoners)
				faction.resources -= (int)prisoner.threat + 1;
			return null;
		}

		public override void OnTriggerDestroyed (IDependable trigger) {
			PrisonerTrait trait = (PrisonerTrait)trigger;
			if (Contains(trait.parahuman)) {
				trait.Release();
				DependencyManager.Flag(this);
			}
		}

		public override void OnListenerDestroyed (IDependable listener) {
			if (listener == parent) {
				RemoveRange(prisoners.ToArray());
				DependencyManager.Destroy(this);
			}
		}

		public bool Accepts (object obj) {
			if (GameObject.TryCast(obj, out Parahuman parahuman)) {
				return TryGetPrisonerTrait(parahuman, out PrisonerTrait trait) && trait.imprisoners == parent.affiliation;
			} else if (GameObject.TryCast(obj, out PrisonerTrait trait)) {
				return trait.imprisoners == parent.affiliation;
			} else {
				return false;
			}
		}
		public bool Contains (object obj) {
			if (GameObject.TryCast(obj, out Parahuman parahuman)) {
				return prisoners.Contains(parahuman);
			} else if (GameObject.TryCast(obj, out PrisonerTrait trait)) {
				return prisoners.Find((prisoner) => GetPrisonerTrait(prisoner) == trait) != null;
			} else {
				return false;
			}
		}
		public void Add (object obj) => AddRange(new List<object> { obj });
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public void AddRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				PrisonerTrait prisonerTrait = obj is Parahuman ? GetPrisonerTrait((Parahuman)obj) : (PrisonerTrait)obj;
				if (prisonerTrait.prison != null)
					prisonerTrait.prison.Remove(prisonerTrait);
				prisoners.Add(prisonerTrait.parahuman);
				prisonerTrait.prison = this;
				prisonerTrait.imprisoners.assignedCaptures.Add(prisonerTrait.parahuman);
				prisonerTrait.imprisoners.unassignedCaptures.Remove(prisonerTrait.parahuman);
				DependencyManager.Connect(prisonerTrait, this);
				DependencyManager.Flag(prisonerTrait);
			}
			DependencyManager.Flag(this);
		}
		public void RemoveRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				PrisonerTrait prisonerTrait = obj is Parahuman ? GetPrisonerTrait((Parahuman)obj) : (PrisonerTrait)obj;
				prisoners.Remove(prisonerTrait.parahuman);
				prisonerTrait.prison = null;
				prisonerTrait.imprisoners.assignedCaptures.Remove(prisonerTrait.parahuman);
				prisonerTrait.imprisoners.unassignedCaptures.Add(prisonerTrait.parahuman);
				DependencyManager.Disconnect(prisonerTrait, this);
				DependencyManager.Flag(prisonerTrait);
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
				InspectableBox header = (InspectableBox)prisoner.GetHeader(context.butInUIContext(this));
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
		public override List<EffectTrigger> trigger { get => new List<EffectTrigger> { EffectTrigger.GetRightClickMenu }; }

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

		//So that it can clean up after it's unparented.
		public Parahuman parahuman;
		public override GameObject parent {
			get => _parent;
			set {
				if (value != null)
					parahuman = (Parahuman)value;
				_parent = value;
			}
		}
		GameObject _parent;

		public IAgent affiliation { get => imprisoners; }

		[Displayable(5, typeof(BasicHContainerField), "move", "release", "execute")]
		public GameAction[] actions { get => new GameAction[] { move, release, execute }; }

		[ChildDisplayable("move", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction move { get; set; }

		[ChildDisplayable("release", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction release { get; set; }

		[ChildDisplayable("execute", typeof(ActionField), 5, visiblePhases = Phase.Mastermind, editablePhases = Phase.Mastermind)]
		public GameAction execute { get; set; }

		public PrisonerTrait (TraitData data) : base(data) {
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
							chosenPrison.Add(parahuman);
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
					Release();
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "release")
			};
			execute = new GameAction {
				name = "Execute [S]",
				description = "Kill this captive in cold blood. This is an S-Class action.",
				action = delegate (Context context) {
					parahuman.health = Health.Deceased;
					if (prison != null) prison.Remove(parahuman);
					imprisoners.unassignedCaptures.Remove(parahuman);
					DependencyManager.Destroy(this);
					DependencyManager.TriggerAllFlags();
				},
				condition = (context) => UIFactory.EditAuthorized(this, "execute")
			};
		}

		public void Release () {
			parahuman.health = Health.Healthy;
			if (prison != null) prison.Remove(this);
			imprisoners.unassignedCaptures.Remove(parahuman);
			DependencyManager.Destroy(this);
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

		public override void OnListenerDestroyed (IDependable listener) {
			if (listener == parent) {
				DependencyManager.Destroy(this);
			} else if (listener == prison) {
				prison.Remove(this);
			}
		}

		public override object Invoke (EffectTrigger trigger, Context context, object obj) {
			Menu rightClickMenu = (Menu)obj;
			if (UIFactory.EditAuthorized(this, "move")) {
				MenuItem moveButton = new MenuItem(move.name);
				moveButton.Activated += (o, a) => move.action(context);
				rightClickMenu.Append(moveButton);
				MenuItem releaseButton = new MenuItem("Release");
				releaseButton.Activated += (o, a) => release.action(context);
				rightClickMenu.Append(releaseButton);
				MenuItem executeButton = new MenuItem("Execute [S]");
				executeButton.Activated += (o, a) => execute.action(context);
				rightClickMenu.Append(executeButton);
			}
			return rightClickMenu;
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
				ActionField action = (ActionField)UIFactory.Fabricate(this, "move", new Context(this));
				((Gtk.Alignment)action.Child).BorderWidth = 1;
				vBox.PackStart(action, false, false, 0);
			}
			vBox.BorderWidth = 10;
			return vBox;
		}

	}

}
