#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Slate{

	///Utilities specific to Cutscenes
	public static class CutsceneUtility {

		[System.NonSerialized]
		private static string copyJson;
		[System.NonSerialized]
		private static System.Type copyType;
		[System.NonSerialized]
		private static IDirectable _selectedObject;

		public static IDirectable selectedObject{
			get {return _selectedObject;}
			set
			{
				//select the root cutscene which in turns display the inspector of the object within it.
				if (value != null){	UnityEditor.Selection.activeObject = value.root.context; }
				_selectedObject = value;
			}
		}

		public static System.Type GetCopyType(){
			return copyType;
		}

		public static void SetCopyType(System.Type type){
			copyType = type;
		}

		public static void CopyClip(ActionClip action){
			copyJson = JsonUtility.ToJson(action, false);
			copyType = action.GetType();
		}

		public static void CutClip(ActionClip action){
			copyJson = JsonUtility.ToJson(action, false);
			copyType = action.GetType();
			(action.parent as CutsceneTrack).DeleteAction(action);
		}

		public static ActionClip PasteClip(CutsceneTrack track, float time){
			if (copyType != null){
				var newAction = track.AddAction(copyType, time);
				JsonUtility.FromJsonOverwrite(copyJson, newAction);
				newAction.startTime = time;
				return newAction;
			}
			return null;
		}		
	}
}

#endif