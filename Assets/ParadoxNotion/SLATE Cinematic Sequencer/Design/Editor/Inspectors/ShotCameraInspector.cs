#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Slate{

	[CustomEditor(typeof(ShotCamera))]
	public class ShotCameraInspector : Editor {

		private bool lookThrough;
		private Vector3 lastPos;
		private Quaternion lastRot;

		private ShotCamera shot{
			get {return target as ShotCamera;}
		}

		public override void OnInspectorGUI(){

			GUI.backgroundColor = lookThrough? new Color(1,0.4f,0.4f) : Color.white;
			if (GUILayout.Button(lookThrough? "Stop Adjusting" : "Adjust In Scene View")){
				lookThrough = !lookThrough;
			}
			GUI.backgroundColor = Color.white;

			shot.fieldOfView = EditorGUILayout.Slider("Field Of View", shot.fieldOfView, 5, 170);
			shot.focalPoint = EditorGUILayout.Slider("Focal Point", shot.focalPoint, 0, 1);
			if (GUI.changed){
				EditorUtility.SetDirty(shot);
			}
		}

		void OnSceneGUI(){

			Handles.color = Prefs.gizmosColor;
			Handles.Label(shot.position + new Vector3(0,0.5f,0), shot.gameObject.name);
			Handles.color = Color.white;

			var sc = SceneView.lastActiveSceneView;
			if (lookThrough && sc != null){

				if (sc.camera.transform.position != lastPos || sc.camera.transform.rotation != lastRot){
					shot.position = sc.camera.transform.position;
					shot.rotation = sc.camera.transform.rotation;
					EditorUtility.SetDirty(shot.gameObject);
				}

				lastPos = sc.camera.transform.position;
				lastRot = sc.camera.transform.rotation;
			}
		}
	}
}

#endif