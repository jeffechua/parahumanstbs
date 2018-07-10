using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {


	public sealed class RatingsListField : ClickableEventBox {

		public RatingsListField (PropertyInfo property, object obj, Context context, object arg) {

			RatingsProfile profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 0);
			Add(alignment);

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

			DoubleClicked += (o, a) => new RatingsEditorDialog((Parahuman)obj, (Window)Toplevel);
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


	public sealed class RatingsTable : Table {


		public static readonly string[] deploymentRows = { " Ξ ", " ʤ ", " ς ", " χ ", "  sum" };

		public static readonly string[] multiplierExplain = {
			"",
			"±10% / Mover lvl\n−10% / Striker lvl",   //Brute
			"±10% / Stranger lvl\n−10% / Shaker lvl", //Blaster
			"−10% / Brute lvl",                     //Shaker
			"−10% / Blaster lvl",                   //Striker
			"No multiplier",                      //Mover
			"No multiplier",                      //Stranger
			"No multiplier",                      //Thinker
			"No multiplier"                       //Trump
		};
		public static readonly string[] metamultiplierExplain = {
			"−10% / Trump lvl",                      //Base
			"−10% / Striker lvl",                   //Tinker
			"−10% / Shaker lvl",                    //Master
			"Nullifies all multipliers"           //Breaker
		};

		public RatingsTable (Context context, RatingsProfile profile, float[] multipliers = null, float[] metamultipliers = null) : base(7, 10, false) {

			float[,] values = profile.values;
			int[,] o_vals = profile.o_vals;

			ColumnSpacing = 5;
			RowSpacing = 5;
			BorderWidth = 10;

			Attach(new HSeparator(), 0, 7, 1, 2);
			Attach(new VSeparator(), 1, 2, 0, 10);

			//Row labels and multipliers
			for (uint i = 1; i <= 8; i++) {
				Label classLabel = new Label(" " + Graphics.classSymbols[i]) {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				Attach(classLabel, 0, 1, i + 1, i + 2);
			}
			//Fill in columns, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(deploymentRows[i]);
				rowLabel.SetAlignment(0.5f, 1f);
				Attach(rowLabel, i + 2, i + 3, 0, 1);
				//Numbers
				for (uint j = 1; j <= 8; j++) {
					Label numberLabel = new Label();
					if (o_vals[i, j] == Ratings.O_NULL) {
						numberLabel.Text = "-";
					} else {
						numberLabel.Text = values[i, j].ToString("0.0");
					}
					numberLabel.SetAlignment(1, 1);
					Attach(numberLabel, i + 2, i + 3, j + 1, j + 2);
				}
			}

			if (multipliers != null && metamultipliers != null) {
				//Row multipliers
				for (uint i = 1; i <= 8; i++) {
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small> ×" + multipliers[i].ToString("0.00") + "</small>",
						HasTooltip = true,
						TooltipText = multiplierExplain[i]
					};
					multiplier.SetAlignment(1, 0);
					Attach(multiplier, 7, 8, i + 1, i + 2);
				}

				//Column metamultipliers
				for (uint i = 0; i < 4; i++) {
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small>  ×" + metamultipliers[i].ToString("0.00") + ((i == 3) ? ", fix" : "") + "</small>",
						HasTooltip = true,
						TooltipText = metamultiplierExplain[i],
						Angle = -90
					};
					multiplier.SetAlignment(0, 0);
					Attach(multiplier, i + 2, i + 3, 10, 11);
				}
			}

		}

	}

}