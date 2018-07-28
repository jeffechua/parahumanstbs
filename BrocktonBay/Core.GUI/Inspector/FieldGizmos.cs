using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public sealed class Banner : Label {
		public Banner (PropertyInfo property, object obj, Context context, object arg) {
			SetAlignment(0.5f, 0.5f);
			Justify = Justification.Center;
			UseMarkup = true;
			Markup = (string)property.GetValue(obj);
		}
	}

	public sealed class TabularContainerField : Table {
		List<PropertyInfo> children;
		Context context;

		public TabularContainerField (PropertyInfo property, object obj, Context context, object arg) : base(1, 1, false) {

			// arg ought to be a string[] containing the names of child properties. Here we turn it into a list and
			// use ConvertAll() to obtain the list of actual PropertyInfos.
			children = new List<object>((object[])arg).ConvertAll((input) => obj.GetType().GetProperty((string)input));
			this.context = context;

			ColumnSpacing = 5;
			RowSpacing = 2;

			Label label = new Label(UIFactory.ToReadable(property.Name) + ": ");

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				label.HasTooltip = true;
				label.TooltipMarkup = tooltipText.text;
			}

			Attach(label, 0, 1, 0, (uint)(2 * children.Count - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);
			Attach(new VSeparator(), 1, 2, 0, (uint)(2 * children.Count - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);

			for (int i = 0; i < children.Count; i++) {
				DisplayableAttribute displayInfo = (DisplayableAttribute)children[i].GetCustomAttribute(typeof(DisplayableAttribute));
				ConstructorInfo childConstructor = displayInfo.widget.GetConstructor(UIFactory.constructorSignature);
				Widget childWidget = (Widget)childConstructor.Invoke(new object[] {
					children[i],
					obj,
					context,
					displayInfo.arg
				});
				if (childWidget is LabelOverridable) {
					ChildAttribute childAttribute = (ChildAttribute)children[i].GetCustomAttribute(typeof(ChildAttribute));
					((LabelOverridable)childWidget).OverrideLabel(childAttribute.name);
				}
				Attach(childWidget, 2, 3, 2 * (uint)i, 2 * (uint)i + 1);
				if (i != children.Count - 1) Attach(new HSeparator(), 2, 3, 2 * (uint)i + 1, 2 * (uint)i + 2);
			}

		}

	}

	public sealed class LinearContainerField : HBox {

		List<PropertyInfo> children;
		Context context;

		public LinearContainerField (PropertyInfo property, object obj, Context context, object arg) {

			children = new List<object>((object[])arg).ConvertAll((input) => obj.GetType().GetProperty((string)input));
			this.context = context;

			Label label = new Label(UIFactory.ToReadable(property.Name) + ": ");

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				label.HasTooltip = true;
				label.TooltipMarkup = tooltipText.text;
			}

			PackStart(label, false, false, 0);

			for (int i = 0; i < children.Count; i++) {
				DisplayableAttribute displayInfo = (DisplayableAttribute)children[i].GetCustomAttribute(typeof(DisplayableAttribute));
				ConstructorInfo childConstructor = displayInfo.widget.GetConstructor(UIFactory.constructorSignature);
				Widget childWidget = (Widget)childConstructor.Invoke(new object[] {
					children[i],
					obj,
					context.butCompact,
					displayInfo.arg
				});
				if (childWidget is LabelOverridable) {
					ChildAttribute childAttribute = (ChildAttribute)children[i].GetCustomAttribute(typeof(ChildAttribute));
					((LabelOverridable)childWidget).OverrideLabel(childAttribute.name);
				}
				PackStart(childWidget, false, false, 0);
				if (i != children.Count - 1) PackStart(new Label("/"), false, false, 0);
			}

		}

	}


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
		public override string ToString () {
			return label + ": " + value;
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

		public void SetValues (params float[] values) {
			for (int i = 0; i < values.Length; i++)
				terms[i].value = values[i];
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
	public sealed class ExpressionField : Gtk.Alignment, LabelOverridable {

		PropertyInfo property;
		object obj;
		Expression exp;
		Context context;
		string title;

		public ExpressionField (PropertyInfo property, object obj, Context context, object arg) : base(0, 0, 1, 1) {

			this.property = property;
			this.obj = obj;
			exp = (Expression)property.GetValue(obj);
			this.context = context;
			title = UIFactory.ToReadable(property.Name);

			Label label = new Label();
			label.UseMarkup = true;
			label.Justify = Justification.Right;
			label.SetAlignment(0, 0);

			//Multi-line expressions are put in an expander with the label text being the formattedResult of the expression.
			if (exp.text.Contains("\n")) {
				Expander expander = new Expander(title + ": " + exp.formattedResult);
				expander.Activated += (o, a) => ReloadLabel();
				label.Markup = exp.ToString();
				expander.Add(new Gtk.Alignment(0, 0, 1, 1) { Child = label, LeftPadding = 10, BottomPadding = 5 });
				Add(expander);
			} else {
				label.Markup = title + ": " + exp;
				Add(label);
			}

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

		}

		public void OverrideLabel (string newLabel) {
			title = newLabel;
			ReloadLabel();
		}

		//The formattedResult label is not shown when the expander is expanded. This implements that functionality.
		public void ReloadLabel () {
			if (Child is Expander) {
				if (((Expander)Child).Expanded) {
					((Expander)Child).Label = title;
				} else {
					((Expander)Child).Label = title + ": " + exp.formattedResult;
				}
			} else {
				((Label)Child).Markup = title + ": " + exp;
			}
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

			if ((bool)arg != false) {
				RowSpacing = 2;
				Label title = new Label("[ " + UIFactory.ToReadable(property.Name) + " ]");
				Attach(title, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 2);
			}

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
				Graphics.SetAllBg(frame, fractions[i].color);

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
			this.obj = (IGUIComplete)obj;
			this.context = context;

			Label label = new Label(UIFactory.ToReadable(property.Name) + ": ");
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

		public ActionField (PropertyInfo property, object obj, Context context, object arg) : base(new Gtk.Alignment(0, 0, 1, 1)) {
			action = (GameAction)property.GetValue(obj);
			Gtk.Alignment alignment = (Gtk.Alignment)Child;
			alignment.Add(new Label(action.name));
			alignment.BorderWidth = 10;
			TooltipText = action.description;
			Sensitive = action.condition(context);
			Clicked += (o, a) => action.action(context);
		}

	}

}