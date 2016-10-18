using UnityEngine;
using System.Collections;

namespace Slate{

	///And interface for all cameras handled by director
	public interface IDirectableCamera{
		GameObject gameObject{get;}
		Camera cam{get;}
		Vector3 position{get;set;}
		Quaternion rotation{get;set;}
		float fieldOfView{get;set;}
		float focalPoint{get;set;}
	}
}