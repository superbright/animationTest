using UnityEngine;
using System.Collections;

namespace Slate{

	public enum TransformSpace{
		CutsceneSpace,
		ActorSpace,
		WorldSpace,
	}

	public enum ActiveState{
		Disable = 0,
		Enable  = 1,
		Toggle  = 2
	}
}