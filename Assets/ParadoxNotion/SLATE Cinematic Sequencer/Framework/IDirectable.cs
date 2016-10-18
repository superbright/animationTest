using UnityEngine;
using System.Collections.Generic;

namespace Slate{

	///Interface for all directable elements of an IDirector (groups, tracks, clips..)
	public interface IDirectable{

		IDirector root{get;}
		IDirectable parent{get;}
		IEnumerable<IDirectable> children{get;}

		GameObject actor{get;}
		bool isActive{get;}
		bool isCollapsed{get;}

		float startTime{get;}
		float endTime{get;}
		float blendIn{get;}
		float blendOut{get;}

		void Validate(IDirector root, IDirectable parent);
		bool Initialize();
		void Enter();
		void Exit();
		void Update(float deltaTime, float previousTime);
		void ReverseEnter();
		void Reverse();

		Vector3 TransformPoint(Vector3 point, TransformSpace space);
		Vector3 InverseTransformPoint(Vector3 point, TransformSpace space);
		Transform GetSpaceTransform(TransformSpace space);
		Vector3 ActorPositionInSpace(TransformSpace space);


#if UNITY_EDITOR
		void DrawGizmos(bool selected);
		void SceneGUI(bool selected);
#endif
		
	}



	///For Directables that contain keyable parameters.
	public interface IKeyable : IDirectable {
		AnimationDataCollection animationData{get;}
		object animatedParametersTarget{get;}
		void RecordKeys(float deltaTime);
	}

	///For Directables that can be blended between one another
	public interface ICrossBlendable : IDirectable{
		new float blendIn{get;set;}
		new float blendOut{get;set;}
	}

	///For Directables that wrap content, like an animation/audio clip
	public interface ISubClipContainable : IDirectable{
		float subClipOffset{get;set;}
	}
}