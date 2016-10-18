using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate.ActionClips{

	[Category("Renderer")]
	public class SetMaterialTexture : ActorActionClip<Renderer> {

		[SerializeField] [HideInInspector]
		private float _length;

		public Texture texture;

		private Material sharedMat;
		private Material instanceMat;
		private bool temporary;

		public override string info{
			get {return string.Format("Set Texture\n{0}", texture? texture.name : "null");}
		}

		public override float length{
			get {return _length;}
			set {_length = value;}
		}

		protected override void OnEnter(){ temporary = length > 0; DoSet(); }
		protected override void OnReverseEnter(){ if (temporary) DoSet(); }

		protected override void OnReverse(){DoReset();}
		protected override void OnExit(){ if (temporary) DoReset(); }


		void DoSet(){
			sharedMat = actor.sharedMaterial;
			instanceMat = Instantiate(sharedMat);
			actor.material = instanceMat;
			instanceMat.mainTexture = texture;
		}

		void DoReset(){
			DestroyImmediate(instanceMat);
			actor.sharedMaterial = sharedMat;
		}
	}
}