﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {


	public sealed class RatingsListField : Bin {

		public RatingsListField (PropertyInfo property, object obj, Context context, object arg) {

			RatingsProfile profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 0);

			if (context.vertical) {
				Frame frame = new Frame(TextTools.ToReadable(property.Name));
				VBox box = new VBox(false, 4) { BorderWidth = 5 };
				frame.Add(box);
				alignment.Add(frame);

				for (int i = 1; i <= 8; i++) {
					if (o_vals[0, i] != Ratings.O_NULL) {
						Label ratingLabel = new Label(TextTools.PrintRating(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel);
					}
				}

				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {

						Label wrapperLabel = new Label(TextTools.PrintRating(k + 8, values[k, 0]));
						wrapperLabel.SetAlignment(0, 0);

						VBox ratingBox = new VBox(false, 5) { BorderWidth = 5 };
						Frame ratingFrame = new Frame { LabelWidget = wrapperLabel, Child = ratingBox };

						for (int i = 1; i <= 8; i++) {
							if (o_vals[k, i] != Ratings.O_NULL) {
								Label ratingLabel = new Label(TextTools.PrintRating(i, values[k, i]));
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
													  + TextTools.PrintRating(i, values[0, i]));
						ratingLabel.SetAlignment(0, 0);
						box.PackStart(ratingLabel, false, false, 0);
					}
				}

				for (int k = 1; k <= 3; k++) {
					if (o_vals[k, 0] != Ratings.O_NULL) {

						Label ratingLabel = new Label((box.Children.Length > 0 ? ", " : "") //Commas to delimit ratings
													  + TextTools.PrintRating(k + 8, values[k, 0], true));
						ratingLabel.SetAlignment(0, 0);

						List<String> subratings = new List<String>();
						for (int i = 1; i <= 8; i++)
							if (o_vals[k, i] != Ratings.O_NULL)
								subratings.Add(TextTools.PrintRating(i, values[k, i]));
						ratingLabel.TooltipText = String.Join("\n", subratings);

						box.PackStart(ratingLabel, false, false, 0);

					}
				}
			}

			if (UIFactory.CurrentlyEditable(property, obj)) {
				ClickableEventBox clickableEventBox = new ClickableEventBox { Child = alignment };
				clickableEventBox.DoubleClicked += (o, a) => new RatingsEditorDialog((Parahuman)obj, (Window)Toplevel);
				Add(clickableEventBox);
			}else {
				Add(alignment);
			}

		}
	}

	public sealed class RatingsEditorDialog : DefocusableWindow {

		Parahuman parahuman;

		TextView editBox;
		Button confirmButton;
		Label errorLabel;

		int timeoutCountdown;

		public RatingsEditorDialog (Parahuman p, Window transientFor) {

			parahuman = p;

			//Setup window
			Title = "Edit ratings of " + parahuman.name;
			SetSizeRequest(300, 300);
			SetPosition(WindowPosition.Center);
			TransientFor = transientFor;
			TypeHint = Gdk.WindowTypeHint.Dialog;

			VBox mainBox = new VBox();

			editBox = new TextView();
			editBox.Buffer.Text = TextTools.PrintRatings(parahuman.baseRatings.values, parahuman.baseRatings.o_vals);
			mainBox.PackStart(editBox, true, true, 0);
			editBox.SetBorderWindowSize(TextWindowType.Top, 10);

			HBox confirmBox = new HBox();
			confirmButton = new Button("Confirm");
			confirmButton.Clicked += AttemptConfirm;
			confirmBox.PackEnd(confirmButton, false, false, 0);
			mainBox.PackStart(confirmBox, false, false, 0);

			errorLabel = new Label("Syntax error");
			confirmBox.PackStart(errorLabel, false, false, 5);

			Add(mainBox);

			ShowAll();
			errorLabel.Hide();

		}

		void AttemptConfirm (object obj, EventArgs args) {
			if (TextTools.TryParseRatings(editBox.Buffer.Text, out RatingsProfile? newRatings)) {
				parahuman.baseRatings = (RatingsProfile)newRatings;
				DependencyManager.Flag(parahuman);
				DependencyManager.TriggerAllFlags();
				this.Destroy();
			} else {
				confirmButton.State = StateType.Insensitive;
				confirmButton.Clicked -= AttemptConfirm;
				errorLabel.Show();
				timeoutCountdown = 8;
				GLib.Timeout.Add(5, new GLib.TimeoutHandler(Shake));
				GLib.Timeout.Add(2000, new GLib.TimeoutHandler(RestoreFunctionalty));
			}
		}

		bool Shake () {
			if (timeoutCountdown > 0) {
				if ((((timeoutCountdown) / 2) % 2) == 0) {
					editBox.LeftMargin += 3;
				} else {
					editBox.LeftMargin -= 3;
				}
				timeoutCountdown--;
				return true;
			} else {
				return false;
			}
		}

		bool RestoreFunctionalty () {
			confirmButton.Sensitive = true;
			errorLabel.Hide();
			confirmButton.Clicked += AttemptConfirm;
			return false;
		}

	}

}