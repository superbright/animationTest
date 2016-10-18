#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Slate{

	[CustomEditor(typeof(ActionClips.PlayAudio))]
	public class PlayAudioInspector : ActionClipInspector<ActionClips.PlayAudio> {

		public override void OnInspectorGUI(){

			base.OnInspectorGUI();

			if (GUILayout.Button("Set Audio Clip")){
				EditorGUIUtility.ShowObjectPicker<AudioClip>(action.audioClip, false, "t:AudioClip", 0);
			}

			if (Event.current.commandName == "ObjectSelectorUpdated"){
				action.audioClip = (AudioClip)EditorGUIUtility.GetObjectPickerObject();
				action.length = action.audioClip.length;
			}

			if (action.audioClip != null && GUILayout.Button("Set at Clip Length")){
				action.length = action.audioClip.length;	
			}
		}
	}
}

#endif