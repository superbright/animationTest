#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	public static class AnimatableParameterEditor {

		private static bool isDraggingTime;
		private static Dictionary<AnimatedParameter, Rect> fixedCurveRects = new Dictionary<AnimatedParameter, Rect>();

		public static void ShowParameter(AnimatedParameter animParam, ActionClip action, SerializedProperty serializedProperty = null){

			if (!animParam.isValid){
				GUILayout.Label("Animatable Parameter is invalid");
				return;
			}

			var e = Event.current;
			var rootTime = action.root.currentTime;
			var localTime = Mathf.Clamp(rootTime - action.startTime, 0, action.length);
			var isRecording = action.RootTimeWithinRange();
			var foldOut = EditorTools.GetObjectFoldOut(animParam);
			var hasKeyNow = animParam.HasKey(localTime);
			var hasAnyKey = animParam.HasAnyKey();
			var targetObject = action.animatedParametersTarget;
			var parameterEnabled = animParam.enabled;
			var lastRect = new Rect();

			GUI.backgroundColor = new Color(0, 0.4f, 0.4f, 0.5f);
			GUILayout.BeginVertical(Slate.Styles.headerBoxStyle);
			GUI.backgroundColor = Color.white;

			GUILayout.BeginHorizontal();


			var sFold = foldOut? "▼" : "▶";
			var sName = animParam.parameterName.SplitCamelCase();
			var sEnable = parameterEnabled? "" : " <i>(Disabled)</i>";


			GUI.backgroundColor = hasAnyKey && parameterEnabled? new Color(1,0.8f,0.8f) : Color.white;
			GUI.backgroundColor = hasAnyKey && parameterEnabled && isRecording? Slate.Styles.recordingColor : GUI.backgroundColor;
			GUILayout.Label(sFold, GUILayout.Width(13));
			lastRect = GUILayoutUtility.GetLastRect();
			GUI.enabled = !animParam.isExternal || isRecording;
			DoParameterField(string.Format("<b>{0} {1}</b>", sName, sEnable), animParam, targetObject);
			GUI.enabled = true;
			GUI.backgroundColor = Color.white;

			EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
			if (e.type == EventType.MouseDown && e.button == 0 && lastRect.Contains(e.mousePosition)){
				EditorTools.SetObjectFoldOut(animParam, !foldOut);
				e.Use();
			}



			GUI.enabled = hasAnyKey && parameterEnabled;
			if (GUILayout.Button(Slate.Styles.previousKeyIcon, GUIStyle.none, GUILayout.Height(20), GUILayout.Width(16))){
				action.root.currentTime = animParam.GetKeyPrevious(localTime) + action.startTime;
			}
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);


			GUI.enabled = parameterEnabled;
			GUI.color = hasKeyNow && parameterEnabled? new Color(1,0.3f,0.3f) : Color.white;
			if (GUILayout.Button(Slate.Styles.keyIcon, GUIStyle.none, GUILayout.Height(20), GUILayout.Width(16))){
				if (hasKeyNow){ animParam.RemoveKey(localTime); }
				else { animParam.SetKeyCurrent(targetObject, localTime); }
			}
			GUI.color = Color.white;
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);


			GUI.enabled = hasAnyKey && parameterEnabled;
			if (GUILayout.Button(Slate.Styles.nextKeyIcon, GUIStyle.none, GUILayout.Height(20), GUILayout.Width(16))){
				action.root.currentTime = animParam.GetKeyNext(localTime) + action.startTime;
			}
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);


			GUILayout.Space(2);


			GUI.enabled = hasAnyKey && parameterEnabled;
			if (GUILayout.Button(Slate.Styles.gearIcon, GUIStyle.none, GUILayout.Height(20), GUILayout.Width(16))){
				var menu = new GenericMenu();
				if (hasKeyNow){
					menu.AddDisabledItem(new GUIContent("Key"));
					menu.AddItem(new GUIContent("Remove Key"), false, ()=>{ animParam.RemoveKey(localTime); });
				} else {
					menu.AddItem(new GUIContent("Key"), false, ()=>{ animParam.SetKeyCurrent(targetObject, localTime); });
					menu.AddDisabledItem(new GUIContent("Remove Key"));
				}
				menu.AddItem(new GUIContent("Post Wrap Mode/Once"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.Once); });
				menu.AddItem(new GUIContent("Post Wrap Mode/Loop"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.Loop); });
				menu.AddItem(new GUIContent("Post Wrap Mode/PingPong"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.PingPong); });

				menu.AddItem(new GUIContent("Pre Wrap Mode/Once"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.Once); });
				menu.AddItem(new GUIContent("Pre Wrap Mode/Loop"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.Loop); });
				menu.AddItem(new GUIContent("Pre Wrap Mode/PingPong"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.PingPong); });
				menu.AddSeparator("/");
				menu.AddItem(new GUIContent("Remove Animation"), false, ()=>
				{
					if (EditorUtility.DisplayDialog("Remove Animation", "All animation keys will be removed for this parameter.\nAre you sure?", "Yes", "No")){
						animParam.Reset();
					}
				});
				menu.ShowAsContext();
				e.Use();
			}
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);


			GUI.enabled = true;
			GUILayout.EndHorizontal();

			//...
			GUILayout.EndVertical();


			if (EditorGUILayout.BeginFadeGroup(EditorTools.GetObjectFoldOutFaded(animParam))){

				GUI.color = new Color(0.5f,0.5f,0.5f,0.3f);
				GUILayout.BeginVertical(Slate.Styles.clipBoxFooterStyle);
				GUI.color = Color.white;

				string info = null;
				if (!parameterEnabled){
					info = "Parameter is disabled or overriden.";
				}

				if (info == null && !hasAnyKey){
					info = "Parameter is not yet animated. You can make it so by creating the first key.";
				}

				if (info == null && action.length == 0 && hasAnyKey){
					info = "Length of Clip is zero. Can not display Curve Editor.";
				}

				if (info != null){

					GUILayout.Label(info);

				} else {

					if (animParam.isExternal && !isRecording){
						GUILayout.Label("This Parameter can only be edited when time is within the clip range.");
					}

					DoCurveBox(animParam, action);
				}

				GUILayout.EndVertical();
				GUILayout.Space(5);
			}

			EditorGUILayout.EndFadeGroup();			
		}


		static void DoCurveBox(AnimatedParameter animParam, ActionClip action){
			
			var e = Event.current;
			var rootTime = action.root.currentTime;
			var localTime = Mathf.Clamp(rootTime - action.startTime, 0, action.length);

			GUILayout.Label("INVISIBLE TEXT", GUILayout.Height(0));
			var lastRect = GUILayoutUtility.GetLastRect();
			GUILayout.Space(250);

			var timeRect = new Rect(0, 0, action.length, 0);
			var posRect = new Rect();
			if (e.type == EventType.Repaint || !fixedCurveRects.TryGetValue(animParam, out posRect)){
				posRect = new Rect(lastRect.x, lastRect.yMax + 5, lastRect.width, 240);
				fixedCurveRects[animParam] = posRect;
			}
			
			GUI.color = EditorGUIUtility.isProSkin? new Color(0,0,0,0.5f) : new Color(0,0,0,0.3f);
			GUI.Box(posRect, "", (GUIStyle)"textfield" );
			GUI.color = Color.white;

			var dragTimeRect = new Rect(posRect.x, posRect.y + 1, posRect.width, 10);
			GUI.Box(dragTimeRect, "");
			if (dragTimeRect.Contains(e.mousePosition)){
				EditorGUIUtility.AddCursorRect(dragTimeRect, MouseCursor.SplitResizeLeftRight);
				if (e.type == EventType.MouseDown && e.button == 0){
					isDraggingTime = true;
					e.Use();
				}
			}

			if (e.rawType == EventType.MouseUp){
				isDraggingTime = false;
			}

			if (e.type == EventType.KeyDown && posRect.Contains(e.mousePosition)){
				if (e.keyCode == KeyCode.Comma){
					GUIUtility.keyboardControl = 0;
					action.root.currentTime = animParam.GetKeyPrevious(localTime) + action.startTime;
					e.Use();
				}

				if (e.keyCode == KeyCode.Period){
					GUIUtility.keyboardControl = 0;
					action.root.currentTime = animParam.GetKeyNext(localTime) + action.startTime;
					Event.current.Use();
				}
			}

			if (isDraggingTime && posRect.Contains(e.mousePosition)){
				var iLerp = Mathf.InverseLerp(posRect.x, posRect.xMax, e.mousePosition.x);
				action.root.currentTime = Mathf.Lerp(action.startTime, action.endTime, iLerp);
			}

			var dopeRect = new Rect(posRect.x, dragTimeRect.yMax + 1, posRect.width, 12);
			GUI.Box(dopeRect, "");
			DopeSheetEditor.DrawDopeSheet(animParam, action, dopeRect, 0, action.length);

			var curvesRect = new Rect(posRect.x, dopeRect.yMax, posRect.width, posRect.height - dopeRect.height - dragTimeRect.height);
			CurveEditor.DrawCurves(animParam, curvesRect, timeRect);

			if (action.RootTimeWithinRange()){
				var iLerp = Mathf.InverseLerp(action.startTime, action.endTime, rootTime);
				var lerp = Mathf.Lerp(posRect.x, posRect.xMax, iLerp);
				var a = new Vector3(lerp, posRect.y, 0);
				var b = new Vector3(lerp, posRect.yMax, 0);
				Handles.color = EditorGUIUtility.isProSkin? Slate.Styles.recordingColor : Color.red;
				Handles.DrawLine(a, b);
				Handles.color = Color.white;
			}				
		}

		///Used when the Animated Parameter is a property and we don't have a SerializedProperty.
		static void DoParameterField(string name, AnimatedParameter animParam, object targetObject){
			try
			{
				if (!animParam.enabled){
					GUILayout.Label(name);
					return;					
				}

				var type = animParam.animatedType;
				var animParamAtt = animParam.animatableAttribute;
				var value = animParam.GetCurrentValue(targetObject);
				var newValue = value;

				if (type == typeof(bool)){
					newValue = EditorGUILayout.Toggle(name, (bool)value);
				}

				if (type == typeof(float)){
					if (animParamAtt != null && animParamAtt.min != null && animParamAtt.max != null){
						var min = animParamAtt.min.Value;
						var max = animParamAtt.max.Value;
						newValue = EditorGUILayout.Slider(name, (float)value, min, max);
					} else {
						newValue = EditorGUILayout.FloatField(name, (float)value);
					}
				}

				if (type == typeof(int)){
					if (animParamAtt != null && animParamAtt.min != null && animParamAtt.max != null){
						var min = animParamAtt.min.Value;
						var max = animParamAtt.max.Value;
						newValue = EditorGUILayout.IntSlider(name, (int)value, (int)min, (int)max);
					} else {
						newValue = EditorGUILayout.IntField(name, (int)value);
					}
				}

				if (type == typeof(Vector2)){
					newValue = EditorGUILayout.Vector2Field(name, (Vector2)value);
				}

				if (type == typeof(Vector3)){
					newValue = EditorGUILayout.Vector3Field(name, (Vector3)value);
				}		

				if (type == typeof(Color)){
					GUI.backgroundColor = Color.white; //to avoid tinting
					newValue = EditorGUILayout.ColorField(name, (Color)value);
				}

				if (GUI.changed && newValue != value){
					animParam.SetCurrentValue(targetObject, newValue);
				}
			}
			
			catch (System.Exception exc)
			{
				GUILayout.Label(exc.Message);
			}
		}

	}
}

#endif