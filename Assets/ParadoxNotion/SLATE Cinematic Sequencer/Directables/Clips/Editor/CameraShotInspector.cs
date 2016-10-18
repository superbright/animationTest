#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Slate{

	[CustomEditor(typeof(CameraShot))]
	public class CameraShotInspector : ActionClipInspector<CameraShot> {

		void OnEnable(){
			action.lookThrough = false;
		}

		void OnDisable(){
			action.lookThrough = false;
		}

		public override void OnInspectorGUI(){

			base.ShowCommonInspector();

			var e = Event.current;

			if (action.parent.children.OfType<CameraShot>().FirstOrDefault() == action){
				if (action.blendInEffect == CameraShot.BlendInEffectType.EaseIn){
					EditorGUILayout.HelpBox("The 'Ease In' option has no effect in the first shot clip of the track.", MessageType.Warning);
				}
				if (action.blendInEffect == CameraShot.BlendInEffectType.CrossDissolve){
					EditorGUILayout.HelpBox("The 'Cross Dissolve' option has no usable effect in the first shot clip of the track.", MessageType.Warning);
				}
			}


			if (action.shotAnimationMode == CameraShot.ShotAnimationMode.UseExternalAnimationClip){
				action.externalAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("External Animation Clip", action.externalAnimationClip, typeof(AnimationClip), true);
			}

			if (GUILayout.Button("Select Shot")){
				if (action.targetShot == null || EditorUtility.DisplayDialog("Change Shot", "Selecting a new target shot will reset all animation data of this clip.", "OK", "Cancel")){
					ShotPicker.Show(Event.current.mousePosition, (shot)=> { action.targetShot = shot; } );
				}
			}

			if (action.targetShot == null && GUILayout.Button("Create Shot")){
				action.targetShot = ShotCamera.Create(action.root.context.transform);
			}


			if (action.targetShot != null){

				if (GUILayout.Button("Find in Scene")){
					Selection.activeGameObject = action.targetShot.gameObject;
				}

				var lastRect = GUILayoutUtility.GetLastRect();
				var rect = new Rect(lastRect.x, lastRect.yMax + 5, lastRect.width, 200);
				
				var res = EditorTools.GetGameViewSize();
				var texture = EditorTools.GetCameraTexture(action.targetShot.cam, (int)res.x, (int)res.y);
				var style = new GUIStyle("Box");
				style.alignment = TextAnchor.MiddleCenter;
				GUI.Box(rect, texture, style);

				GUILayout.Space(205);

				var helpRect = new Rect(rect.x + 10, rect.yMax - 20, rect.width - 20, 16);
				GUI.color = EditorGUIUtility.isProSkin? new Color(0,0,0,0.6f) : new Color(1,1,1,0.6f);
				GUI.DrawTexture(helpRect, Slate.Styles.whiteTexture);
				GUI.color = Color.white;
				GUI.Label(helpRect, "Left: Rotate, Middle: Pan, Right: Dolly, Alt+Right: Zoom");

				if (rect.Contains(e.mousePosition)){
					EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
					if (e.type == EventType.MouseDrag){

						Undo.RecordObject(action.targetShot.transform, "Shot Change");
						Undo.RecordObject(action.targetShot.cam, "Shot Change");
						Undo.RecordObject(action.targetShot, "Shot Change");

						//look
						if (e.button == 0){
							var deltaRot = new Vector3(e.delta.y, e.delta.x, 0) * 0.5f;
							action.targetShot.localEulerAngles += deltaRot;
							e.Use();
						}
						//pan
						if (e.button == 2){
							var deltaPos = new Vector3(-e.delta.x, e.delta.y, 0) * (e.shift? 0.01f : 0.05f);
							action.targetShot.transform.Translate(deltaPos);
							e.Use();
						}
						//dolly in/out
						if (e.button == 1 && !e.alt){
							action.targetShot.transform.Translate(0, 0, e.delta.x * 0.05f);
							e.Use();
						}
						//fov
						if (e.button == 1 && e.alt){
							action.fieldOfView -= e.delta.x;
							e.Use();
						}

						EditorUtility.SetDirty(action.targetShot.transform);
						EditorUtility.SetDirty(action.targetShot.cam);
						EditorUtility.SetDirty(action.targetShot);
					}
				}

				if (action.shotAnimationMode == CameraShot.ShotAnimationMode.UseInternal){
					base.ShowAnimatableParameters();
				} else {
					action.fieldOfView = EditorGUILayout.Slider("Field Of View", action.fieldOfView, 5, 170);
					action.focalPoint = EditorGUILayout.Slider("Focal Point", action.focalPoint, 0, 1);					
				}
			}
		}
	}
}

#endif