#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Slate{

	[CustomEditor(typeof(DirectorCamera))]
	public class DirectorCameraInspector : Editor {

		public override void OnInspectorGUI(){
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("This is the master Director Camera Root. The child 'Render Camera' is from within where all cutscenes are rendered from. You can add any Image Effects in that Camera and even animate them if so required by using a Properties Track in the Director Group.\n\nMatch Main, will copy the Main Camera settings to Render Camera when it becomes active.\n\nSet Main, will set the Render Camera as MainCamera (Camera.main) for the duration of cutscenes.", MessageType.Info);
			DirectorCamera.matchMainCamera = EditorGUILayout.Toggle("Match Main When Active", DirectorCamera.matchMainCamera);
			DirectorCamera.setMainWhenActive = EditorGUILayout.Toggle("Set Main When Active", DirectorCamera.setMainWhenActive);
		}
	}
}

#endif