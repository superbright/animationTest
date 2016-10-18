using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Slate.ActionClips{

	[Name("Animation Clip")]
	[Description("All animation clips in the same track, will play at an animation layer equal to their track layer order. Thus, animations in tracks on top will play over animations in tracks bellow. You can trim or loop the animation by scaling the clip.")]
	[Attachable(typeof(AnimationTrack))]
	public class PlayAnimationClip : ActorActionClip<Animation>, ICrossBlendable, ISubClipContainable{

		[SerializeField] [HideInInspector]
		private float _length = 1f;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0f;


		public AnimationClip animationClip = null;
		public float clipOffset;
		[Range(0.1f, 2)]
		public float playbackSpeed = 1;

		private TransformSnapshot snapShot;
		private Transform mixTransform;
		private AnimationState state;
		private bool isListClip;

		float ISubClipContainable.subClipOffset{
			get {return clipOffset;}
			set {clipOffset = value;}
		}

		public override string info{
			get {return animationClip? animationClip.name : base.info;}
		}

		public override bool isValid{
			get {return base.isValid && animationClip != null && animationClip.legacy;}
		}

		public override float length{
			get { return _length; }
			set	{ _length = value; }
		}

		public override float blendIn{
			get {return _blendIn;}
			set {_blendIn = value;}
		}

		public override float blendOut{
			get {return _blendOut;}
			set {_blendOut = value;}
		}


		private AnimationTrack track{ get {return (AnimationTrack)parent;} }


		protected override void OnEnter(){
			snapShot = new TransformSnapshot(actor.gameObject);
			
			isListClip = actor[animationClip.name] != null;
			if (!isListClip){
				actor.AddClip(animationClip, animationClip.name);
			}

			mixTransform = track.GetMixTransform();
			if (mixTransform != null){
				actor[animationClip.name].AddMixingTransform(mixTransform, true);
			}
		}

		protected override void OnUpdate(float deltaTime){

			state = actor[animationClip.name];
			state.time = deltaTime * playbackSpeed;

			var animLength = animationClip.length / playbackSpeed;
			if (length <= animLength){
				state.time = Mathf.Min(state.time - clipOffset, animationClip.length);
				state.wrapMode = WrapMode.Once;
			}

			if (length > animLength){
				state.time = Mathf.Repeat(state.time - clipOffset, animationClip.length);
				state.wrapMode = WrapMode.Loop;
			}

			state.layer = track.layerOrder;
			state.weight = GetClipWeight(deltaTime) * track.weight;
			state.blendMode = track.animationBlendMode;
			state.enabled = true;

			actor.Sample();
		}

		protected override void OnReverse(){
			snapShot.Restore();
			state.enabled = false;
			state = null;
			if (!isListClip){
				actor.RemoveClip(animationClip);
			}
		}

		protected override void OnExit(){
			state.enabled = false;
			state = null;
		}



		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnClipGUI(Rect rect){
			if (animationClip != null){
				var loop = animationClip.length/playbackSpeed;
				if (length > loop){
					UnityEditor.Handles.color = new Color(0,0,0,0.2f);
					for (var f = clipOffset; f < length; f += loop){
						var posX = (f/length) * rect.width;
						UnityEditor.Handles.DrawLine(new Vector2( posX, 0 ), new Vector2( posX, rect.height ));
					}
					UnityEditor.Handles.color = Color.white;
				}
			}
		}

		#endif
	}
}