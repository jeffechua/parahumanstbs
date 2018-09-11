using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public enum Status {
		Healthy = 0,
		Resting = 1,
		Injured = 2,
		Down = 3,
		Deceased = 4,
		Captured = -1
	}

	public sealed class ParahumanData {
		public string name = "New Trigger";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public Threat threat = Threat.C;
		public Status status = Status.Healthy;
		public int reputation = 0;
		public List<TraitData> mechanics = new List<TraitData>();
		public int[,] ratings = new int[5, 9];

		public ParahumanData () { }

		public ParahumanData (Parahuman parahuman) {
			name = parahuman.name;
			ID = parahuman.ID;
			alignment = parahuman.alignment;
			threat = parahuman.threat;
			status = parahuman.status;
			reputation = parahuman.reputation;
			mechanics = parahuman.traits.ConvertAll((input) => new TraitData(input));
			ratings = parahuman.baseRatings.o_vals;
		}

	}

	public sealed partial class Parahuman : GameObject, IRated, IAgent {

		public override bool isEngaged => base.isEngaged || status == Status.Captured;

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

		[Displayable(7, typeof(EnumField<Status>))]
		public Status status { get; set; }

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
			status = data.status;
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
			foreach (Trait mechanic in traits)
				if (mechanic.trigger.Contains(EffectTrigger.GetRatings))
					ratingsProfile = (RatingsProfile)mechanic.Invoke(EffectTrigger.GetRatings, context, ratingsProfile);
			return ratingsProfile;
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				Label nameLabel = new Label(name);
				if (status > Status.Healthy && status < Status.Deceased) {
					Graphics.SetAllFg(nameLabel, Graphics.GetColor(status));
				} else if (status == Status.Deceased) { // Deceased
					nameLabel.UseMarkup = true;
					nameLabel.Markup = "<s>" + name + "</s>";
				} else if (status == Status.Captured) {
					nameLabel.Sensitive = false;
				}
				Gdk.Color color = Graphics.GetColor(alignment);
				if (status != Status.Healthy) {
					Gtk.Global.RgbToHsv(((double)color.Red) / 65535, ((double)color.Green) / 65535, ((double)color.Blue) / 65535,
										out double h, out double s, out double v);
					s /= 3; v = (1 + v) / 2;
					Gtk.HSV.ToRgb(h, s, v, out double r, out double g, out double b);
					color = new Gdk.Color((byte)(ushort)(r * 255), (byte)(ushort)(g * 255), (byte)(ushort)(b * 255));
				}
				header.PackStart(nameLabel, false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, color, Graphics.textSize), false, false, (uint)(Graphics.textSize / 5));
				return new InspectableBox(header, this, context);
			} else {
				VBox headerBox = new VBox(false, 5);
				InspectableBox namebox = new InspectableBox(new Label(name), this, context);
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

		public override void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget) {
			if (member is Trait && UIFactory.EditAuthorized(this, "traits")) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateRemoveButton(this, member));
			}
		}

		public override bool Accepts (object obj) => obj is Trait;
		public override bool Contains (object obj) => traits.Contains((Trait)obj);

		public override void AddRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Trait trait = (Trait)obj;
				traits.Add(trait);
				trait.parent = this;
				DependencyManager.Flag(trait);
				DependencyManager.Connect(trait, this);
			}
			traits.Sort((a, b) => a.secrecy.CompareTo(b.secrecy));
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (IEnumerable<T> objs) {
			foreach (object obj in objs) {
				Trait trait = (Trait)obj;
				traits.Remove(trait);
				trait.parent = null;
				DependencyManager.Destroy(trait);
				DependencyManager.Disconnect(trait, this);
			}
			DependencyManager.Flag(this);
		}

	}

}