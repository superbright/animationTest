using UnityEngine;
using System.Collections.Generic;

namespace Slate{

	///Interface for an IDirectable player. e.g. the Cutscene component
	///This is used for IDirectables to interface with their root
	public interface IDirector{
		GameObject context{get;}
		float length{get;}
		float currentTime{get; set;}
		float previousTime{get;}
		IEnumerable<GameObject> GetAffectedActors();
		void Sample(float time);
		void ReSample();
		void Validate();
		void SendGlobalMessage(string message, object value);
	}
}