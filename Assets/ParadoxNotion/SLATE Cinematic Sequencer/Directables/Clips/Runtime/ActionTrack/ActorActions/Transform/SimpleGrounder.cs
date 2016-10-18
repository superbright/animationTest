using UnityEngine;
using System.Collections;

namespace Slate.ActionClips{

	[Category("Transform")]
	[Description("Grounds the actor gameobject to the nearest collider object beneath it")]
	public class SimpleGrounder : ActorActionClip {

		[SerializeField] [HideInInspector]
		private float _length = 1;

		[Range(1, 100)]
		public float maxCheckDistance = 10;
		public float offset = 0;

		private RaycastHit hit;
		private Vector3 lastPos;

		public override float length{
			get {return _length;}
			set {_length = value;}
		}

		protected override void OnEnter(){
			lastPos = actor.transform.position;
		}

		protected override void OnUpdate(float time){
			var a = new Vector3(actor.transform.position.x, actor.transform.position.y + maxCheckDistance, actor.transform.position.z);
			var b = actor.transform.position - new Vector3(0, maxCheckDistance, 0);
			if (Physics.Linecast(a, b, out hit)){
				var pos = actor.transform.position;
				pos.y = hit.point.y + offset;
				actor.transform.position = pos;
			}
		}

		protected override void OnReverse(){
			actor.transform.position = lastPos;
		}
	}
}