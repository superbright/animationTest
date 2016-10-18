using UnityEngine;
using System.Collections;

namespace Slate{

	///Used to interface with AnimatedParameter and AnimationDataCollection the same
	public interface IAnimatableData {
		bool isValid{get;}
		AnimationCurve[] GetCurves();
		void SetTransformContext(Transform context);
		void SetSnapshot(object o);
		void RestoreSnapshot(object o);
		void SetEvaluatedValues(object o, float time);
		void SetKeyCurrent(object o, float time);
		bool TryKeyChangedValues(object o, float time);
		bool TryKeyIdentity(object o, float time);
		void RemoveKey(float time);
		bool HasKey(float time);
		bool HasAnyKey();
		float GetKeyNext(float time);
		float GetKeyPrevious(float time);
		string GetKeyLabel(float time);
		void SetPreWrapMode(WrapMode mode);
		void SetPostWrapMode(WrapMode mode);
		void Reset();
	}
}