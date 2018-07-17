using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {


	public sealed class RatingsListField : Gtk.Alignment {

		public RatingsListField (PropertyInfo property, object obj, Context context, object arg) : base(0, 0, 1, 1) {
			
			RatingsProfile profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 0);

			if (context.vertical) {
				Frame frame = new Frame(UIFactory.ToReadable(property.Name));
				VBox box = new VBox(false, 4) { BorderWidth = 5 };
				frame.Add(box);
				alignment.Add(frame);

				for (int i = 1; i <= 8; i++) {
					if (o_vals[0, i] != Ratings.O_NULL) {
						Label ratingLabel = new Label(Ratings.PrintRating(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel);
					}
				}

				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {

						Label wrapperLabel = new Label(Ratings.PrintRating(k + 8, values[k, 0]));
						wrapperLabel.SetAlignment(0, 0);

						VBox ratingBox = new VBox(false, 5) { BorderWidth = 5 };
						Frame ratingFrame = new Frame { LabelWidget = wrapperLabel, Child = ratingBox };

						for (int i = 1; i <= 8; i++) {
							if (o_vals[k, i] != Ratings.O_NULL) {
								Label ratingLabel = new Label(Ratings.PrintRating(i, values[k, i]));
								ratingLabel.SetAlignment(0, 0);
								ratingBox.PackStart(ratingLabel, false, false, 0);
							}
						}

						box.PackStart(ratingFrame);

					}
				}

			} else {
				HBox box = new HBox(false, 0) { BorderWidth = 5 };
				alignment.Add(box);
				for (int i = 1; i <= 8; i++) {
					if (o_vals[0, i] != Ratings.O_NULL) {
						Label ratingLabel = new Label((box.Children.Length > 0 ? ", " : "") //Commas to delimit ratings
													  + Ratings.PrintRating(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel, false, false, 0);
					}
				}

				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {

						Label ratingLabel = new Label((box.Children.Length > 0 ? ", " : "") //Commas to delimit ratings
													  + Ratings.PrintRating(k + 8, values[k, 0], true));
						ratingLabel.SetAlignment(0, 0);

						List<String> subratings = new List<String>();
						for (int i = 1; i <= 8; i++)
							if (o_vals[k, i] != Ratings.O_NULL)
								subratings.Add(Ratings.PrintRating(i, values[k, i]));
						ratingLabel.TooltipText = String.Join("\n", subratings);

						box.PackStart(ratingLabel, false, false, 0);

					}
				}
			}

			if (UIFactory.CurrentlyEditable(property, obj)) {
				ClickableEventBox clickableEventBox = new ClickableEventBox { Child = alignment };
				Parahuman parahuman = (Parahuman)obj;
				clickableEventBox.DoubleClicked += (o, a) => new TextEditingDialog(
					"Edit ratings of " + parahuman.name,
					(Window)Toplevel,
					() => Ratings.PrintRatings(parahuman.baseRatings.values, parahuman.baseRatings.o_vals),
					delegate (string input) {
						if (Ratings.TryParseRatings(input, out RatingsProfile? newRatings)) {
							parahuman.baseRatings = (RatingsProfile)newRatings;
							DependencyManager.Flag(parahuman);
							DependencyManager.TriggerAllFlags();
							return true;
						}
						return false;
					}
				);
				Add(clickableEventBox);
			} else {
				Add(alignment);
			}

		}
	}

}