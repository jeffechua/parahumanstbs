using System.Reflection;
using System;
using Gtk;

namespace Parahumans.Core {

	public abstract class ReadonlyField : Label {
		protected PropertyInfo property;
		protected object obj;
		public ReadonlyField (PropertyInfo p, object o, bool vert, object arg) {
			property = p;
			obj = o;
			Text = TextTools.ToReadable(p.Name) + ": " + GetValueAsString();
			SetAlignment(0, 0.5f);
		}
		protected abstract string GetValueAsString ();
	}

	public class BasicReadonlyField : ReadonlyField {
		public BasicReadonlyField (PropertyInfo p, object o, bool vert, object arg) : base(p, o, vert, arg) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
	}

	public abstract class TextEditableField : HBox {

		protected PropertyInfo property;
		protected GUIComplete obj;
		protected Menu rightclickMenu;
		protected bool vertical;

		public TextEditableField (PropertyInfo p, object o, bool vert, object arg, bool suppressReload = false) {

			property = p;
			obj = (GUIComplete)o;
			vertical = vert;
			Label label = new Label(TextTools.ToReadable(property.Name) + ": ");
			PackStart(label, false, false, 0);

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				HasTooltip = true;
				TooltipMarkup = tooltipText.text;
			}
			                                                               
			rightclickMenu = new Menu();
			MenuItem edit = new MenuItem("Edit");
			edit.Activated += (a, b) => Open();
			rightclickMenu.Append(edit);
			if (!suppressReload)
				Reload();
		}

		public void OverrideLabel (string text) => ((Label)Children[0]).Text = text;

		protected abstract string GetValueAsString ();
		protected abstract void SetValueFromString (string text);

		void LabelClicked (object o, ButtonPressEventArgs args) {
			if (args.Event.Type == Gdk.EventType.TwoButtonPress) {
				Open();
				args.RetVal = true;
			}
		}
		void EntrySubmitted (object o, EventArgs args) {
			SetValueFromString(((Entry)o).Text);
			DependencyManager.Flag(obj);
			DependencyManager.TriggerAllFlags();
		}

		void FocusLost (object o, EventArgs args) {
			((Window)Toplevel).Focus = null;
			Reload();
		}

		void Open () {
			Entry entry = new Entry();
			entry.SetSizeRequest(Math.Max(Children[1].Allocation.Width, Children[1].SizeRequest().Width + 10), -1);
			entry.Activated += EntrySubmitted;
			entry.FocusOutEvent += FocusLost;
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
			ClickableEventBox eventBox = new ClickableEventBox() { Child = val };
			eventBox.ButtonPressEvent += LabelClicked;
			eventBox.RightClicked += delegate {
				rightclickMenu.Popup();
				rightclickMenu.ShowAll();
			};
			PackStart(eventBox, true, true, 0);
			ShowAll();
		}

	}

	public class EnumField<T> : TextEditableField where T : struct, IConvertible {
		public EnumField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (Enum.TryParse(text, true, out T newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class IntField : TextEditableField {
		public IntField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) {
			if (int.TryParse(text, out int newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class FloatField : TextEditableField {
		public FloatField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }
		protected override string GetValueAsString () => ((float)property.GetValue(obj)).ToString("0.00");
		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class PercentageField : TextEditableField {
		public PercentageField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }
		protected override string GetValueAsString () => ((float)property.GetValue(obj)).ToString("P");
		protected override void SetValueFromString (string text) {
			if (float.TryParse(text, out float newVal))
				property.SetValue(obj, newVal);
		}
	}

	public class StringField : TextEditableField {
		public StringField (PropertyInfo p, object o, bool comp, object arg) : base(p, o, comp, arg) { }
		protected override string GetValueAsString () => property.GetValue(obj).ToString();
		protected override void SetValueFromString (string text) => property.SetValue(obj, text);
	}

}