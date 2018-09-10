using System;
using System.Collections.Generic;
using Gtk;

namespace BrocktonBay {

	public sealed class WeaknessTrait : Trait {

		public RatingsProfile difference;

		[Displayable(3, typeof(DialogTextEditableField), emphasized = true)]
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
		public override List<EffectTrigger> trigger { get => new List<EffectTrigger> { EffectTrigger.GetRatings }; }

		public WeaknessTrait (TraitData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (EffectTrigger trigger, Context context, object obj) {
			if (Known(context) && context.requester != parent.affiliation)
				return (RatingsProfile)obj * difference;
			return obj;
		}

	}

	public sealed class TrueFormTrait : Trait {

		public RatingsProfile trueform { get; set; }

		[Displayable(0, typeof(RatingsListField), generate = false)]
		public Func<Context, RatingsProfile> GetTrueForm { get => (context) => trueform; }

		[Displayable(3, typeof(DialogTextEditableField), emphasized = true)]
		public override string effect {
			get {
				return "True form:\n" + Ratings.Print(trueform.values, trueform.o_vals);
			}
			set {
				if (value.Length == 0) {
					trueform = RatingsProfile.Null;
				} else {
					value = value.Substring(11);
					if (Ratings.TryParse(value, out RatingsProfile? ratings)) {
						trueform = (RatingsProfile)ratings;
					} else {
						throw new ArgumentException();
					}
				}
			}
		}
		public override List<EffectTrigger> trigger { get => new List<EffectTrigger> { EffectTrigger.GetRatings }; }

		public TrueFormTrait (TraitData data) : base(data)
			=> effect = data.effect;

		public override object Invoke (EffectTrigger trigger, Context context, object obj) {
			if (Known(context) && context.requester != parent.affiliation)
				return trueform;
			return obj;
		}

		public override Widget GetCellContents (Context context) {
			HBox hBox = new HBox(false, 2) { BorderWidth = 5 };
			hBox.PackStart(new Label { Angle = 90, Markup = "<small>True Form</small>", UseMarkup = true }, false, false, 0);
			Widget ratings = UIFactory.Fabricate(this, "GetTrueForm", context.butCompact);
			hBox.PackStart(ratings, false, false, 0);
			return hBox;
		}

	}

}
