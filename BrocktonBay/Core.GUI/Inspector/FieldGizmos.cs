using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	// A pair consisting of a string and a float. Two functions:
	//      - It is used to hold numbers associated with words, e.g. {"Strength", 0} to indicate a strength of 0.
	//      - It is used to hold numbers and their preferred ToString argument for printing. Mainly used in Expression and ExpressionField.
	public sealed class FormattedFloat {
		public float value;
		public string format;
		public FormattedFloat (float val, string str) {
			this.value = val;
			this.format = str;
		}
		public override String ToString () {
			return value.ToString(format);
		}
	}

	public sealed class LabeledValue<T> where T : IConvertible {
		public string label;
		public T value;
		public LabeledValue (string label, T value) {
			this.label = label;
			this.value = value;
		}
	}

	// Essentially, a mathematical expression of a calculation or equality, e.g. "54 + 6 - 5 / 2 = 22".
	public sealed class Expression {

		public string text;
		public List<FormattedFloat> terms;

		//The last term is assumed to be the "result" of the calculation. This is purely for convenience.
		public float result { get { return terms[terms.Count - 1].value; } }
		public string formattedResult { get { return terms[terms.Count - 1].ToString(); } }

		//t is the textual part of the expression with the values represented by @x, e.g. "@0 + @1 / @2 = @3".
		//f is the string formats of each value, e.g. {"0.0", "P1", "P0", "0.000"}.
		//The values of the expressions are manually assigned. To assign the number for @2, write to terms[2].val
		public Expression (string text, params string[] formatting) {
			this.text = text;
			terms = new List<FormattedFloat>();
			for (int i = 0; i < formatting.Length; i++) {
				terms.Add(new FormattedFloat(0, formatting[i]));
			}
		}

		//Obtains the expression for 
		public override string ToString () {
			string parsedText = text;
			for (int i = 0; i < terms.Count - 1; i++) {
				parsedText = parsedText.Replace("@" + i, terms[i].ToString());
			}
			parsedText = parsedText.Replace("@" + (terms.Count - 1), "<b>" + terms[terms.Count - 1].ToString() + "</b>");
			return parsedText;
		}
	}

	//The graphical implementation of Expression in the UI.
	public sealed class ExpressionField : Gtk.Alignment {

		PropertyInfo property;
		object obj;
		Expression exp;
		Context context;

		public ExpressionField (PropertyInfo property, object obj, Context context, object arg) : base(0, 0, 1, 1) {

			this.property = property;
			this.obj = obj;
			exp = (Expression)property.GetValue(obj);
			this.context = context;

			Label label = new Label();
			label.UseMarkup = true;
			label.Justify = Justification.Right;
			label.SetAlignment(0, 0);

			//Multi-line expressions are put in an expander with the label text being the formattedResult of the expression.
			if (exp.text.Contains("\n")) {
				Expander expander = new Expander(TextTools.ToReadable(property.Name) + ": " + exp.formattedResult);
				expander.Activated += OnToggled;
				label.Markup = exp.ToString();
				expander.Add(new Gtk.Alignment(0, 0, 1, 1) { Child = label, LeftPadding = 10, BottomPadding = 5 });
				Add(expander);
			} else {
				label.Markup = TextTools.ToReadable(property.Name) + ": " + exp.ToString();
				Add(label);
			}

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

		}

		//The formattedResult label is not shown when the expander is expanded. This implements that functionality.
		public void OnToggled (object obj, EventArgs args) {
			if (((Expander)obj).Expanded) {
				((Expander)obj).Label = TextTools.ToReadable(property.Name);
			} else {
				((Expander)obj).Label = TextTools.ToReadable(property.Name) + ": " + exp.formattedResult;
			}
		}

	}

	/*  Renders an array of StringFloatPairs in a tabular manner:
	 *                  | <string 1>: <value 1>
	 *  <property name> | <string 2>: <value 2>
	 *                  | <string 3>: <value 3>
	 */
	public sealed class TabularLabeledValuesField<T> : Table where T : IConvertible {

		LabeledValue<T>[] pairs;
		Context context;

		public TabularLabeledValuesField (PropertyInfo property, object obj, Context context, object arg) : base(1, 1, false) {

			pairs = (LabeledValue<T>[])property.GetValue(obj);
			this.context = context;

			ColumnSpacing = 5;
			RowSpacing = 2;

			Label label = new Label(TextTools.ToReadable(property.Name) + ": ");

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			Attach(label, 0, 1, 0, (uint)(2 * pairs.Length - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);
			Attach(new VSeparator(), 1, 2, 0, (uint)(2 * pairs.Length - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);

			for (uint i = 0; i < pairs.Length; i++) {
				Attach(new LabeledValuesArrayElementField<T>(property, obj, context, null, (int)i, "G6"), 2, 3, 2 * i, 2 * i + 1);
				if (i != pairs.Length - 1) Attach(new HSeparator(), 2, 3, 2 * i + 1, 2 * i + 2);
			}


		}

	}

	// Renders an array of StringFloatPairs in a single line.
	// <property name>: <val1>/<val2>/<val3>
	// e.g. Spent XP: 0/2/4
	public sealed class LinearLabeledValuesField<T> : TextEditableField where T : IConvertible {

		LabeledValue<T>[] array;

		public LinearLabeledValuesField (PropertyInfo property, object obj, Context context, object arg) : base(property, obj, context, arg) { }

		protected override string GetValueAsString () {
			if (array == null) array = (LabeledValue<T>[])property.GetValue(obj);
			string text = "";
			for (int i = 0; i < array.Length; i++) {
				if (i != 0) text += "/";
				text += array[i].value.ToString();
			}
			return text;
		}

		protected override void SetValueFromString (string text) {
			string[] fragments = text.Split('/');
			for (int i = 0; i < fragments.Length && i < array.Length; i++)
				if (float.TryParse(fragments[i], out float newVal))
					array[i].value = (T)Convert.ChangeType(newVal, typeof(T));
		}

	}

	// This inherits from PrimitiveField for the assignment functionality but pretty much doensn't actually use reflection at all.
	// It intercepts the GetValueAsString and SetValueFromString methods, and instead of piping them to property.GetValue() and property.SetValue() like it's supposed to,
	// just directly assigns them into the array element that it extracted from the property beforehand.
	public sealed class LabeledValuesArrayElementField<T> : TextEditableField where T : IConvertible {

		int index;
		string format;
		LabeledValue<T> labeledValue;

		//The "true" suppresses Reload() at the end of the base constructor, allowing us to define "target" before Reload()ing manually. Otherwise, GetValueAsString() will fail.
		public LabeledValuesArrayElementField (PropertyInfo property, object obj, Context context, object arg, int index, string format) : base(property, obj, context, arg, true) {
			this.index = index;
			this.format = format;
			labeledValue = ((LabeledValue<T>[])property.GetValue(obj))[index];
			OverrideLabel(labeledValue.label + ": ");
			Reload();
		}

		protected override string GetValueAsString ()
			=> labeledValue.value.ToString();

		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				labeledValue.value = (T)Convert.ChangeType(newVal, typeof(T));
		}

	}

	//Used in FractionsBar.
	public struct Fraction {
		public string name;
		public float val;
		public Gdk.Color color;
		public Fraction (string name, float val, Gdk.Color color) {
			this.name = name;
			this.val = val;
			if (val > 1) val = 1;
			if (val < 0) val = 0;
			this.color = color;
		}
	}

	// Try not to touch this unless you have a really good grasp of Gtk's alignment and size systems.
	// Making this was painful.
	// The table is used to allow 'Gtk.Alignment's to be superimposed on each other. The arguments for Alignment initialization are used to position each fraction.
	public sealed class FractionsBar : Table {

		public Fraction[] fractions;
		public Context context;

		public FractionsBar (PropertyInfo property, object obj, Context context, object arg) : base(1, 3, false) {

			fractions = (Fraction[])property.GetValue(obj);
			this.context = context;

			RowSpacing = 2;
			Label title = new Label("[ " + TextTools.ToReadable(property.Name) + " ]");
			Attach(title, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 2);

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			float leftspace = 0;
			for (int i = 0; i < fractions.Length; i++) {

				float spaceAlloc;
				if (fractions[i].val > 0.99) {
					spaceAlloc = 0;
				} else if (fractions[i].val < 0.01) {
					leftspace += fractions[i].val;
					continue;
				} else {
					spaceAlloc = leftspace / (1 - fractions[i].val);
					//xalign = % of free space allocated to the left = (free space to left)/(total free space).
					//Conditional eliminates division by zero issue
				}

				Frame frame = new Frame();
				Graphics.SetAllFg(frame, fractions[i].color);

				Gtk.Alignment alignment = new Gtk.Alignment(spaceAlloc, 0, fractions[i].val, 1);
				alignment.Add(frame);
				frame.SetSizeRequest(0, -1);

				Attach(alignment, 0, 1, 1, 2);

				//A bunch of logic to try and make sure the label fits in the box.
				if (fractions[i].val > 0.1 || fractions.Length <= 2) {
					if (fractions[i].val <= 0.2) {
						frame.Child = new Label(fractions[i].val.ToString("P0"));
					} else {
						frame.Child = new Label(fractions[i].val.ToString("P1"));
					}
					frame.Child.SetSizeRequest(0, -1);
					Label label = new Label { UseMarkup = true, Markup = "<small>" + fractions[i].name + "</small>" };
					label.SetAlignment(spaceAlloc, 0);
					label.SetSizeRequest(0, -1);
					Attach(label, 0, 1, 2, 3);
				}

				leftspace += fractions[i].val;

			}

		}

	}

	public class ColorField : HBox {

		ClickableEventBox colorButton;
		PropertyInfo property;
		object obj;
		Context context;

		public ColorField (PropertyInfo property, object obj, Context context, object arg) {

			this.property = property;
			this.obj = (GUIComplete)obj;
			this.context = context;

			Label label = new Label(TextTools.ToReadable(property.Name) + ": ");
			PackStart(label, false, false, 0);

			colorButton = new ClickableEventBox { VisibleWindow = true, BorderWidth = 1 };
			Graphics.SetAllBg(colorButton, (Gdk.Color)property.GetValue(obj));
			PackStart(colorButton, false, false, 0);

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			colorButton.SetSizeRequest(0, 0);

			SizeAllocated += InitializeDisplay;
			colorButton.DoubleClicked += OpenPicker;
		}

		public void InitializeDisplay (object obj, SizeAllocatedArgs args) {
			SizeAllocated -= InitializeDisplay;
			colorButton.SetSizeRequest(args.Allocation.Height, 0);
		}

		public void OpenPicker (object eventBox, ButtonPressEventArgs args) {
			ColorSelectionDialog dialog = new ColorSelectionDialog("Pick new color for faction.");
			dialog.ColorSelection.PreviousColor = dialog.ColorSelection.CurrentColor = (Gdk.Color)property.GetValue(obj);
			dialog.Response += delegate (object o, ResponseArgs response) {
				if (response.ResponseId == ResponseType.Ok) {
					property.SetValue(obj, dialog.ColorSelection.CurrentColor);
					DependencyManager.Flag((IDependable)obj);
					DependencyManager.TriggerAllFlags();
				}
			};
			dialog.Run();
			dialog.Destroy();
		}

	}

	public sealed class ActionField : Button {
		public GameAction action;

		public ActionField (PropertyInfo property, object obj, Context context, object arg) : base(new Gtk.Alignment(0,0,1,1)) {
			action = (GameAction)property.GetValue(obj);
			Gtk.Alignment alignment = (Gtk.Alignment)Child;
			alignment.Add(new Label(action.name));
			alignment.BorderWidth = 10;
			TooltipText = action.description;
			Sensitive = action.condition(context);
			Clicked += (o, a) => action.action();
		}

	}

}