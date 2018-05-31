﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core.GUI {

	// A pair consisting of a string and a float. Two functions:
	//      - It is used to hold numbers associated with words, e.g. {"Strength", 0} to indicate a strength of 0.
	//      - It is used to hold numbers and their preferred ToString argument for printing. Mainly used in Expression and ExpressionField.
	public sealed class StringFloatPair {
		public string str;
		public float val;
		public StringFloatPair (string s, float f) {
			str = s;
			val = f;
		}
		public override String ToString () {
			return val.ToString(str);
		}
	}

	// Essentially, a mathematical expression of a calculation or equality, e.g. "54 + 6 - 5 / 2 = 22".
	public sealed class Expression {

		public string text;
		public List<StringFloatPair> terms;

		//The last term is assumed to be the "result" of the calculation. This is purely for convenience.
		public float result { get { return terms[terms.Count - 1].val; } }
		public string formattedResult { get { return terms[terms.Count - 1].ToString(); } }

		//t is the textual part of the expression with the values represented by @x, e.g. "@0 + @1 / @2 = @3".
		//f is the string formats of each value, e.g. {"0.0", "P1", "P0", "0.000"}.
		//The values of the expressions are manually assigned. To assign the number for @2, write to terms[2].val
		public Expression (string t, params string[] f) {
			text = t;
			terms = new List<StringFloatPair>();
			for (int i = 0; i < f.Length; i++) {
				terms.Add(new StringFloatPair(f[i], 0));
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
		bool vertical;

		public ExpressionField (PropertyInfo p, object o, bool vert, object arg) : base(0, 0, 1, 1) {

			property = p;
			obj = o;
			exp = (Expression)p.GetValue(o);
			vertical = vert;

			Label label = new Label();
			label.UseMarkup = true;
			label.Justify = Justification.Right;
			label.SetAlignment(0, 0);

			//Multi-line expressions are put in an expander with the label text being the formattedResult of the expression.
			if (exp.text.Contains("\n")) {
				Expander expander = new Expander(TextTools.ToReadable(p.Name) + ": " + exp.formattedResult);
				expander.Activated += OnToggled;
				label.Markup = exp.ToString();
				expander.Add(new Gtk.Alignment(0, 0, 1, 1) { Child = label, LeftPadding = 10, BottomPadding = 5 });
				Add(expander);
			} else {
				label.Markup = TextTools.ToReadable(p.Name) + ": " + exp.ToString();
				Add(label);
			}

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)p.GetCustomAttribute(typeof(TooltipTextAttribute));
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
	public sealed class TabularStringFloatPairsField : Table {

		StringFloatPair[] pairs;
		bool vertical;

		public TabularStringFloatPairsField (PropertyInfo p, object o, bool vert, object arg) : base(1, 1, false) {

			pairs = (StringFloatPair[])p.GetValue(o);
			vertical = vert;

			ColumnSpacing = 5;
			RowSpacing = 2;

			Label label = new Label(TextTools.ToReadable(p.Name) + ": ");

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)p.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}

			Attach(label, 0, 1, 0, (uint)(2 * pairs.Length - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);
			Attach(new VSeparator(), 1, 2, 0, (uint)(2 * pairs.Length - 1), AttachOptions.Shrink, AttachOptions.Fill, 0, 0);

			for (uint i = 0; i < pairs.Length; i++) {
				Attach(new StringFloatPairArrayElementField(p, o, !vert, null, (int)i, "G6"), 2, 3, 2 * i, 2 * i + 1);
				if (i != pairs.Length - 1) Attach(new HSeparator(), 2, 3, 2 * i + 1, 2 * i + 2);
			}


		}

	}

	// Renders an array of StringFloatPairs in a single line.
	// <property name>: <val1>/<val2>/<val3>
	// e.g. Spent XP: 0/2/4
	public sealed class LinearStringFloatPairsField : TextEditableField {

		StringFloatPair[] array;

		public LinearStringFloatPairsField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }

		protected override string GetValueAsString () {
			if (array == null) array = (StringFloatPair[])property.GetValue(obj);
			string text = "";
			for (int i = 0; i < array.Length; i++) {
				if (i != 0) text += "/";
				text += array[i].val.ToString();
			}
			return text;
		}

		protected override void SetValueFromString (string text) {
			string[] fragments = text.Split('/');
			for (int i = 0; i < fragments.Length && i < array.Length; i++)
				if (float.TryParse(fragments[i], out float newVal))
					array[i].val = newVal;
		}

	}

	// This inherits from PrimitiveField for the assignment functionality but pretty much doensn't actually use reflection at all.
	// It intercepts the GetValueAsString and SetValueFromString methods, and instead of piping them to property.GetValue() and property.SetValue() like it's supposed to,
	// just directly assigns them into the array element that it extracted from the property beforehand.
	public sealed class StringFloatPairArrayElementField : TextEditableField {

		int index;
		string format;
		StringFloatPair target;

		//The "true" suppresses Reload() at the end of the base constructor, allowing us to define "target" before Reload()ing manually. Otherwise, GetValueAsString() will fail.
		public StringFloatPairArrayElementField (PropertyInfo p, object o, bool comp, object arg, int ind, string f) : base(p, o, comp, arg, true) {
			index = ind;
			format = f;
			target = ((StringFloatPair[])property.GetValue(obj))[index];
			OverrideLabel(target.str + ": ");
			Reload();
		}

		protected override string GetValueAsString ()
			=> target.val.ToString(format);

		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				target.val = newVal;
		}

	}

	//Used in FractionsBar.
	public struct Fraction {
		public string name;
		public float val;
		public Gdk.Color color;
		public Fraction (string n, float v, Gdk.Color c) {
			name = n;
			val = v;
			if (val > 1) val = 1;
			if (val < 0) val = 0;
			color = c;
		}
	}

	//Try not to touch this unless you have a really good grasp of Gtk's alignment and size systems.
	//All of this was achieved through painful trial and error.
	//The table is used to allow 'Gtk.Alignment's to be superimposed on each other. The arguments for Alignment initialization are used to position each fraction.
	public sealed class FractionsBar : Table {

		public Fraction[] fractions;
		public bool vertical;

		public FractionsBar (PropertyInfo p, object o, bool vert, object arg) : base(1, 3, false) {

			fractions = (Fraction[])p.GetValue(o);

			RowSpacing = 2;
			Label title = new Label("[ " + TextTools.ToReadable(p.Name) + " ]");
			Attach(title, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 2);

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)p.GetCustomAttribute(typeof(TooltipTextAttribute));
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
				EnumTools.SetAllStates(frame, fractions[i].color);

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


}