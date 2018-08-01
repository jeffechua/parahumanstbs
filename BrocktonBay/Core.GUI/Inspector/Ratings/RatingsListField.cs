using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {


	public sealed class RatingsListField : Gtk.Alignment {

		public RatingsListField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute) : base(0, 0, 1, 1) {

			RatingsProfile profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 0);

			if (context.vertical && !context.compact) {

				Frame frame = new Frame(UIFactory.ToReadable(property.Name));
				VBox box = new VBox(false, 4) { BorderWidth = 5 };
				frame.Add(box);
				alignment.Add(frame);

				for (int i = 1; i <= 8; i++) {
					if (o_vals[0, i] != Ratings.O_NULL) {
						Label ratingLabel = new Label(Ratings.PrintSingle(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel);
					}
				}

				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {

						Label wrapperLabel = new Label(Ratings.PrintSingle(k + 8, values[k, 0]));
						wrapperLabel.SetAlignment(0, 0);

						VBox ratingBox = new VBox(false, 5) { BorderWidth = 5 };
						Frame ratingFrame = new Frame { LabelWidget = wrapperLabel, Child = ratingBox };

						for (int i = 1; i <= 8; i++) {
							if (o_vals[k, i] != Ratings.O_NULL) {
								Label ratingLabel = new Label(Ratings.PrintSingle(i, values[k, i]));
								ratingLabel.SetAlignment(0, 0);
								ratingBox.PackStart(ratingLabel, false, false, 0);
							}
						}

						box.PackStart(ratingFrame);

					}
				}

			} else {
				Box box;
				if (context.compact) {
					box = new VBox(false, 0) { BorderWidth = 5 };
				} else {
					box = new HBox(false, 0) { BorderWidth = 5 };
				}
				alignment.Add(box);
				for (int i = 1; i <= 8; i++) {
					if (o_vals[0, i] != Ratings.O_NULL) {
						bool comma = !context.compact && box.Children.Length > 0;
						Label ratingLabel = new Label((comma ? ", " : "") //Commas to delimit ratings
													  + Ratings.PrintSingle(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel, false, false, 0);
					}
				}
				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {
						bool comma = !context.compact && box.Children.Length > 0;
						Label ratingLabel = new Label((comma ? ", " : "") //Commas to delimit ratings
													  + Ratings.PrintSingle(k + 8, values[k, 0], true));
						ratingLabel.SetAlignment(0, 0);
						List<String> subratings = new List<String>();
						for (int i = 1; i <= 8; i++)
							if (o_vals[k, i] != Ratings.O_NULL)
								subratings.Add(Ratings.PrintSingle(i, values[k, i]));
						ratingLabel.TooltipText = String.Join("\n", subratings);
						box.PackStart(ratingLabel, false, false, 0);
					}
				}

			}
			if (UIFactory.CurrentlyEditable(property, obj)) {
				ClickableEventBox clickableEventBox = new ClickableEventBox { Child = alignment };
				clickableEventBox.DoubleClicked += delegate {
					// The property this is attached to gets the *current ratings*, not the *base ratings*, which are
					// what we logically want to let the user manipulate. Hence, the optional arg supplied is the name
					// of the base profile.
					PropertyInfo baseProfileProperty = obj.GetType().GetProperty((string)attribute.arg);
					TextEditingDialog dialog = new TextEditingDialog(
						"Edit ratings",
						(Window)Toplevel,
						delegate {
							RatingsProfile baseProfile = (RatingsProfile)baseProfileProperty.GetValue(obj);
							return Ratings.Print(baseProfile.values, baseProfile.o_vals);
						},
						delegate (string input) {
							if (Ratings.TryParse(input, out RatingsProfile? newRatings)) {
								baseProfileProperty.SetValue(obj, (RatingsProfile)newRatings);
								IDependable dependable = obj as IDependable;
								if (dependable != null) {
									DependencyManager.Flag(dependable);
									DependencyManager.TriggerAllFlags();
								}
								return true;
							}
							return false;
						}
					);
				};
				Add(clickableEventBox);
			} else {
				Add(alignment);
			}
		}
	}

}