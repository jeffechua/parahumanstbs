using System;
using System.Collections.Generic;
using Gtk;

namespace Parahumans.Core {
	public sealed class WeaknessMechanic : Mechanic {

		public RatingsProfile difference;

		public override string effect {
			get {
				return Ratings.PrintCompact(difference.values, difference.o_vals);
			}
			set {
				if (Ratings.TryParseCompact(value, out RatingsProfile? ratings)) {
					difference = (RatingsProfile)ratings;
				} else {
					throw new ArgumentException();
				}
			}
		}
		public override InvocationTrigger trigger { get { return InvocationTrigger.GetRatings; } }

		public WeaknessMechanic (MechanicData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (Context context, object obj) {
			if (Known(context)) {
				return (RatingsProfile)obj * difference;
			} else {
				return obj;
			}
		}

	}
}
