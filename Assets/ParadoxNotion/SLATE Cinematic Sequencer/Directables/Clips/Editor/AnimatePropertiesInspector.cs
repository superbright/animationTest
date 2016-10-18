#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Slate{

	[CustomEditor(typeof(ActionClips.AnimateProperties))]
	public class AnimatePropertiesInspector : ActionClipInspector<ActionClips.AnimateProperties> {

		public override void OnInspectorGUI(){

			base.OnInspectorGUI();

			GUILayout.Space(10);

			if (GUILayout.Button("Add Property")){
				EditorTools.ShowAnimatedPropertySelectionMenu(action.actor.gameObject, AnimatedParameter.supportedTypes, (prop, target)=>{
					action.animationData.TryAddParameter(prop, target, action.actor.transform);
				});
			}

			if (action.isValid){
				if (GUILayout.Button("Remove Property")){
					var menu = new GenericMenu();
					foreach(var _animParam in action.animationData.animatedParameters){
						var animParam = _animParam;
						var cat = string.IsNullOrEmpty(animParam.transformHierarchyPath)? "Self/" : (animParam.transformHierarchyPath + "/");
						cat += animParam.declaringType.Name + "/";
						var path = cat + animParam.parameterName.SplitCamelCase();
						menu.AddItem(new GUIContent(path), false, ()=>{ action.animationData.animatedParameters.Remove(animParam); });
					}
					menu.ShowAsContext();
				}
			}
		}
	}
}

#endif