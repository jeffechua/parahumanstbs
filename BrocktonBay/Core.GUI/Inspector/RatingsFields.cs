using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core.GUI {


	public sealed class RatingListField : ClickableEventBox {

		List<Rating> ratings;
		public bool vertical;

		public RatingListField (PropertyInfo property, object obj, bool vert, object arg) {

			ratings = (List<Rating>)property.GetValue(obj);
			vertical = vert;

			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 0);
			Add(alignment);

			if (vertical) {
				Frame frame = new Frame(TextTools.ToReadable(property.Name));
				VBox box = new VBox(false, 4) { BorderWidth = 5 };
				frame.Add(box);
				alignment.Add(frame);

				for (int i = 0; i < ratings.Count; i++) {
					box.PackStart(new RatingListFieldElement(ratings, i, true));
				}
			} else {
				HBox box = new HBox(false, 0) { BorderWidth = 5 };
				alignment.Add(box);
				for (int i = 0; i < ratings.Count; i++) {
					if (i != 0)
						box.PackStart(new Label(", "), false, false, 0);
					box.PackStart(new RatingListFieldElement(ratings, i, false), false, false, 0);
				}
			}

			DoubleClicked += (o, a) => new RatingsEditorDialog((Parahuman)obj);
		}
	}

	public sealed class RatingsEditorDialog : DefocusableWindow {

		Parahuman parahuman;

		TextView editBox;
		Button confirmButton;
		Label errorLabel;

		int timeoutCountdown;

		public RatingsEditorDialog (Parahuman p) {
			
			parahuman = p;

			//Setup window
			Title = "Edit ratings of " + parahuman.name;
			SetSizeRequest(300, 300);
			SetPosition(WindowPosition.Center);
			TransientFor = (Window)Inspector.main.Toplevel;
			TypeHint = Gdk.WindowTypeHint.Dialog;

			VBox mainBox = new VBox();

			editBox = new TextView();
			editBox.Buffer.Text = TextTools.PrintRatings(parahuman.ratings);
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
			if (TextTools.TryParseRatings(editBox.Buffer.Text, out List<Rating> newRatings)) {
				parahuman.ratings = newRatings;
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

	public sealed class RatingListFieldElement : Gtk.Alignment {

		public List<Rating> parentList;
		public int index;
		public bool expanded;

		public RatingListFieldElement (List<Rating> ratings, int ind, bool e = true) : base(0, 0, 1, 0) {

			parentList = ratings;
			index = ind;
			expanded = e;

			Rating rating = ratings[ind];

			Label ratingLabel = new Label(rating.ToString(!expanded));
			ratingLabel.SetAlignment(0, 0);

			if (rating.subratings.Count > 0) {
				if (expanded) {
					VBox ratingBox = new VBox(false, 5) { BorderWidth = 5 };
					Frame ratingFrame = new Frame { LabelWidget = ratingLabel, Child = ratingBox };
					for (int i = 0; i < rating.subratings.Count; i++) {
						RatingListFieldElement subrating = new RatingListFieldElement(rating.subratings, i, false);
						ratingBox.PackStart(subrating, false, false, 0);
					}
					Add(ratingFrame);
				} else {
					String[] subratings = new string[rating.subratings.Count];
					for (int j = 0; j < subratings.Length; j++) {
						subratings[j] = rating.subratings[j].ToString();
					}
					ratingLabel.TooltipText = String.Join("\n", subratings);
					Add(ratingLabel);
				}
			} else {
				Add(ratingLabel);
			}
		}

	}

	public sealed class RatingsSumField : Expander {

		public float[,] ratings;
		public bool vertical;

		public RatingsSumField (PropertyInfo property, object obj, bool vert, object arg) : base(TextTools.ToReadable(property.Name)) {

			ratings = (float[,])property.GetValue(obj);
			vertical = vert;

			Expanded = (bool)arg;

			Table table = new Table(7, 10, false) {
				ColumnSpacing = 5,
				RowSpacing = 5,
				BorderWidth = 10
			};

			table.Attach(new HSeparator(), 0, 10, 1, 2);
			table.Attach(new VSeparator(), 1, 2, 0, 7);
			//Column labels and multipliers
			for (uint i = 0; i < 8; i++) {
				Label classLabel = new Label(" " + EnumTools.classSymbols[i] + " ") {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				table.Attach(classLabel, i + 2, i + 3, 0, 1);
			}
			//Fill in rows, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(TextTools.deploymentRows[i]);
				rowLabel.SetAlignment(1, 0);
				table.Attach(rowLabel, 0, 1, i + 2, i + 3);
				//Numbers
				for (uint j = 0; j < 8; j++) {
					Label numberLabel = new Label(ratings[i, j].ToString());
					numberLabel.SetAlignment(1, 1);
					table.Attach(numberLabel, j + 2, j + 3, i + 2, i + 3);
				}
			}

			Add(table);

		}

	}

	public sealed class RatingsComparisonField : Expander {

		public RatingsComparison comparison;
		public bool compact;

		public RatingsComparisonField (PropertyInfo property, object obj, bool comp, object arg) : base(TextTools.ToReadable(property.Name)) {

			comparison = (RatingsComparison)property.GetValue(obj);
			compact = comp;

			Expanded = (bool)arg;

			Notebook notebook = new Notebook {
				TabPos = PositionType.Right
			};
			Table table0 = new Table(9, 11, false) {
				ColumnSpacing = 5,
				RowSpacing = 5,
				BorderWidth = 10
			};
			Table table1 = new Table(9, 11, false) {
				ColumnSpacing = 5,
				RowSpacing = 5,
				BorderWidth = 10
			};
			Table table2 = new Table(9, 11, false) {
				ColumnSpacing = 5,
				RowSpacing = 5,
				BorderWidth = 10
			};
			notebook.AppendPage(new Gtk.Alignment(0, 0, 0, 0) { Child = table0 }, new Label("Raw"));
			notebook.AppendPage(new Gtk.Alignment(0, 0, 0, 0) { Child = table1 }, new Label("Mid"));
			notebook.AppendPage(new Gtk.Alignment(0, 0, 0, 0) { Child = table2 }, new Label("Final"));
			Add(notebook);

			//Raw ratings table
			table0.Attach(new HSeparator(), 0, 10, 1, 2);
			table0.Attach(new VSeparator(), 1, 2, 0, 7);
			//Column labels and multipliers
			for (uint i = 0; i < 8; i++) {
				Label classLabel = new Label(" " + EnumTools.classSymbols[i] + " ") {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				table0.Attach(classLabel, i + 2, i + 3, 0, 1);
			}
			//Fill in rows, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(TextTools.deploymentRows[i]);
				rowLabel.SetAlignment(1, 0);
				table0.Attach(rowLabel, 0, 1, i + 2, i + 3);
				//Numbers
				for (uint j = 0; j < 8; j++) {
					Label numberLabel = new Label(comparison.values[0][i, j].ToString());
					numberLabel.SetAlignment(1, 1);
					table0.Attach(numberLabel, j + 2, j + 3, i + 2, i + 3);
				}
			}

			//Thought ratings table
			table1.Attach(new HSeparator(), 0, 10, 1, 2);
			table1.Attach(new VSeparator(), 1, 2, 0, 7);
			//Column labels and multipliers
			for (uint i = 0; i < 8; i++) {
				Label classLabel = new Label(" " + EnumTools.classSymbols[i] + " ") {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				table1.Attach(classLabel, i + 2, i + 3, 0, 1);
				if (i == 4 || i == 5) {
					Label deduction = new Label {
						UseMarkup = true,
						Markup = "<small> − " + (comparison.values[1][4, i] - comparison.values[0][4, i]).ToString("0.0") + "</small>",
						HasTooltip = true,
						TooltipText = "−1 / Thinker lvl between Mover, Stranger",
						Angle = -90
					};
					deduction.SetAlignment(1, 0);
					table1.Attach(deduction, i + 2, i + 3, 7, 8);
				}
			}
			//Fill in rows, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(TextTools.deploymentRows[i]);
				rowLabel.SetAlignment(1, 0);
				table1.Attach(rowLabel, 0, 1, i + 2, i + 3);
				//Numbers
				for (uint j = 0; j < 8; j++) {
					Label numberLabel = new Label(comparison.values[1][i, j].ToString());
					numberLabel.SetAlignment(1, 1);
					table1.Attach(numberLabel, j + 2, j + 3, i + 2, i + 3);
				}
			}

			//Final ratings table
			table2.Attach(new HSeparator(), 0, 10, 1, 2);
			table2.Attach(new VSeparator(), 1, 2, 0, 7);
			//Column labels and multipliers
			for (uint i = 0; i < 8; i++) {
				Label classLabel = new Label(" " + EnumTools.classSymbols[i] + " ") {
					HasTooltip = true,
					TooltipText = Enum.GetName(typeof(Classification), i)
				};
				table2.Attach(classLabel, i + 2, i + 3, 0, 1);
				if (i < 4) {
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small> ×" + comparison.multipliers[i].ToString("0.00") + "</small>",
						HasTooltip = true,
						TooltipText = TextTools.multiplierExplain[i],
						Angle = -90
					};
					multiplier.SetAlignment(1, 0);
					table2.Attach(multiplier, i + 2, i + 3, 7, 8);
				}
			}
			//Fill in rows, including labels, numbers and multipliers
			for (uint i = 0; i < 5; i++) {
				//Row label
				Label rowLabel = new Label(TextTools.deploymentRows[i]);
				rowLabel.SetAlignment(1, 0);
				table2.Attach(rowLabel, 0, 1, i + 2, i + 3);
				//Numbers
				for (uint j = 0; j < 8; j++) {
					Label numberLabel = new Label(comparison.values[2][i, j].ToString("F0"));
					numberLabel.SetAlignment(1, 1);
					table2.Attach(numberLabel, j + 2, j + 3, i + 2, i + 3);
				}
				if (i < 4) {
					//Row multiplier
					Label multiplier = new Label {
						UseMarkup = true,
						Markup = "<small>  ×" + ((i == 3) ? " fix" : comparison.metamultipliers[i].ToString("0.00")) + "</small>",
						HasTooltip = true,
						TooltipText = TextTools.metamultiplierExplain[i]
					};
					multiplier.SetAlignment(0, 0);
					table2.Attach(multiplier, 10, 11, i + 2, i + 3);
				}
			}
		}
	}

}