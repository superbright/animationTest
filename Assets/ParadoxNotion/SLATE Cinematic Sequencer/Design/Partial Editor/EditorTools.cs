#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace Slate{

	///A collections of tools for the editor only
	public static class EditorTools {

		public static void Header(string text){
			GUI.backgroundColor = new Color(0, 0, 0, 0.3f);
			GUILayout.BeginVertical(Slate.Styles.headerBoxStyle);
			GUILayout.Label(string.Format("<b>{0}</b>", text));
			GUILayout.EndHorizontal();
			GUI.backgroundColor = Color.white;
		}

		public static void BoldSeparator(){
			var tex = Slate.Styles.whiteTexture;
			var lastRect= GUILayoutUtility.GetLastRect();
			GUILayout.Space(14);
			GUI.color = new Color(0, 0, 0, 0.25f);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), tex);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 1), tex);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 9, Screen.width, 1), tex);
			GUI.color = Color.white;
		}

		///Gets the render texture of a camera
		public static RenderTexture GetCameraTexture(Camera cam, int width, int height){
			var last = cam.targetTexture;
			var rt = RenderTexture.GetTemporary(width, height);
			cam.targetTexture = rt;
			cam.Render();
			cam.targetTexture = last;
			RenderTexture.ReleaseTemporary(rt);
			return rt;
		}

		///Gets the GameView panel's size
		public static Vector2 GetGameViewSize(){
		    return Handles.GetMainGameViewSize();
		}

		///Pops a menu for animatable properties selection
		public static void ShowAnimatedPropertySelectionMenu(GameObject go, System.Type[] paramTypes, System.Action<MemberInfo, Component> callback){

			var menu = new GenericMenu();
			foreach (var _comp in go.GetComponentsInChildren<Component>()){
				var comp = _comp;

				if (comp == null){
					continue;
				}

				var path = AnimationUtility.CalculateTransformPath(comp.transform, go.transform);
				if (comp.gameObject != go){
					path = "Children/" + path;
				} else {
					path = "Self";
				}

				if (comp is Transform){
					menu.AddItem(new GUIContent(path + "/Transform/Position"), false, ()=>{ callback( typeof(Transform).GetProperty("localPosition"), comp );} );
					menu.AddItem(new GUIContent(path + "/Transform/Rotation"), false, ()=>{ callback( typeof(Transform).GetProperty("localEulerAngles"), comp);} );
					menu.AddItem(new GUIContent(path + "/Transform/Scale"), false, ()=>{ callback( typeof(Transform).GetProperty("localScale"), comp);} );
					continue;
				}

				var type = comp.GetType();
				foreach (var _prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)){
					var prop = _prop;

					if (!prop.CanRead || !prop.CanWrite){
						continue;
					}

					if (!paramTypes.Contains(prop.PropertyType)){
						continue;
					}

					var finalPath = string.Format("{0}/{1}/{2}", path, type.Name.SplitCamelCase(), prop.Name.SplitCamelCase());
					menu.AddItem(new GUIContent(finalPath), false, ()=>{ callback(prop, comp); } );
				}

				foreach (var _field in type.GetFields(BindingFlags.Instance | BindingFlags.Public)){
					var field = _field;
					if (paramTypes.Contains(field.FieldType)){
						var finalPath = string.Format("{0}/{1}/{2}", path, type.Name.SplitCamelCase(), field.Name.SplitCamelCase());
						menu.AddItem(new GUIContent(finalPath), false, ()=>{ callback(field, comp); });
					}
				}
			}

			menu.ShowAsContext();
			Event.current.Use();
		}

		///Generic Popup for selection of any element within a list
		public static T Popup<T>(string prefix, T selected, List<T> options, params GUILayoutOption[] GUIOptions){

			var index = 0;
			if (options.Contains(selected)){
				index = options.IndexOf(selected) + 1;
			}

			var stringedOptions = new List<string>();
			if (options.Count == 0){
				stringedOptions.Add("NONE AVAILABLE");
			} else {
				stringedOptions.Add("NONE");
				stringedOptions.AddRange( options.Select(o => o != null? o.ToString() : "NONE" ) );
			}

			GUI.enabled = stringedOptions.Count > 1;
			if (!string.IsNullOrEmpty(prefix)) index = EditorGUILayout.Popup(prefix, index, stringedOptions.ToArray(), GUIOptions);
			else index = EditorGUILayout.Popup(index, stringedOptions.ToArray(), GUIOptions);
			GUI.enabled = true;

			return index == 0? default(T) : options[index - 1];
		}

		///Generic Popup for selection of any element within a list without a adding NONE
		public static T CleanPopup<T>(string prefix, T selected, List<T> options, params GUILayoutOption[] GUIOptions){

			var index = -1;
			if (options.Contains(selected)){
				index = options.IndexOf(selected);
			}

			var stringedOptions = options.Select(o => o != null? o.ToString() : "NONE" );

			GUI.enabled = options.Count > 0;
			if (!string.IsNullOrEmpty(prefix)) index = EditorGUILayout.Popup(prefix, index, stringedOptions.ToArray(), GUIOptions);
			else index = EditorGUILayout.Popup(index, stringedOptions.ToArray(), GUIOptions);
			GUI.enabled = true;

			return index == -1? default(T) : options[index];
		}


		public struct TypeMetaInfo{
			public Type type;
			public string name;
			public string category;
			public Type[] attachableTypes;
		}

		//Get all non abstract derived types of base type in the current loaded assemplies
		public static List<TypeMetaInfo> GetTypeMetaDerivedFrom(Type baseType){
			var infos = new List<TypeMetaInfo>();
			foreach(var type in RuntimeTools.GetDerivedTypesOf(baseType)){
				
				if (type.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).FirstOrDefault() != null){
					continue;
				}

				var info = new TypeMetaInfo();
				info.type = type;

				var nameAtt = type.GetCustomAttributes(typeof(NameAttribute), true).FirstOrDefault() as NameAttribute;
				info.name = nameAtt != null? nameAtt.name : type.Name.SplitCamelCase();

				var catAtt = type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
				if (catAtt != null){ info.category = catAtt.category; }

				var attachAtt = type.GetCustomAttributes(typeof(AttachableAttribute), true).FirstOrDefault() as AttachableAttribute;
				if (attachAtt != null){ info.attachableTypes = attachAtt.types; }

				infos.Add(info);
			}
			
			infos = infos.OrderBy(i => i.name).OrderBy(i => i.category).ToList();
			return infos;
		}


		///Fold States per object
		private static Dictionary<object, AnimBool> foldOutStates = new Dictionary<object, AnimBool>();
		///Get an object's fold state
		public static float GetObjectFoldOutFaded(object o){
			AnimBool fold = null;
			return foldOutStates.TryGetValue(o, out fold)? fold.faded : (foldOutStates[o] = new AnimBool(false)).faded;
		}
		public static bool GetObjectFoldOut(object o){
			AnimBool fold = null;
			return foldOutStates.TryGetValue(o, out fold)? fold.target : (foldOutStates[o] = new AnimBool(false)).target;
		}
		///Set an object's fold state
		public static void SetObjectFoldOut(object o, bool value){
			foldOutStates[o].target = value;
		}


		///Get a texture from an audio clip
		private static Dictionary<AudioClip, Texture2D> audioTextures = new Dictionary<AudioClip, Texture2D>();
		public static Texture2D GetAudioClipTexture(AudioClip clip, int width, int height){
			
			if (clip == null){
				return null;
			}

			//do this?
			width = 4096;

			Texture2D texture = null;
			if (audioTextures.TryGetValue(clip, out texture)){
				if (texture != null){
					return texture;
				}
			}

			if (clip.loadType != AudioClipLoadType.DecompressOnLoad){
				audioTextures[clip] = new Texture2D(1,1);
				Debug.LogWarning(string.Format("Can't get preview audio texture from audio clip '{0}' because it's load type is set to compressed", clip.name), clip);
				return null;
			}

			texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
	        float[] samples = new float[clip.samples * clip.channels];
	        int step = Mathf.CeilToInt((clip.samples * clip.channels) / width);
	        clip.GetData(samples, 0);
	        Color[] xy = new Color[width * height];
	        for (int x = 0; x < width * height; x++){
	            xy[x] = new Color(0, 0, 0, 0);
	        }
	 
	        texture.SetPixels(xy);
	 
	        int i = 0;
	        while (i < width){
	            int barHeight = Mathf.CeilToInt(Mathf.Clamp(Mathf.Abs(samples[i * step]) * height, 0, height));
	            int add = samples[i * step] > 0 ? 1 : -1;
	            for (int j = 0; j < barHeight; j++){
	                texture.SetPixel(i, Mathf.FloorToInt(height / 2) - (Mathf.FloorToInt(barHeight / 2) * add) + (j * add), Color.white);
	            }
	            ++i;
	 
	        }
	 
	        texture.Apply();
			audioTextures[clip] = texture;
			return texture;
		}


/*
		///NOT USED ANYMORE

		public static float TimeToPos(float time, float width, float maxTime){
			return (time / maxTime) * width;
		}

		public static float PosToTime(float pos, float width, float maxTime){
			return (pos / width) * maxTime;
		}

		static AnimationCurve selectedCurve;
		static Dictionary<AnimationCurve, int> draggingKeyIndex = new Dictionary<AnimationCurve, int>();
		public static void DrawCurves(AnimationCurve[] curves, Rect posRect, Rect timeRect, Color infoColor, bool editable = true){

			if (curves == null || curves.Length == 0)
				return;

			var e = Event.current;

			var min = 0f;
			var max = 0f;
			foreach(var c in curves){
				foreach(var k in c.keys){
					min = Mathf.Min(min, k.value);
					max = Mathf.Max(max, k.value);
				}
			}

			timeRect = new Rect(0, min, timeRect.width, max);

			for (int i = 0; i < curves.Length; i++){
				var curveColor = Color.white;
				if (i == 0) curveColor = Color.red;
				if (i == 1) curveColor = Color.green;
				if (i == 2) curveColor = Color.blue;
				if (i == 3) curveColor = Color.cyan;
				DrawCurve(curves[i], posRect, timeRect, curveColor, infoColor, editable);
			}

			if (editable){
				if (e.type == EventType.MouseUp && e.button == 1){
					var menu = new GenericMenu();
					var time = PosToTime(e.mousePosition.x, posRect.width, timeRect.width);
					menu.AddItem(new GUIContent("Add Key"), false, ()=> {
						foreach (var curve in curves){
							var doAdd = true;
							for (int i = 0; i < curve.keys.Length; i++){
								if ( Mathf.Abs(time - curve.keys[i].time) <= 0.5f ){
									doAdd = false;
									break;
								}
							}

							if (doAdd){
								curve.AddKey(time, curve.Evaluate(time));
							}
						}
					});
					menu.ShowAsContext();
					e.Use();
				}

				if (e.type == EventType.MouseDown && e.button == 0){
					selectedCurve = null;
				}
			}

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}

		public static void DrawCurve(AnimationCurve curve, Rect posRect, Rect timeRect, Color curveColor, Color infoColor, bool editable = true){

			if (curve == null)
				return;

			var e = Event.current;
			var width = posRect.width;
			var height = posRect.height;
			var minTime = timeRect.x;
			var maxTime = timeRect.width;
			var minValue = timeRect.y;
			var maxValue = timeRect.height;

			for (int i = 0; i < curve.keys.Length; i ++){

				var key = curve.keys[i];
				var keyPos = new Vector3(TimeToPos(key.time, width, maxTime), height - TimeToPos(key.value, height, maxValue), 0);

				var nextKey = i == curve.keys.Length -1? key : curve.keys[i+1];
				var nextKeyPos = new Vector3(TimeToPos(nextKey.time, width, maxTime), height - TimeToPos(nextKey.value, height, maxValue), 0);

				var num = Mathf.Abs(nextKey.time - key.time) * 0.333333f;

				var vectorA = new Vector2( key.time + num, key.value + num * key.outTangent );
				var outTangentPos = new Vector3( TimeToPos(vectorA.x, width, maxTime), height - TimeToPos(vectorA.y, height, maxValue), 0);

				var vectorB = new Vector2( nextKey.time - num, nextKey.value - num * nextKey.inTangent);
				var inTangentPos = new Vector3( TimeToPos(vectorB.x, width, maxTime), height - TimeToPos(vectorB.y, height, maxValue), 0);

				curveColor = curve == selectedCurve? curveColor : new Color(curveColor.r, curveColor.g, curveColor.b, 0.5f);
				Handles.DrawBezier(keyPos, nextKeyPos, outTangentPos, inTangentPos, curveColor, null, 2f);

				if (i == 0){
					Handles.DrawBezier(keyPos, new Vector3(0, keyPos.y, 0), keyPos, keyPos, curveColor, null, 2f);
				}

				if (i == curve.keys.Length-1){
					Handles.DrawBezier(keyPos, new Vector3(width, keyPos.y, 0), keyPos, keyPos, curveColor, null, 2f);
				}

				GUI.color = curve == selectedCurve? Color.white : new Color(1,1,1,0.9f);

				if (editable){

					var keyRect = new Rect(keyPos.x-5, keyPos.y-5, 10, 10);
					GUI.DrawTexture(keyRect, keyIcon);

					if (keyRect.Contains(e.mousePosition) && e.type == EventType.MouseDown){
						draggingKeyIndex[curve] = i;
						selectedCurve = curve;
						e.Use();
					}

					if (e.type == EventType.MouseUp && e.button == 1 && keyRect.Contains(e.mousePosition)){
						var menu = new GenericMenu();
						menu.AddItem(new GUIContent("Remove Key"), false, delegate(object index){curve.RemoveKey((int)index);}, i);
						menu.ShowAsContext();
						draggingKeyIndex.Clear();
						e.Use();
					}

					if (curve == selectedCurve){
						Handles.color = new Color(1,1,1,0.5f);
						var a = new Vector2(outTangentPos.x - keyPos.x, outTangentPos.y - keyPos.y).normalized * 40;
						var outTangentHandlePos = new Vector3(keyPos.x + a.x, keyPos.y + a.y, 0f);
						Handles.DrawLine(new Vector3(keyPos.x, keyPos.y, 0f), outTangentHandlePos);
						var handleRect = new Rect(0,0,10,10);
						handleRect.center = outTangentHandlePos;
						if (e.button == 0 && e.type == EventType.MouseDown && handleRect.Contains(e.mousePosition)){

						}

						if (i < curve.length -1){
							var b = new Vector2(inTangentPos.x - nextKeyPos.x, inTangentPos.y - nextKeyPos.y).normalized * 40;
							var inTangentHandlePos = new Vector3(nextKeyPos.x + b.x, nextKeyPos.y + b.y, 0f);
							Handles.DrawLine(new Vector3(nextKeyPos.x, nextKeyPos.y, 0), inTangentHandlePos);
						}
					}
				}
			}

			if (editable){

				if (e.type == EventType.MouseUp){
					draggingKeyIndex.Clear();
				}

				if (draggingKeyIndex.ContainsKey(curve) && draggingKeyIndex[curve] >= 0 && e.button == 0){

					var newKey = new Keyframe();
					newKey.time = curve[draggingKeyIndex[curve]].time;
					newKey.value = curve[draggingKeyIndex[curve]].value;
					newKey.inTangent = curve[draggingKeyIndex[curve]].inTangent;
					newKey.outTangent = curve[draggingKeyIndex[curve]].outTangent;

					var dragInfoRect = new Rect( TimeToPos(newKey.time, width, maxTime) + 20, height - TimeToPos(newKey.value, height, maxValue), 100, 100);
					GUI.color = infoColor;
					GUI.Label(dragInfoRect, string.Format("<size=8>{0}\n{1}</size>", newKey.time.ToString("0.00"), newKey.value.ToString("0.00")));
					GUI.color = Color.white;

					if (e.type == EventType.MouseDrag){
						newKey.time = PosToTime(e.mousePosition.x, width, maxTime);
						newKey.time = Mathf.Clamp(newKey.time, minTime, maxTime);

						newKey.value = maxValue - PosToTime(e.mousePosition.y, height, maxValue);
						newKey.value = Mathf.Clamp(newKey.value, minValue, maxValue);
						draggingKeyIndex[curve] = curve.MoveKey(draggingKeyIndex[curve], newKey);
					}
				}

				if (e.button == 0 && e.clickCount == 2){
					var eval = curve.Evaluate( PosToTime(e.mousePosition.x, width, maxTime) );
					var posY = TimeToPos(eval, height, maxValue);
					if (Mathf.Abs( (height - e.mousePosition.y) - posY) < 5){
						var time = PosToTime(e.mousePosition.x, width, maxTime);
						var index = curve.AddKey(new Keyframe(time, curve.Evaluate(time)));
						if (index > 0){
							curve.SmoothTangents(index, 0f);
						}
						e.Use();
					}
				}

				if (e.button == 0 && e.type == EventType.MouseDown){
					selectedCurve = null;
				}
			}

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}
*/

	}
}

#endif