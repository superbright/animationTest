#if UNITY_EDITOR && UNITY_5_4_OR_NEWER

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Slate{

	[CustomEditor(typeof(ActionClips.PlayAnimatorClip))]
	public class PlayAnimatorClipInspector : ActionClipInspector<ActionClips.PlayAnimatorClip> {

		public override void OnInspectorGUI(){

			base.OnInspectorGUI();

			if (GUILayout.Button("Set Animation Clip")){
				EditorGUIUtility.ShowObjectPicker<AnimationClip>(action.animationClip, false, "t:AnimationClip", 0);
			}

			if (Event.current.commandName == "ObjectSelectorUpdated"){
				action.animationClip = (AnimationClip)EditorGUIUtility.GetObjectPickerObject();
				action.length = action.animationClip.length;
			}

			if (action.animationClip != null && GUILayout.Button("Set at Clip Length")){
				action.length = action.animationClip.length;	
			}
		}
	}
}

#endif