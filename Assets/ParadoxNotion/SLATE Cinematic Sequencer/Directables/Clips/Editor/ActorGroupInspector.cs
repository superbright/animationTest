#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Slate{

	[CustomEditor(typeof(ActorGroup))]
	public class ActorGroupInspector : CutsceneGroupInspector {

		private ActorGroup group{
			get {return (ActorGroup)target;}
		}

		public override void OnInspectorGUI(){

			base.OnInspectorGUI();

			group.referenceMode = (CutsceneGroup.ActorReferenceMode)EditorGUILayout.EnumPopup("Reference Mode", group.referenceMode);
			group.initialTransformation = (CutsceneGroup.ActorInitialTransformation)EditorGUILayout.EnumPopup("Initial Coordinates", group.initialTransformation);
			if (group.initialTransformation == CutsceneGroup.ActorInitialTransformation.UseLocal){
				group.initialLocalPosition = EditorGUILayout.Vector3Field("Initial Local Position", group.initialLocalPosition);
				group.initialLocalRotation = EditorGUILayout.Vector3Field("Initial Local Rotation", group.initialLocalRotation);
			}

			if (group.actor != null && group.actor.transform.parent != null){
				EditorGUILayout.HelpBox("For best workflow, it is highly recommended for actors to not have a transform parent.", MessageType.Info);
			}
		}
	}
}

#endif