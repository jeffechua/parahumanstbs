using System;
using System.Reflection;
using Gtk;
using Gdk;

namespace BrocktonBay {

	public class SquareContainer : Bin {
		protected override void OnSizeRequested (ref Requisition requisition) {
			Requisition childRequisition = Child.SizeRequest();
			int width = childRequisition.Width;
			int height = childRequisition.Height;
			int size = Math.Max(width, height);
			requisition = new Requisition {
				Width = size,
				Height = size
			};
		}
		protected override void OnSizeAllocated (Rectangle allocation) {
			int size, x, y;
			if (allocation.Width > allocation.Height) {
				size = allocation.Height;
				x = (allocation.Width - size) / 2;
				y = 0;
			} else {
				size = allocation.Width;
				x = 0;
				y = (allocation.Height - size) / 2;
			}
			Child.SizeAllocate(new Rectangle(allocation.X + x, allocation.Y + y, size, size));
		}
	}

	public class RatingsMultiviewField : Frame {

		protected Notebook notebook;
		protected Context context;

		string[] labels = { "Table", "Radar Chart" };

		public RatingsMultiviewField (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute) {

			this.context = context;
			BorderWidth = 5;

			// Nest containers
			VBox mainBox = new VBox();
			HBox navigation = new HBox();
			SquareContainer square = new SquareContainer();
			notebook = new Notebook { ShowTabs = false, ShowBorder = false };
			square.Add(notebook);
			mainBox.PackStart(navigation, false, false, 5);
			mainBox.PackStart(square, true, true, 0);
			Add(mainBox);

			// Navigation
			Label pageLabel = new Label("Table");
			ClickableEventBox leftArrow = new ClickableEventBox();
			leftArrow.Add(Graphics.GetIcon(DirectionType.Left, new Gdk.Color(100, 100, 100), Graphics.textSize));
			leftArrow.Clicked += delegate {
				if (notebook.CurrentPage > 0) {
					notebook.CurrentPage--;
				} else {
					notebook.CurrentPage = notebook.NPages - 1;
				}
				pageLabel.Text = labels[notebook.CurrentPage];
			};
			ClickableEventBox rightArrow = new ClickableEventBox();
			rightArrow.Add(Graphics.GetIcon(DirectionType.Right, new Gdk.Color(100, 100, 100), Graphics.textSize));
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
			RatingsProfile profile = ((Func<Context, RatingsProfile>)property.GetValue(obj))(context);
			RatingsRadarChart radar = new RatingsRadarChart(context.butCompact, profile, null, null);
			RatingsTable table = new RatingsTable(context.butCompact, profile);
			notebook.AppendPage(radar, null);
			notebook.AppendPage(UIFactory.Align(table, 0.5f, 0.5f, 0, 0), null);
		}

	}

	public class EffectiveRatingsMultiview : Frame {

		protected Notebook notebook;
		protected Context context;

		string[] labels = { "Table (final)", "Radar Chart (final)", "Table (original)", "Radar Chart (original)" };

		public EffectiveRatingsMultiview (PropertyInfo property, object obj, Context context, DisplayableAttribute attribute, object arg) {

			this.context = context;
			BorderWidth = 5;

			// Nest containers
			VBox mainBox = new VBox();
			HBox navigation = new HBox();
			SquareContainer square = new SquareContainer();
			notebook = new Notebook { ShowTabs = false, ShowBorder = false };
			square.Add(notebook);
			mainBox.PackStart(navigation, false, false, 5);
			mainBox.PackStart(square, true, true, 0);
			Add(mainBox);

			// Navigation
			Label pageLabel = new Label("Table (final)");
			ClickableEventBox leftArrow = new ClickableEventBox();
			leftArrow.Add(Graphics.GetIcon(DirectionType.Left, new Gdk.Color(100, 100, 100), Graphics.textSize));
			leftArrow.Clicked += delegate {
				if (notebook.CurrentPage > 0) {
					notebook.CurrentPage--;
				} else {
					notebook.CurrentPage = notebook.NPages - 1;
				}
				pageLabel.Text = labels[notebook.CurrentPage];
			};
			ClickableEventBox rightArrow = new ClickableEventBox();
			rightArrow.Add(Graphics.GetIcon(DirectionType.Right, new Gdk.Color(100, 100, 100), Graphics.textSize));
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
			EffectiveRatingsProfile erp = (EffectiveRatingsProfile)property.GetValue(obj);
			RatingsRadarChart radar1 = new RatingsRadarChart(context.butCompact, erp.final, erp.multipliers, erp.metamultipliers);
			RatingsTable table1 = new RatingsTable(context.butCompact, erp.final, erp.multipliers, erp.metamultipliers);
			RatingsRadarChart radar2 = new RatingsRadarChart(context.butCompact, erp.original);
			RatingsTable table2 = new RatingsTable(context.butCompact, erp.original);
			notebook.AppendPage(radar1, null);
			notebook.AppendPage(UIFactory.Align(table1, 0.5f, 0.5f, 0, 0), null);
			notebook.AppendPage(radar2, null);
			notebook.AppendPage(UIFactory.Align(table2, 0.5f, 0.5f, 0, 0), null);

		}
	}

}
