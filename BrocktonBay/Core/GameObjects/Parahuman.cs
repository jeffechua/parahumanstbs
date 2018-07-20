using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public enum Health {
		Deceased = 0,
		Down = 1,
		Injured = 2,
		Healthy = 3
	}

	public sealed class ParahumanData {
		public string name = "New Trigger";
		public int ID = 0;
		public string civilianName = "";
		public Alignment alignment = Alignment.Rogue;
		public Threat threat = Threat.C;
		public Health health = Health.Healthy;
		public int reputation = 0;
		public int[,] ratings = new int[5, 9];

		public ParahumanData () { }

		public ParahumanData (Parahuman parahuman) {
			name = parahuman.name;
			ID = parahuman.ID;
			civilianName = null;
			alignment = parahuman.alignment;
			threat = parahuman.threat;
			health = parahuman.health;
			reputation = parahuman.reputation;
			ratings = parahuman.baseRatings.o_vals;
		}

	}

	public sealed class Parahuman : GameObject, IRated, IAgent {

		public override int order { get { return 1; } }
		public Gdk.Color color { get { return new Gdk.Color(0, 0, 0); } }
		public Dossier knowledge { get; set; }
		bool _active;
		[Displayable(2, typeof(BasicReadonlyField)), PlayerInvisible]
		public bool active {
			get => _active;
			set {
				if (value) {
					if (!Game.city.Contains(this)) Game.city.activeAgents.Add(this);
					if (knowledge == null) knowledge = new Dossier();
				} else {
					Game.city.activeAgents.Remove(this);
					knowledge = null;
				}
				_active = value;
			}
		}

		public String civilian_name { get; set; }

		[Displayable(3, typeof(ObjectField)), ForceHorizontal]
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

		[Displayable(4, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(5, typeof(EnumField<Threat>))]
		public Threat threat { get; set; }

		[Displayable(6, typeof(EnumField<Health>))]
		public Health health { get; set; }

		[Displayable(7, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(7, typeof(CellTabularListField<Mechanic>), 3), Emphasized]
		public List<Mechanic> mechanics { get; set; }

		[Displayable(8, typeof(RatingsListField)), Padded(5, 5), EmphasizedIfHorizontal]
		public Func<Context, RatingsProfile> ratings { get { return GetRatingsProfile; } }

		public RatingsProfile baseRatings { get; set; }

		public Parahuman () : this(new ParahumanData()) { }

		public Parahuman (ParahumanData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			threat = data.threat;
			health = data.health;
			reputation = data.reputation;
			mechanics = new List<Mechanic>();
			baseRatings = new RatingsProfile(data.ratings);
		}

		public override void Reload () { }

		public RatingsProfile GetRatingsProfile (Context context) {
			RatingsProfile ratingsProfile = baseRatings;
			foreach (Mechanic mechanic in mechanics) {
				if (mechanic.trigger == InvocationTrigger.GetRatings) {
					ratingsProfile = (RatingsProfile)mechanic.Invoke(context, ratingsProfile);
				}
			}
			return ratingsProfile;
		}

		public override Widget GetHeader (Context context) {
			if (context.compact) {
				HBox header = new HBox(false, 0);
				header.PackStart(new Label(name), false, false, 0);
				header.PackStart(Graphics.GetIcon(threat, Graphics.GetColor(health), Graphics.textSize),
								 false, false, (uint)(Graphics.textSize / 5));
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
			VBox ratingsBox = new VBox(false, 0) { BorderWidth = 5 };
			RatingsProfile profile = ratings(context);
			float[,] cRatings = profile.values;
			for (int i = 1; i <= 8; i++) {
				if (profile.o_vals[0, i] != Ratings.O_NULL) {
					Label ratingLabel = new Label(Ratings.PrintRating(i, cRatings[0, i]));
					ratingLabel.SetAlignment(0, 0);
					ratingsBox.PackStart(ratingLabel, false, false, 0);
				}
			}
			for (int k = 1; k <= 3; k++) {
				if (profile.o_vals[k, 0] != Ratings.O_NULL) {
					Label ratingLabel = new Label(Ratings.PrintRating(k + 8, cRatings[k, 0], true));
					ratingLabel.SetAlignment(0, 0);
					List<String> subratings = new List<String>();
					for (int i = 1; i <= 8; i++)
						if (profile.o_vals[k, i] != Ratings.O_NULL)
							subratings.Add(Ratings.PrintRating(i, cRatings[k, i]));
					ratingLabel.TooltipText = String.Join("\n", subratings);
					ratingsBox.PackStart(ratingLabel, false, false, 0);
				}
			}
			ClickableEventBox clickableEventBox = new ClickableEventBox { BorderWidth = 5 };
			clickableEventBox.Add(ratingsBox);
			clickableEventBox.DoubleClicked += (o, a) => new TextEditingDialog(
				"Edit ratings of " + name,
				(Window)clickableEventBox.Toplevel,
				() => Ratings.PrintRatings(baseRatings.values, baseRatings.o_vals),
				delegate (string input) {
					if (Ratings.TryParseRatings(input, out RatingsProfile? newRatings)) {
						baseRatings = (RatingsProfile)newRatings;
						DependencyManager.Flag(this);
						DependencyManager.TriggerAllFlags();
						return true;
					}
					return false;
				}
			);
			return UIFactory.Align(clickableEventBox, 0, 0, 1f, 0);
		}

		public override bool Accepts (object obj) => obj is Mechanic;
		public override bool Contains (object obj) => mechanics.Contains((Mechanic)obj);

		public override void AddRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Mechanic mechanic = (Mechanic)obj;
				mechanics.Add(mechanic);
				mechanic.parent = this;
				DependencyManager.Connect(mechanic, this);
			}
			mechanics.Sort((a, b) => a.secrecy.CompareTo(b));
			DependencyManager.Flag(this);
		}

		public override void RemoveRange<T> (List<T> objs) {
			foreach (object obj in objs) {
				Mechanic mechanic = (Mechanic)obj;
				mechanics.Remove(mechanic);
				mechanic.parent = null;
				DependencyManager.DisconnectAll(mechanic);
			}
			DependencyManager.Flag(this);
		}

	}

}