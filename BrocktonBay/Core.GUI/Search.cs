﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;

namespace Parahumans.Core.GUI {

	public class SelectorDialog : DefocusableWindow {
		public SelectorDialog(string title, Action<GameObject> ClickedAction, Func<GameObject, bool> FilterFunction = null) {
			//Setup window
			Title = title;
			SetPosition(WindowPosition.Center);
			TransientFor = (Window)Inspector.main.Toplevel;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			//Setup search
			Search search = new Search(FilterFunction, delegate (GameObject obj) { ClickedAction(obj); this.Destroy(); });
			Add(search);
			//Define default dimensions and Show
			search.resultsWindow.Realize();
			DefaultHeight = search.resultsWindow.Child.Requisition.Height + 30;
			DefaultWidth = search.resultsWindow.Child.Requisition.Width + 10;
			ShowAll();
		}
	}

	public class Search : VBox, IDependable {

		public int order { get { return 5; } }

		public List<WeakReference<IDependable>> dependencies { get; set; } = new List<WeakReference<IDependable>>();
		public List<WeakReference<IDependable>> dependents { get; set; } = new List<WeakReference<IDependable>>();

		Toolbar searchBar;
		Entry searchText;

		ToggleButton typesButton;
		ToggleMenu typesMenu;
		Checklist types;

		ToggleMenu traitsMenu;
		Checklist alignments;
		Checklist threats;

		CheckButton toplevelOnlyButton;

		ComboBox presentation;

		CachingLister<GameObject> lister;
		CachingTesselator<GameObject> tesselator;
		CachingLister<GameObject> headerer;
		public ScrolledWindow resultsWindow;

		Func<GameObject, bool> Filter;
		Action<GameObject> OnClicked;


		public Search(Func<GameObject, bool> FilterFunction = null, Action<GameObject> ClickedAction = null) {

			DependencyManager.Connect(City.city, this);
			Filter = FilterFunction == null ? delegate { return true; } : FilterFunction;
			OnClicked = ClickedAction == null ? delegate { } : ClickedAction;

			//Search bar
			searchBar = new Toolbar();
			PackStart(searchBar, false, false, 0);

			//Search results
			resultsWindow = new ScrolledWindow();
			PackStart(resultsWindow, true, true, 0);

			lister = new CachingLister<GameObject>(City.city.gameObjects, SetupListing);
			tesselator = new CachingTesselator<GameObject>(City.city.gameObjects, SetupCell, resultsWindow);
			headerer = new CachingLister<GameObject>(City.city.gameObjects, SetupHeader);

			//Search
			searchText = new Entry();
			searchText.Activated += (o, a) => Reload();
			searchBar.Insert(new ToolItem() { Child = searchText }, -1);

			//Types
			typesButton = new ToggleButton("All types");
			typesMenu = new ToggleMenu(typesButton);
			typesMenu.Hidden += delegate { TypeChanged(); Reload(); };
			types = new Checklist(true,
								  new string[] { "Parahuman", "Team", "Faction" },
								  new object[] { typeof(Parahuman), typeof(Team), typeof(Faction) }) { BorderWidth = 2 };
			typesMenu.Add(types);
			searchBar.Insert(new SeparatorToolItem() { }, 1);
			searchBar.Insert(new ToolItem { Child = new Label("Scope: ") }, -1);
			searchBar.Insert(new ToolItem { Child = typesButton }, -1);

			//Toplevel only
			toplevelOnlyButton = new CheckButton("Toplevel only");
			toplevelOnlyButton.Toggled += (o, a) => Reload();
			searchBar.Insert(new SeparatorToolItem(), -1);
			searchBar.Insert(new ToolItem { Child = toplevelOnlyButton }, -1);
			searchBar.Insert(new SeparatorToolItem(), -1);

			//Traits
			ToggleButton traitsButton = new ToggleButton("More Filters");
			traitsMenu = new ToggleMenu(traitsButton);
			traitsMenu.Hidden += (o, a) => Reload();
			HBox columns = new HBox(false, 5) { BorderWidth = 2 };
			alignments = new Checklist(true,
									   new string[] { "Hero", "Vigilante", "Rogue", "Mercenary", "Villain" },
									   new object[] { Alignment.Hero, Alignment.Vigilante, Alignment.Rogue, Alignment.Mercenary, Alignment.Villain });
			threats = new Checklist(true,
									new string[] { "C", "B", "A", "S", "X" },
									new object[] { Threat.C, Threat.B, Threat.A, Threat.S, Threat.X });
			columns.PackStart(alignments, false, false, 0);
			columns.PackStart(threats, false, false, 5);
			traitsMenu.Add(columns);
			searchBar.Insert(new ToolItem { Child = traitsButton }, -1);

			//Presentation
			presentation = new ComboBox(new string[] { "Listings", "Cells", "Headers" });
			presentation.Changed += (o, args) => PresentationChanged();
			presentation.Active = 0;
			searchBar.Insert(new SeparatorToolItem(), -1);
			searchBar.Insert(new ToolItem { Child = presentation }, -1);

			Reload();

		}

		bool SatisfiesFilters(GameObject obj) {
			if (toplevelOnlyButton.Active && obj.parent != null)
				return false;
			if (searchText.Text != "")
				if (!obj.name.ToLower().Contains(searchText.Text.ToLower()))
					return false;
			for (int i = 0; i < types.Children.Length; i++)
				if (!types.elements[i].Active && obj.GetType() == (Type)types.metadata[i])
					return false;
			PropertyInfo alignment = obj.GetType().GetProperty("alignment");
			if (alignment != null)
				for (int i = 0; i < alignments.Children.Length; i++)
					if (!alignments.elements[i].Active && (Core.Alignment)alignment.GetValue(obj) == (Core.Alignment)alignments.metadata[i])
						return false;
			PropertyInfo threat = obj.GetType().GetProperty("threat");
			if (threat != null)
				for (int i = 0; i < threats.Children.Length; i++)
					if (!threats.elements[i].Active && (Threat)threat.GetValue(obj) == (Threat)threats.metadata[i])
						return false;
			return Filter(obj);
		}

		void TypeChanged() {
			CheckButton[] checkButtons = Array.ConvertAll<Widget, CheckButton>(((VBox)typesMenu.Child).Children, (input) => (CheckButton)input);
			string text = "";
			int words = 0;
			for (int i = 0; i < checkButtons.Length; i++) {
				if (checkButtons[i].Active) {
					words++;
					if (text.Length > 0) text += ", ";
					text += checkButtons[i].Label;
				}
			}
			if (words == checkButtons.Length) {
				text = "All types";
			} else if (words > 3) {
				text = "...";
			} else if (words == 0) {
				text = "None";
			} else {
				typesButton.Label = text;
			}
		}

		void PresentationChanged() {
			switch (presentation.Active) {
				case 0:
					if (tesselator.Parent != null) ((Container)tesselator.Parent).Remove(tesselator);
					if (headerer.Parent != null) ((Container)headerer.Parent).Remove(headerer);
					if (resultsWindow.Child != null) resultsWindow.Child.Destroy();
					resultsWindow.AddWithViewport(lister);
					break;
				case 1:
					if (lister.Parent != null) ((Container)lister.Parent).Remove(lister);
					if (headerer.Parent != null) ((Container)headerer.Parent).Remove(headerer);
					if (resultsWindow.Child != null) resultsWindow.Child.Destroy();
					resultsWindow.AddWithViewport(tesselator);
					break;
				default:
					if (lister.Parent != null) ((Container)lister.Parent).Remove(lister);
					if (tesselator.Parent != null) ((Container)tesselator.Parent).Remove(tesselator);
					if (resultsWindow.Child != null) resultsWindow.Child.Destroy();
					resultsWindow.AddWithViewport(headerer);
					break;
			}
			Reload();
		}

		Widget SetupListing(GameObject obj) {
			Gtk.Alignment align = new Gtk.Alignment(0, 0, 0, 0) {
				Child = new Listing(obj),
				BorderWidth = 5,
				LeftPadding = 10,
				RightPadding = 10
			};
			ClickableEventBox eventbox = new ClickableEventBox { Child = align };
			eventbox.Clicked += (o, a) => OnClicked(obj);
			return eventbox;
		}

		Widget SetupCell(GameObject obj) {
			Frame baseCell = new Cell(obj);
			baseCell.BorderWidth = 5;
			ClickableEventBox eventbox = new ClickableEventBox { Child = baseCell };
			eventbox.Clicked += (o, a) => OnClicked(obj);
			return eventbox;
		}

		Widget SetupHeader(GameObject obj) {
			Widget baseHeader = obj.GetHeader(true);
			HBox hbox = new HBox();
			hbox.PackStart(baseHeader, false, false, 5);
			ClickableEventBox eventbox = new ClickableEventBox { Child = hbox };
			eventbox.Clicked += (o, a) => OnClicked(obj);
			return eventbox;
		}

		public void Reload() {

			List<GameObject> resultsList = City.city.gameObjects.FindAll(SatisfiesFilters);

			switch (presentation.Active) {
				case 0:
					lister.Load(resultsList);
					ShowAll();
					lister.Render();
					break;
				case 1:
					tesselator.Load(resultsList);
					ShowAll();
					tesselator.Render();
					break;
				default:
					headerer.Load(resultsList);
					ShowAll();
					headerer.Render();
					break;
			}

		}

	}

}