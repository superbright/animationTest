using UnityEngine;
using System.Collections;

namespace Slate.ActionClips{

	[Category("Control")]
	public class SampleParticleSystem : DirectorActionClip {

		[SerializeField] [HideInInspector]
		private float _length = 1f;

		public ParticleSystem particles;

		private ParticleSystem.EmissionModule em;

		public override string info{
			get {return string.Format("Particles ({0})\n{1}", particles && particles.loop? "Looping" : "OneShot", particles? particles.gameObject.name : "NONE"); }
		}

		public override bool isValid{
			get {return particles != null;}
		}

		public override float length{
			get	{return particles == null || particles.loop? _length : particles.duration + particles.startLifetime;}
			set {_length = value;}
		}
		
		public override float blendOut{
			get {return isValid && !particles.loop? particles.startLifetime : 0.1f;}
		}


		protected override void OnEnter(){
			em = particles.emission;
			em.enabled = true;
			particles.Play();
		}

		protected override void OnUpdate(float time){
			if (!Application.isPlaying){
				em.enabled = time < length;
				particles.Simulate(time);
			}
		}

		protected override void OnExit(){
			em.enabled = false;
			particles.Stop();
		}
	}
}