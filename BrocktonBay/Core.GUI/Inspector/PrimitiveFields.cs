using System.Reflection;
using System;
using Gtk;

namespace BrocktonBay {

	public abstract class ReadonlyField : Label {
		protected PropertyInfo property;
		protected object obj;
		Context context;
		string text;
		public ReadonlyField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute) {
			this.property = property;
			this.obj = obj;
			this.context = context;
			text = GetValueAsString();
			if (context.compact) {
				Text = text;
			} else {
				Text = (attribute.overrideLabel == "" ?
						UIFactory.ToReadable(property.Name) :
						attribute.overrideLabel) + ": " + text;
			}
			SetAlignment(0, 0.5f);
		}
		protected abstract string GetValueAsString ();
	}

	public class BasicReadonlyField : ReadonlyField {
		public BasicReadonlyField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
	}

	public abstract class TextEditableField : HBox {

		protected PropertyInfo property;
		protected IDependable obj;
		protected Menu rightclickMenu;
		protected Context context;
		protected bool editable;

		public TextEditableField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute) {

			this.property = property;
			this.obj = (IDependable)obj;
			this.context = context;
			editable = attribute.EditAuthorized(obj);

			if (!context.compact) {
				Label label = new Label((attribute.overrideLabel == ""
				                         ?UIFactory.ToReadable(property.Name) :
				                         attribute.overrideLabel) + ": ");
				PackStart(label, false, false, 0);
			}

			if (attribute.tooltipText != "") {
				HasTooltip = true;
				TooltipMarkup = attribute.tooltipText;
			}

			rightclickMenu = new Menu();
			MenuItem edit = new MenuItem("Edit");
			edit.Activated += Open;
			rightclickMenu.Append(edit);
			Reload();
		}

		protected abstract string GetValueAsString ();
		protected abstract void SetValueFromString (string text);

		void Submit (object entry, EventArgs args) {
			SetValueFromString(((Entry)entry).Text);
			DependencyManager.Flag(obj);
			DependencyManager.TriggerAllFlags();
		}

		void Cancel (object widget, EventArgs args) {
			((Window)Toplevel).Focus = null;
			Reload();
		}

		void Open (object widget, EventArgs args) {
			Entry entry = new Entry();
			entry.SetSizeRequest(Math.Max(Children[1].Allocation.Width, Children[1].SizeRequest().Width + 10), -1);
			entry.Activated += Submit;
			entry.FocusOutEvent += Cancel;
			entry.Text = GetValueAsString();
			entry.SelectRegion(0, -1);

			Children[1].Destroy();
			PackStart(entry, true, true, 0);

			ShowAll();
			entry.GrabFocus();
		}

		public void Reload () {
			if (Children.Length > 1) Children[1].Destroy();
			Label val = new Label(GetValueAsString());
			val.SetAlignment(0, 0.5f);
			if (editable) {
				ClickableEventBox eventBox = new ClickableEventBox() { Child = val };
				eventBox.DoubleClicked += Open;
				eventBox.RightClicked += delegate {
					rightclickMenu.Popup();
					rightclickMenu.ShowAll();
				};
				PackStart(eventBox, true, true, 0);
			} else {
				PackStart(val, true, true, 0);
			}
			ShowAll();
		}

	}

	public class EnumField<T> : TextEditableField where T : struct, IConvertible {
		public EnumField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (Enum.TryParse(text, true, out T newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class IntField : TextEditableField {
		public IntField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (int.TryParse(text, out int newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class FloatField : TextEditableField {
		public FloatField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => ((float)property.GetValue(obj)).ToString("0.00");
		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class PercentageField : TextEditableField {
		public PercentageField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => ((float)property.GetValue(obj)).ToString("P");
		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class StringField : TextEditableField {
		public StringField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) => property.SetValue(obj, text);
	}

	public class Vector2Field : TextEditableField {
		public Vector2Field (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (text[0] != '(' || text[text.Length - 1] != ')') return;
			text = text.Substring(1, text.Length - 2);
			string[] halves = text.Split(',');
			if (halves.Length != 2) return;
			if (!float.TryParse(halves[0].Trim(), out float x)) return;
			if (!float.TryParse(halves[1].Trim(), out float y)) return;
			property.SetValue(obj, new Vector2(x, y));
		}
	}

	public class IntVector2Field : TextEditableField {
		public IntVector2Field (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute)
			: base(property, obj, context, attribute) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (text[0] != '(' || text[text.Length - 1] != ')') return;
			text = text.Substring(1, text.Length - 2);
			string[] halves = text.Split(',');
			if (halves.Length != 2) return;
			if (!float.TryParse(halves[0].Trim(), out float x)) return;
			if (!float.TryParse(halves[1].Trim(), out float y)) return;
			property.SetValue(obj, new IntVector2(x, y));
		}
	}

}