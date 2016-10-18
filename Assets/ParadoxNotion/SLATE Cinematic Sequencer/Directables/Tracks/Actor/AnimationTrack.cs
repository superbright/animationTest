using UnityEngine;
using System.Collections;
using System.Linq;

namespace Slate{

	[Description("The Animation Track works with the legacy 'Animation' Component. Each Animation Track represents a different layer of the animation system. The zero layered track (bottom) will blend in/out with the default animation clip set on the Animation Component of the actor if any, while all other Animation Tracks will play above.")]
	[Attachable(typeof(ActorGroup))]
	public class AnimationTrack : CutsceneTrack {


		[SerializeField] [Range(0,1)]
		private float _weight = 1f;
		[SerializeField] [Range(0,1)]
		private float _blendIn  = 0.5f;
		[SerializeField] [Range(0,1)]
		private float _blendOut = 0.5f;
		[SerializeField] 
		private AnimationBlendMode _animationBlendMode = AnimationBlendMode.Blend;
		[SerializeField] 
		private string _mixTransformName = string.Empty;

		private Animation anim;
		private AnimationState state;
		private float originalWeight;

		public override string info{
			get
			{
				var blendName = animationBlendMode == AnimationBlendMode.Blend? "Override" : "Additive";
				return string.Format("Layer: {0}, {1} {2}", layerOrder != 0? layerOrder-11 : -1, blendName, (string.IsNullOrEmpty(mixTransformName)? "" : ", " + mixTransformName) );
			}
		}

		public override float blendIn{
			get {return _blendIn;}
		}

		public override float blendOut{
			get {return _blendOut;}
		}

		public float weight{
			get {return _weight;}
			private set {_weight = value;}
		}

		public AnimationBlendMode animationBlendMode{
			get {return _animationBlendMode;}
			private set {_animationBlendMode = value;}
		}

		public string mixTransformName{
			get {return _mixTransformName;}
			private set {_mixTransformName = value;}
		}

		protected override bool OnInitialize(){

			//Play animations on layer 11+ for all the play animation action clips
			layerOrder += 11;
			originalWeight = weight;

			anim = actor.GetComponent<Animation>();
			if (anim == null){
				Debug.LogError("The Animation Track requires the actor to have the 'Animation' Component attached", actor);
				return false;
			}

			return true;
		}

		protected override void OnEnter(){

			anim = actor.GetComponent<Animation>();
			if (anim == null || anim.clip == null || anim.IsPlaying(anim.clip.name) ){
				state = null;
				return;
			}
			
			state = anim[anim.clip.name];
			state.layer = 10; //set the base state to 10. Everything else is playing above it
			state.wrapMode = WrapMode.Loop;
			state.blendMode = AnimationBlendMode.Blend;
			state.enabled = true;
		}

		protected override void OnUpdate(float time, float previousTime){
			
			weight = Easing.Ease(EaseType.QuadraticInOut, 0, originalWeight, GetTrackWeight(time));

			if (state != null){
				state.time = Mathf.Repeat(time, state.length);
				state.weight = weight;
				anim.Sample();
			}			
		}

		protected override void OnExit(){
			if (state != null){
				state.enabled = false;
			}
		}

		protected override void OnReverseEnter(){
			if (state != null){
				state.enabled = true;
			}
		}

		protected override void OnReverse(){
			weight = originalWeight;
			if (state != null){
				state.enabled = false;
			}
		}

		public Transform GetMixTransform(){
			if (string.IsNullOrEmpty(mixTransformName)){
				return null;
			}
			var o = actor.transform.GetComponentsInChildren<Transform>().ToList().Find(t => t.name == mixTransformName);
			if (o == null){
				Debug.LogWarning("Cant find transform with name '" + mixTransformName + "' for PlayAnimation Action", actor);
			}
			return o;
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public override Texture icon{
			get {return Styles.animationIcon;}
		}
			
		#endif
	}
}