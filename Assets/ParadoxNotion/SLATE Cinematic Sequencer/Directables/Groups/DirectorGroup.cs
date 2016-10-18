using UnityEngine;
using System.Collections.Generic;

namespace Slate{

	[Description("The DirectorGroup is the master group of the Cutscene. There is always one and can't be removed. The target actor of this group is always the 'Director Camera', thus it's possible to add a CurveTrack to animate any of the components attached on the DirectorCamera game object. Usualy Image Effects.")]
	public class DirectorGroup : CutsceneGroup {

		public override string name{
			get {return "★ DIRECTOR";}
		}

		public override GameObject actor{
			get { return DirectorCamera.current != null? DirectorCamera.current.gameObject : null; }
			set { /*Non Setable*/ }
		}

		public override ActorReferenceMode referenceMode{
			get {return ActorReferenceMode.UseOriginal;}
			set { /*Non Setable*/ }
		}

		public override ActorInitialTransformation initialTransformation{
			get {return ActorInitialTransformation.UseOriginal;}
			set { /*Non Setable*/ }
		}

		public override Vector3 initialLocalPosition{
			get {return Vector3.zero;}
			set { /*Non Setable*/ }
		}

		public override Vector3 initialLocalRotation{
			get {return Vector3.zero;}
			set { /*Non Setable*/ }
		}
	}
}