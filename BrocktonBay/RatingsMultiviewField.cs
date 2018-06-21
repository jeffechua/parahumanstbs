using System;
using System.Reflection;
using Gtk;
using Gdk;

namespace Parahumans.Core {

	public class SquareContainer : Gtk.Alignment {
		public SquareContainer () : base(0.5f, 0.5f, 1, 1) {
			SizeRequested += delegate (object obj, SizeRequestedArgs args) {
				Requisition childRequisition = Child.SizeRequest();
				int width = childRequisition.Width;
				int height = childRequisition.Height;
				int size = Math.Max(width, height);
				Requisition requisition = args.Requisition;
				requisition.Width = requisition.Height = size;
				args.Requisition = requisition;
				if(width>height){
					Xscale = 0;
					Yscale = 1;
				}else {
					Xscale = 1;
					Yscale = 0;
				}
			};
		}
	}

	public class RatingsMultiviewField : Frame {

		Notebook notebook;
		Context context;

		readonly string[] labels = { "Table", "Radar Chart" };

		public RatingsMultiviewField (PropertyInfo property, object obj, Context context, object arg) {

			this.context = context;
			BorderWidth = 5;

			// Nest containers
			VBox mainBox = new VBox();
			HBox navigation = new HBox();
			SquareContainer aspectFrame = new SquareContainer();
			notebook = new Notebook { ShowTabs = false, ShowBorder = false };
			aspectFrame.Add(notebook);
			mainBox.PackStart(navigation, false, false, 5);
			mainBox.PackStart(aspectFrame, true, true, 0);
			Add(mainBox);

			// Navigation
			Label pageLabel = new Label("Table");
			ClickableEventBox leftArrow = new ClickableEventBox();
			leftArrow.Add(Graphics.GetIcon(DirectionType.Left, new Gdk.Color(100, 100, 100), MainClass.textSize));
			leftArrow.Clicked += delegate {
				if (notebook.CurrentPage > 0) {
					notebook.CurrentPage--;
				} else {
					notebook.CurrentPage = notebook.NPages - 1;
				}
				pageLabel.Text = labels[notebook.CurrentPage];
			};
			ClickableEventBox rightArrow = new ClickableEventBox();
			rightArrow.Add(Graphics.GetIcon(DirectionType.Right, new Gdk.Color(100, 100, 100), MainClass.textSize));
			rightArrow.Clicked += delegate {
				if (notebook.CurrentPage < notebook.NPages - 1) {
					notebook.CurrentPage++;
				} else {
					notebook.CurrentPage = 0;
				}
				pageLabel.Text = labels[notebook.CurrentPage];
			};
			navigation.PackStart(leftArrow, false, false, 5);
			navigation.PackStart(pageLabel, true, true, 0);
			navigation.PackStart(rightArrow, false, false, 5);

			// Fill notebook
			RatingsTableField table = new RatingsTableField(property, obj, context.butCompact, arg);
			RatingsRadarChart radar = new RatingsRadarChart(property, obj, context.butCompact, arg);
			notebook.AppendPage(new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = table }, null);
			notebook.AppendPage(Child = radar, null);

		}

	}

}
