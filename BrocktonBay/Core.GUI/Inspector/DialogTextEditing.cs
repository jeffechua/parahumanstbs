using System;
using System.Reflection;
using Gtk;

namespace Parahumans.Core {

	public sealed class DialogTextEditableField : VBox, LabelOverridable {

		PropertyInfo property;
		IDependable obj;
		Menu rightclickMenu;
		Context context;
		HBox topBox;
		bool editable;

		public DialogTextEditableField (PropertyInfo property, object obj, Context context, object arg) : base(false, 2) {

			this.property = property;
			this.obj = (IDependable)obj;
			this.context = context;
			editable = UIFactory.CurrentlyEditable(property, obj);

			topBox = new HBox();
			Label label = new Label(context.compact ? "" : (UIFactory.ToReadable(property.Name) + ": "));
			topBox.PackStart(label, false, false, 0);
			PackStart(topBox, false, false, 0);

			TooltipTextAttribute tooltipText = (TooltipTextAttribute)property.GetCustomAttribute(typeof(TooltipTextAttribute));
			if (tooltipText != null) {
				label.HasTooltip = true;
				label.TooltipMarkup = tooltipText.text;
			}

			Label val = new Label((string)property.GetValue(obj));
			if (val.Text == "") val.Text = "-";
			val.SetAlignment(0, 0);
			Gtk.Alignment alignment = new Gtk.Alignment(0, 0, 1, 1) { LeftPadding = 10 };
			if (editable) {
				ClickableEventBox eventBox = new ClickableEventBox() { Child = val };
				eventBox.DoubleClicked += OpenDialog;
				eventBox.RightClicked += delegate {
					rightclickMenu.Popup();
					rightclickMenu.ShowAll();
				};
				alignment.Add(eventBox);
			} else {
				alignment.Add(val);
			}
			PackStart(alignment, true, true, 0);

			rightclickMenu = new Menu();
			MenuItem edit = new MenuItem("Edit");
			edit.Activated += OpenDialog;
			rightclickMenu.Append(edit);
		}

		public void OverrideLabel (string text) {
			if (!context.compact)
				((Label)Children[0]).Text = text + ": ";
		}

		public void OpenDialog (object widget, EventArgs args) {
			TextEditingDialog dialog = new TextEditingDialog(
				"Edit " + UIFactory.ToReadable(property.Name),
				(Window)Toplevel,
				() => (string)property.GetValue(obj),
				delegate (string input) {
					try {                              // Muahahahaha I'm evil
						property.SetValue(obj, input); // This is the easiest way though for real
					} catch (Exception e) {
						e = e.InnerException;
						return false;
					}
					IDependable dependable = obj as IDependable;
					if (dependable != null) {
						DependencyManager.Flag(obj);
						DependencyManager.TriggerAllFlags();
					}
					return true;
				}
			);
		}

	}

	public sealed class TextEditingDialog : DefocusableWindow {

		TextView editBox;
		Button confirmButton;
		Label errorLabel;

		Func<string> Get;
		Func<string, bool> TrySet;

		int timeoutCountdown;

		public TextEditingDialog (string title, Window transientFor, Func<string> Get, Func<string, bool> TrySet) {

			this.Get = Get;
			this.TrySet = TrySet;

			//Setup window
			Title = title;
			SetSizeRequest(300, 300);
			SetPosition(WindowPosition.Center);
			TransientFor = transientFor;
			TypeHint = Gdk.WindowTypeHint.Dialog;

			VBox mainBox = new VBox();

			editBox = new TextView();
			editBox.Buffer.Text = Get();
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
			if (TrySet(editBox.Buffer.Text)) {
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
