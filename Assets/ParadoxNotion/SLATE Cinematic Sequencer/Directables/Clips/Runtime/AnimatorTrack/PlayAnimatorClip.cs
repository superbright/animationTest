#if UNITY_5_4_OR_NEWER

using UnityEngine;
using System.Collections;

namespace Slate.ActionClips{

	[Name("Animation Clip")]
	[Attachable(typeof(AnimatorTrack) )]
	public class PlayAnimatorClip : ActorActionClip<Animator>, ICrossBlendable, ISubClipContainable {

		[SerializeField] [HideInInspector]
		private float _length = 1f;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0f;

		public AnimationClip animationClip;
		public float clipOffset;

		float ISubClipContainable.subClipOffset{
			get {return clipOffset;}
			set {clipOffset = value;}
		}

		public override string info{
			get {return animationClip? animationClip.name : base.info;}
		}

		public override bool isValid{
			get {return base.isValid && animationClip != null && !animationClip.legacy;}
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

		private AnimatorTrack track{
			get {return (AnimatorTrack)parent;}
		}

		protected override void OnEnter(){ track.EnableClip(this); }
		protected override void OnReverseEnter(){ track.EnableClip(this); }

		protected override void OnUpdate(float time, float previousTime){
			track.UpdateClip(this, time - clipOffset, previousTime - clipOffset, GetClipWeight(time));
		}

		protected override void OnExit(){ track.DisableClip(this); }
		protected override void OnReverse(){ track.DisableClip(this); }


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnClipGUI(Rect rect){
			if (animationClip != null){
				var loop = animationClip.length/1;
				if (length > loop){
					UnityEditor.Handles.color = new Color(0,0,0,0.2f);
					for (var f = clipOffset; f < length; f += loop ){
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

#endif