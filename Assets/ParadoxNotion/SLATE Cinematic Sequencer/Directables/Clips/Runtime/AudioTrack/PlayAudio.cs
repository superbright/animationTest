using UnityEngine;
using System.Collections;

namespace Slate.ActionClips{

	[Name("Audio Clip")]
	[Description("The audio clip will be send to the AudioMixer selected in it's track if any. You can trim or loop the audio by scaling the clip and you can optionaly show subtitles as well.")]
	[Attachable(typeof(ActorAudioTrack), typeof(DirectorAudioTrack))]
	public class PlayAudio : ActionClip, ISubClipContainable {

		[SerializeField] [HideInInspector]
		private float _length = 1f;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0.25f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0.25f;
		
		public AudioClip audioClip;
		public float clipOffset;
		[Multiline(5)]
		public string subtitlesText;
		public Color subtitlesColor = Color.white;

		float ISubClipContainable.subClipOffset{
			get {return clipOffset;}
			set {clipOffset = value;}
		}

		public override float length{
			get { return _length;}
			set	{_length = value;}
		}

		public override float blendIn{
			get {return _blendIn;}
			set {_blendIn = value;}
		}

		public override float blendOut{
			get {return _blendOut;}
			set {_blendOut = value;}
		}

		public override bool isValid{
			get {return audioClip != null;}
		}

		public override string info{
			get { return isValid? (string.IsNullOrEmpty(subtitlesText)? audioClip.name : string.Format("<i>'{0}'</i>", subtitlesText) ): base.info; }
		}

		private AudioSource source{
			get {return AudioSampler.GetSourceForID(parent);}
		}

	
		protected override void OnEnter(){
			source.clip = audioClip;
		}

		protected override void OnUpdate(float time, float previousTime){
			var weight = Easing.Ease(EaseType.QuadraticInOut, 0, 1, GetClipWeight(time));
			var totalVolume = weight * (parent as AudioTrack).weight; //put 'weight' in interface?
			
			AudioSampler.SampleForID(parent, audioClip, time - clipOffset, previousTime - clipOffset, totalVolume);

			if (!string.IsNullOrEmpty(subtitlesText)){
				var lerpColor = subtitlesColor;
				lerpColor.a = weight;
				DirectorGUI.UpdateSubtitles(string.Format("{0}{1}", parent is ActorAudioTrack? (actor.name + ": ") : "", subtitlesText), lerpColor);
			}
		}

		protected override void OnExit(){
			source.clip = null;
		}

		protected override void OnReverse(){
			source.clip = null;
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnClipGUI(Rect rect){
			if (audioClip != null){
				var totalWidth = rect.width;
				var audioRect = rect;
				audioRect.width = (audioClip.length/length) * totalWidth;
				var t = EditorTools.GetAudioClipTexture(audioClip, (int)audioRect.width, (int)audioRect.height);
				if (t != null){
					UnityEditor.Handles.color = new Color(0,0,0,0.2f);
					GUI.color = new Color(0.4f, 0.435f, 0.576f);
					audioRect.yMin += 2;
					audioRect.yMax -= 2;
					for (var f = clipOffset; f < length; f += audioClip.length){
						audioRect.x = (f/length) * totalWidth;
						rect.x = audioRect.x;
						GUI.DrawTexture(audioRect, t);
						UnityEditor.Handles.DrawLine(new Vector2( rect.x, 0 ), new Vector2( rect.x, rect.height ));
					}
					UnityEditor.Handles.color = Color.white;
					GUI.color = Color.white;
				}
			}
		}			

		#endif
	}
}