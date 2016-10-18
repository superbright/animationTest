using UnityEngine;
using System.Collections.Generic;

namespace Slate{

	///Can store a complete hierarchy transform pose
	public class TransformSnapshot{

		struct TransformData{
			public Transform transform;
			public Transform parent;
			public Vector3 pos;
			public Quaternion rot;
			public Vector3 scale;
			public TransformData(Transform transform, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale){
				this.transform = transform;
				this.parent = parent;
				this.pos = pos;
				this.rot = rot;
				this.scale = scale;
			}
		}

		private List<TransformData> data;

		public TransformSnapshot(GameObject root){
			Store(root);
		}

		public void Store(GameObject root){
			if (root == null) return;
			data = new List<TransformData>();
			foreach (var transform in root.GetComponentsInChildren<Transform>(true)){
				data.Add(new TransformData(transform, transform.parent, transform.localPosition, transform.localRotation, transform.localScale));
			}
		}

		public void Restore(){
			foreach (var d in data){
				
				if (d.transform == null){
					continue;
				}

				d.transform.parent = d.parent;
				d.transform.localPosition = d.pos;
				d.transform.localRotation = d.rot;
				d.transform.localScale = d.scale;
			}
		}
	}
}