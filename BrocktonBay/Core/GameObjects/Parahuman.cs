using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public enum Health {
		Deceased = 0,
		Down = 1,
		Injured = 2,
		Healthy = 3,
		Captured = 4
	}

	public sealed class ParahumanData {
		public string name = "New Trigger";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public Threat threat = Threat.C;
		public Health health = Health.Healthy;
		public int reputation = 0;
		public List<MechanicData> mechanics = new List<MechanicData>();
		public int[,] ratings = new int[5, 9];

		public ParahumanData () { }

		public ParahumanData (Parahuman parahuman) {
			name = parahuman.name;
			ID = parahuman.ID;
			alignment = parahuman.alignment;
			threat = parahuman.threat;
			health = parahuman.health;
			reputation = parahuman.reputation;
			mechanics = parahuman.traits.ConvertAll((input) => new MechanicData(input));
			ratings = parahuman.baseRatings.o_vals;
		}

	}

	public sealed partial class Parahuman : GameObject, IRated, IAgent {

		public override int order { get { return 1; } }
		public Gdk.Color color { get { return new Gdk.Color(0, 0, 0); } }
		public Dossier knowledge { get; set; }
		bool _active;
		[Displayable(3, typeof(BasicReadonlyField), visiblePhases = Phase.None)]
		public bool active {
			get => _active;
			set {
				if (value) {
					if (!Game.city.activeAgents.Contains(this)) Game.city.activeAgents.Add(this);
					if (knowledge == null) knowledge = new Dossier();
				} else {
					Game.city.activeAgents.Remove(this);
					knowledge = null;
				}
				_active = value;
			}
		}

		[Displayable(4, typeof(ObjectField), forceHorizontal = true)]
		public override IAgent affiliation {
			get {
				if (parent != null) {
					if (parent.parent != null) {
						return (IAgent)parent.parent;
					} else {
						return (IAgent)parent;
					}
				}
				return this;
			}
		}

		[Displayable(5, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(6, typeof(EnumField<Threat>))]
		public Threat threat { get; set; }

		[Displayable(7, typeof(EnumField<Health>))]
		public Health health { get; set; }

		[Displayable(8, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(9, typeof(MechanicCellTabularListField), 2, emphasized = true, verticalOnly = true)]
		public override List<Trait> traits { get; set; }

		[Displayable(10, typeof(RatingsListField), "baseRatings", emphasizedIfHorizontal = true, topPadding = 5, bottomPadding = 5)]
		public Func<Context, RatingsProfile> ratings { get => GetRatingsProfile; }

		public RatingsProfile baseRatings { get; set; }

		public Parahuman () : this(new ParahumanData())
			=> active = true;

		public Parahuman (ParahumanData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			threat = data.threat;
			health = data.health;
			reputation = data.reputation;
			traits = data.mechanics.ConvertAll((input) => Trait.Load(input));
			foreach (Trait mechanic in traits) {
				DependencyManager.Connect(mechanic, this);
				mechanic.parent = this;
			}
			baseRatings = new RatingsProfile(data.ratings);
		}

		public override void Reload () { }

		public RatingsProfile GetRatingsProfile (Context context) {
			RatingsProfile ratingsProfile = baseRatings;
			foreach (Trait mechanic in traits) {
				if (mechanic.trigger == InvocationTrigger.GetRatings) {
					ratingsProfile = (RatingsProfile)mechanic.Invoke(context, ratingsProfile);
				}
			}
			return ratingsProfile;
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				Label nameLabel = new Label(name);
				header.PackStart(nameLabel, false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, Graphics.GetColor(alignment), Graphics.textSize),
								 false, false, (uint)(Graphics.textSize / 5));
				switch (health) {
					case Health.Injured:
						Graphics.SetAllFg(nameLabel, Graphics.GetColor(Health.Injured));
						break;
					case Health.Down:
						Graphics.SetAllFg(nameLabel, Graphics.GetColor(Health.Down));
						break;
					case Health.Deceased:
						nameLabel.UseMarkup = true;
						nameLabel.Markup = "<s>" + name + "</s>";
						break;
					case Health.Captured:
						nameLabel.State = StateType.Insensitive;
						return new InspectableBox(header, this);
				}
				return new InspectableBox(header, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this);
				Gtk.Alignment align = UIFactory.Align(namebox, 0.5f, 0.5f, 0, 0);
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null) {
					HBox row2 = new HBox(false, 0);
					row2.PackStart(new Label(), true, true, 0);
					row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent), false, false, 0);
					if (parent.parent != null) {
						row2.PackStart(new VSeparator(), false, false, 10);
						row2.PackStart(Graphics.GetSmartHeader(context.butCompact, parent.parent), false, false, 0);
					}
					row2.PackStart(new Label(), true, true, 0);
					headerBox.PackStart(row2);
				}
				return headerBox;
			}
		}

		public override Widget GetCellContents (Context context) {
			RatingsListField field = (RatingsListField)UIFactory.Fabricate(this, "ratings", context.butCompact);
			field.BorderWidth = 5;
			return field;
		}

		public override bool Accepts (object obj) => obj is Trait;
		public override bool Contains (object obj) => traits.Contains((Trait)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Trait mechanic = (Trait)obj;
				traits.Add(mechanic);
				mechanic.parent = this;
				DependencyManager.Connect(mechanic, this);
			}
			traits.Sort((a, b) => a.secrecy.CompareTo(b.secrecy));
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Trait mechanic = (Trait)obj;
				traits.Remove(mechanic);
				mechanic.parent = null;
				DependencyManager.DisconnectAll(mechanic);
			}
			DependencyManager.Flag(this);
		}

	}

}