using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace Slate{

	[Name("Audio Track")]
	[Description("All audio clips played by this track will be send to the selected AudioMixer if any.")]
	///AudioTracks are able to play AudioClips through the PlayAudio ActionClip
	abstract public class AudioTrack : CutsceneTrack {

		[SerializeField]
		private AudioMixerGroup _outputMixer;
		[SerializeField] [Range(0,1)]
		private float _masterVolume = 1f;
		[SerializeField] [Range(0,1)]
		private float _spatialBlend;
		[SerializeField] [Range(-1,1)]
		private float _stereoPan;

		private AudioSource source;

		public override string info{
			get {return string.Format("Mixer: {0} ({1})", mixer != null? mixer.name : "NONE", weight.ToString("0.0"));}
		}

		public float weight{
			get {return _masterVolume;}
		}

		public AudioMixerGroup mixer{
			get {return _outputMixer;}
			set	{_outputMixer = value;}
		}

		public float spatialBlend{
			get {return _spatialBlend;}
			set {_spatialBlend = value;}
		}

		public float stereoPan{
			get {return _stereoPan;}
			set {_stereoPan = value;}
		}


		protected override void OnEnter(){
			source = AudioSampler.GetSourceForID(this);
			ApplySettings();
		}

		protected override void OnUpdate(float time, float previousTime){
			if (source != null && !(parent is DirectorGroup)){
				source.transform.position = actor.transform.position;
			}
		}

		protected override void OnExit(){
			AudioSampler.ReleaseSourceForID(this);
		}

		protected override void OnReverseEnter(){
			source = AudioSampler.GetSourceForID(this);
			ApplySettings();
		}

		protected override void OnReverse(){
			AudioSampler.ReleaseSourceForID(this);
		}


		void ApplySettings(){
			if (source != null){
				source.outputAudioMixerGroup = mixer;
				source.volume                = weight;
				source.spatialBlend          = spatialBlend;
				source.panStereo             = stereoPan;
			}
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
	
		public override Texture icon{
			get {return Styles.audioIcon;}
		}

		#endif


	}
}