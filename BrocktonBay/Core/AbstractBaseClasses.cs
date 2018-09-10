using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	// A GameObject is a base class for most objects in the game. This includes players, teams, factions and territories - NOT assets.
	// GameObjects are assigned IDs mainly used for importing/exporting from/to JSON files, as they allow parent/child relationships to be reduced to primitive expressions.
	public abstract class GameObject : IGUIComplete, IComparable<GameObject>, IContainer, IAffiliated {

		[Displayable(0, typeof(StringField))]
		public string name { get; set; }

		[Displayable(1, typeof(IntField), visiblePhases = Phase.None)]
		public int ID { get; set; }

		[Displayable(2, typeof(IntField))] //Only wrt current perspective
		public int intel {
			get => Game.player.knowledge[this];
			set => Game.player.knowledge[this] = value;
		}

		// This is a field and not a Displayable property as parentage is usually displayed in implementation of GetHeader(), so it would be redundant.
		public virtual GameObject parent { get; set; }
		public abstract IAgent affiliation { get; }
		public abstract List<Trait> traits { get; set; }

		//IGUIComplete stuff
		public abstract Widget GetHeader (Context context);
		public abstract Widget GetCellContents (Context context);
		public virtual Menu GetRightClickMenu (Context context, Widget rightClickedWidget) {
			Menu rightClickMenu = new Menu();
			rightClickMenu.Append(MenuFactory.CreateInspectButton(this, rightClickedWidget));
			rightClickMenu.Append(MenuFactory.CreateInspectInNewWindowButton(this));
			foreach (Trait trait in traits)
				if (trait.trigger.Contains(EffectTrigger.GetRightClickMenu))
					trait.Invoke(EffectTrigger.GetRightClickMenu, context);
			if (Game.omnipotent) {
				rightClickMenu.Append(new SeparatorMenuItem());
				rightClickMenu.Append(MenuFactory.CreateDeleteButton(this));
				if (TryCast(out IAgent agent) && agent.active) {
					rightClickMenu.Append(new SeparatorMenuItem());
					MenuItem setPlayerButton = new MenuItem("Set player");
					setPlayerButton.Activated += delegate {
						Game.player = (IAgent)this;
						Game.RefreshUIAndTriggerAllFlags();
					};
					rightClickMenu.Append(setPlayerButton);
				}
			}
			return rightClickMenu;
		}

		public abstract void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget);

		//IDependable stuff
		public abstract int order { get; }
		public bool destroyed { get; set; }
		public List<IDependable> triggers { get; set; } = new List<IDependable>();
		public List<IDependable> listeners { get; set; } = new List<IDependable>();
		public abstract void Reload ();
		public virtual void OnTriggerDestroyed (IDependable trigger) {
			if (Contains(trigger)) {
				Remove(trigger);
				DependencyManager.Flag(this);
			}
		}
		public virtual void OnListenerDestroyed (IDependable listener) {
			if (listener == parent)
				parent.Remove(this);
		}

		//IContainer stuff
		public virtual bool Accepts (object obj) => false;
		public virtual bool Contains (object obj) => false; //It is NOT assumed that the invoker has already checked if Accept(obj)
		public void Add (object obj) => AddRange(new List<object> { obj }); //It is assumed that the invoker has already checked if Accept(obj).
		public void Remove (object obj) => RemoveRange(new List<object> { obj });
		public virtual void AddRange<T> (IEnumerable<T> objs) { } //It is assumed that the invoker has already checked if Accept(obj).
		public virtual void RemoveRange<T> (IEnumerable<T> objs) { }
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

		public bool TryCast<T> (out T output) where T : class {
			output = this as T;
			return output != null;
		}

		public static bool TryCast<T> (object obj, out T output) where T : class {
			output = obj as T;
			return output != null;
		}

	}

	// A GUIComplete is an object that can be fully expressed and manipulated by the modular GUI system employed.
	// It is necessarily IDependable as it has to send update notifications to UI elements displaying it.
	public interface IGUIComplete : IDependable {

		string name { get; }

		// Gets a "header" to denote the GUIComplete object:
		// compact=true returns a simple one-line name with icons, compact=false returns the multi-line header shown at the top of the inspector.
		Widget GetHeader (Context context);

		// Gets a approximate square of key information on the object, APART FROM information already in the header
		// For example, a Parahuman returns a list of its condensed ratings. A Team returns a list of its members.
		Widget GetCellContents (Context context);

		Menu GetRightClickMenu (Context context, Widget rightClickedWidget);

		void ContributeMemberRightClickMenu (object member, Menu rightClickMenu, Context context, Widget rightClickedWidget);

	}

	public interface IAffiliated {
		IAgent affiliation { get; }
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
		void Add (object obj);
		void Remove (object obj);
		void AddRange<T> (IEnumerable<T> objs); //AddRange() assumes that the invoker has already checked if we Accept(obj).
		void RemoveRange<T> (IEnumerable<T> objs);
	}

}
