﻿using System;
using System.Collections;
using System.Collections.Generic;
using Parahumans.Core.GUI;
using Gtk;

namespace Parahumans.Core {

	public enum Alignment {
		Hero = 2,
		Vigilante = 1,
		Rogue = 0,
		Mercenary = -1,
		Villain = -2
	}

	public enum Threat {
		C = 0, //Default
		B = 1, //Confirmed team takedown
		A = 2, //Confirmed kill
		S = 3, //Kill order receievd
		X = 4  //World-ending
	}

	public sealed class TeamData {
		public String name = "New Team";
		public int ID = 0;
		public Alignment alignment = Alignment.Rogue;
		public int reputation = 0;
		public int unused_XP = 0;
		public StringFloatPair[] spent_XP = new StringFloatPair[3] {
			new StringFloatPair("Strength", 0),
			new StringFloatPair("Mobility", 0),
			new StringFloatPair("Insight", 0)
		};
		public List<int> roster = new List<int>();
	}

	public sealed class Team : GameObject {

		public override int order { get { return 2; } }

		[Displayable(2, typeof(EnumField<Alignment>))]
		public Alignment alignment { get; set; }

		[Displayable(3, typeof(BasicReadonlyField))]
		public Threat threat { get; set; }

		[Displayable(4, typeof(IntField))]
		public int reputation { get; set; }

		[Displayable(5, typeof(IntField))]
		public int unused_XP { get; set; }

		[BimorphicDisplayable(6,typeof(TabularStringFloatPairsField), typeof(LinearStringFloatPairsField)), EmphasizedIfVertical]
		public StringFloatPair[] spent_XP { get; set; }

		[Displayable(7, typeof(CellObjectListField<Parahuman>), 3), Emphasized, Padded(0, 5)]
		public List<Parahuman> roster { get; set; }

		[Displayable(8, typeof(RatingsSumField), true), Emphasized, VerticalOnly]
		public float[,] ratings_sum { get; set; }

		public Team () : this(new TeamData()) { }

		public Team (TeamData data) {
			name = data.name;
			ID = data.ID;
			alignment = data.alignment;
			unused_XP = data.unused_XP;
			spent_XP = data.spent_XP;
			roster = data.roster.ConvertAll((input) => City.city.Get<Parahuman>(input));
			for (int i = 0; i < roster.Count; i++) {
				DependencyManager.Connect(roster[i], this);
				roster[i].parent = this;
			}
			Reload();
		}

		public override void Reload () {
			ratings_sum = new float[5, 8];
			for (int i = 0; i < roster.Count; i++) {
				for (int j = 0; j < roster[i].ratings.Count; j++) {
					if ((int)roster[i].ratings[j].clssf <= 7) {
						ratings_sum[4, (int)roster[i].ratings[j].clssf] += roster[i].ratings[j].num;
						ratings_sum[0, (int)roster[i].ratings[j].clssf] += roster[i].ratings[j].num;
					} else {
						for (int k = 0; k < roster[i].ratings[j].subratings.Count; k++) {
							Rating subrating = roster[i].ratings[j].subratings[k];
							ratings_sum[4, (int)subrating.clssf] += subrating.num;
							ratings_sum[(int)roster[i].ratings[j].clssf - 7, (int)subrating.clssf] += subrating.num;
						}
					}
				}
			}
			threat = Threat.C;
			for (int i = 0; i < roster.Count; i++)
				if (roster[i].threat > threat)
					threat = roster[i].threat;
		}

		public override Widget GetHeader (bool compact) {
			if (compact) {
				HBox frameHeader = new HBox(false, 0);
				Label icon = new Label(" " + EnumTools.GetSymbol(threat) + " ");
				EnumTools.SetAllStates(icon, EnumTools.GetColor(alignment));
				frameHeader.PackStart(new Label(name), false, false, 0);
				frameHeader.PackStart(icon, false, false, 0);
				return new InspectableBox(frameHeader, this);
			} else {
				VBox headerBox = new VBox(false, 5);
				Label nameLabel = new Label(name);
				InspectableBox namebox = new InspectableBox(nameLabel, this);
				Gtk.Alignment align = new Gtk.Alignment(0.5f, 0.5f, 0, 0) { Child = namebox };
				align.WidthRequest = 200;
				headerBox.PackStart(align, false, false, 0);
				if (parent != null)
					headerBox.PackStart(new Gtk.Alignment(0.5f,0.5f,0,0){Child = parent.GetHeader(true)});
				return headerBox;
			}
		}

		public override Widget GetCell () {
			VBox rosterBox = new VBox(false, 0) { BorderWidth = 10 };
			for (int i = 0; i < roster.Count; i++)
				rosterBox.PackStart(roster[i].GetHeader(true), false, false, 0);
			return rosterBox;
		}

		public override bool Contains (object obj) => obj is Parahuman && roster.Contains((Parahuman)obj);
		public override bool Accepts (object obj) => obj is Parahuman;
		public override void Sort () => roster.Sort();
		public override void AddRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				if (obj.parent != null) obj.parent.Remove(obj);
				obj.parent = this;
				roster.Add((Parahuman)obj);
				DependencyManager.Connect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
			Sort();
		}
		public override void RemoveRange<T> (List<T> objs) {
			foreach (object element in objs) {
				GameObject obj = (GameObject)element;
				roster.Remove((Parahuman)obj);
				((Parahuman)obj).parent = null;
				DependencyManager.Disconnect(obj, this);
				DependencyManager.Flag(obj);
			}
			DependencyManager.Flag(this);
		}

	}

}