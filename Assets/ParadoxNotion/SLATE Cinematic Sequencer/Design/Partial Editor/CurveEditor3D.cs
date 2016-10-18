#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Slate{

	///Draw a 3D curve editor in scene view.
	public static class CurveEditor3D {

		private static Dictionary<AnimationCurve[], CurveEditor3DRenderer> cache = new Dictionary<AnimationCurve[], CurveEditor3DRenderer>();
		public static void Draw3DCurve(AnimationCurve[] curves, Object serializeContext, Transform transformContext, float time, float timeSpan = 50f){
			CurveEditor3DRenderer editor = null;
			if (!cache.TryGetValue(curves, out editor)){
				cache[curves] = editor = new CurveEditor3DRenderer();
			}
			editor.Draw3DCurve(curves, serializeContext, transformContext, time, timeSpan);
		}

		class CurveEditor3DRenderer {

			enum ContextAction{
				None,
				SetBrokenMode,
				SetTangentMode,
				Delete
			}

			const float RESOLUTION = 0.1f;
			const float THRESHOLD = 0.01f;
			const float HANDLE_DISTANCE_COMPENSATION = 2f;

			private int lastCurveLength;
			private int kIndex = -1;
			private ContextAction contextAction;
			private bool contextBrokenMode;
			private TangentMode contextTangentMode;

			///Display curves that belong to serializeContext and transformContext parent, at time and with timeSpan.
			public void Draw3DCurve(AnimationCurve[] curves, Object serializeContext, Transform transformContext, float time, float timeSpan = 50f){

				if (curves == null || curves.Length != 3){
					return;
				}

				var e = Event.current;

				var curveX = curves[0];
				var curveY = curves[1];
				var curveZ = curves[2];


				var start = (float)Mathf.FloorToInt(time - (timeSpan/2));
				var end = (float)Mathf.CeilToInt(time + (timeSpan/2));

				//1st pass. Keyframes.
				if (curveX.length == curveY.length && curveY.length == curveZ.length){

					if (curveX.length != lastCurveLength){
						lastCurveLength = curveX.length;
						kIndex = 0;
					}


					for (var k = 0; k < curveX.length; k++){

						EditorGUI.BeginChangeCheck();
						var forceChanged = false;

						var keyX = curveX[k];
						var keyY = curveY[k];
						var keyZ = curveZ[k];

						if (keyX.time < start){	continue; }
						if (keyX.time > end){ break; }

						var tangentModeX = KeyframeUtility.GetKeyTangentMode(keyX, 0);
						var tangentModeY = KeyframeUtility.GetKeyTangentMode(keyY, 0);
						var tangentModeZ = KeyframeUtility.GetKeyTangentMode(keyZ, 0);
						var haveSameTangents = tangentModeX == tangentModeY && tangentModeY == tangentModeZ;
						var tangentMode = haveSameTangents? tangentModeX : TangentMode.Editable;
						var isBroken = KeyframeUtility.GetIsKeyBroken(keyX) && KeyframeUtility.GetIsKeyBroken(keyY) && KeyframeUtility.GetIsKeyBroken(keyZ);


						var value = new Vector3(keyX.value, keyY.value, keyZ.value);

						if (transformContext != null){
							value = transformContext.TransformPoint(value);
						}

						Handles.Label(value, keyX.time.ToString("0.00"));


						///MOUSE EVENTS
						var screenPos = HandleUtility.WorldToGUIPoint(value);
						if (((Vector2)screenPos - e.mousePosition).magnitude < 10){
							if (e.type == EventType.MouseDown && kIndex == k){
								if (e.button == 1){
									var menu = new GenericMenu();
									menu.AddItem( new GUIContent("Smooth"), tangentMode == TangentMode.Smooth, ()=>	{ contextAction = ContextAction.SetTangentMode; contextTangentMode = TangentMode.Smooth; });
									menu.AddItem( new GUIContent("Linear"), tangentMode == TangentMode.Linear, ()=>	{ contextAction = ContextAction.SetTangentMode; contextTangentMode = TangentMode.Linear; });
									menu.AddItem( new GUIContent("Constant"), tangentMode == TangentMode.Constant, ()=> { contextAction = ContextAction.SetTangentMode; contextTangentMode = TangentMode.Constant; });
									menu.AddItem( new GUIContent("Editable"), tangentMode == TangentMode.Editable, ()=> { contextAction = ContextAction.SetTangentMode; contextTangentMode = TangentMode.Editable; });
									if (tangentMode == TangentMode.Editable){
										menu.AddItem( new GUIContent("Tangents/Connected"), !isBroken, ()=> { contextAction = ContextAction.SetBrokenMode; contextBrokenMode = false; });
										menu.AddItem( new GUIContent("Tangents/Broken"), isBroken, ()=>	{ contextAction = ContextAction.SetBrokenMode; contextBrokenMode = true; });
									}
									menu.AddSeparator("/");
									menu.AddItem( new GUIContent("Delete"), false, ()=>	{ contextAction = ContextAction.Delete; });
									menu.ShowAsContext();
								}
							}

							if (e.type == EventType.MouseDown && e.button == 0){
								if (kIndex != k){
									kIndex = k;
									GUIUtility.hotControl = 0;
									SceneView.RepaintAll();
									e.Use();
								}
							}
						}

						///APPLY CONTEXT ACTIONS
						if (contextAction != ContextAction.None && k == kIndex){
							var _contextAction = contextAction;
							contextAction = ContextAction.None;
							forceChanged = true;
							if (_contextAction == ContextAction.SetBrokenMode){
								keyX = KeyframeUtility.SetKeyBroken(keyX, contextBrokenMode);
								keyY = KeyframeUtility.SetKeyBroken(keyY, contextBrokenMode);
								keyZ = KeyframeUtility.SetKeyBroken(keyZ, contextBrokenMode);
							}
							if (_contextAction == ContextAction.SetTangentMode){
								Undo.RecordObject(serializeContext, "Animation Curve Change");
								keyX = KeyframeUtility.GetNewModeFromExistingKey(keyX, contextTangentMode);
								keyY = KeyframeUtility.GetNewModeFromExistingKey(keyY, contextTangentMode);
								keyZ = KeyframeUtility.GetNewModeFromExistingKey(keyZ, contextTangentMode);

								curveX.MoveKey(k, keyX);
								curveY.MoveKey(k, keyY);
								curveZ.MoveKey(k, keyZ);

								curveX.UpdateTangentsFromMode();
								curveY.UpdateTangentsFromMode();
								curveZ.UpdateTangentsFromMode();
								return;
							}
							if (_contextAction == ContextAction.Delete){
								Undo.RecordObject(serializeContext, "Animation Curve Change");
								curveX.RemoveKey(k);
								curveY.RemoveKey(k);
								curveZ.RemoveKey(k);
								kIndex = -1;
								return;
							}
						}


						///POSITION
						var pointSize = HandleUtility.GetHandleSize(value) * 0.05f;
						var newValue = value;
						if (kIndex == k){
							newValue = Handles.FreeMoveHandle(value, Quaternion.identity, pointSize, Vector3.zero, Handles.RectangleCap);
						} else {
							var cam = SceneView.lastActiveSceneView.camera;
							Handles.RectangleCap(0, value, cam.transform.rotation, pointSize);
						}


						if (transformContext != null){
							newValue = transformContext.InverseTransformPoint(newValue);
						}

						keyX.value = newValue.x;
						keyY.value = newValue.y;
						keyZ.value = newValue.z;


						///TANGENTS
						if (haveSameTangents && tangentMode == TangentMode.Editable){

							if (kIndex == k){

								if (k != 0){
									var inHandle = new Vector3(-keyX.inTangent, -keyY.inTangent, -keyZ.inTangent);
									inHandle /= HANDLE_DISTANCE_COMPENSATION;
									inHandle = newValue + inHandle;
									if (transformContext != null){
										inHandle = transformContext.TransformPoint(inHandle);
									}
									var handleSize = HandleUtility.GetHandleSize(inHandle) * 0.05f;
									var newInHandle = Handles.FreeMoveHandle(inHandle, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleCap);
									Handles.DrawLine(value, newInHandle);
									if (transformContext != null){
										newInHandle = transformContext.InverseTransformPoint(newInHandle);
									}

									newInHandle -= newValue;
									newInHandle *= HANDLE_DISTANCE_COMPENSATION;
									keyX.inTangent = -newInHandle.x;
									keyY.inTangent = -newInHandle.y;
									keyZ.inTangent = -newInHandle.z;
									if (!isBroken){
										keyX.outTangent = keyX.inTangent;
										keyY.outTangent = keyY.inTangent;
										keyZ.outTangent = keyZ.inTangent;
									}
								}

								if (k < curveX.length -1 ){
									var outHandle = new Vector3(keyX.outTangent, keyY.outTangent, keyZ.outTangent);
									outHandle /= HANDLE_DISTANCE_COMPENSATION;
									outHandle = newValue + outHandle;
									if (transformContext != null){
										outHandle = transformContext.TransformPoint(outHandle);
									}
									var handleSize = HandleUtility.GetHandleSize(outHandle) * 0.05f;
									var newOutHandle = Handles.FreeMoveHandle(outHandle, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleCap);
									Handles.DrawLine(value, newOutHandle);
									if (transformContext != null){
										newOutHandle = transformContext.InverseTransformPoint(newOutHandle);
									}
									newOutHandle -= newValue;
									newOutHandle *= HANDLE_DISTANCE_COMPENSATION;
									keyX.outTangent = newOutHandle.x;
									keyY.outTangent = newOutHandle.y;
									keyZ.outTangent = newOutHandle.z;
									if (!isBroken){
										keyX.inTangent = keyX.outTangent;
										keyY.inTangent = keyY.outTangent;
										keyZ.inTangent = keyZ.outTangent;
									}
								}
							}

						}
/*
						///TIMING
						if (kIndex == k){
							var t = keyX.time;
							t = Handles.ScaleSlider(t, newValue, Vector3.down, Quaternion.identity, HandleUtility.GetHandleSize(value) * 0.5f, 0f);
							var min = k > 0? curveX[k-1].time + 0.01f : start;
							var max = k < curveX.length-1? curveX[k+1].time - 0.01f : end;
							t = Mathf.Clamp(t, min, max);
							if (t != keyX.time){
								keyX.time = t;
								keyY.time = t;
								keyZ.time = t;
							}
						}
*/
						///APPLY
						if (EditorGUI.EndChangeCheck() || forceChanged){
							Undo.RecordObject(serializeContext, "Animation Curve Change");
							curveX.MoveKey(k, keyX);
							curveY.MoveKey(k, keyY);
							curveZ.MoveKey(k, keyZ);
							EditorUtility.SetDirty(serializeContext);
						}
					}
				}


				///2nd pass. Path
				for (var t = start; t <= end; t += RESOLUTION){
					var value = new Vector3(curveX.Evaluate(t), curveY.Evaluate(t), curveZ.Evaluate(t));
					var nextValue = new Vector3(curveX.Evaluate(t + RESOLUTION), curveY.Evaluate(t + RESOLUTION), curveZ.Evaluate(t + RESOLUTION));

					if (transformContext != null){
						value = transformContext.TransformPoint(value);
						nextValue = transformContext.TransformPoint(nextValue);
					}

					var color = Prefs.trajectoryColor;
					Handles.color = color;
					Handles.SphereCap(0, value, Quaternion.identity, 0.02f);

					if ((nextValue - value).magnitude > THRESHOLD ){
						Handles.DrawLine(value, nextValue);
					}
					
					Handles.color = Color.white;
				}
			}
		}


	}
}

#endif