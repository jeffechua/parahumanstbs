using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {

	public sealed class WeaknessMechanic : Mechanic {

		public RatingsProfile difference;

		public override string effect {
			get {
				return Ratings.PrintCompact(difference.values, difference.o_vals);
			}
			set {
				if (Ratings.TryParseCompact(value, out RatingsProfile? ratings)) {
					difference = (RatingsProfile)ratings;
				} else {
					throw new ArgumentException();
				}
			}
		}
		public override InvocationTrigger trigger { get { return InvocationTrigger.GetRatings; } }

		public WeaknessMechanic (MechanicData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (Context context, object obj) {
			if (Known(context) && context.agent != parent.affiliation)
				return (RatingsProfile)obj * difference;
			return obj;
		}

	}

	public sealed class TrueFormMechanic : Mechanic {

		public RatingsProfile trueform { get; set; }
		public Func<Context, RatingsProfile> GetTrueForm { get => (context) => trueform; }

		public override string effect {
			get {
				return "True form:\n" + Ratings.Print(trueform.values, trueform.o_vals);
			}
			set {
				if (value.Length == 0) {
					trueform = RatingsProfile.Null;
				} else {
					value = value.Substring(11);
					if (Ratings.TryParse(value, out RatingsProfile? ratings)) {
						trueform = (RatingsProfile)ratings;
					} else {
						throw new ArgumentException();
					}
				}
			}
		}
		public override InvocationTrigger trigger { get { return InvocationTrigger.GetRatings; } }

		public TrueFormMechanic (MechanicData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (Context context, object obj) {
			if (Known(context) && context.agent != parent.affiliation)
				return trueform;
			return obj;
		}

		public override Widget GetCellContents (Context context) {
			HBox hBox = new HBox(false, 2) { BorderWidth = 5 };
			hBox.PackStart(new Label { Angle = 90, Markup = "<small>True Form</small>", UseMarkup = true }, false, false, 0);
			Widget ratings = new RatingsListField(GetType().GetProperty("GetTrueForm"), this, context.butCompact, "trueform");
			hBox.PackStart(ratings, false, false, 0);
			return hBox;
		}

	}

}
