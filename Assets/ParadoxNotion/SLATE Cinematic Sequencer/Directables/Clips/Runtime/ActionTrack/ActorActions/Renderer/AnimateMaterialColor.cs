using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate.ActionClips{

	[Category("Renderer")]
	public class AnimateMaterialColor : ActorActionClip<Renderer> {

		[SerializeField] [HideInInspector]
		private float _length = 1;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0.2f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0.2f;

		[AnimatableParameter]
		public Color color = Color.white;
		public EaseType interpolation = EaseType.QuadraticInOut;

		private Color originalColor;
		private Material sharedMat;
		private Material instanceMat;

		public override string info{
			get {return "Material Color";}
		}

		public override bool isValid{
			get {return actor != null && actor.sharedMaterial != null;}
		}

		public override float length{
			get {return _length;}
			set {_length = value;}
		}

		public override float blendIn{
			get {return _blendIn;}
			set {_blendIn = value;}
		}

		public override float blendOut{
			get {return _blendOut;}
			set {_blendOut = value;}
		}

		protected override void OnEnter(){DoSet();}
		protected override void OnReverseEnter(){DoSet();}

		protected override void OnUpdate(float deltaTime){
			var lerpColor = Easing.Ease(interpolation, originalColor, color, GetClipWeight(deltaTime));
			instanceMat.color = lerpColor;
		}

		protected override void OnReverse(){DoReset();}
		protected override void OnExit(){DoReset();}


		void DoSet(){
			sharedMat = actor.sharedMaterial;
			instanceMat = Instantiate(sharedMat);
			actor.material = instanceMat;
			originalColor = instanceMat.color;
		}

		void DoReset(){
			DestroyImmediate(instanceMat);
			actor.sharedMaterial = sharedMat;
		}
	}
}