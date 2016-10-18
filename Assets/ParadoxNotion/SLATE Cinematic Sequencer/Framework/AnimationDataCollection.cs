using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Slate{

	[System.Serializable]
	///A wrapped collection of Animated Parameters
	public class AnimationDataCollection : IAnimatableData{

		[SerializeField]
		private List<AnimatedParameter> _animatedParameters;
		public List<AnimatedParameter> animatedParameters{
			get {return _animatedParameters;}
			private set {_animatedParameters = value;}
		}

		public bool isValid{
			get {return animatedParameters != null && animatedParameters.Count > 0;}
		}

		public AnimationDataCollection(){}
		public AnimationDataCollection(MemberInfo[] parameters, object obj, Transform root){
			foreach(var param in parameters){
				TryAddParameter(param, obj, root);
			}
		}

		///Adds a parameter (property/field), that exists on the target object, that optionaly is a component bellow the root transform.
		public void TryAddParameter(MemberInfo member, object obj, Transform root){
			
			if (animatedParameters == null){
				animatedParameters = new List<AnimatedParameter>();
			}

			var newParam = new AnimatedParameter(member, obj, root);
			if (newParam.isValid){

				var found = animatedParameters.Find(p => p.CompareTo(newParam));
				if (found != null){
					//handle possible changes from property to field and the reverse
					if (found.parameterType != newParam.parameterType){
						found.ChangeMemberType(newParam.parameterType);
					}
					return;
				}
			}

			animatedParameters.Add(newParam);
		}

		///Fetch a parameter with specified name
		public AnimatedParameter GetParameterOfName(string name){
			if (animatedParameters == null){
				return null;
			}
			return animatedParameters.Find(d => d.parameterName.ToLower() == name.ToLower());
		}

		///Get all parameter animation curves
		public AnimationCurve[] GetCurves(){return Internal_GetCurves(true);}
		public AnimationCurve[] GetCurvesAll(){return Internal_GetCurves(false);}
		AnimationCurve[] Internal_GetCurves(bool enabledParamsOnly){

			if (animatedParameters == null){
				return new AnimationCurve[0];
			}

			var result = new List<AnimationCurve>();
			for (var i = 0; i < animatedParameters.Count; i++){
				if (!enabledParamsOnly || animatedParameters[i].enabled){
					var curves = animatedParameters[i].GetCurves();
					if (curves != null){
						result.AddRange(curves);
					}
				}
			}
			return result.ToArray();
		}

		///0. If a context is set, transforms will be virtually parented to context
		public void SetTransformContext(Transform context){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetTransformContext(context);
				}
			}			
		}

		///1. Set snapshot of current value
		public void SetSnapshot(object obj){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetSnapshot(obj);
				}
			}
		}

		///2. Update evaluated values
		public void SetEvaluatedValues(object obj, float time){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetEvaluatedValues(obj, time);
				}
			}
		}

		///3. Restore stored snapshot
		public void RestoreSnapshot(object obj){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].RestoreSnapshot(obj);
				}
			}
		}

		///Will key all parameters that have their value changed
		public bool TryKeyChangedValues(object obj, float time){ return TryKeyChangedValues(obj, time, false); }
		public bool TryKeyChangedValues(object obj, float time, bool paired){
			if (animatedParameters != null){
				var anyKeyAdded = false;
				for (var i = 0; i < animatedParameters.Count; i++){
					if (animatedParameters[i].TryKeyChangedValues(obj, time)){
						anyKeyAdded = true;
					}
				}
				
				//key all parameters of this animation data collection?
				if (paired && anyKeyAdded){
					for (var i = 0; i < animatedParameters.Count; i++){
						animatedParameters[i].SetKeyCurrent(obj, time);
					}
				}

				return anyKeyAdded;
			}

			return false;
		}

		///Try add key at time, with identity value either from existing curves or in case of now curves, from current property value.
		public bool TryKeyIdentity(object obj, float time){
			if (animatedParameters != null){
				var anyKeyAdded = false;
				for (var i = 0; i < animatedParameters.Count; i++){
					if (animatedParameters[i].TryKeyIdentity(obj, time)){
						anyKeyAdded = true;
					}
				}

				return anyKeyAdded;
			}

			return false;	
		}

		///Remove keys at time
		public void RemoveKey(float time){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].RemoveKey(time);
				}
			}			
		}

		///Is there any keys at time?
		public bool HasKey(float time){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					if (animatedParameters[i].HasKey(time)){
						return true;
					}
				}
			}

			return false;
		}

		///Is there any keys at all?
		public bool HasAnyKey(){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					if (animatedParameters[i].HasAnyKey()){
						return true;
					}
				}
			}

			return false;			
		}

		///Set key in all parameters at current value
		public void SetKeyCurrent(object obj, float time){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetKeyCurrent(obj, time);
				}
			}			
		}


		///The next key time aftet time
		public float GetKeyNext(float time){
			if (animatedParameters != null){
				return animatedParameters.Select(p => p.GetKeyNext(time)).OrderBy(t => t).FirstOrDefault(t => t > time);
			}
			return 0;
		}

		///The previous key time aftet time
		public float GetKeyPrevious(float time){
			if (animatedParameters != null){
				return animatedParameters.Select(p => p.GetKeyPrevious(time)).OrderBy(t => t).LastOrDefault(t => t < time);
			}
			return 0;
		}

		///A value label at time
		public string GetKeyLabel(float time){
			if (animatedParameters != null){
				if (animatedParameters.Count == 1){
					return animatedParameters[0].GetKeyLabel(time);
				}
			}
			return string.Empty;
		}

		///...
		public void SetPreWrapMode(WrapMode mode){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetPreWrapMode(mode);
				}
			}			
		}

		///...
		public void SetPostWrapMode(WrapMode mode){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].SetPostWrapMode(mode);
				}
			}			
		}

		///Reset all animated parameters
		public void Reset(){
			if (animatedParameters != null){
				for (var i = 0; i < animatedParameters.Count; i++){
					animatedParameters[i].Reset();
				}
			}
		}


		public override string ToString(){
			if (animatedParameters == null || animatedParameters.Count == 0){
				return "No Parameters";
			}

			return animatedParameters.Count == 1? animatedParameters[0].ToString() : "Multiple Parameters";
		}
	}
}