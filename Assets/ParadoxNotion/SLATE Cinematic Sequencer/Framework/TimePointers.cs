using UnityEngine;
using System.Collections;

namespace Slate{

	///An interface for TimePointers
	public interface IDirectableTimePointer{
		float time{get;}
		void TriggerForward(float currentTime, float previousTime);
		void TriggerBackward(float currentTime, float previousTime);
		void Update(float currentTime, float previousTime);
	} 

	///Wraps the startTime of a group, track or clip (IDirectable) along with it's relevant execution
	public struct TimeInPointer : IDirectableTimePointer{
		
		private bool triggered;
		private float lastTime;
		private IDirectable target;
		public float time{ get {return target.startTime;} }

		public TimeInPointer(IDirectable target){
			this.target = target;
			triggered = false;
			lastTime = 0f;
		}
		
		public void TriggerForward(float currentTime, float previousTime){
			if (currentTime >= target.startTime){
				if (!triggered){
					triggered = true;
					target.Enter();
					target.Update(0, 0);
				}
			}
		}

		public void Update(float currentTime, float previousTime){
			if (currentTime >= target.startTime && currentTime <= target.endTime && currentTime > 0){

				var localCurrentTime = (currentTime - target.startTime);
				var localPreviousTime = (previousTime - target.startTime) + (target.startTime - lastTime);

				#if UNITY_EDITOR
				if (target is IKeyable && !Application.isPlaying && localCurrentTime == localPreviousTime){
					(target as IKeyable).RecordKeys(localCurrentTime);
				}
				#endif

				// if ( !Application.isPlaying || localCurrentTime != localPreviousTime ){
					target.Update(localCurrentTime, localPreviousTime );
				// }

				lastTime = target.startTime;
			}
		}

		public void TriggerBackward(float currentTime, float previousTime){
			if (currentTime < target.startTime || currentTime <= 0){
				if (triggered){
					triggered = false;
					target.Update(0, Mathf.Min(target.endTime - target.startTime, previousTime - target.startTime) );
					target.Reverse();
				}
			}
		}
	}

	///Wraps the endTime of a group, track or clip (IDirectable) along with it's relevant execution
	public struct TimeOutPointer : IDirectableTimePointer{
		
		private bool triggered;
		private IDirectable target;
		public float time{ get {return target.endTime;} }

		public TimeOutPointer(IDirectable target){
			this.target = target;
			triggered = false;
		}

		public void TriggerForward(float currentTime, float previousTime){
			if (currentTime > target.endTime || currentTime == target.root.length){
				if (!triggered){
					triggered = true;
					target.Update(target.endTime - target.startTime, Mathf.Max(0, previousTime - target.startTime) );
					target.Exit();
				}
			}
		}

		public void Update(float currentTime, float previousTime){
			//Update is/should never be called in TimeOutPointers
			throw new System.NotImplementedException();
		}

		public void TriggerBackward(float currentTime, float previousTime){
			if ( (currentTime <= target.endTime || currentTime <= 0) && currentTime != target.root.length){
				if (triggered){
					triggered = false;
					target.ReverseEnter();
					target.Update(target.endTime - target.startTime, target.endTime - target.startTime);
				}
			}
		}
	}
}