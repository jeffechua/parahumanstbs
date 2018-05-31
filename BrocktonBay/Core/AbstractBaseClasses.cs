﻿using System;
using System.Collections.Generic;
using Parahumans.Core.GUI;
using System.Collections;
using Gtk;

namespace Parahumans.Core {

	// A GameObject is a base class for most objects in the game. This includes players, teams, factions and territories - NOT assets.
	// GameObjects are assigned IDs mainly used for importing/exporting from/to JSON files, as they allow parent/child relationships to be reduced to primitive expressions.
	public abstract class GameObject : GUIComplete, IComparable<GameObject>, IContainer {

		[Displayable(0, typeof(StringField))]
		public override string name { get; set; } = "";

		[Displayable(1, typeof(IntField))]
		public int ID { get; set; }

		// This is a field and not a Displayable property as parentage is usually displayed in implementation of GetHeader(), so it would be redundant.
		public GameObject parent;

		//IContainer methods
		public virtual bool Accepts (object obj) => false;
		public virtual bool Contains (object obj) => false;
		public void Add (object obj) => AddRange(new List<object> { obj }); //It is assumed that the invoker has already checked if we Accept(obj).
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public virtual void AddRange<T> (List<T> objs) { } //It is assumed that the invoker has already checked if we Accept(obj).
		public virtual void RemoveRange<T> (List<T> objs) { }
		public virtual void Sort () { }
		public int CompareTo (GameObject obj) => ID.CompareTo(obj.ID);

		//Engagement management: is this GameObject currently occupied in the game, e.g. deployed to a battle?
		static int currentEngagement = 1;
		int lastEngaged = 0;
		//The isEngaged property returns whether I, or any of my ancestors, have been Engaged() since the last ClearEngagements().
		public bool isEngaged { get { return lastEngaged == currentEngagement || ((parent == null) ? false : parent.isEngaged); } }
		public void Engage () => lastEngaged = currentEngagement;
		public void Disengage () => lastEngaged = currentEngagement - 1;
		public static void ClearEngagements () => currentEngagement++;

	}

	// A GUIComplete is an object that can be fully expressed and manipulated by the modular GUI system employed.
	// It is necessarily IDependable as it has to send update notifications to UI elements displaying it.
	public abstract class GUIComplete : IDependable {

		virtual public string name { get; set; } = "";

		//IDependable requirements
		public abstract int order { get; }
		public List<WeakReference<IDependable>> dependents { get; set; } = new List<WeakReference<IDependable>>();
		public List<WeakReference<IDependable>> dependencies { get; set; } = new List<WeakReference<IDependable>>();
		public virtual void Reload () { }

		// Gets a "header" to denote the GUIComplete object:
		// compact=true returns a simple one-line name with icons, compact=false returns the multi-line header shown at the top of the inspector.
		public virtual Widget GetHeader (bool compact) => new Label(name);

		// Gets a approximate square of key information on the object, APART FROM information already in the header
		// For example, a Parahuman returns a list of its condensed ratings. A Team returns a list of its members.
		public virtual Widget GetCell () => new Label();

	}

	/* 
	 *  An IContainer is an interface for managing parenting and containing.
	 *       - IContainers can and usually contain multiple arrays/list, which are all handled through the same methods.
	 *         Figuring out which list to place an added object is handled within the AddRange() method.
	 *       - Implementation of the interface methods operate under the following assumptions:
	 *           - At any point in time, no object is an element of more than one array in the same IContainer.
	 *             However, an object can be an element of multiple arrays as long as they are on different IContainers.
	 *           - The arrays in an IContainer must be type specific and must contain mutually exclusive types.
	 *             For example, a faction can have a List<Parahuman>, a List<Team> and a List<Asset>.
	 *             However, something may not have a List<GameObject> and a List<Parahuman>, since Parahuman derives from GameObject.
	 *  This is to allow generic UI elements to add and remove from IContainers with minimum hassle and type manipulation.
	 */
	public interface IContainer {
		bool Accepts (object obj);
		bool Contains (object obj);
		void AddRange<T> (List<T> objs); //AddRange() assumes that the invoker has already checked if we Accept(obj).
		void RemoveRange<T> (List<T> objs);
		void Sort ();
	}

}