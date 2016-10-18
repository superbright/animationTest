using UnityEngine;
using System.Collections;
using System.Linq;

namespace Slate{

	///Action Tracks contain general purpose ActionClips
	[Name("Action Track")]
	[Description("Action Tracks are generic purpose tracks. Once a action clip has been placed the Action Track will lock to accept only clips of the same category.")]
	abstract public class ActionTrack : CutsceneTrack {

		#if UNITY_EDITOR
		public override Texture icon{
			get {return Styles.actionIcon;}
		}
		#endif		
	}
}