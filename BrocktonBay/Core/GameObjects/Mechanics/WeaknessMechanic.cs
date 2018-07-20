using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {
	public class WeaknessMechanic : Mechanic {

		public RatingsProfile difference;

		public override string effect {
			get {
				return Ratings.PrintRatings(difference.values, difference.o_vals);
			}
			set {
				if (Ratings.TryParseRatings(value, out RatingsProfile? ratings)) {
					difference = (RatingsProfile)ratings;
				} else {
					throw new ArgumentException();
				}
			}
		}
		public override InvocationTrigger trigger { get { return InvocationTrigger.GetRatings; } }

		public WeaknessMechanic (MechanicData data) : base(data) {
			if (Ratings.TryParseRatings(data.effect, out RatingsProfile? ratings)) {
				difference = (RatingsProfile)ratings;
			} else {
				difference = new RatingsProfile(Ratings.ALL_NULL);
			}
		}

		public override object Invoke (Context context, object obj) {
			if (Known(context)) {
				return (RatingsProfile)obj * difference;
			} else {
				return obj;
			}
		}

		public override Widget GetCellContents (Context context) {

			if (!Known(context)) return base.GetCellContents(context);

			VBox ratingsBox = new VBox(false, 0) { BorderWidth = 5 };
			float[,] values = difference.values;

			for (int i = 1; i <= 8; i++) {
				if (difference.o_vals[0, i] != Ratings.O_NULL) {
					Label ratingLabel = new Label(Ratings.PrintRating(i, values[0, i]));
					ratingLabel.SetAlignment(0, 0);
					ratingsBox.PackStart(ratingLabel, false, false, 0);
				}
			}

			for (int k = 1; k <= 3; k++) {
				if (difference.o_vals[k, 0] != Ratings.O_NULL) {

					Label ratingLabel = new Label(Ratings.PrintRating(k + 8, values[k, 0], true));
					ratingLabel.SetAlignment(0, 0);

					List<String> subratings = new List<String>();
					for (int i = 1; i <= 8; i++)
						if (difference.o_vals[k, i] != Ratings.O_NULL)
							subratings.Add(Ratings.PrintRating(i, values[k, i]));
					ratingLabel.TooltipText = String.Join("\n", subratings);

					ratingsBox.PackStart(ratingLabel, false, false, 0);

				}
			}

			ClickableEventBox clickableEventBox = new ClickableEventBox { BorderWidth = 5 };
			clickableEventBox.Add(ratingsBox);
			clickableEventBox.DoubleClicked += (o, a) => new TextEditingDialog(
				"Edit weakness of " + parent.name,
				(Window)clickableEventBox.Toplevel,
				() => Ratings.PrintRatings(difference.values, difference.o_vals),
				delegate (string input) {
					if (Ratings.TryParseRatings(input, out RatingsProfile? newRatings)) {
						difference = (RatingsProfile)newRatings;
						DependencyManager.Flag(this);
						DependencyManager.TriggerAllFlags();
						return true;
					}
					return false;
				}
			);
			return UIFactory.Align(clickableEventBox, 0, 0, 1f, 0);

		}

	}
}
