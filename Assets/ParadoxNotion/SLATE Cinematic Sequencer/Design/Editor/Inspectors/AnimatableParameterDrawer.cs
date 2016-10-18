#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Slate{

	[CustomPropertyDrawer(typeof(ActionClip.AnimatableParameterAttribute))]
	public class AnimatableParameterDrawer : PropertyDrawer {

		public override float GetPropertyHeight(SerializedProperty prop, GUIContent label){ return -2; }
		public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label){
			var clip = prop.serializedObject.targetObject as ActionClip;
			if (clip != null){
				var animParam = clip.GetParameter(fieldInfo.Name);
				if (animParam != null){
					AnimatableParameterEditor.ShowParameter(animParam, clip, prop);
				}
			}
		}
	}
}

#endif