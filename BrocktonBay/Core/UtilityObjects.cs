using System;
using Gdk;

namespace BrocktonBay {

	public struct GameAction {
		public string name;
		public string description;
		public Action<Context> action;
		public Func<Context, bool> condition;
	}

	public struct Context {

		public IAgent agent;
		public object UIContext;
		public bool vertical;
		public bool compact;

		//Convenience properties and methods for on-the-go modifications.
		public Context butCompact { get { return new Context(agent, UIContext, vertical, true); } }
		public Context butVertical { get { return new Context(agent, UIContext, true, compact); } }
		public Context butNotCompact { get { return new Context(agent, UIContext, vertical, false); } }
		public Context butHorizontal { get { return new Context(agent, UIContext, false, compact); } }
		public Context butRequestedBy (IAgent newRequester) => new Context(newRequester, UIContext, vertical, compact);
		public Context butKnownBy (Dossier newKnowledge) => new Context(agent, newKnowledge, vertical, compact);

		public Context (IAgent requester, object UIContext, bool vertical = true, bool compact = false) {
			this.agent = requester;
			this.UIContext = UIContext;
			this.vertical = vertical;
			this.compact = compact;
		}

	}

	public struct Vector2 {
		public double x;
		public double y;
		public Vector2 (double n1, double n2) {
			x = n1;
			y = n2;
		}
		public static implicit operator Vector2 (IntVector2 v)
			=> new Vector2(v.x, v.y);
		public static implicit operator Point (Vector2 v)
			=> v.ToPoint();
		public static Vector2 operator + (Vector2 a, Vector2 b)
			=> new Vector2(a.x + b.x, a.y + b.y);
		public static Vector2 operator - (Vector2 a, Vector2 b)
			=> new Vector2(a.x - b.x, a.y - b.y);
		public static Vector2 operator * (double k, Vector2 v)
			=> new Vector2(k * v.x, k * v.y);
		public static Vector2 operator * (Vector2 v, double k)
			=> new Vector2(k * v.x, k * v.y);
		public static Vector2 operator / (Vector2 v, double k)
			=> new Vector2(v.x / k, v.y / k);
		public override string ToString ()
			=> "(" + x.ToString() + ", " + y.ToString() + ")";
		public Point ToPoint ()
			=> new Point((int)Math.Round(x), (int)Math.Round(y));
	}

	public struct IntVector2 {
		public int x;
		public int y;
		public IntVector2 (int n1, int n2) {
			x = n1;
			y = n2;
		}
		public IntVector2 (double n1, double n2) {
			x = (int)Math.Round(n1);
			y = (int)Math.Round(n2);
		}
		public static implicit operator IntVector2 (Vector2 v)
			=> new IntVector2((int)Math.Round(v.x), (int)Math.Round(v.y));
		public static IntVector2 operator + (IntVector2 a, IntVector2 b)
			=> new IntVector2(a.x + b.x, a.y + b.y);
		public static IntVector2 operator - (IntVector2 a, IntVector2 b)
			=> new IntVector2(a.x - b.x, a.y - b.y);
		public static IntVector2 operator * (double k, IntVector2 v)
			=> new IntVector2(k * v.x, k * v.y);
		public static IntVector2 operator * (IntVector2 v, double k)
			=> new IntVector2(k * v.x, k * v.y);
		public static IntVector2 operator / (IntVector2 v, double k)
			=> new IntVector2(v.x / k, v.y / k);
		public override bool Equals (object obj)
			=> obj is IntVector2 && this == (IntVector2)obj;
		public override int GetHashCode ()
			=> base.GetHashCode();
		public static bool operator == (IntVector2 u, IntVector2 v)
			=> u.x == v.x && u.y == v.y;
		public static bool operator != (IntVector2 u, IntVector2 v)
		=> u.x != v.x || u.y != v.y;
		public override string ToString ()
			=> "(" + x.ToString() + ", " + y.ToString() + ")";
	}

}
