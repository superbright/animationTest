﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	[Description("(** This Track will be deprecated in the future. Please use Animator Track instead **)\n\nThe Mecanim Track works with an 'Animator' component attached on the actor and with it's assigned Controller by modifying the Controller's parameters.\n\nConsider working with the new Animator Track instead to playback animation clips directly without the need of a Controller, which is more intuitive for animations.")]
	[Attachable(typeof(ActorGroup))]
	public class MecanimTrack : CutsceneTrack {

		private Animator animator;

		private AnimatorDispatcher _dispatcher;
		public AnimatorDispatcher dispatcher{
			get
			{
				if (actor == null) { return null; }
				if (_dispatcher == null || _dispatcher.gameObject != actor.gameObject){
					_dispatcher = actor.GetComponent<AnimatorDispatcher>();
					if (_dispatcher == null){
						_dispatcher = actor.gameObject.AddComponent<AnimatorDispatcher>();
					}
				}
				return _dispatcher;		
			}
		}

		protected override bool OnInitialize(){
			animator = actor.GetComponent<Animator>();
			if (animator == null){
				Debug.LogError("Mecanim Track requires that the actor has the Animator Component attached.", actor);
				return false;
			}

			if (animator.runtimeAnimatorController == null){
				Debug.LogWarning(string.Format("The Mecanim Track requires the target actor '{0}' to have an assigned Runtime Animator Controller", actor.name));
				return false;
			}

			return true;	
		}

#if UNITY_EDITOR

		private AnimatorCullingMode wasCullingMode;
		const int RECORDING_FRAMERATE = 30;

		protected override void OnEnter(){

			animator = actor.GetComponent<Animator>();
			if (animator == null){
				return;
			}

			if (Application.isPlaying || layerOrder != 0){ //only 0 MecanimTrack layer does the recording
				animator = null;
				return;
			}


			wasCullingMode = animator.cullingMode;
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

			var updateInterval = (1f/RECORDING_FRAMERATE);

			animator.recorderStartTime = this.startTime;
			animator.recorderStopTime = this.endTime + updateInterval;
			animator.StartRecording(0);

			var clips = new List<IDirectable>();
			foreach (var track in (parent as CutsceneGroup).tracks.OfType<MecanimTrack>().Where(t => t.isActive).Reverse() ){
				clips.AddRange( track.actions.OfType<ActionClips.MecanimBaseClip>().Where(a => a.isValid).Cast<IDirectable>() );
			}
			clips = clips.OrderBy(a => a.startTime).ToList();

			var lastTime = -1f;
			for (var i = startTime; i <= endTime + updateInterval; i += updateInterval){
				foreach (var clip in clips){

					if (i >= clip.startTime && lastTime < clip.startTime){
						clip.Enter();
					}

					if (i >= clip.startTime && i <= clip.endTime){
						clip.Update(i - clip.startTime, i - clip.startTime - updateInterval);
					}

					if (i > clip.endTime && lastTime <= clip.endTime){
						clip.Exit();
					}
				}

				animator.Update(updateInterval);
				lastTime = i;
			}
	
			foreach (var clip in clips.AsEnumerable().Reverse()){
				clip.Update(0,0);
				clip.Reverse();
			}

			animator.StopRecording();
			animator.StartPlayback();
		}

		protected override void OnUpdate(float time, float previousTime){
			if (animator != null && time != endTime){
				animator.playbackTime = time;
				animator.Update(0);
			}
		}

		public override Texture icon{
			get {return Styles.animatorIcon;}
		}

#endif


		protected override void OnReverse(){
			
			DestroyDispatcher();

			#if UNITY_EDITOR
			if (animator != null){
				animator.cullingMode = wasCullingMode;
				animator.StopPlayback();
				animator = null;
			}
			#endif
		}

		protected override void OnExit(){
			DestroyDispatcher();			
		}

		void DestroyDispatcher(){
			var dispatcher = actor.GetComponent<AnimatorDispatcher>();
			if (dispatcher != null){
				DestroyImmediate(dispatcher);
			}
		}
	}
}